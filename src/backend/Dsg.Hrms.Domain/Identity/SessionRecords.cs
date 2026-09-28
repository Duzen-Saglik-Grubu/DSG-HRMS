namespace Dsg.Hrms.Domain.Identity;

/// <summary>
/// Yenileme jetonu (SYG-KMLK-037, 040, 043). Jetonun kendisi SAKLANMAZ; SHA-256 ozeti tutulur.
/// </summary>
/// <remarks>
/// Jeton 256 bit rastgeledir; bu buyuklukte bir degerin duz ozeti yeterlidir (dogrulama
/// kodunun aksine denenerek bulunamaz). Her kullanimda yenisi uretilir ve eskisi
/// <see cref="UsedAt"/> ile isaretlenir; isaretli bir jeton tekrar gelirse bu calinma
/// belirtisidir (SYG-KMLK-040).
/// </remarks>
public sealed class RefreshToken
{
    private RefreshToken()
    {
    }

    /// <summary>Veritabani ici birincil anahtar.</summary>
    public long Id { get; private set; }

    /// <summary>Oturum.</summary>
    public long SessionId { get; private set; }

    /// <summary>
    /// Jetonun SHA-256 ozeti (Base64). Ad bilincli secildi: ad tabanli maskeleme kurali bu
    /// alani gunluge hic yazmaz.
    /// </summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>Uretilme ani.</summary>
    public DateTimeOffset IssuedAt { get; private set; }

    /// <summary>Kullanildigi (yenisiyle degistirildigi) an; kullanilmadiysa <c>null</c>.</summary>
    public DateTimeOffset? UsedAt { get; private set; }

    /// <summary>Yeni jeton kaydi.</summary>
    public static RefreshToken Issue(long sessionId, string tokenHash, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sessionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        return new RefreshToken { SessionId = sessionId, TokenHash = tokenHash, IssuedAt = now };
    }
}

/// <summary>
/// Girilen e-posta basina hatali giris sayaci (SYG-KMLK-033).
/// </summary>
/// <remarks>
/// Sayac HESABA degil girilen e-postaya baglidir: var olmayan bir adres de kilitlenir.
/// Hesaba bagli olsaydi "kilitlendi" yaniti hesabin var oldugunu ele verirdi (AN-05).
/// E-posta duz saklanmaz; anahtarli ozeti tutulur.
/// </remarks>
public sealed class LoginThrottle
{
    private LoginThrottle()
    {
    }

    /// <summary>Veritabani ici birincil anahtar.</summary>
    public long Id { get; private set; }

    /// <summary>Girilen e-postanin anahtarli ozeti.</summary>
    public string EmailHash { get; private set; } = string.Empty;

    /// <summary>Art arda hatali deneme sayisi.</summary>
    public int FailedCount { get; private set; }

    /// <summary>Kilidin kalkacagi an; kilitli degilse <c>null</c>.</summary>
    public DateTimeOffset? LockedUntil { get; private set; }

    /// <summary>Son degisiklik ani.</summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>Yeni sayac.</summary>
    public static LoginThrottle For(string emailHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(emailHash);
        return new LoginThrottle { EmailHash = emailHash, UpdatedAt = now };
    }

    /// <summary>Simdi kilitli mi. Suresi dolmus kilit sayaci sifirlar.</summary>
    public bool IsLocked(DateTimeOffset now)
    {
        if (LockedUntil is null)
        {
            return false;
        }

        if (now < LockedUntil)
        {
            return true;
        }

        LockedUntil = null;
        FailedCount = 0;
        UpdatedAt = now;
        return false;
    }

    /// <summary>
    /// Hatali denemeyi kaydeder. Sinira ulasildiysa kilitler ve kilidin kalkacagi ani doner.
    /// </summary>
    public DateTimeOffset? RegisterFailure(int maxFailures, TimeSpan lockDuration, DateTimeOffset now)
    {
        FailedCount++;
        UpdatedAt = now;

        if (FailedCount < maxFailures)
        {
            return null;
        }

        LockedUntil = now + lockDuration;
        return LockedUntil;
    }

    /// <summary>Basarili giriste sayaci sifirlar.</summary>
    public void Reset(DateTimeOffset now)
    {
        FailedCount = 0;
        LockedUntil = null;
        UpdatedAt = now;
    }
}

/// <summary>
/// Parolasi dogrulanmis, iki adimli dogrulamayi bekleyen giris (SYG-KMLK-034).
/// </summary>
public sealed class LoginChallenge
{
    /// <summary>Kodun istenip dogrulanabilecegi sure.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

    private LoginChallenge()
    {
    }

    /// <summary>Veritabani ici birincil anahtar.</summary>
    public long Id { get; private set; }

    /// <summary>Dis kimlik.</summary>
    public Guid PublicId { get; private set; } = Guid.CreateVersion7();

    /// <summary>Hesap.</summary>
    public long UserAccountId { get; private set; }

    /// <summary>Sunulan kanallar.</summary>
    public RegistrationChannels OfferedChannels { get; private set; }

    /// <summary>Gonderilen son kodun dis kimligi.</summary>
    public Guid? VerificationCodeId { get; private set; }

    /// <summary>Gecerlilik sonu.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Tamamlandi (oturum acildi) mi.</summary>
    public bool Completed { get; private set; }

    /// <summary>Istemcinin IP adresi.</summary>
    public string? IpAddress { get; private set; }

    /// <summary>Yeni bekleyen giris.</summary>
    public static LoginChallenge Start(long userAccountId, RegistrationChannels channels, string? ipAddress, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userAccountId);

        if (channels == RegistrationChannels.None)
        {
            throw new ArgumentException("En az bir kanal sunulmalidir.", nameof(channels));
        }

        return new LoginChallenge
        {
            UserAccountId = userAccountId,
            OfferedChannels = channels,
            IpAddress = ipAddress,
            ExpiresAt = now + Lifetime,
        };
    }

    /// <summary>Kullanilabilir mi.</summary>
    public bool IsUsable(DateTimeOffset now) => !Completed && now < ExpiresAt;

    /// <summary>Gonderilen kodu kaydeder.</summary>
    public void CodeSent(Guid verificationCodeId) => VerificationCodeId = verificationCodeId;

    /// <summary>Oturum acildi; bekleyen giris kapanir.</summary>
    public void Complete() => Completed = true;
}
