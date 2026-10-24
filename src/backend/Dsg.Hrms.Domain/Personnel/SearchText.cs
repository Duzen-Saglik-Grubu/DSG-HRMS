using System.Text;

namespace Dsg.Hrms.Domain.Personnel;

/// <summary>
/// Turkce karaktersiz arama (ADR-0021 §7; SYG-PERS-030): "isik" IŞIK'i, "ismail" İSMAİL'i bulur,
/// "ışık" yazmak da ayni sonucu verir.
/// </summary>
/// <remarks>
/// Kural acik bir normallestirmedir (ADR-0004 §10): Turkce harfler ASCII karsiliklarina cevrilir,
/// sonra kultur bagimsiz kucuk harfe indirilir. Veritabanindaki arama anahtari ve arama terimi ayni
/// kuralla uretilir; migration'daki SQL karsiligi (<c>translate</c> + <c>lower</c>) bununla esdir.
/// </remarks>
public static class SearchText
{
    /// <summary>Cevrilen Turkce harfler (buyuk ve kucuk).</summary>
    public const string TurkishLetters = "ÇĞIİÖŞÜçğıöşü";

    /// <summary>Karsiliklari, ayni sirayla.</summary>
    public const string AsciiLetters = "CGIIOSUcgiosu";

    /// <summary>Metni arama bicimine indirir; bos metin bos doner.</summary>
    public static string Fold(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var c in value.Trim())
        {
            var index = TurkishLetters.IndexOf(c, StringComparison.Ordinal);
            builder.Append(index >= 0 ? AsciiLetters[index] : c);
        }

        return builder.ToString().ToLowerInvariant();
    }

    /// <summary>Kisinin arama anahtari: ad, soyad ve (gosterildigi kosulda) onceki soyadi.</summary>
    /// <remarks>Onceki soyadi yalnizca gosterildigi kosulda girer; gosterilmeyen bir degerle bulunabilmek, degerin varligini ele verirdi (B-17).</remarks>
    public static string KeyFor(string firstName, string lastName, string? previousSurname) =>
        string.Join(' ', new[] { firstName, lastName, previousSurname }.Where(v => !string.IsNullOrWhiteSpace(v)).Select(Fold));
}
