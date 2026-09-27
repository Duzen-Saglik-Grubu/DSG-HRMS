using System.ComponentModel.DataAnnotations;

namespace Dsg.Hrms.Infrastructure.Identity;

/// <summary>
/// Dogrulama kodu ozet anahtari (SYG-KMLK-025, ADR-0008).
/// </summary>
/// <remarks>
/// <para>
/// Anahtar KAYNAK KODA ve VERITABANINA YAZILMAZ: <c>Identity__CodeHashKey</c> ortam
/// degiskeni (UAT/uretim) veya User Secrets (gelistirme). Deger 32 baytlik rastgele bir
/// anahtarin Base64 bicimidir (<c>openssl rand -base64 32</c>).
/// </para>
/// <para>
/// Duz ozet (SHA-256) yetmez: 6 haneli kodun bir milyon olasiligi saniyeler icinde
/// denenir. Anahtarli ozette veritabanini ele geciren, anahtar olmadan kodu bulamaz.
/// </para>
/// <para>
/// Tanimli degilse uygulama acilir ama kod uretilemez. Anahtar degistirilirse o an acik
/// olan kodlar gecersizlesir; kullanicilar yeni kod ister.
/// </para>
/// </remarks>
public sealed class CodeHashOptions : IValidatableObject
{
    /// <summary>Yapilandirma bolumunun adi.</summary>
    public const string SectionName = "Identity";

    /// <summary>Anahtar uzunlugu (bayt).</summary>
    public const int KeySizeBytes = 32;

    /// <summary>Base64 bicimli anahtar.</summary>
    public string? CodeHashKey { get; init; }

    /// <summary>Anahtar tanimli mi.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(CodeHashKey);

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
            bytes = Convert.FromBase64String(CodeHashKey!);
        }
        catch (FormatException)
        {
            // Asagida anlasilir bir iletiye donusturulur; deger iletiye YAZILMAZ.
        }

        if (bytes is null || bytes.Length != KeySizeBytes)
        {
            yield return new ValidationResult(
                $"Identity:CodeHashKey, {KeySizeBytes} baytlik bir anahtarin Base64 bicimi olmalidir (openssl rand -base64 32).",
                [nameof(CodeHashKey)]);
        }
    }
}
