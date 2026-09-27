using Dsg.Hrms.Domain.Common;

namespace Dsg.Hrms.Domain.Identity;

/// <summary>
/// Tek kullanimlik dogrulama kodu (ADR-0006 §3, SYG-KMLK-022…027).
/// </summary>
/// <remarks>
/// <para>
/// Kodun kendisi SAKLANMAZ; yalnizca anahtarli ozeti (<see cref="CodeHash"/>) tutulur
/// (SYG-KMLK-025). Uyelik, parola sifirlama ve iki adimli dogrulama ayni varligi
/// kullanir (REQ-KMLK-051: ikinci bir kod mekanizmasi ikinci bir acik yuzeyidir).
/// </para>
/// <para>
/// Durum makinesi SYG-KMLK §3.2: <see cref="VerificationCodeStatus.Issued"/> disindaki
/// her durum SONDUR; kod bir daha kabul edilmez.
/// </para>
/// </remarks>
public sealed class VerificationCode : Entity, IAuditable
{
    private VerificationCode()
    {
    }

    /// <summary>Kodun gonderildigi kisinin kimligi.</summary>
    public long PersonId { get; private set; }

    /// <summary>Kodun amaci.</summary>
    public VerificationPurpose Purpose { get; private set; }

    /// <summary>Kodun gonderildigi kanal.</summary>
    public VerificationChannel Channel { get; private set; }

    /// <summary>
    /// Kodun anahtarli ozeti (HMAC-SHA256, Base64). Ad bilincli secildi: denetim izinde
    /// ad tabanli kural bu alani HIC yazmaz (SYG-KMLK-026).
    /// </summary>
    public string CodeHash { get; private set; } = string.Empty;

    /// <summary>Gecerliligin bittigi an (UTC).</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Izin verilen en fazla yanlis deneme sayisi (uretim anindaki parametre).</summary>
    public int MaxFailedAttempts { get; private set; }

    /// <summary>Yapilan yanlis deneme sayisi.</summary>
    public int FailedAttempts { get; private set; }

    /// <summary>Durum.</summary>
    public VerificationCodeStatus Status { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public long? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }

    /// <summary>
    /// Yeni kod kaydi olusturur. Ozet, kodun dis kimligine baglanarak hesaplanacagi icin
    /// <see cref="SetHash"/> ile ayrica yazilir.
    /// </summary>
    public static VerificationCode Issue(
        long personId,
        VerificationPurpose purpose,
        VerificationChannel channel,
        DateTimeOffset expiresAt,
        int maxFailedAttempts)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(personId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxFailedAttempts);

        return new VerificationCode
        {
            PersonId = personId,
            Purpose = purpose,
            Channel = channel,
            ExpiresAt = expiresAt,
            MaxFailedAttempts = maxFailedAttempts,
            Status = VerificationCodeStatus.Issued,
        };
    }

    /// <summary>Kodun anahtarli ozetini yazar. Yalnizca bir kez.</summary>
    public void SetHash(string codeHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(codeHash);

        if (CodeHash.Length != 0)
        {
            throw new InvalidOperationException("Kod ozeti zaten yazilmis.");
        }

        CodeHash = codeHash;
    }

    /// <summary>
    /// Girilen kodu dogrular.
    /// </summary>
    /// <param name="matches">Girilen kodun ozeti <see cref="CodeHash"/> ile eslesiyor mu (sabit surede karsilastirilmis).</param>
    /// <param name="now">Simdiki an.</param>
    /// <remarks>
    /// <para>
    /// Sure, eslesmeden ONCE denetlenir: suresi dolmus kod dogru girilse de kabul
    /// edilmez ve kullaniciya bu durum ayri bildirilir (SYG-KMLK-023).
    /// </para>
    /// <para>
    /// Deneme siniri "en fazla N kez yanlis" anlamindadir: N yanlis denemeden sonra kod
    /// hala gecerlidir, N+1'inci yanlis deneme kodu iptal eder (REQ-KMLK-017 kabul
    /// olcutu: "4. yanlis denemede kod iptal oluyor").
    /// </para>
    /// </remarks>
    public VerificationResult Verify(bool matches, DateTimeOffset now)
    {
        if (Status != VerificationCodeStatus.Issued)
        {
            return Status switch
            {
                VerificationCodeStatus.Expired => VerificationResult.Expired,
                VerificationCodeStatus.AttemptsExceeded => VerificationResult.AttemptsExceeded,
                _ => VerificationResult.NotUsable,
            };
        }

        if (now >= ExpiresAt)
        {
            Status = VerificationCodeStatus.Expired;
            return VerificationResult.Expired;
        }

        if (matches)
        {
            Status = VerificationCodeStatus.Verified;
            return VerificationResult.Verified;
        }

        FailedAttempts++;
        if (FailedAttempts > MaxFailedAttempts)
        {
            Status = VerificationCodeStatus.AttemptsExceeded;
            return VerificationResult.AttemptsExceeded;
        }

        return VerificationResult.Mismatch;
    }

    /// <summary>
    /// Kodu gecersiz kilar (kanal degisimi veya yeni kod istendi, SYG-KMLK-019).
    /// Kod zaten son durumdaysa hicbir sey yapmaz.
    /// </summary>
    public bool Invalidate()
    {
        if (Status != VerificationCodeStatus.Issued)
        {
            return false;
        }

        Status = VerificationCodeStatus.Invalidated;
        return true;
    }
}

/// <summary>Kodun amaci.</summary>
public enum VerificationPurpose
{
    /// <summary>Uyelik (SYG-KMLK-013).</summary>
    Registration = 1,

    /// <summary>Parola sifirlama (SYG-KMLK-047).</summary>
    PasswordReset = 2,

    /// <summary>Iki adimli dogrulama (SYG-KMLK-034).</summary>
    TwoFactor = 3,
}

/// <summary>Kodun gonderildigi kanal.</summary>
public enum VerificationChannel
{
    /// <summary>Kurumsal e-posta.</summary>
    Email = 1,

    /// <summary>Cep telefonu (SMS).</summary>
    Sms = 2,
}

/// <summary>Kod durumu (SYG-KMLK §3.2).</summary>
public enum VerificationCodeStatus
{
    /// <summary>Uretildi; dogrulanmayi bekliyor.</summary>
    Issued = 1,

    /// <summary>Dogrulandi (tek kullanim).</summary>
    Verified = 2,

    /// <summary>Suresi doldu.</summary>
    Expired = 3,

    /// <summary>Yanlis deneme siniri asildi.</summary>
    AttemptsExceeded = 4,

    /// <summary>Kanal degisimi veya yeni kod nedeniyle gecersiz kilindi.</summary>
    Invalidated = 5,
}

/// <summary>Dogrulama denemesinin sonucu.</summary>
public enum VerificationResult
{
    /// <summary>Kod dogru; islem devam edebilir.</summary>
    Verified = 1,

    /// <summary>Kod yanlis; deneme hakki suruyor.</summary>
    Mismatch = 2,

    /// <summary>Suresi doldu; yeni kod istenmeli.</summary>
    Expired = 3,

    /// <summary>Deneme siniri asildi; yeni kod istenmeli.</summary>
    AttemptsExceeded = 4,

    /// <summary>Kod kullanilmis, gecersiz kilinmis veya bulunamadi.</summary>
    NotUsable = 5,
}
