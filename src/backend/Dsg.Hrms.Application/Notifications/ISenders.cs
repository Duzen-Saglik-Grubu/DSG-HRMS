namespace Dsg.Hrms.Application.Notifications;

/// <summary>SMS gonderici (ADR-0012 §2). Uygulama katmani saglayiciyi bilmez.</summary>
public interface ISmsSender
{
    /// <summary>Tek bir SMS gonderir. Istisna firlatmaz; sonucu dondurur.</summary>
    /// <param name="phone"><c>5XXXXXXXXX</c> bicimli numara.</param>
    /// <param name="messageBody">Metin (tek SMS boyu: 150 karakter).</param>
    /// <param name="reference">Takip ve mutabakat icin tekil kimlik.</param>
    /// <param name="cancellationToken">Iptal.</param>
    Task<SendResult> SendAsync(string phone, string messageBody, Guid reference, CancellationToken cancellationToken);
}

/// <summary>E-posta gonderici (ADR-0012 §3).</summary>
public interface IEmailSender
{
    /// <summary>Tek bir e-posta gonderir. Istisna firlatmaz; sonucu dondurur.</summary>
    Task<SendResult> SendAsync(string address, string subject, string messageBody, CancellationToken cancellationToken);
}

/// <summary>Gonderim denemesinin sonucu.</summary>
/// <param name="Outcome">Sonuc turu.</param>
/// <param name="ResultCode">Saglayici kodu veya bilinen hata turu; istisna metni DEGIL.</param>
/// <param name="ExternalId">Saglayicinin dis kimligi (NetGSM <c>jobid</c>).</param>
public sealed record SendResult(SendOutcome Outcome, string? ResultCode, string? ExternalId = null)
{
    /// <summary>Basarili sonuc.</summary>
    public static SendResult Sent(string? resultCode, string? externalId = null) => new(SendOutcome.Sent, resultCode, externalId);

    /// <summary>Gecici hata; yeniden denenebilir.</summary>
    public static SendResult Transient(string resultCode) => new(SendOutcome.TransientFailure, resultCode);

    /// <summary>Kalici hata; yeniden denenmez.</summary>
    public static SendResult Permanent(string resultCode) => new(SendOutcome.PermanentFailure, resultCode);

    /// <summary>Yapilandirma hatasi (kimlik bilgisi, gonderici adi); yoneticinin mudahalesi gerekir.</summary>
    public static SendResult Misconfigured(string resultCode) => new(SendOutcome.ConfigurationError, resultCode);
}

/// <summary>Gonderim sonucu turu (<c>analiz/02</c> §3.6).</summary>
public enum SendOutcome
{
    /// <summary>Saglayici kabul etti.</summary>
    Sent = 1,

    /// <summary>Ag, zaman asimi, 5xx, kota: yeniden denenir.</summary>
    TransientFailure = 2,

    /// <summary>Hatali istek veya alici: yeniden denenmez.</summary>
    PermanentFailure = 3,

    /// <summary>Kimlik bilgisi, gonderici adi veya eksik ayar: yeniden denenmez, kritik gunluk.</summary>
    ConfigurationError = 4,
}
