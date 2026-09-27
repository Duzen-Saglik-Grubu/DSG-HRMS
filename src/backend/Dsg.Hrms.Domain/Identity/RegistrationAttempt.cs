using Dsg.Hrms.Domain.Common;

namespace Dsg.Hrms.Domain.Identity;

/// <summary>
/// Bir uyelik denemesi (ADR-0006 §1, SYG-KMLK-013…021).
/// </summary>
/// <remarks>
/// <para>
/// <b>Eslesme olsa da olmasa da olusturulur</b> (<c>KR-016</c>). Eslesmeyen denemede
/// <see cref="PersonId"/> bostur ve kod gonderilmez; ama deneme, gercek bir kod varmis
/// gibi ayni sure ve deneme sinirlariyla isler (<see cref="VerifyDecoy"/>). Boylece
/// yanitlarin hicbiri "bu bilgiler bir personele ait mi" sorusunu cevaplamaz.
/// </para>
/// <para>
/// TCKN duz metin SAKLANMAZ: yabancilarin yazdigi rastgele TCKN'ler de burada durur.
/// Yalnizca anahtarli ozeti (<see cref="NationalIdHash"/>) hiz siniri icin tutulur;
/// alan adi denetim izinde ad tabanli kuralla hic yazilmaz.
/// </para>
/// </remarks>
public sealed class RegistrationAttempt : Entity, IAuditable
{
    /// <summary>Denemenin toplam omru: bu sure icinde kod dogrulanmalidir.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(30);

    /// <summary>Dogrulamadan sonra parolanin belirlenebilecegi sure.</summary>
    public static readonly TimeSpan CompletionWindow = TimeSpan.FromMinutes(15);

    private RegistrationAttempt()
    {
    }

    /// <summary>Girilen TCKN'nin anahtarli ozeti (hiz siniri).</summary>
    public string NationalIdHash { get; private set; } = string.Empty;

    /// <summary>Eslesen kisi; eslesme yoksa <c>null</c>.</summary>
    public long? PersonId { get; private set; }

    /// <summary>Istemcinin IP adresi (IP basina hiz siniri, SYG-KMLK-059).</summary>
    public string? IpAddress { get; private set; }

    /// <summary>Kullaniciya sunulan kanallar.</summary>
    public RegistrationChannels OfferedChannels { get; private set; }

    /// <summary>Durum.</summary>
    public RegistrationStatus Status { get; private set; }

    /// <summary>Denemenin gecerliliginin bittigi an.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Eslesmede gonderilen son kodun dis kimligi.</summary>
    public Guid? VerificationCodeId { get; private set; }

    /// <summary>Eslesmeyen denemede sahte kodun "gecerlilik" sonu.</summary>
    public DateTimeOffset? DecoyCodeExpiresAt { get; private set; }

    /// <summary>Eslesmeyen denemede yapilan yanlis deneme sayisi.</summary>
    public int DecoyFailedAttempts { get; private set; }

    /// <summary>Eslesmeyen denemede izin verilen yanlis deneme sayisi.</summary>
    public int DecoyMaxAttempts { get; private set; }

    /// <summary>Kodun dogrulandigi an.</summary>
    public DateTimeOffset? VerifiedAt { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public long? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }

    /// <summary>Eslesme varsa <c>true</c>.</summary>
    public bool IsMatch => PersonId is not null;

    /// <summary>Yeni deneme baslatir.</summary>
    public static RegistrationAttempt Start(
        string nationalIdHash,
        long? personId,
        string? ipAddress,
        RegistrationChannels offeredChannels,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nationalIdHash);

        if (offeredChannels == RegistrationChannels.None)
        {
            throw new ArgumentException("En az bir kanal sunulmalidir.", nameof(offeredChannels));
        }

        return new RegistrationAttempt
        {
            NationalIdHash = nationalIdHash,
            PersonId = personId,
            IpAddress = ipAddress,
            OfferedChannels = offeredChannels,
            Status = RegistrationStatus.Started,
            ExpiresAt = now + Lifetime,
        };
    }

    /// <summary>Kod istenebilir mi (deneme suruyor, dogrulanmamis).</summary>
    public bool CanRequestCode(DateTimeOffset now) =>
        now < ExpiresAt && Status is RegistrationStatus.Started or RegistrationStatus.CodeSent;

    /// <summary>Eslesen denemede gonderilen kodu kaydeder.</summary>
    public void CodeSent(Guid verificationCodeId)
    {
        EnsureMatch(true);
        VerificationCodeId = verificationCodeId;
        Status = RegistrationStatus.CodeSent;
    }

    /// <summary>
    /// Eslesmeyen denemede "kod gonderildi" durumuna gecer; sahte kodun suresi ve deneme
    /// hakki yeniden baslar (gercek akista yeni kod oncekini gecersiz kilar).
    /// </summary>
    public void DecoyCodeSent(DateTimeOffset expiresAt, int maxAttempts)
    {
        EnsureMatch(false);
        DecoyCodeExpiresAt = expiresAt;
        DecoyFailedAttempts = 0;
        DecoyMaxAttempts = maxAttempts;
        Status = RegistrationStatus.CodeSent;
    }

    /// <summary>
    /// Eslesmeyen denemede girilen kodu "dogrular": hicbir kod dogru degildir; sonuc,
    /// gercek koddaki yanlis girisle AYNI kurallari izler (sure, deneme siniri).
    /// </summary>
    public VerificationResult VerifyDecoy(DateTimeOffset now)
    {
        EnsureMatch(false);

        if (Status != RegistrationStatus.CodeSent || DecoyCodeExpiresAt is null)
        {
            return VerificationResult.NotUsable;
        }

        if (DecoyFailedAttempts > DecoyMaxAttempts)
        {
            return VerificationResult.AttemptsExceeded;
        }

        if (now >= DecoyCodeExpiresAt)
        {
            return VerificationResult.Expired;
        }

        DecoyFailedAttempts++;
        return DecoyFailedAttempts > DecoyMaxAttempts ? VerificationResult.AttemptsExceeded : VerificationResult.Mismatch;
    }

    /// <summary>Kod dogrulandi.</summary>
    public void MarkVerified(DateTimeOffset now)
    {
        EnsureMatch(true);
        Status = RegistrationStatus.Verified;
        VerifiedAt = now;
    }

    /// <summary>Parola belirlenebilir mi (dogrulanmis, sure dolmamis).</summary>
    public bool CanComplete(DateTimeOffset now) =>
        Status == RegistrationStatus.Verified && VerifiedAt is not null && now < VerifiedAt.Value + CompletionWindow;

    /// <summary>Hesap olusturuldu; deneme kapanir.</summary>
    public void Complete()
    {
        if (Status != RegistrationStatus.Verified)
        {
            throw new InvalidOperationException("Dogrulanmamis deneme tamamlanamaz.");
        }

        Status = RegistrationStatus.Completed;
    }

    private void EnsureMatch(bool expected)
    {
        if (IsMatch != expected)
        {
            throw new InvalidOperationException(expected ? "Eslesmeyen denemede gercek kod kullanilamaz." : "Eslesen denemede sahte kod kullanilamaz.");
        }
    }
}

/// <summary>Uyelikte sunulan kanallar.</summary>
[Flags]
public enum RegistrationChannels
{
    /// <summary>Kanal yok.</summary>
    None = 0,

    /// <summary>Kurumsal e-posta.</summary>
    Email = 1,

    /// <summary>SMS.</summary>
    Sms = 2,
}

/// <summary>Uyelik denemesinin durumu.</summary>
public enum RegistrationStatus
{
    /// <summary>Bilgiler girildi; kanal secimi bekleniyor.</summary>
    Started = 1,

    /// <summary>Kod gonderildi (eslesmede) veya gonderilmis gibi davranildi.</summary>
    CodeSent = 2,

    /// <summary>Kod dogrulandi; parola bekleniyor.</summary>
    Verified = 3,

    /// <summary>Hesap olusturuldu.</summary>
    Completed = 4,
}
