using System.ComponentModel.DataAnnotations;

namespace Dsg.Hrms.Infrastructure.Logo;

/// <summary>
/// LOGO baglanti ayarlari (ADR-0003, ADR-0008).
/// </summary>
/// <remarks>
/// <para>
/// Baglanti dizesi KAYNAK KODA YAZILMAZ. Gelistirmede .NET User Secrets, UAT ve uretimde
/// ortam degiskeni ile saglanir: <c>Logo__ConnectionString</c>.
/// </para>
/// <para>
/// Baglanti dizesi <b>salt okunur</b> LOGO oturumunu kullanmalidir (<c>KR-004</c>).
/// Yanlislikla yazma yetkili bir oturum verilirse senkronizasyon bunu her calismadan
/// once tespit eder ve calismayi reddeder.
/// </para>
/// <para>
/// Tanimli degilse senkronizasyon DEVRE DISI kalir ve uygulama calismaya devam eder
/// (gelistirme ortami, LOGO'ya erisimi olmayan test ortamlari).
/// </para>
/// </remarks>
public sealed class LogoOptions
{
    /// <summary>Yapilandirma bolumunun adi.</summary>
    public const string SectionName = "Logo";

    /// <summary>Salt okunur LOGO oturumunun baglanti dizesi. Bossa senkronizasyon devre disidir.</summary>
    public string? ConnectionString { get; init; }

    /// <summary>Komut zaman asimi (saniye).</summary>
    [Range(5, 600, ErrorMessage = "LOGO komut zaman asimi 5-600 saniye araliginda olmalidir.")]
    public int CommandTimeoutSeconds { get; init; } = 60;

    /// <summary>Baglanti tanimli mi.</summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ConnectionString);
}
