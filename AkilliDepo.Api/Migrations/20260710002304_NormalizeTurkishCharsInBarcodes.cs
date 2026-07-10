using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AkilliDepo.Api.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeTurkishCharsInBarcodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Önceki normalizasyon (NormalizeLegacyBarcodes) yalnızca İ/ı'yı düzeltmişti. Daha sonra
            // keşfedildi: .ToUpperInvariant() Türkçe Ş/ğ/Ü/ö/ç gibi karakterleri de ASCII'ye çevirmiyor
            // (bkz. BarcodeText.ToBarcodeSafeUpper, Managers/BarcodeText.cs) — bu yüzden tüm Türkçe
            // özel karakterler burada kapsamlı şekilde normalize ediliyor.
            var replacements = new (string From, string To)[]
            {
                ("İ", "I"), ("ı", "I"),
                ("Ş", "S"), ("ş", "S"),
                ("Ğ", "G"), ("ğ", "G"),
                ("Ü", "U"), ("ü", "U"),
                ("Ö", "O"), ("ö", "O"),
                ("Ç", "C"), ("ç", "C"),
            };

            string[] tableColumns =
            {
                "Products.Barcode", "Boxes.Barcode", "DispatchBoxes.Barcode",
                "DispatchPallets.Barcode", "Stores.StoreCode", "StoreOrders.OrderCode", "Brands.ShortCode"
            };

            foreach (var tc in tableColumns)
            {
                var parts = tc.Split('.');
                var table = parts[0];
                var column = parts[1];
                var expr = $"[{column}]";
                foreach (var (from, to) in replacements)
                {
                    expr = $"REPLACE({expr}, N'{from}', N'{to}')";
                }
                migrationBuilder.Sql($"UPDATE [{table}] SET [{column}] = {expr}");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
