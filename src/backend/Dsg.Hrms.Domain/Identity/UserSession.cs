namespace Dsg.Hrms.Domain.Identity;

/// <summary>
/// Bir oturum: tek bir giristen cikisa kadar (ADR-0006 §8, SYG-KMLK-037…043).
/// </summary>
/// <remarks>
/// <para>
/// Oturumun iki suresi vardir: <b>toplam</b> sure (<see cref="ExpiresAt"/>, girisle baslar,
/// uzamaz) ve <b>hareketsizlik</b> suresi (<see cref="LastActivityAt"/>'ten itibaren). Yalnizca
/// kullanici etkilesimi ve etkinlik sinyali hareketsizlik sayacini sifirlar; jeton yenileme
/// ve arka plan istekleri sifirlamaz (SYG-KMLK-038).
/// </para>
/// <para>
/// Oturum acildigi andaki guvenlik damgasini tasir. Hesap pasiflesir veya parola degisirse
/// damga degisir ve oturum gecersiz olur (SYG-KMLK-054).
/// </para>
/// <para>
/// <c>Entity</c> turunden turemez ve denetim izine girmez: her jeton yenileme ve etkinlik
/// sinyali denetim izine satir eklerdi. Oturum olaylari ayri kaydedilir.
/// </para>
/// </remarks>
public sealed class UserSession
{
    /// <summary>Etkinlik sinyalinin dakikalik ust siniri (SYG-KMLK-039).</summary>
    public const int ActivitySignalsPerMinute = 2;

    private UserSession()
    {
    }

    /// <summary>Veritabani ici birincil anahtar.</summary>
    public long Id { get; private set; }

    /// <summary>Dis kimlik; erisim jetonunda <c>sid</c> olarak tasinir.</summary>
    public Guid PublicId { get; private set; } = Guid.CreateVersion7();

    /// <summary>Hesap.</summary>
    public long UserAccountId { get; private set; }

    /// <summary>Oturum acildigindaki guvenlik damgasi.</summary>
    public Guid SecurityStamp { get; private set; }

    /// <summary>Giris ani.</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>Toplam sure siniri (SYG-KMLK-037).</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Son kullanici etkilesimi (SYG-KMLK-038).</summary>
    public DateTimeOffset LastActivityAt { get; private set; }

    /// <summary>Etkinlik sinyali sayacinin dakika penceresinin basi.</summary>
    public DateTimeOffset ActivityWindowStartedAt { get; private set; }

    /// <summary>Pencerede kabul edilen sinyal sayisi.</summary>
    public int ActivityWindowCount { get; private set; }

    /// <summary>Oturumun kapandigi an; aciksa <c>null</c>.</summary>
    public DateTimeOffset? EndedAt { get; private set; }

    /// <summary>Kapanma nedeni.</summary>
    public SessionEndReason? EndReason { get; private set; }

    /// <summary>Istemcinin IP adresi.</summary>
    public string? IpAddress { get; private set; }

    /// <summary>Oturum acik mi.</summary>
    public bool IsOpen => EndedAt is null;

    /// <summary>Yeni oturum acar.</summary>
    public static UserSession Start(long userAccountId, Guid securityStamp, string? ipAddress, DateTimeOffset now, TimeSpan maxLifetime)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userAccountId);

        return new UserSession
        {
            UserAccountId = userAccountId,
            SecurityStamp = securityStamp,
            IpAddress = ipAddress,
            StartedAt = now,
            ExpiresAt = now + maxLifetime,
            LastActivityAt = now,
            ActivityWindowStartedAt = now,
        };
    }

    /// <summary>
    /// Oturum simdi gecerli mi; degilse nedeni doner ve oturumu kapatir.
    /// </summary>
    /// <param name="currentStamp">Hesabin guncel guvenlik damgasi.</param>
    /// <param name="accountActive">Hesap aktif mi.</param>
    /// <param name="now">Simdiki an.</param>
    /// <param name="idleTimeout">Hareketsizlik suresi.</param>
    public SessionEndReason? Check(Guid currentStamp, bool accountActive, DateTimeOffset now, TimeSpan idleTimeout)
    {
        if (!IsOpen)
        {
            return EndReason;
        }

        SessionEndReason? reason =
            !accountActive || currentStamp != SecurityStamp ? SessionEndReason.AccountChanged
            : now >= ExpiresAt ? SessionEndReason.Expired
            : now - LastActivityAt >= idleTimeout ? SessionEndReason.IdleTimeout
            : null;

        if (reason is not null)
        {
            End(reason.Value, now);
        }

        return reason;
    }

    /// <summary>
    /// Kullanici etkilesimini veya etkinlik sinyalini kaydeder (SYG-KMLK-038, 039). Dakikada
    /// <see cref="ActivitySignalsPerMinute"/>'den fazlasi kabul edilmez. Toplam sureyi UZATMAZ.
    /// </summary>
    /// <returns>Kabul edildiyse <c>true</c>.</returns>
    public bool RecordActivity(DateTimeOffset now)
    {
        if (!IsOpen)
        {
            return false;
        }

        if (now - ActivityWindowStartedAt >= TimeSpan.FromMinutes(1))
        {
            ActivityWindowStartedAt = now;
            ActivityWindowCount = 0;
        }

        if (ActivityWindowCount >= ActivitySignalsPerMinute)
        {
            return false;
        }

        ActivityWindowCount++;
        LastActivityAt = now;
        return true;
    }

    /// <summary>Oturumu kapatir. Zaten kapaliysa ilk neden korunur.</summary>
    public void End(SessionEndReason reason, DateTimeOffset now)
    {
        if (!IsOpen)
        {
            return;
        }

        EndedAt = now;
        EndReason = reason;
    }
}

/// <summary>
/// Oturumun kapanma nedeni. Istemciye makine tarafindan okunabilir kod olarak doner
/// (SYG-KMLK-042).
/// </summary>
public enum SessionEndReason
{
    /// <summary>Kullanici cikis yapti (SYG-KMLK-043).</summary>
    LoggedOut = 1,

    /// <summary>Ayni hesapla baska bir cihazdan giris yapildi (SYG-KMLK-041, 042).</summary>
    SignedInElsewhere = 2,

    /// <summary>Kullanilmis yenileme jetonu tekrar sunuldu (SYG-KMLK-040).</summary>
    TokenReuse = 3,

    /// <summary>Hareketsizlik suresi doldu (SYG-KMLK-038).</summary>
    IdleTimeout = 4,

    /// <summary>Toplam oturum suresi doldu (SYG-KMLK-037).</summary>
    Expired = 5,

    /// <summary>Hesap pasiflesti veya parola degisti (SYG-KMLK-054).</summary>
    AccountChanged = 6,
}
