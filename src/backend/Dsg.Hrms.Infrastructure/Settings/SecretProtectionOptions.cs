using System.ComponentModel.DataAnnotations;

namespace Dsg.Hrms.Infrastructure.Settings;

/// <summary>
/// Sir parametrelerin sifreleme anahtari (SYG-KMLK-075, ADR-0008).
/// </summary>
/// <remarks>
/// <para>
/// Anahtar KAYNAK KODA ve VERITABANINA YAZILMAZ. Gelistirmede .NET User Secrets, UAT ve
/// uretimde ortam degiskeni ile saglanir: <c>ParameterProtection__Key</c>. Deger,
/// 32 baytlik rastgele bir anahtarin Base64 bicimidir (<c>openssl rand -base64 32</c>).
/// </para>
/// <para>
/// Tanimli degilse uygulama calisir; sir parametreler yalnizca ortam degiskeninden
/// okunur ve ekrandan girilemez. Anahtar kaybolursa veritabanindaki sir degerleri
/// cozulemez; ekrandan yeniden girilmeleri gerekir.
/// </para>
/// </remarks>
public sealed class SecretProtectionOptions : IValidatableObject
{
    /// <summary>Yapilandirma bolumunun adi.</summary>
    public const string SectionName = "ParameterProtection";

    /// <summary>Anahtar uzunlugu (bayt): AES-256.</summary>
    public const int KeySizeBytes = 32;

    /// <summary>Base64 bicimli anahtar. Bossa sir parametreler veritabanina yazilamaz.</summary>
    public string? Key { get; init; }

    /// <summary>Anahtar tanimli mi.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(Key);

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!IsConfigured)
        {
            yield break;
        }

        byte[]? bytes = null;
        try
        {
            bytes = Convert.FromBase64String(Key!);
        }
        catch (FormatException)
        {
            // Asagida anlasilir bir iletiye donusturulur; deger iletiye YAZILMAZ.
        }

        if (bytes is null || bytes.Length != KeySizeBytes)
        {
            yield return new ValidationResult(
                $"ParameterProtection:Key, {KeySizeBytes} baytlik bir anahtarin Base64 bicimi olmalidir (openssl rand -base64 32).",
                [nameof(Key)]);
        }
    }
}
