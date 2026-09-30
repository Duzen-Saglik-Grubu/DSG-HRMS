namespace Dsg.Hrms.Domain.Notifications;

/// <summary>
/// Bir iletinin gonderim kaydi (ADR-0012 §6).
/// </summary>
/// <remarks>
/// <para>
/// Ileti <b>icerigi kaydedilmez</b>: dogrulama kodu icerebilir (SYG-KMLK-026). Alici,
/// kisi kimligi ve maskeli adres/numarayla tanimlanir.
/// </para>
/// <para>
/// <b>Entity turunden turemez</b> ve denetim izine girmez: kendisi bir kayittir
/// (<c>PersonnelSyncRun</c> gibi).
/// </para>
/// </remarks>
public sealed class NotificationDelivery
{
    private NotificationDelivery()
    {
    }

    /// <summary>Veritabani ici birincil anahtar.</summary>
    public long Id { get; private set; }

    /// <summary>Dis kimlik. NetGSM'e <c>referansID</c> olarak da gonderilir.</summary>
    public Guid PublicId { get; private set; } = Guid.CreateVersion7();

    /// <summary>Kanal.</summary>
    public NotificationChannel Channel { get; private set; }

    /// <summary>Amac.</summary>
    public NotificationPurpose Purpose { get; private set; }

    /// <summary>Alici kisinin kimligi; kisiye bagli degilse <c>null</c>.</summary>
    public long? PersonId { get; private set; }

    /// <summary>Maskeli alici (<c>ah***@duzen.com.tr</c>, <c>532****567</c>).</summary>
    public string RecipientMasked { get; private set; } = string.Empty;

    /// <summary>Sonuc.</summary>
    public DeliveryStatus Status { get; private set; }

    /// <summary>
    /// Saglayici sonuc kodu (NetGSM <c>00</c>, <c>30</c>…; SMTP durum kodu) veya bilinen
    /// bir hata turu. Istisna metni YAZILMAZ: alici adresini tasiyabilir.
    /// </summary>
    public string? ResultCode { get; private set; }

    /// <summary>Saglayicinin dis kimligi (NetGSM <c>jobid</c>).</summary>
    public string? ExternalId { get; private set; }

    /// <summary>Gonderim denemesi sayisi.</summary>
    public int Attempts { get; private set; }

    /// <summary>Kuyruga alinma ani (UTC).</summary>
    public DateTimeOffset QueuedAt { get; private set; }

    /// <summary>Son durumun olustugu an (UTC).</summary>
    public DateTimeOffset CompletedAt { get; private set; }

    /// <summary>Gonderim kaydi olusturur.</summary>
    public static NotificationDelivery Record(
        NotificationChannel channel,
        NotificationPurpose purpose,
        long? personId,
        string recipientMasked,
        DeliveryStatus status,
        string? resultCode,
        string? externalId,
        int attempts,
        DateTimeOffset queuedAt,
        DateTimeOffset completedAt)
    {
        ArgumentNullException.ThrowIfNull(recipientMasked);

        return new NotificationDelivery
        {
            Channel = channel,
            Purpose = purpose,
            PersonId = personId,
            RecipientMasked = recipientMasked,
            Status = status,
            ResultCode = resultCode,
            ExternalId = externalId,
            Attempts = attempts,
            QueuedAt = queuedAt,
            CompletedAt = completedAt,
        };
    }
}

/// <summary>Iletim kanali.</summary>
public enum NotificationChannel
{
    /// <summary>E-posta.</summary>
    Email = 1,

    /// <summary>SMS.</summary>
    Sms = 2,
}

/// <summary>Iletinin amaci (ADR-0012 §6).</summary>
public enum NotificationPurpose
{
    /// <summary>Uyelik dogrulama kodu.</summary>
    RegistrationCode = 1,

    /// <summary>Parola sifirlama kodu.</summary>
    PasswordResetCode = 2,

    /// <summary>Iki adimli dogrulama kodu.</summary>
    TwoFactorCode = 3,

    /// <summary>IK davetiyle parola olusturma baglantisi (SYG-KMLK-051).</summary>
    Invitation = 4,
}

/// <summary>Gonderim sonucu.</summary>
public enum DeliveryStatus
{
    /// <summary>Saglayici iletiyi kabul etti (SMTP sunucusu / NetGSM kuyrugu).</summary>
    Sent = 1,

    /// <summary>Gonderilemedi; yeniden deneme hakki bitti veya hata kalici.</summary>
    Failed = 2,

    /// <summary>Gonderim kipi geregi gonderilmedi (<c>LogOnly</c> veya izin listesi disi).</summary>
    Suppressed = 3,
}
