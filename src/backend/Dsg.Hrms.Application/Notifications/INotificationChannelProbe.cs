using Dsg.Hrms.Domain.Notifications;

namespace Dsg.Hrms.Application.Notifications;

/// <summary>
/// Gonderim kanallarinin ILETI GONDERMEDEN yoklanmasi (#85).
/// </summary>
/// <remarks>
/// E-postada SMTP sunucusuna baglanip kimlik dogrulanir; SMS'te NetGSM bakiyesi ve
/// gonderici adi sorgulanir. Yanlis parola veya tanimsiz gonderici adi, ilk kullanici
/// kod bekleyene kadar fark edilmemis olmaz.
/// </remarks>
public interface INotificationChannelProbe
{
    /// <summary>Kanali yoklar. Sonuc kisa bir sure onbellekte tutulur.</summary>
    Task<ChannelProbeResult> ProbeAsync(NotificationChannel channel, CancellationToken cancellationToken);
}

/// <summary>Yoklama sonucu.</summary>
/// <param name="State">Durum.</param>
/// <param name="ResultCode">Saglayici kodu veya hata turu; sir veya adres ICERMEZ.</param>
public sealed record ChannelProbeResult(ChannelState State, string? ResultCode);

/// <summary>Kanal durumu.</summary>
public enum ChannelState
{
    /// <summary>Erisim bilgileri tanimli degil; kanal bilincli olarak kapali.</summary>
    NotConfigured = 0,

    /// <summary>Kimlik dogrulamasi basarili, kanal kullanilabilir.</summary>
    Healthy = 1,

    /// <summary>Kimlik dogrulamasi veya gonderici adi hatali; ileti gonderilemez.</summary>
    Misconfigured = 2,

    /// <summary>Sunucuya ulasilamadi.</summary>
    Unreachable = 3,
}
