using System.Globalization;
using System.Text;
using Dsg.Hrms.Application.Settings;

namespace Dsg.Hrms.Application.Identity.Passwords;

/// <summary>
/// Parola kurallari (ADR-0006 §6, SYG-KMLK-044, 045).
/// </summary>
/// <remarks>
/// <para>
/// En az uzunluk 6'dir (<c>KR-070</c>); bu bilincli bir odundur ve yaygin parola
/// denetimi parola guvenliginin asil dayanagidir. Bu denetim <b>devre disi
/// birakilamaz</b>: parametresi yoktur.
/// </para>
/// <para>
/// Parola, denetimden ve ozetlemeden once NFKC bicimine getirilir: gorunusu ayni iki
/// yazim (bilesik ve ayrik "ü") ayni parola sayilir.
/// </para>
/// </remarks>
public sealed class PasswordPolicy
{
    /// <summary>En fazla uzunluk (SYG-KMLK-044).</summary>
    public const int MaxLength = 128;

    /// <summary>
    /// Kuruma ozgu sozcukler (SYG-KMLK-045): kurum ve grup sirketi adlari ile sistemin adi.
    /// Turkce harfler ASCII karsiligina indirgenmis ve kucuk harfle yazilir.
    /// </summary>
    public static readonly IReadOnlyList<string> OrganizationWords =
    [
        "duzen", "duzensaglik", "duzensaglikgrubu", "duzenlaboratuvar", "duzenlaboratuvarlar", "duzenlab",
        "zeytinim", "labpt", "dsg", "dsghrms", "hrms", "insankaynaklari",
    ];

    private readonly ISystemParameters _parameters;
    private readonly ICommonPasswordList _commonPasswords;

    /// <summary>Yeni ornek olusturur.</summary>
    public PasswordPolicy(ISystemParameters parameters, ICommonPasswordList commonPasswords)
    {
        _parameters = parameters;
        _commonPasswords = commonPasswords;
    }

    /// <summary>Parolayi NFKC bicimine getirir. Ozetleme de bu bicimle yapilir.</summary>
    public static string Normalize(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        return password.Normalize(NormalizationForm.FormKC);
    }

    /// <summary>Parolayi denetler; ihlal yoksa bos liste doner.</summary>
    /// <param name="password">Kullanicinin girdigi parola.</param>
    /// <param name="personalWords">Kisinin adi, soyadi ve e-posta adresinin yerel kismi.</param>
    /// <param name="cancellationToken">Iptal.</param>
    public async Task<IReadOnlyList<PasswordViolation>> ValidateAsync(
        string password,
        IEnumerable<string> personalWords,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(password);
        ArgumentNullException.ThrowIfNull(personalWords);

        var normalized = Normalize(password);
        var violations = new List<PasswordViolation>();

        // Uzunluk kod noktasi olarak sayilir: "ğ" bir karakterdir, iki UTF-16 birimi degil.
        var length = normalized.EnumerateRunes().Count();
        var minLength = await _parameters.GetIntegerAsync(ParameterCatalog.MinPasswordLength, cancellationToken).ConfigureAwait(false);

        if (length < minLength)
        {
            violations.Add(PasswordViolation.TooShort);
        }

        if (length > MaxLength)
        {
            violations.Add(PasswordViolation.TooLong);
        }

        if (await _parameters.GetBooleanAsync(ParameterCatalog.RequireComplexPassword, cancellationToken).ConfigureAwait(false)
            && !IsComplex(normalized))
        {
            violations.Add(PasswordViolation.NotComplex);
        }

        var lowered = normalized.ToLowerInvariant();
        if (_commonPasswords.Contains(lowered))
        {
            violations.Add(PasswordViolation.Common);
        }

        var words = OrganizationWords.Concat(personalWords.Select(Fold)).Where(w => w.Length >= 3);
        if (IsDerivedFrom(Fold(normalized), words))
        {
            violations.Add(PasswordViolation.ContainsPersonalOrOrganizationWord);
        }

        return violations;
    }

    /// <summary>
    /// Parola, sozcugun kendisi veya sozcuk + rakam/simge ekleri mi ("ahmet1985",
    /// "Duzen2026!", "!!zeytinim"). Sozcugun parolanin icinde gecmesi tek basina yeterli
    /// sayilmaz: "bayramduzenleme" gibi uzun parolalari reddetmek gereksiz olurdu.
    /// </summary>
    private static bool IsDerivedFrom(string foldedPassword, IEnumerable<string> words)
    {
        var core = foldedPassword.Trim(NonLetterCharacters(foldedPassword));
        return words.Any(word => string.Equals(core, word, StringComparison.Ordinal));
    }

    private static char[] NonLetterCharacters(string value) =>
        value.Where(c => !char.IsLetter(c)).Distinct().ToArray();

    /// <summary>Kucuk harfe cevirir, Turkce harfleri ASCII karsiligina indirger, bosluklari atar.</summary>
    private static string Fold(string value)
    {
        var lowered = value.Normalize(NormalizationForm.FormKC).ToLower(CultureInfo.GetCultureInfo("tr-TR"));
        var builder = new StringBuilder(lowered.Length);

        foreach (var c in lowered)
        {
            builder.Append(c switch
            {
                'ç' => 'c',
                'ğ' => 'g',
                'ı' => 'i',
                'ö' => 'o',
                'ş' => 's',
                'ü' => 'u',
                'â' => 'a',
                'î' => 'i',
                'û' => 'u',
                ' ' or '.' or '-' or '_' => '\0',
                _ => c,
            });
        }

        return builder.ToString().Replace("\0", string.Empty, StringComparison.Ordinal);
    }

    private static bool IsComplex(string password) =>
        password.Any(char.IsUpper)
        && password.Any(char.IsLower)
        && password.Any(char.IsDigit)
        && password.Any(c => !char.IsLetterOrDigit(c));
}

/// <summary>Parola kurali ihlali.</summary>
public enum PasswordViolation
{
    /// <summary>Parametredeki en az uzunluktan kisa.</summary>
    TooShort = 1,

    /// <summary>128 karakterden uzun.</summary>
    TooLong = 2,

    /// <summary>Karmasiklik zorunlu ve saglanmiyor.</summary>
    NotComplex = 3,

    /// <summary>Yaygin parola listesinde.</summary>
    Common = 4,

    /// <summary>Kisinin adi, soyadi, e-postasi veya kurum adindan turetilmis.</summary>
    ContainsPersonalOrOrganizationWord = 5,

    /// <summary>Yeni parola mevcut parolayla ayni (oturum icinde degisiklik, SYG-KMLK-048).</summary>
    SameAsCurrent = 6,
}

/// <summary>Uygulamaya gomulu yaygin parola listesi (SYG-KMLK-045).</summary>
public interface ICommonPasswordList
{
    /// <summary>Kayit sayisi.</summary>
    int Count { get; }

    /// <summary>Kucuk harfli ve NFKC bicimli parola listede mi.</summary>
    bool Contains(string loweredPassword);
}

/// <summary>Parola ozetleme (SYG-KMLK-049).</summary>
public interface IPasswordHasher
{
    /// <summary>NFKC bicimli parolanin ozetini uretir.</summary>
    string Hash(string normalizedPassword);

    /// <summary>Parola ozetle eslesiyor mu.</summary>
    bool Verify(string passwordHash, string normalizedPassword);

    /// <summary>
    /// Hic bir parolayla eslesmeyen, gercekle ayni maliyette bir ozet. Var olmayan kullanici
    /// icin de parola dogrulamasi yapilir; yanit suresi hesabin varligini ele vermez
    /// (SYG-KMLK-032).
    /// </summary>
    string DummyHash { get; }
}
