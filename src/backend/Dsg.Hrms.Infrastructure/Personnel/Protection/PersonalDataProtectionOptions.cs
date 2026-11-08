using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Dsg.Hrms.Infrastructure.Personnel.Protection;

/// <summary>
/// Ozel nitelikli verinin (kan grubu) veri anahtarlari (ADR-0020 §1–3; SYG-PERS-060, 061).
/// </summary>
/// <remarks>
/// <para>
/// Anahtarlar KAYNAK KODA, VERITABANINA ve GUNLUGE YAZILMAZ; sir dosyasinda durur (UAT:
/// <c>.env.uat</c>, yerel: <c>docker/.env</c>). Ortam degiskeni bicimi:
/// <c>PersonalDataProtection__Keys__k1</c>. Deger, 32 baytlik rastgele bir anahtarin Base64
/// bicimidir (<c>openssl rand -base64 32</c>).
/// </para>
/// <para>
/// Sir parametre anahtarindan (<c>ParameterProtection:Key</c>) ayridir: biri ele gecerse
/// digeri korunmaya devam eder. Sir parametre anahtarinin aksine <b>zorunludur</b>: anahtarsiz
/// calismak ya duz metin yazmak ya da veriyi okuyamamak demektir (ADR-0020 §2).
/// </para>
/// <para>
/// Yeni yazimlar en buyuk numarali anahtarla yapilir (<c>k2</c>, <c>k1</c>'den yenidir). Eski
/// anahtar, onunla sifreli kayit kalmayinca cikarilir (ADR-0020 §3).
/// </para>
/// </remarks>
public sealed partial class PersonalDataProtectionOptions : IValidatableObject
{
    /// <summary>Yapilandirma bolumunun adi.</summary>
    public const string SectionName = "PersonalDataProtection";

    /// <summary>Anahtar uzunlugu (bayt): AES-256.</summary>
    public const int KeySizeBytes = 32;

    /// <summary>Anahtar kimligi -> Base64 anahtar. Kimlik <c>k&lt;n&gt;</c> bicimindedir.</summary>
    public Dictionary<string, string> Keys { get; init; } = [];

    /// <summary>
    /// Degeri bos olmayan anahtarlar. Bos deger tanimsiz sayilir: compose dosyalari ikinci anahtari
    /// (`k2`) her zaman aktarir, sir dosyasinda deger yoksa bos gelir (SYG-PERS-062, anahtar dondurme).
    /// </summary>
    public IReadOnlyDictionary<string, string> ActiveKeys =>
        Keys.Where(k => !string.IsNullOrWhiteSpace(k.Value)).ToDictionary(k => k.Key, k => k.Value, StringComparer.Ordinal);

    /// <summary>Yeni yazimlarda kullanilan anahtarin kimligi: en buyuk numarali anahtar.</summary>
    public string CurrentKeyId =>
        ActiveKeys.Keys.Where(id => KeyIdPattern().IsMatch(id)).MaxBy(Number)
        ?? throw new InvalidOperationException($"{SectionName}:Keys tanimli degil.");

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ActiveKeys.Count == 0)
        {
            yield return new ValidationResult(
                $"{SectionName}:Keys tanimli degil. Ozel nitelikli veri (kan grubu) anahtarsiz yazilamaz ve okunamaz; " +
                $"en az bir anahtar verin: {SectionName}__Keys__k1 (openssl rand -base64 32).",
                [nameof(Keys)]);
            yield break;
        }

        // Iletilerde yalnizca anahtar KIMLIGI yer alir; deger hicbir iletiye yazilmaz.
        foreach (var (id, value) in ActiveKeys)
        {
            if (!KeyIdPattern().IsMatch(id))
            {
                yield return new ValidationResult(
                    $"{SectionName}:Keys altindaki '{id}' kimligi gecersiz; k1, k2 gibi olmalidir.",
                    [nameof(Keys)]);
                continue;
            }

            if (!IsValidKey(value))
            {
                yield return new ValidationResult(
                    $"{SectionName}:Keys:{id}, {KeySizeBytes} baytlik bir anahtarin Base64 bicimi olmalidir (openssl rand -base64 32).",
                    [nameof(Keys)]);
            }
        }
    }

    private static bool IsValidKey(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            return Convert.FromBase64String(value).Length == KeySizeBytes;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static int Number(string id) => int.Parse(id.AsSpan(1), CultureInfo.InvariantCulture);

    [GeneratedRegex("^k[1-9][0-9]{0,3}$", RegexOptions.CultureInvariant)]
    internal static partial Regex KeyIdPattern();
}
