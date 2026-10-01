using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Application.Identity.Sessions;

/// <summary>Oturumlarin veritabani tarafi.</summary>
public interface ISessionStore
{
    /// <summary>
    /// Giris e-postasiyla hesabi bulur. Adres birden fazla kisiye tanimliysa veya kisinin
    /// hesabi yoksa <c>null</c> (SYG-KMLK-008, 031).
    /// </summary>
    Task<SignInCandidate?> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    /// <summary>Hesabi kimligiyle bulur (izlenen).</summary>
    Task<SignInCandidate?> FindByAccountIdAsync(long userAccountId, CancellationToken cancellationToken);

    /// <summary>Girilen e-postanin sayacini bulur (izlenen); yoksa <c>null</c>.</summary>
    Task<LoginThrottle?> FindThrottleAsync(string emailHash, CancellationToken cancellationToken);

    /// <summary>Hesabin acik oturumlari (izlenen).</summary>
    Task<IReadOnlyList<UserSession>> GetOpenSessionsAsync(long userAccountId, CancellationToken cancellationToken);

    /// <summary>Oturumu dis kimligiyle bulur (izlenen).</summary>
    Task<UserSession?> FindSessionAsync(Guid publicId, CancellationToken cancellationToken);

    /// <summary>Jeton ozetiyle jetonu ve oturumunu bulur (izlenen).</summary>
    Task<(RefreshToken Token, UserSession Session)?> FindRefreshTokenAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Jetonu KOSULLU olarak kullanilmis isaretler: yalnizca hala kullanilmamissa. Eszamanli iki
    /// yenilemeden yalnizca biri basarili olur; digeri yeniden kullanim sayilir (SYG-KMLK-040).
    /// </summary>
    Task<bool> TryMarkUsedAsync(long refreshTokenId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Bekleyen iki adimli girisi bulur (izlenen).</summary>
    Task<LoginChallenge?> FindChallengeAsync(Guid publicId, CancellationToken cancellationToken);

    /// <summary>Yeni kaydi ekler.</summary>
    void Add(object entity);

    /// <summary>Degisiklikleri kaydeder.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Giris icin aday hesap.</summary>
/// <param name="Account">Hesap (izlenen).</param>
/// <param name="Person">Kisi.</param>
/// <param name="HasActiveEmployment">En az bir aktif istihdam var mi.</param>
public sealed record SignInCandidate(UserAccount Account, Person Person, bool HasActiveEmployment);

/// <summary>Erisim jetonu uretir (JWT).</summary>
public interface IAccessTokenIssuer
{
    /// <summary>Jeton uretir.</summary>
    /// <param name="userAccountId">Hesap kimligi (<c>hrms:user_id</c>).</param>
    /// <param name="session">Oturum (<c>sid</c>).</param>
    /// <param name="issuedAt">Uretim ani; uygulamanin saatinden (#97).</param>
    /// <param name="expiresAt">Gecerlilik sonu.</param>
    string Issue(long userAccountId, UserSession session, DateTimeOffset issuedAt, DateTimeOffset expiresAt);
}

/// <summary>Acik oturum bilgisi; istemciye doner.</summary>
/// <param name="AccessToken">Erisim jetonu (tarayici belleginde tutulur).</param>
/// <param name="AccessTokenExpiresAt">Erisim jetonunun gecerlilik sonu.</param>
/// <param name="RefreshToken">Yenileme jetonu (YALNIZCA HttpOnly cerezde tasinir).</param>
/// <param name="SessionExpiresAt">Toplam oturum suresi siniri.</param>
/// <param name="IdleTimeout">Hareketsizlik suresi.</param>
/// <param name="FirstName">Kullanicinin adi.</param>
/// <param name="LastName">Kullanicinin soyadi.</param>
/// <param name="Permissions">Kullanicinin izinleri; istemci yalnizca GOSTERIM icin kullanir (ADR-0007 §3).</param>
/// <param name="PasswordChangeRequired">Oturum parola degisimi bekliyorsa nedeni (SYG-KMLK-046, 050).</param>
public sealed record SessionTokens(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset SessionExpiresAt,
    TimeSpan IdleTimeout,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Permissions,
    PasswordChangeReason? PasswordChangeRequired = null)
{
    /// <inheritdoc />
    public override string ToString() => nameof(SessionTokens);
}

/// <summary>Giris sonucu.</summary>
/// <param name="Session">Oturum acildiysa jetonlar.</param>
/// <param name="ChallengeId">Iki adimli dogrulama gerekiyorsa bekleyen girisin kimligi.</param>
/// <param name="Channels">Iki adimli dogrulamada sunulan kanallar.</param>
/// <param name="ChallengeExpiresAt">Bekleyen girisin gecerlilik sonu.</param>
public sealed record SignInResult(SessionTokens? Session, Guid? ChallengeId, RegistrationChannels Channels, DateTimeOffset? ChallengeExpiresAt);

/// <summary>E-posta veya parola hatali (SYG-KMLK-032). Hesap olsa da olmasa da ayni yanit.</summary>
public sealed class InvalidCredentialsException()
    : HrmsException("E-posta adresi veya parola hatalı.")
{
    /// <inheritdoc />
    public override int StatusCode => 401;

    /// <inheritdoc />
    public override string ErrorType => "invalid-credentials";

    /// <inheritdoc />
    public override string Title => "Giriş başarısız";
}

/// <summary>Girilen e-posta gecici olarak kilitli (SYG-KMLK-033).</summary>
public sealed class SignInLockedException(int minutes)
    : HrmsException($"Çok fazla hatalı giriş denemesi yapıldı. {minutes} dakika sonra tekrar deneyin.")
{
    /// <inheritdoc />
    public override int StatusCode => 429;

    /// <inheritdoc />
    public override string ErrorType => "sign-in-locked";

    /// <inheritdoc />
    public override string Title => "Giriş geçici olarak kapalı";
}

/// <summary>Hesap pasif (SYG-KMLK-055). Yalnizca parola dogruysa soylenir.</summary>
public sealed class AccountDisabledException()
    : HrmsException("Hesabınız kullanıma kapalı. İnsan Kaynakları birimiyle iletişime geçin.")
{
    /// <inheritdoc />
    public override int StatusCode => 403;

    /// <inheritdoc />
    public override string ErrorType => "account-disabled";

    /// <inheritdoc />
    public override string Title => "Hesap kapalı";
}

/// <summary>
/// Oturum sona erdi; istemci giris ekranina doner. Hata turu nedeni tasir
/// (<c>session-ended/signed-in-elsewhere</c> gibi), boylece istemci dogru iletiyi gosterir
/// (SYG-KMLK-042).
/// </summary>
public sealed class SessionEndedException(SessionEndReason? reason)
    : HrmsException(MessageFor(reason))
{
    /// <summary>Neden; bilinmiyorsa <c>null</c> (gecersiz veya bulunamayan jeton).</summary>
    public SessionEndReason? Reason { get; } = reason;

    /// <inheritdoc />
    public override int StatusCode => 401;

    /// <inheritdoc />
    public override string ErrorType => "session-ended/" + Reason switch
    {
        SessionEndReason.LoggedOut => "logged-out",
        SessionEndReason.SignedInElsewhere => "signed-in-elsewhere",
        SessionEndReason.TokenReuse => "token-reuse",
        SessionEndReason.IdleTimeout => "idle-timeout",
        SessionEndReason.Expired => "expired",
        SessionEndReason.AccountChanged => "account-changed",
        SessionEndReason.PasswordChanged => "password-changed",
        _ => "invalid",
    };

    /// <inheritdoc />
    public override string Title => "Oturum sona erdi";

    private static string MessageFor(SessionEndReason? reason) => reason switch
    {
        SessionEndReason.SignedInElsewhere => "Hesabınıza başka bir cihazdan giriş yapıldı. Devam etmek için yeniden giriş yapın.",
        SessionEndReason.IdleTimeout => "Uzun süre işlem yapılmadığı için oturumunuz kapandı. Lütfen yeniden giriş yapın.",
        SessionEndReason.Expired => "Oturum süreniz doldu. Lütfen yeniden giriş yapın.",
        SessionEndReason.TokenReuse => "Güvenlik nedeniyle tüm oturumlarınız kapatıldı. Lütfen yeniden giriş yapın.",
        SessionEndReason.AccountChanged => "Hesap bilgileriniz değiştiği için oturumunuz kapandı. Lütfen yeniden giriş yapın.",
        SessionEndReason.PasswordChanged => "Parolanız değiştirildiği için oturumunuz kapandı. Yeni parolanızla giriş yapın.",
        _ => "Oturumunuz sona erdi. Lütfen yeniden giriş yapın.",
    };
}

/// <summary>Her istekte yapilan oturum denetiminin sonucu.</summary>
public enum SessionAccess
{
    /// <summary>Oturum kapali veya gecersiz.</summary>
    Closed = 0,

    /// <summary>Oturum acik.</summary>
    Open = 1,

    /// <summary>
    /// Oturum acik ama parola degisimi bekliyor: yalnizca parola degistirme ve oturum uclari
    /// kullanilabilir (SYG-KMLK-046, 050).
    /// </summary>
    PasswordChangeRequired = 2,
}

/// <summary>
/// Oturum parola degisimi bekliyor; istenen uc bu durumda kullanilamaz (SYG-KMLK-046, 050).
/// </summary>
public sealed class PasswordChangeRequiredException()
    : HrmsException("Devam etmek için önce parolanızı değiştirmeniz gerekiyor.")
{
    /// <inheritdoc />
    public override int StatusCode => 403;

    /// <inheritdoc />
    public override string ErrorType => "password-change-required";

    /// <inheritdoc />
    public override string Title => "Parola değişikliği gerekli";
}

/// <summary>Oturum icinde parola degisikliginin sonucu (SYG-KMLK-048).</summary>
/// <param name="CurrentPasswordInvalid">Mevcut parola hataliydi; hicbir sey degismedi.</param>
/// <param name="Violations">Yeni parolanin kural ihlalleri; bossa ve mevcut parola dogruysa parola degisti.</param>
public sealed record PasswordChangeResult(bool CurrentPasswordInvalid, IReadOnlyList<PasswordViolation> Violations)
{
    /// <summary>Parola degisti mi.</summary>
    public bool Succeeded => !CurrentPasswordInvalid && Violations.Count == 0;
}
