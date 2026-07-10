using System.Text.RegularExpressions;
using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Entities;
using UglyToad.PdfPig;

namespace AkilliDepo.Api.Services;

/// <summary>
/// "Mağaza Sipariş Formu" sabit şablonundaki PDF'leri (bkz. frontend/public/magaza-siparis-sablonu.pdf)
/// ayrıştırır. Tasarım kararı: ürün adı/renk gibi görsel alanları PDF metninden tahmin etmeye çalışmak
/// kırılgan olur (çok kelimeli ürün adları sütun hizasını bozabilir) — bunun yerine satırda geçen
/// BARKOD'u sistemdeki bilinen barkodlarla eşleştiriyoruz (barkodlar zaten eşsiz ve güvenilir), ürün
/// adı/rengi DB'deki kayıttan alınır. Satırdaki son tam sayı miktar olarak kabul edilir.
/// </summary>
public interface IPdfOrderParser
{
    ParsedStoreOrderDto Parse(Stream pdfStream, List<Product> knownProducts);
}

public class PdfOrderParser : IPdfOrderParser
{
    private static readonly (string Label, Regex Pattern)[] HeaderFields =
    {
        ("StoreId", new Regex(@"^Ma[ğg]aza\s*Kodu\s*:\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
        ("StoreName", new Regex(@"^Ma[ğg]aza\s*Ad[ıi]\s*:\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
        ("Address", new Regex(@"^(Teslimat\s*Adresi|Adres)\s*:\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)),
    };

    private static readonly Regex TrailingInteger = new(@"(\d+)\s*$", RegexOptions.CultureInvariant);

    public ParsedStoreOrderDto Parse(Stream pdfStream, List<Product> knownProducts)
    {
        var lines = ExtractLines(pdfStream);
        var result = new ParsedStoreOrderDto();

        var barcodeMap = knownProducts
            .GroupBy(p => p.Barcode.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.First());

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0) continue;

            var matchedHeader = false;
            foreach (var (label, pattern) in HeaderFields)
            {
                var m = pattern.Match(trimmed);
                if (!m.Success) continue;

                var value = m.Groups[m.Groups.Count - 1].Value.Trim();
                switch (label)
                {
                    case "StoreId": result.StoreId = value; break;
                    case "StoreName": result.StoreName = value; break;
                    case "Address": result.Address = value; break;
                }
                matchedHeader = true;
                break;
            }
            if (matchedHeader) continue;

            // Satırda bilinen bir barkod token'ı var mı diye bak.
            var tokens = trimmed.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            var barcodeToken = tokens.FirstOrDefault(t => barcodeMap.ContainsKey(t.ToUpperInvariant()));
            if (barcodeToken is null) continue;

            var product = barcodeMap[barcodeToken.ToUpperInvariant()];
            var qtyMatch = TrailingInteger.Match(trimmed);
            var quantity = qtyMatch.Success ? int.Parse(qtyMatch.Groups[1].Value) : 0;

            result.Items.Add(new ParsedOrderItemDto
            {
                ProductId = product.Id,
                ProductBarcode = product.Barcode,
                ProductName = product.Name,
                Color = product.Color,
                Quantity = quantity,
                Matched = true
            });
        }

        if (string.IsNullOrWhiteSpace(result.StoreId))
            result.Warnings.Add("Mağaza Kodu satırı bulunamadı, elle girmelisiniz.");
        if (string.IsNullOrWhiteSpace(result.StoreName))
            result.Warnings.Add("Mağaza Adı satırı bulunamadı, elle girmelisiniz.");
        if (string.IsNullOrWhiteSpace(result.Address))
            result.Warnings.Add("Teslimat Adresi satırı bulunamadı, elle girmelisiniz.");
        if (result.Items.Count == 0)
            result.Warnings.Add("PDF içinde sistemdeki ürünlerle eşleşen hiçbir barkod bulunamadı.");
        if (result.Items.Any(i => i.Quantity <= 0))
            result.Warnings.Add("Bazı satırların miktarı okunamadı (0), lütfen elle düzeltin.");

        return result;
    }

    /// <summary>
    /// PdfPig'in kelime bazlı çıktısını, aynı satırdaki kelimeleri Y koordinatına göre gruplayıp
    /// soldan sağa sıralayarak satır satır metne çevirir. Basit tablo/etiket düzenleri için güvenilirdir.
    /// </summary>
    private static List<string> ExtractLines(Stream pdfStream)
    {
        var lines = new List<string>();
        using var document = PdfDocument.Open(pdfStream);

        foreach (var page in document.GetPages())
        {
            var words = page.GetWords().ToList();
            var groups = words
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom / 3.0) * 3.0)
                .OrderByDescending(g => g.Key);

            foreach (var group in groups)
            {
                var lineText = string.Join(" ", group.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text));
                lines.Add(lineText);
            }
        }

        return lines;
    }
}
