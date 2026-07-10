using System.Collections.Generic;
using System.Linq;

namespace AkilliDepo.Api.Managers;

/// <summary>
/// Barkod/kısa kod üretiminde kullanılan tüm metinler CODE128 (ASCII) uyumlu olmalıdır.
/// `.ToUpperInvariant()` tek başına yeterli DEĞİLDİR: Türkçe 'ı' (U+0131) ve 'İ' (U+0130) invariant
/// kültürde ASCII 'I'ya çevrilmez (ı olduğu gibi kalır, İ zaten büyük harf sayılır) — bu, barkod
/// üretiminde CODE128'de geçersiz karakter üretilmesine yol açan, `.ToUpper()` → `.ToUpperInvariant()`
/// düzeltmesinden SONRA bile devam eden ayrı bir hataydı (canlı seed verisiyle keşfedildi: "Sırt
/// Çantası", "Kızılay" gibi 'ı' içeren isimlerden üretilen barkodlar hâlâ 'ı' içeriyordu).
/// </summary>
public static class BarcodeText
{
    private static readonly Dictionary<char, char> TurkishMap = new()
    {
        ['İ'] = 'I',
        ['ı'] = 'I',
        ['Ş'] = 'S',
        ['ş'] = 'S',
        ['Ğ'] = 'G',
        ['ğ'] = 'G',
        ['Ü'] = 'U',
        ['ü'] = 'U',
        ['Ö'] = 'O',
        ['ö'] = 'O',
        ['Ç'] = 'C',
        ['ç'] = 'C',
    };

    /// <summary>Verilen metni önce Türkçe karakter eşlemesinden geçirir, sonra büyük harfe çevirir —
    /// sonuç her zaman CODE128-güvenli ASCII harf/rakamlardan oluşur (diğer her şey olduğu gibi kalır,
    /// çağıran taraf zaten yalnızca harf/rakam seçiyor olmalı).</summary>
    public static string ToBarcodeSafeUpper(string input)
    {
        var mapped = input.Select(c => TurkishMap.TryGetValue(c, out var replacement) ? replacement : c);
        return new string(mapped.ToArray()).ToUpperInvariant();
    }
}
