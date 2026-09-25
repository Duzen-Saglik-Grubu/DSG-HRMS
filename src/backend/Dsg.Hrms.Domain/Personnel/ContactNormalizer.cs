namespace Dsg.Hrms.Domain.Personnel;

/// <summary>
/// Kaynak sistemden gelen iletisim bilgilerinin normallestirilmesi (SYG-KMLK-009).
/// </summary>
/// <remarks>
/// LOGO'da ayni adres farkli bicimlerde girilebiliyor: basta/sonda bosluk, satir
/// sonu, buyuk harf. Normallestirilmemis adres, uyelikte ve paylasilan adres
/// tespitinde (SYG-KMLK-008) ayni adresin iki farkli adres sayilmasina yol acardi.
/// </remarks>
public static class ContactNormalizer
{
    /// <summary>
    /// E-postayi normallestirir: bosluk ve satir sonu karakterleri atilir, kucuk harfe
    /// cevrilir. Bos veya "@" icermeyen deger icin <c>null</c> doner.
    /// </summary>
    /// <remarks>
    /// Kucuk harfe cevirme KULTURDEN BAGIMSIZDIR: Turkce kulturde "I" harfinin
    /// kucugu "ı" olur ve "INFO@..." adresi "ınfo@..." bicimine bozulurdu.
    /// </remarks>
    public static string? NormalizeEmail(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var compact = RemoveWhitespace(value).ToLowerInvariant();
        var at = compact.IndexOf('@', StringComparison.Ordinal);

        return at > 0 && at < compact.Length - 1 && compact.IndexOf('@', at + 1) < 0
            ? compact
            : null;
    }

    /// <summary>
    /// Cep telefonunu 10 haneli bicime getirir (<c>5XXXXXXXXX</c>). Gecersizse <c>null</c> doner.
    /// </summary>
    /// <remarks>
    /// Bosluk, satir sonu, sekme, tire ve parantez atilir; bastaki <c>+90</c>, <c>90</c>
    /// veya <c>0</c> onegi kaldirilir. Sonuc 10 hane degilse veya <c>5</c> ile
    /// baslamiyorsa numara cep telefonu degildir ve SMS gonderilemez.
    /// </remarks>
    public static string? NormalizeMobilePhone(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var digits = new string(value.Where(char.IsAsciiDigit).ToArray());

        if (digits.StartsWith("90", StringComparison.Ordinal) && digits.Length == 12)
        {
            digits = digits[2..];
        }
        else if (digits.StartsWith('0') && digits.Length == 11)
        {
            digits = digits[1..];
        }

        return digits.Length == 10 && digits[0] == '5' ? digits : null;
    }

    /// <summary>
    /// Ad ve soyad gibi metin alanlarindan satir sonu ve sekme karakterlerini atar,
    /// ardisik bosluklari teke indirir.
    /// </summary>
    public static string NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var parts = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', parts);
    }

    private static string RemoveWhitespace(string value) =>
        new(value.Where(c => !char.IsWhiteSpace(c)).ToArray());
}
