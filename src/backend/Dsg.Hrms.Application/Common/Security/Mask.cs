using System.Text.RegularExpressions;
using Dsg.Hrms.Domain.Common;

namespace Dsg.Hrms.Application.Common.Security;

/// <summary>
/// Kisisel verinin gunluk ve denetim kayitlarinda maskelenmesi (ADR-0009 §4).
/// </summary>
/// <remarks>
/// <para>
/// <b>Temel ilke: SIZDIRMA YERINE FAZLA MASKELE (fail-closed).</b> Girdi beklenen
/// bicimde degilse orijinal deger DONDURULMEZ; tamamen maskelenir. Cunku beklenmeyen
/// bicimdeki bir deger, yanlis alana yazilmis bir TCKN veya kopyala-yapistir sirasinda
/// bozulmus bir IBAN olabilir. Boyle bir degeri "tanimadim" diye oldugu gibi gunluge
/// yazmak, tam olarak onlemek istedigimiz sizintiyi uretir.
/// </para>
/// <para>
/// Bu sinif saf fonksiyonlardan olusur: dis bagimliligi yoktur, yan etkisi yoktur ve
/// izole olarak test edilebilir. Maskeleme kurallarinin tek kaynagi burasidir.
/// </para>
/// </remarks>
public static partial class Mask
{
    /// <summary>Tamamen gizlenmis deger.</summary>
    public const string Redacted = "***";

    /// <summary>
    /// TCKN maskeler: <c>12345678901</c> → <c>123*****901</c>
    /// </summary>
    /// <remarks>
    /// Ilk ve son uc hane, kaydin dogru kisiye ait olup olmadigini teyit etmeye yeter;
    /// tam numarayi yeniden olusturmaya yetmez.
    /// </remarks>
    public static string? NationalId(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();

        // TCKN 11 hanedir. Farkli uzunluk veya rakam disi karakter = beklenmeyen girdi.
        return IsAllDigits(trimmed) && trimmed.Length == 11
            ? $"{trimmed[..3]}*****{trimmed[^3..]}"
            : Redacted;
    }

    /// <summary>
    /// TCKN'yi EKRAN icin maskeler: <c>12345678901</c> → <c>123******01</c> (SYG-PERS-053).
    /// </summary>
    /// <remarks>
    /// Gunluk maskesinden (<see cref="NationalId"/>, REQ-KMLK-041) farklidir ve PG-PERS-31'e
    /// dayanir: son iki hane kontrol hanesidir ve ilk dokuz haneden hesaplanir; yalnizca
    /// onlari gostermek gizli haneler hakkinda neredeyse bilgi vermez. Gunluk bicimi onayli
    /// bir gereksinimde yazili oldugu icin degistirilmedi.
    /// </remarks>
    public static string? NationalIdForDisplay(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        return IsAllDigits(trimmed) && trimmed.Length == 11
            ? $"{trimmed[..3]}******{trimmed[^2..]}"
            : Redacted;
    }

    /// <summary>
    /// Cep telefonu maskeler: <c>5321234567</c> → <c>532*****67</c>
    /// </summary>
    /// <remarks>
    /// Operator on eki gorunur birakilir; bir SMS gonderim sorununu tanilarken
    /// "hangi operatore gitti" bilgisi ise yarar, kisiyi tanimlamaya yetmez.
    /// Bastaki <c>0</c> ve <c>+90</c> normalize edilir (ADR-0012 §8).
    /// </remarks>
    public static string? Phone(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var digits = OnlyDigits(value);

        // +90 veya 0 on ekini at, 10 haneye indir.
        if (digits.Length == 12 && digits.StartsWith("90", StringComparison.Ordinal))
        {
            digits = digits[2..];
        }
        else if (digits.Length == 11 && digits.StartsWith('0'))
        {
            digits = digits[1..];
        }

        return digits.Length == 10 && digits.StartsWith('5')
            ? $"{digits[..3]}*****{digits[^2..]}"
            : Redacted;
    }

    /// <summary>
    /// E-posta adresi maskeler: <c>ahmet.yilmaz@duzen.com.tr</c> → <c>ah***@duzen.com.tr</c>
    /// </summary>
    /// <remarks>
    /// Alan adi gorunur birakilir: bir dogrulama kodunun kurumsal adrese mi yoksa
    /// kisisel adrese mi gittigi, gunluge bakarak anlasilabilmelidir (bkz. <c>R-13</c>).
    /// Yerel bolum iki karakterden kisaysa tamamen maskelenir.
    /// </remarks>
    public static string? Email(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var trimmed = value.Trim();
        var atIndex = trimmed.LastIndexOf('@');

        // Gecerli bir adres degilse alan adini da acmayiz.
        if (atIndex <= 0 || atIndex == trimmed.Length - 1)
        {
            return Redacted;
        }

        var local = trimmed[..atIndex];
        var domain = trimmed[(atIndex + 1)..];

        return local.Length >= 2
            ? $"{local[..2]}***@{domain}"
            : $"***@{domain}";
    }

    /// <summary>
    /// IBAN maskeler: <c>TR330006100519786457841326</c> → <c>TR***1326</c>
    /// </summary>
    public static string? Iban(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var compact = value.Replace(" ", string.Empty, StringComparison.Ordinal).Trim();

        // Turkiye IBAN'i 26 karakterdir; farkli uzunlukta ise risk almayiz.
        return compact.Length is >= 15 and <= 34
            ? $"{compact[..2]}***{compact[^4..]}"
            : Redacted;
    }

    /// <summary>
    /// Serbest arama metnini maskeler: <c>ahmet 12345678901</c> → <c>ahmet 123*****901</c>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Arama kutusu ad, sicil, TCKN, telefon veya e-posta alabilir. Erisim kaydi "ne
    /// arandi" sorusunu cevaplamalidir; bu yuzden ad gibi duz metin okunur kalir. Ama
    /// metindeki kisisel numaralar kayda duz metin dusmemelidir (KR-059, #244 B-07).
    /// </para>
    /// <list type="bullet">
    /// <item>E-posta adresi: <see cref="Email"/> kurali.</item>
    /// <item>
    /// En az 6 rakamli dizi (aradaki bosluk, tire, nokta, parantez ve <c>+</c> dahil):
    /// 11 rakam TCKN kuraliyla, 10-12 rakamli Turkiye cep telefonu telefon kuraliyla,
    /// digerleri tamamen maskelenir. Bosluklu yazilan telefon (<c>532 123 45 67</c>)
    /// boylece kacmaz.
    /// </item>
    /// <item>5 ve daha az rakamli diziler (sicil, kisa sayilar) okunur kalir.</item>
    /// </list>
    /// </remarks>
    public static string? SearchText(string? value)
    {
        if (value is null)
        {
            return null;
        }

        var masked = EmailPattern().Replace(value, match => Email(match.Value)!);
        return DigitRunPattern().Replace(masked, match =>
        {
            var digits = OnlyDigits(match.Value);
            if (digits.Length < 6)
            {
                return match.Value;
            }

            if (digits.Length == 11 && !digits.StartsWith('0'))
            {
                return NationalId(digits)!;
            }

            // Cep telefonu degilse Phone zaten tamamen maskeler (fail-closed).
            return Phone(digits)!;
        });
    }

    [GeneratedRegex(@"[^\s@]+@[^\s@]+", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex EmailPattern();

    // Rakamla baslayip rakamla biten, arada ayirici iceren dizi. "+90 532 ..." de yakalanir.
    [GeneratedRegex(@"\+?\d[\d\s\-().]*\d|\d", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 1000)]
    private static partial Regex DigitRunPattern();

    /// <summary>
    /// Sir niteligindeki degerlerin yerine yazilan sabit.
    /// </summary>
    /// <remarks>
    /// Parola, dogrulama kodu, jeton, API anahtari ve SMS/e-posta govdesi
    /// <b>maskelenmez, tamamen dislanir</b>: kismen gorunen bir dogrulama kodu bile
    /// kaba kuvvet denemesini kolaylastirir. Bu yuzden sirlar icin ayri bir
    /// maskeleyici yoktur; her zaman bu sabit yazilir.
    /// </remarks>
    public const string SecretPlaceholder = Redacted;

    /// <summary>
    /// Tur bilgisine gore uygun maskeleyiciyi uygular.
    /// </summary>
    public static string? ByKind(PersonalDataKind kind, string? value) => kind switch
    {
        PersonalDataKind.NationalId => NationalId(value),
        PersonalDataKind.Phone => Phone(value),
        PersonalDataKind.Email => Email(value),
        PersonalDataKind.Iban => Iban(value),
        PersonalDataKind.SearchText => SearchText(value),

        // Tanimlanmamis bir tur gelirse en guvenli davranis tamamen maskelemektir.
        _ => value is null ? null : Redacted,
    };

    private static bool IsAllDigits(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (!char.IsAsciiDigit(character))
            {
                return false;
            }
        }

        return true;
    }

    private static string OnlyDigits(string value)
    {
        Span<char> buffer = value.Length <= 64 ? stackalloc char[value.Length] : new char[value.Length];
        var length = 0;

        foreach (var character in value)
        {
            if (char.IsAsciiDigit(character))
            {
                buffer[length++] = character;
            }
        }

        return new string(buffer[..length]);
    }
}
