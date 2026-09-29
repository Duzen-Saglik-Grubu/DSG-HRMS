using System.Security.Cryptography;
using System.Text;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Application.Identity.Registration;
using Dsg.Hrms.Application.Identity.Verification;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace Dsg.Hrms.Application.Identity.Sessions;

/// <summary>
/// Giris, iki adimli dogrulama, jeton yenileme ve cikis (ADR-0006 §5, §7, §8; SYG-KMLK-031…043).
/// </summary>
/// <remarks>
/// <para>
/// <b>Hesap sizmaz (SYG-KMLK-032, 033):</b> hatali giriste hesap olsa da olmasa da ayni yanit
/// doner ve parola ozeti her durumda hesaplanir. Hatali giris sayaci girilen e-postaya
/// baglidir; var olmayan bir adres de kilitlenir.
/// </para>
/// <para>
/// Pasif hesap ancak parola DOGRUYSA "hesabiniz kapali" yanitini alir: bu bilgi parolayi
/// bilmeyene verilmez.
/// </para>
/// </remarks>
public sealed partial class SessionService
{
    private const string LoginIdentifierPrefix = "login:";

    private readonly ISessionStore _store;
    private readonly IPasswordHasher _passwordHasher;
    private readonly PasswordPolicy _passwordPolicy;
    private readonly IIdentifierHasher _identifierHasher;
    private readonly IAccessTokenIssuer _tokens;
    private readonly VerificationCodeService _codes;
    private readonly AccessControlService _access;
    private readonly ISystemParameters _parameters;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<SessionService> _logger;

    /// <summary>Yeni ornek olusturur.</summary>
    public SessionService(
        ISessionStore store,
        IPasswordHasher passwordHasher,
        PasswordPolicy passwordPolicy,
        IIdentifierHasher identifierHasher,
        IAccessTokenIssuer tokens,
        VerificationCodeService codes,
        AccessControlService access,
        ISystemParameters parameters,
        IDateTimeProvider clock,
        ILogger<SessionService> logger)
    {
        _store = store;
        _passwordHasher = passwordHasher;
        _passwordPolicy = passwordPolicy;
        _identifierHasher = identifierHasher;
        _tokens = tokens;
        _codes = codes;
        _access = access;
        _parameters = parameters;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>
    /// Giris sayacinin ozetlenen kimligi (SYG-KMLK-033). Sayac girilen e-postaya baglidir;
    /// parola sifirlama ve degisikligi de ayni sayaci kullanir.
    /// </summary>
    public static string LoginIdentifier(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        return LoginIdentifierPrefix + email.Trim().ToLowerInvariant();
    }

    // ------------------------------------------------------------------ giris

    /// <summary>E-posta ve parolayla giris (SYG-KMLK-031…034).</summary>
    /// <exception cref="InvalidCredentialsException">E-posta veya parola hatali.</exception>
    /// <exception cref="SignInLockedException">Girilen e-posta kilitli.</exception>
    /// <exception cref="AccountDisabledException">Parola dogru ama hesap pasif.</exception>
    public async Task<SignInResult> SignInAsync(string email, string password, string? ipAddress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(password);

        var now = _clock.UtcNow;
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var emailHash = _identifierHasher.HashIdentifier(LoginIdentifier(normalizedEmail));

        var throttle = await _store.FindThrottleAsync(emailHash, cancellationToken).ConfigureAwait(false);
        if (throttle is null)
        {
            throttle = LoginThrottle.For(emailHash, now);
            _store.Add(throttle);
        }

        var lockoutMinutes = await _parameters.GetIntegerAsync(ParameterCatalog.LockoutMinutes, cancellationToken).ConfigureAwait(false);
        var candidate = await _store.FindByEmailAsync(normalizedEmail, cancellationToken).ConfigureAwait(false);

        // Parola ozeti HER DURUMDA hesaplanir: kilitli, var olmayan veya parolasiz hesapta da.
        var normalizedPassword = PasswordPolicy.Normalize(password);
        var hash = candidate?.Account.PasswordHash ?? _passwordHasher.DummyHash;
        var passwordMatches = _passwordHasher.Verify(hash, normalizedPassword) && candidate?.Account.PasswordHash is not null;

        if (throttle.IsLocked(now))
        {
            await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            LogLocked(_logger);
            throw new SignInLockedException(lockoutMinutes);
        }

        if (!passwordMatches || candidate is null)
        {
            var maxFailures = await _parameters.GetIntegerAsync(ParameterCatalog.MaxFailedLogins, cancellationToken).ConfigureAwait(false);
            var lockedUntil = throttle.RegisterFailure(maxFailures, TimeSpan.FromMinutes(lockoutMinutes), now);
            if (lockedUntil is not null)
            {
                // Kilitlenme denetim izine duser (SYG-KMLK-058); hesap yoksa yalnizca sayac kilitlenir.
                candidate?.Account.RecordLockout(lockedUntil.Value);
                LogLockedNow(_logger, candidate?.Account.Id);
            }

            await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new InvalidCredentialsException();
        }

        throttle.Reset(now);
        candidate.Account.ClearLockout();

        if (candidate.Account.Status != AccountStatus.Active || !candidate.HasActiveEmployment)
        {
            await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            LogDisabled(_logger, candidate.Account.Id);
            throw new AccountDisabledException();
        }

        if (await _parameters.GetBooleanAsync(ParameterCatalog.TwoFactorEnabled, cancellationToken).ConfigureAwait(false))
        {
            var channels = await TwoFactorChannelsAsync(candidate, cancellationToken).ConfigureAwait(false);
            var challenge = LoginChallenge.Start(candidate.Account.Id, channels, ipAddress, now);
            _store.Add(challenge);
            await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            return new SignInResult(null, challenge.PublicId, channels, challenge.ExpiresAt);
        }

        var session = await OpenSessionAsync(candidate, ipAddress, now, cancellationToken).ConfigureAwait(false);
        return new SignInResult(session, null, RegistrationChannels.None, null);
    }

    // ------------------------------------------------------------------ iki adimli dogrulama

    /// <summary>Bekleyen giris icin kod gonderir (SYG-KMLK-034).</summary>
    /// <exception cref="NotFoundException">Bekleyen giris yoksa veya suresi dolduysa.</exception>
    /// <exception cref="TooManyRequestsException">Kod gonderim siniri asildiysa.</exception>
    public async Task<DateTimeOffset> RequestTwoFactorCodeAsync(Guid challengeId, RegistrationChannels channel, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var challenge = await FindUsableChallengeAsync(challengeId, now, cancellationToken).ConfigureAwait(false);

        if (channel is not (RegistrationChannels.Email or RegistrationChannels.Sms) || !challenge.OfferedChannels.HasFlag(channel))
        {
            throw new BusinessRuleException("Seçilen doğrulama yöntemi kullanılamıyor.");
        }

        var candidate = await _store.FindByAccountIdAsync(challenge.UserAccountId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(ChallengeExpiredMessage);
        var recipient = channel == RegistrationChannels.Email ? candidate.Person.Email : candidate.Person.MobilePhone;
        if (string.IsNullOrEmpty(recipient))
        {
            throw new BusinessRuleException("Seçilen doğrulama yöntemi kullanılamıyor.");
        }

        var issued = await _codes.IssueAsync(
            new VerificationRequest(candidate.Person.Id, VerificationPurpose.TwoFactor,
                channel == RegistrationChannels.Email ? VerificationChannel.Email : VerificationChannel.Sms, recipient),
            cancellationToken).ConfigureAwait(false);

        if (issued.IsRateLimited)
        {
            throw new TooManyRequestsException();
        }

        challenge.CodeSent(issued.CodeId!.Value);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return issued.ExpiresAt!.Value;
    }

    /// <summary>Kodu dogrular; dogruysa oturum acar (SYG-KMLK-034).</summary>
    /// <exception cref="NotFoundException">Bekleyen giris yoksa veya suresi dolduysa.</exception>
    public async Task<(VerificationResult Result, SessionTokens? Session)> CompleteTwoFactorAsync(
        Guid challengeId,
        string? code,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var challenge = await FindUsableChallengeAsync(challengeId, now, cancellationToken).ConfigureAwait(false);

        if (challenge.VerificationCodeId is null)
        {
            return (VerificationResult.NotUsable, null);
        }

        var result = await _codes.VerifyAsync(challenge.VerificationCodeId.Value, code, cancellationToken).ConfigureAwait(false);
        if (result != VerificationResult.Verified)
        {
            return (result, null);
        }

        var candidate = await _store.FindByAccountIdAsync(challenge.UserAccountId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(ChallengeExpiredMessage);

        // Kod beklenirken hesap pasiflesmis olabilir.
        if (candidate.Account.Status != AccountStatus.Active || !candidate.HasActiveEmployment)
        {
            throw new AccountDisabledException();
        }

        challenge.Complete();
        var session = await OpenSessionAsync(candidate, ipAddress ?? challenge.IpAddress, now, cancellationToken).ConfigureAwait(false);
        return (VerificationResult.Verified, session);
    }

    // ------------------------------------------------------------------ yenileme, etkinlik, cikis

    /// <summary>
    /// Yenileme jetonuyla yeni jetonlar uretir; eski jeton gecersizlesir (SYG-KMLK-040).
    /// Yenileme HAREKETSIZLIK SAYACINI SIFIRLAMAZ (SYG-KMLK-038).
    /// </summary>
    /// <exception cref="SessionEndedException">Oturum kapandiysa; neden hata turundedir.</exception>
    public async Task<SessionTokens> RefreshAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new SessionEndedException(null);
        }

        var now = _clock.UtcNow;
        var found = await _store.FindRefreshTokenAsync(HashToken(refreshToken), cancellationToken).ConfigureAwait(false)
            ?? throw new SessionEndedException(null);
        var (token, session) = found;

        if (token.UsedAt is not null || !await _store.TryMarkUsedAsync(token.Id, now, cancellationToken).ConfigureAwait(false))
        {
            // Oturum zaten kapaliysa (baska cihazdan giris, cikis) eski sekmenin tekrar denemesidir;
            // hesabin diger oturumlarina DOKUNULMAZ. Aksi halde eski bir sekme yeni cihazdaki
            // mesru oturumu da kapatirdi.
            if (!session.IsOpen)
            {
                throw new SessionEndedException(session.EndReason);
            }

            // Acik bir oturumun kullanilmis jetonu tekrar geldi: jeton calinmis olabilir.
            // Hesabin TUM oturumlari kapanir.
            foreach (var open in await _store.GetOpenSessionsAsync(session.UserAccountId, cancellationToken).ConfigureAwait(false))
            {
                open.End(SessionEndReason.TokenReuse, now);
            }

            await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            LogTokenReuse(_logger, session.UserAccountId);
            throw new SessionEndedException(SessionEndReason.TokenReuse);
        }

        var candidate = await _store.FindByAccountIdAsync(session.UserAccountId, cancellationToken).ConfigureAwait(false);
        var idle = TimeSpan.FromMinutes(await _parameters.GetIntegerAsync(ParameterCatalog.IdleTimeoutMinutes, cancellationToken).ConfigureAwait(false));
        var ended = candidate is null
            ? SessionEndReason.AccountChanged
            : session.Check(candidate.Account.SecurityStamp, IsUsable(candidate), now, idle);

        if (ended is not null)
        {
            session.End(ended.Value, now);
            await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new SessionEndedException(ended);
        }

        return await IssueTokensAsync(candidate!, session, idle, now, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Kullanici etkilesimi veya etkinlik sinyali (SYG-KMLK-038, 039). Dakikada en fazla iki
    /// kez kabul edilir; toplam oturum suresini uzatmaz.
    /// </summary>
    /// <exception cref="SessionEndedException">Oturum kapaliysa.</exception>
    /// <exception cref="TooManyRequestsException">Dakikalik sinir asildiysa.</exception>
    public async Task RecordActivityAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var session = await _store.FindSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null || !session.IsOpen)
        {
            throw new SessionEndedException(session?.EndReason);
        }

        if (!session.RecordActivity(now))
        {
            throw new TooManyRequestsException("Etkinlik bildirimi çok sık gönderildi.");
        }

        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Cikis: oturum ve jetonlari sunucuda iptal edilir (SYG-KMLK-043).</summary>
    public async Task SignOutAsync(string? refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var found = await _store.FindRefreshTokenAsync(HashToken(refreshToken), cancellationToken).ConfigureAwait(false);
        if (found is null)
        {
            return;
        }

        found.Value.Session.End(SessionEndReason.LoggedOut, _clock.UtcNow);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    // ------------------------------------------------------------------ parola degisikligi

    /// <summary>
    /// Oturum icinde parola degisikligi (SYG-KMLK-048). Mevcut parola istenir: acik birakilmis
    /// bir oturumu bulan kisi parolayi degistirip hesabi ele geciremez. Yanlis mevcut parola
    /// giris sayacina eklenir; sinir asilinca e-posta, giristeki gibi kilitlenir (SYG-KMLK-033).
    /// Degisiklikten sonra hesabin DIGER oturumlari kapanir; bu oturum acik kalir.
    /// </summary>
    /// <exception cref="SessionEndedException">Oturum kapaliysa.</exception>
    /// <exception cref="SignInLockedException">Hatali deneme siniri asildiysa.</exception>
    public async Task<PasswordChangeResult> ChangePasswordAsync(
        Guid sessionId,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentPassword);
        ArgumentNullException.ThrowIfNull(newPassword);

        var now = _clock.UtcNow;
        var session = await _store.FindSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null || !session.IsOpen)
        {
            throw new SessionEndedException(session?.EndReason);
        }

        var candidate = await _store.FindByAccountIdAsync(session.UserAccountId, cancellationToken).ConfigureAwait(false)
            ?? throw new SessionEndedException(SessionEndReason.AccountChanged);
        var account = candidate.Account;
        var person = candidate.Person;

        // Hesabin giris e-postasi yoksa sayac oturum kimligine baglanir; kilit yine isler.
        var throttleKey = _identifierHasher.HashIdentifier(
            person.Email is null ? $"session:{sessionId}" : LoginIdentifier(person.Email));
        var throttle = await _store.FindThrottleAsync(throttleKey, cancellationToken).ConfigureAwait(false);
        if (throttle is null)
        {
            throttle = LoginThrottle.For(throttleKey, now);
            _store.Add(throttle);
        }

        var lockoutMinutes = await _parameters.GetIntegerAsync(ParameterCatalog.LockoutMinutes, cancellationToken).ConfigureAwait(false);
        if (throttle.IsLocked(now))
        {
            await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new SignInLockedException(lockoutMinutes);
        }

        if (account.PasswordHash is null || !_passwordHasher.Verify(account.PasswordHash, PasswordPolicy.Normalize(currentPassword)))
        {
            var maxFailures = await _parameters.GetIntegerAsync(ParameterCatalog.MaxFailedLogins, cancellationToken).ConfigureAwait(false);
            var lockedUntil = throttle.RegisterFailure(maxFailures, TimeSpan.FromMinutes(lockoutMinutes), now);
            if (lockedUntil is not null)
            {
                account.RecordLockout(lockedUntil.Value);
                LogLockedNow(_logger, account.Id);
            }

            await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new PasswordChangeResult(CurrentPasswordInvalid: true, []);
        }

        var normalizedNew = PasswordPolicy.Normalize(newPassword);
        var violations = new List<PasswordViolation>(
            await _passwordPolicy.ValidateAsync(newPassword, RegistrationService.PersonalWords(person.FirstName, person.LastName, person.Email), cancellationToken)
                .ConfigureAwait(false));
        if (violations.Count == 0 && _passwordHasher.Verify(account.PasswordHash, normalizedNew))
        {
            violations.Add(PasswordViolation.SameAsCurrent);
        }

        if (violations.Count > 0)
        {
            // Mevcut parola dogruydu: sayac sifirlanir, ihlal kaydedilmez.
            throttle.Reset(now);
            await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new PasswordChangeResult(CurrentPasswordInvalid: false, violations);
        }

        account.SetPassword(_passwordHasher.Hash(normalizedNew), now);
        session.AdoptSecurityStamp(account.SecurityStamp);
        throttle.Reset(now);

        foreach (var other in await _store.GetOpenSessionsAsync(account.Id, cancellationToken).ConfigureAwait(false))
        {
            if (other.PublicId != session.PublicId)
            {
                other.End(SessionEndReason.PasswordChanged, now);
            }
        }

        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogPasswordChanged(_logger, account.Id);
        return new PasswordChangeResult(CurrentPasswordInvalid: false, []);
    }

    /// <summary>
    /// Erisim jetonunun oturumu hala acik mi. Her istekte cagrilir: pasiflesen hesap, baska
    /// cihazdan giris veya cikis erisim jetonunun suresini beklemeden etkili olur (SYG-KMLK-054).
    /// </summary>
    public async Task<bool> IsSessionActiveAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var session = await _store.FindSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);
        if (session is null || !session.IsOpen)
        {
            return false;
        }

        var candidate = await _store.FindByAccountIdAsync(session.UserAccountId, cancellationToken).ConfigureAwait(false);
        var idle = TimeSpan.FromMinutes(await _parameters.GetIntegerAsync(ParameterCatalog.IdleTimeoutMinutes, cancellationToken).ConfigureAwait(false));
        var ended = candidate is null ? SessionEndReason.AccountChanged : session.Check(candidate.Account.SecurityStamp, IsUsable(candidate), now, idle);

        if (ended is not null)
        {
            session.End(ended.Value, now);
            await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return false;
        }

        return true;
    }

    /// <summary>Yenileme jetonunun ozeti (SHA-256, Base64).</summary>
    public static string HashToken(string refreshToken) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));

    // ------------------------------------------------------------------ yardimcilar

    private const string ChallengeExpiredMessage = "Giriş işleminin süresi doldu. Lütfen yeniden giriş yapın.";

    private static bool IsUsable(SignInCandidate candidate) =>
        candidate.Account.Status == AccountStatus.Active && candidate.HasActiveEmployment;

    private async Task<SessionTokens> OpenSessionAsync(SignInCandidate candidate, string? ipAddress, DateTimeOffset now, CancellationToken cancellationToken)
    {
        // Tek aktif oturum (SYG-KMLK-041): yeni giris, onceki acik oturumlari kapatir.
        if (await _parameters.GetBooleanAsync(ParameterCatalog.SingleActiveSession, cancellationToken).ConfigureAwait(false))
        {
            foreach (var open in await _store.GetOpenSessionsAsync(candidate.Account.Id, cancellationToken).ConfigureAwait(false))
            {
                open.End(SessionEndReason.SignedInElsewhere, now);
            }
        }

        var maxHours = await _parameters.GetIntegerAsync(ParameterCatalog.SessionMaxHours, cancellationToken).ConfigureAwait(false);
        var idle = TimeSpan.FromMinutes(await _parameters.GetIntegerAsync(ParameterCatalog.IdleTimeoutMinutes, cancellationToken).ConfigureAwait(false));

        var session = UserSession.Start(candidate.Account.Id, candidate.Account.SecurityStamp, ipAddress, now, TimeSpan.FromHours(maxHours));
        _store.Add(session);

        // Ilk sistem yoneticisi kurulum yapilandirmasiyla belirlenir (SYG-KMLK-074); atama
        // oturumla birlikte kaydedilir.
        await _access.EnsureBootstrapAdministratorAsync(candidate.Account.Id, candidate.Person.Email, cancellationToken).ConfigureAwait(false);

        // Oturumun kimligi (Id) yenileme jetonu icin gereklidir.
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        LogSignedIn(_logger, candidate.Account.Id, session.PublicId);
        return await IssueTokensAsync(candidate, session, idle, now, cancellationToken).ConfigureAwait(false);
    }

    private async Task<SessionTokens> IssueTokensAsync(
        SignInCandidate candidate,
        UserSession session,
        TimeSpan idle,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        _store.Add(RefreshToken.Issue(session.Id, HashToken(raw), now));
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var accessMinutes = await _parameters.GetIntegerAsync(ParameterCatalog.AccessTokenLifetimeMinutes, cancellationToken).ConfigureAwait(false);
        var accessExpiresAt = now.AddMinutes(accessMinutes) < session.ExpiresAt ? now.AddMinutes(accessMinutes) : session.ExpiresAt;

        // Izinler her yenilemede yeniden okunur: rol degisikligi en gec erisim jetonunun omru
        // kadar sonra ekrana yansir. Sunucu tarafindaki denetim her istekte yapilir.
        var permissions = await _access.GetPermissionsAsync(candidate.Account.Id, cancellationToken).ConfigureAwait(false);

        return new SessionTokens(
            _tokens.Issue(candidate.Account.Id, session, now, accessExpiresAt),
            accessExpiresAt,
            raw,
            session.ExpiresAt,
            idle,
            candidate.Person.FirstName,
            candidate.Person.LastName,
            [.. permissions.Order(StringComparer.Ordinal)]);
    }

    private async Task<RegistrationChannels> TwoFactorChannelsAsync(SignInCandidate candidate, CancellationToken cancellationToken)
    {
        var allowed = RegistrationChannels.None;
        foreach (var item in await _parameters.GetListAsync(ParameterCatalog.VerificationChannels, cancellationToken).ConfigureAwait(false))
        {
            allowed |= item == "email" ? RegistrationChannels.Email : item == "sms" ? RegistrationChannels.Sms : RegistrationChannels.None;
        }

        var channels = RegistrationChannels.None;
        if (allowed.HasFlag(RegistrationChannels.Email) && candidate.Person.Email is not null)
        {
            channels |= RegistrationChannels.Email;
        }

        if (allowed.HasFlag(RegistrationChannels.Sms) && candidate.Person.MobilePhone is not null)
        {
            channels |= RegistrationChannels.Sms;
        }

        // Giris e-postasi her zaman vardir; kanal parametresi hicbirine izin vermiyorsa e-posta kullanilir.
        return channels == RegistrationChannels.None ? RegistrationChannels.Email : channels;
    }

    private async Task<LoginChallenge> FindUsableChallengeAsync(Guid challengeId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var challenge = await _store.FindChallengeAsync(challengeId, cancellationToken).ConfigureAwait(false);
        return challenge is not null && challenge.IsUsable(now) ? challenge : throw new NotFoundException(ChallengeExpiredMessage);
    }

    [LoggerMessage(EventId = 3800, Level = LogLevel.Information, Message = "Oturum acildi: hesap {AccountId}, oturum {SessionId}.")]
    private static partial void LogSignedIn(ILogger logger, long accountId, Guid sessionId);

    [LoggerMessage(EventId = 3801, Level = LogLevel.Warning, Message = "Giris kilitli e-postayla denendi (SYG-KMLK-033).")]
    private static partial void LogLocked(ILogger logger);

    [LoggerMessage(EventId = 3802, Level = LogLevel.Warning, Message = "Hatali giris siniri asildi; e-posta kilitlendi (hesap {AccountId}).")]
    private static partial void LogLockedNow(ILogger logger, long? accountId);

    [LoggerMessage(EventId = 3803, Level = LogLevel.Information, Message = "Pasif hesapla giris denendi: hesap {AccountId}.")]
    private static partial void LogDisabled(ILogger logger, long accountId);

    [LoggerMessage(EventId = 3804, Level = LogLevel.Warning, Message = "GUVENLIK: kullanilmis yenileme jetonu tekrar sunuldu; hesap {AccountId} icin tum oturumlar kapatildi (SYG-KMLK-040).")]
    private static partial void LogTokenReuse(ILogger logger, long accountId);

    [LoggerMessage(EventId = 3805, Level = LogLevel.Information, Message = "Parola oturum icinde degistirildi: hesap {AccountId}; diger oturumlar kapatildi (SYG-KMLK-048).")]
    private static partial void LogPasswordChanged(ILogger logger, long accountId);
}
