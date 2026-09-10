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
public static class Mask
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
