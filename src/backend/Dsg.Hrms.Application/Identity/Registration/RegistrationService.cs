using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Application.Identity.Sessions;
using Dsg.Hrms.Application.Identity.Verification;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace Dsg.Hrms.Application.Identity.Registration;

/// <summary>
/// Uyelik ve parola sifirlama akisi (ADR-0006 §1–4, §6; SYG-KMLK-013…021, 047).
/// </summary>
/// <remarks>
/// <para>
/// <b>Tek akis (SYG-KMLK-047):</b> Parola sifirlama uyelikle ayni eslestirme, kanal, kod, hiz
/// siniri ve eslesme gizliligi kurallarini kullanir. Denemenin amaci yalnizca kod iletisinin
/// metnini belirler. Dogrulanmis kimlik, hesap yoksa hesap olusturmaya
/// (<see cref="CompleteAsync"/>), varsa parola sifirlamaya (<see cref="ResetPasswordAsync"/>)
/// gider.
/// </para>
/// <para>
/// <b>Eslesme gizliligi (<c>KR-016</c>):</b> Eslesme olsa da olmasa da her adimin yaniti
/// ayni bicimdedir. Eslesme yoksa kod gonderilmez, ama deneme sahte bir kod varmis gibi
/// ayni sure ve deneme sinirlariyla isler. Hiz siniri TCKN ozetine ve IP'ye gore
/// sayilir; kisiye gore sayilsaydi "429" yalnizca gercek personelde gorulurdu.
/// </para>
/// <para>
/// <b>Bilinen ve kabul edilmis fark (<c>KR-078</c>):</b> Eslesmede cep telefonu olmayan
/// kisiye yalnizca e-posta kanali sunulur (REQ-KMLK-010); eslesme yoksa iki kanal
/// sunulur. Bu fark uc bilginin de dogru bilinmesini gerektirir.
/// </para>
/// </remarks>
public sealed partial class RegistrationService
{
    /// <summary>TCKN ve IP basina deneme siniri penceresi ("saatte").</summary>
    public static readonly TimeSpan AttemptWindow = TimeSpan.FromHours(1);

    private readonly IRegistrationStore _store;
    private readonly ISessionStore _sessions;
    private readonly IIdentifierHasher _identifierHasher;
    private readonly VerificationCodeService _codes;
    private readonly PasswordPolicy _passwordPolicy;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ISystemParameters _parameters;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<RegistrationService> _logger;

    /// <summary>Yeni ornek olusturur.</summary>
    /// <remarks>
    /// Iki depo ayni veritabani baglamini (is birimini) paylasir: parola sifirlamada hesap,
    /// oturumlar ve giris sayaci TEK kayitla birlikte yazilir.
    /// </remarks>
    public RegistrationService(
        IRegistrationStore store,
        ISessionStore sessions,
        IIdentifierHasher identifierHasher,
        VerificationCodeService codes,
        PasswordPolicy passwordPolicy,
        IPasswordHasher passwordHasher,
        ISystemParameters parameters,
        IDateTimeProvider clock,
        ILogger<RegistrationService> logger)
    {
        _store = store;
        _sessions = sessions;
        _identifierHasher = identifierHasher;
        _codes = codes;
        _passwordPolicy = passwordPolicy;
        _passwordHasher = passwordHasher;
        _parameters = parameters;
        _clock = clock;
        _logger = logger;
    }

    // ------------------------------------------------------------------ 1. baslat

    /// <summary>
    /// Uyeligi baslatir. Eslesme olsa da olmasa da deneme olusturulur ve ayni bicimde yanit
    /// doner (SYG-KMLK-015, 016).
    /// </summary>
    /// <exception cref="TooManyRequestsException">TCKN veya IP basina saatlik sinir asildiysa.</exception>
    public async Task<RegistrationStarted> StartAsync(StartRegistration request, string? ipAddress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = _clock.UtcNow;
        var nationalIdHash = _identifierHasher.HashIdentifier(request.NationalId);

        await EnforceAttemptLimitsAsync(nationalIdHash, ipAddress, now, cancellationToken).ConfigureAwait(false);

        var allowed = await AllowedChannelsAsync(cancellationToken).ConfigureAwait(false);
        var candidate = await _store.FindCandidateAsync(request.NationalId, cancellationToken).ConfigureAwait(false);
        var matched = await MatchAsync(candidate, request, allowed, cancellationToken).ConfigureAwait(false);

        // Eslesme yoksa izin verilen tum kanallar sunulur (SYG-KMLK-016).
        var attempt = RegistrationAttempt.Start(nationalIdHash, matched?.PersonId, ipAddress, matched?.Channels ?? allowed, now, request.Purpose);
        _store.Add(attempt);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        LogStarted(_logger, attempt.PublicId, attempt.IsMatch);
        return new RegistrationStarted(attempt.PublicId, attempt.OfferedChannels, attempt.ExpiresAt);
    }

    // ------------------------------------------------------------------ 2. kod iste

    /// <summary>
    /// Secilen kanala kod gonderir; eslesme yoksa gonderilmis gibi davranir. Yeni kod
    /// oncekini gecersiz kilar (SYG-KMLK-019).
    /// </summary>
    /// <exception cref="NotFoundException">Deneme yoksa, suresi dolduysa veya dogrulanmissa.</exception>
    /// <exception cref="BusinessRuleException">Kanal bu denemede sunulmadiysa.</exception>
    /// <exception cref="TooManyRequestsException">15 dakikalik kod siniri asildiysa.</exception>
    public async Task<CodeRequested> RequestCodeAsync(Guid registrationId, RegistrationChannels channel, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var attempt = await FindActiveAsync(registrationId, now, cancellationToken).ConfigureAwait(false);

        if (!attempt.CanRequestCode(now))
        {
            throw new NotFoundException(ExpiredMessageFor(attempt));
        }

        if (channel is not (RegistrationChannels.Email or RegistrationChannels.Sms) || !attempt.OfferedChannels.HasFlag(channel))
        {
            throw new BusinessRuleException("Seçilen doğrulama yöntemi kullanılamıyor.");
        }

        var limit = await _parameters.GetIntegerAsync(ParameterCatalog.CodeSendLimit, cancellationToken).ConfigureAwait(false);
        var recent = await _store.CountCodeRequestsSinceAsync(attempt.NationalIdHash, now - VerificationCodeService.RateLimitWindow, cancellationToken).ConfigureAwait(false);
        if (recent >= limit)
        {
            LogRateLimited(_logger, "kod", registrationId);
            throw new TooManyRequestsException();
        }

        _store.Add(RegistrationCodeRequest.Record(attempt.NationalIdHash, now));

        var lifetime = await _parameters.GetIntegerAsync(ParameterCatalog.VerificationCodeLifetimeMinutes, cancellationToken).ConfigureAwait(false);
        DateTimeOffset expiresAt;

        if (attempt.IsMatch)
        {
            var candidate = await _store.FindCandidateAsync(attempt.PersonId!.Value, cancellationToken).ConfigureAwait(false)
                ?? throw new NotFoundException(ExpiredMessageFor(attempt));

            var recipient = channel == RegistrationChannels.Email ? candidate.Person.Email : candidate.Person.MobilePhone;
            if (string.IsNullOrEmpty(recipient))
            {
                // Deneme basladiktan sonra senkronizasyon iletisim bilgisini silmis olabilir.
                throw new BusinessRuleException("Seçilen doğrulama yöntemi kullanılamıyor.");
            }

            var issued = await _codes.IssueAsync(
                new VerificationRequest(
                    candidate.Person.Id,
                    attempt.Purpose,
                    channel == RegistrationChannels.Email ? VerificationChannel.Email : VerificationChannel.Sms,
                    recipient),
                cancellationToken).ConfigureAwait(false);

            if (issued.IsRateLimited)
            {
                throw new TooManyRequestsException();
            }

            attempt.CodeSent(issued.CodeId!.Value);
            expiresAt = issued.ExpiresAt!.Value;
        }
        else
        {
            var maxAttempts = await _parameters.GetIntegerAsync(ParameterCatalog.MaxVerificationAttempts, cancellationToken).ConfigureAwait(false);
            expiresAt = now.AddMinutes(lifetime);
            attempt.DecoyCodeSent(expiresAt, maxAttempts);
        }

        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return new CodeRequested(expiresAt);
    }

    // ------------------------------------------------------------------ 3. dogrula

    /// <summary>
    /// Kodu dogrular. Kisinin zaten hesabi varsa bu bilgi YALNIZCA dogrulamadan sonra
    /// verilir (SYG-KMLK-020).
    /// </summary>
    /// <exception cref="NotFoundException">Deneme yoksa veya suresi dolduysa.</exception>
    public async Task<RegistrationVerification> VerifyAsync(Guid registrationId, string? code, CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var attempt = await FindActiveAsync(registrationId, now, cancellationToken).ConfigureAwait(false);

        if (!attempt.IsMatch)
        {
            var decoy = attempt.VerifyDecoy(now);
            await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return new RegistrationVerification(decoy, AccountExists: false);
        }

        if (attempt.Status != RegistrationStatus.CodeSent || attempt.VerificationCodeId is null)
        {
            return new RegistrationVerification(VerificationResult.NotUsable, AccountExists: false);
        }

        var result = await _codes.VerifyAsync(attempt.VerificationCodeId.Value, code, cancellationToken).ConfigureAwait(false);
        if (result != VerificationResult.Verified)
        {
            return new RegistrationVerification(result, AccountExists: false);
        }

        attempt.MarkVerified(now);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var candidate = await _store.FindCandidateAsync(attempt.PersonId!.Value, cancellationToken).ConfigureAwait(false);
        var accountExists = candidate?.Account is not null;

        LogVerified(_logger, registrationId, accountExists);
        return new RegistrationVerification(VerificationResult.Verified, accountExists);
    }

    // ------------------------------------------------------------------ 4. hesap olustur

    /// <summary>
    /// Parolayi denetler ve hesabi olusturur. Ihlal varsa hicbir sey kaydedilmez ve
    /// ihlaller doner.
    /// </summary>
    /// <exception cref="NotFoundException">Deneme dogrulanmamissa veya suresi dolduysa.</exception>
    /// <exception cref="ConflictException">Kisinin zaten hesabi varsa (SYG-KMLK-020, 021).</exception>
    /// <exception cref="BusinessRuleException">Kisinin artik aktif istihdami yoksa.</exception>
    public async Task<IReadOnlyList<PasswordViolation>> CompleteAsync(Guid registrationId, string password, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(password);

        var now = _clock.UtcNow;
        var attempt = await _store.FindAttemptAsync(registrationId, cancellationToken).ConfigureAwait(false);

        if (attempt is null || !attempt.IsMatch || !attempt.CanComplete(now))
        {
            throw new NotFoundException(ExpiredMessageFor(attempt));
        }

        var candidate = await _store.FindCandidateAsync(attempt.PersonId!.Value, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(ExpiredMessageFor(attempt));

        if (candidate.Account is not null)
        {
            throw new ConflictException(AccountExistsMessage);
        }

        if (!candidate.HasActiveEmployment)
        {
            throw new BusinessRuleException("Aktif çalışma kaydınız bulunmadığı için hesap oluşturulamıyor.");
        }

        var person = candidate.Person;
        var violations = await _passwordPolicy.ValidateAsync(password, PersonalWords(person.FirstName, person.LastName, person.Email), cancellationToken)
            .ConfigureAwait(false);
        if (violations.Count > 0)
        {
            return violations;
        }

        var hash = _passwordHasher.Hash(PasswordPolicy.Normalize(password));
        _store.Add(UserAccount.Register(person.Id, hash, now));
        attempt.Complete();

        // Es zamanli iki istekten ikincisi hesabin tekillik kisitina takilir (SYG-KMLK-021).
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        LogCompleted(_logger, registrationId, person.Id);
        return [];
    }

    // ------------------------------------------------------------------ 4. parola sifirla

    /// <summary>
    /// Dogrulanmis kimlikle parolayi sifirlar (SYG-KMLK-047). Hesabin TUM oturumlari kapanir ve
    /// giris kilidi kalkar: kimligini kanitlayan kisi, kilit suresini beklemeden yeni parolasiyla
    /// giris yapabilir. Ihlal varsa hicbir sey kaydedilmez ve ihlaller doner.
    /// </summary>
    /// <exception cref="NotFoundException">Deneme dogrulanmamissa veya suresi dolduysa.</exception>
    /// <exception cref="BusinessRuleException">Kisinin hesabi yoksa.</exception>
    /// <exception cref="AccountDisabledException">Hesap kullanima kapaliysa.</exception>
    public async Task<IReadOnlyList<PasswordViolation>> ResetPasswordAsync(Guid registrationId, string password, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(password);

        var now = _clock.UtcNow;
        var attempt = await _store.FindAttemptAsync(registrationId, cancellationToken).ConfigureAwait(false);

        if (attempt is null || !attempt.IsMatch || !attempt.CanComplete(now))
        {
            throw new NotFoundException(ExpiredMessageFor(attempt));
        }

        var registration = await _store.FindCandidateAsync(attempt.PersonId!.Value, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(ExpiredMessageFor(attempt));

        if (registration.Account is null)
        {
            throw new BusinessRuleException(NoAccountMessage);
        }

        var candidate = await _sessions.FindByAccountIdAsync(registration.Account.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(ExpiredMessageFor(attempt));
        var account = candidate.Account;

        // Elle pasife alinan hesap parola sifirlamayla acilamaz (SYG-KMLK-057).
        if (account.Status != AccountStatus.Active || !candidate.HasActiveEmployment)
        {
            throw new AccountDisabledException();
        }

        var person = candidate.Person;
        var violations = await _passwordPolicy.ValidateAsync(password, PersonalWords(person.FirstName, person.LastName, person.Email), cancellationToken)
            .ConfigureAwait(false);
        if (violations.Count > 0)
        {
            return violations;
        }

        account.SetPassword(_passwordHasher.Hash(PasswordPolicy.Normalize(password)), now);
        account.ClearLockout();

        foreach (var session in await _sessions.GetOpenSessionsAsync(account.Id, cancellationToken).ConfigureAwait(false))
        {
            session.End(SessionEndReason.PasswordChanged, now);
        }

        if (person.Email is not null)
        {
            var throttle = await _sessions.FindThrottleAsync(_identifierHasher.HashIdentifier(SessionService.LoginIdentifier(person.Email)), cancellationToken)
                .ConfigureAwait(false);
            throttle?.Reset(now);
        }

        attempt.Complete();
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        LogPasswordReset(_logger, registrationId, account.Id);
        return [];
    }

    /// <summary>Parola denetiminde kullanilacak kisisel sozcukler (SYG-KMLK-045).</summary>
    public static IEnumerable<string> PersonalWords(string firstName, string lastName, string? email)
    {
        var words = new List<string>();
        foreach (var name in new[] { firstName, lastName })
        {
            words.Add(name);
            words.AddRange(name.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        words.Add($"{firstName}{lastName}");

        if (email is not null)
        {
            var local = email.Split('@')[0];
            words.Add(local);
            words.AddRange(local.Split(['.', '_', '-'], StringSplitOptions.RemoveEmptyEntries));
        }

        return words;
    }

    // ------------------------------------------------------------------ yardimcilar

    private const string ExpiredMessage = "Üyelik işleminin süresi doldu. Lütfen baştan başlayın.";

    private const string ResetExpiredMessage = "Parola sıfırlama işleminin süresi doldu. Lütfen baştan başlayın.";

    private const string NoAccountMessage = "Adınıza açılmış bir hesap bulunmuyor. Üye olarak hesap oluşturabilirsiniz.";

    private static string ExpiredMessageFor(RegistrationAttempt? attempt) =>
        attempt?.Purpose == VerificationPurpose.PasswordReset ? ResetExpiredMessage : ExpiredMessage;

    private const string AccountExistsMessage = "Adınıza açılmış bir hesap zaten var. Parolanızı unuttuysanız parola sıfırlamayı kullanın.";

    private async Task EnforceAttemptLimitsAsync(string nationalIdHash, string? ipAddress, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var since = now - AttemptWindow;

        var perNationalId = await _parameters.GetIntegerAsync(ParameterCatalog.RegistrationLimitPerNationalId, cancellationToken).ConfigureAwait(false);
        if (await _store.CountAttemptsSinceAsync(nationalIdHash, since, cancellationToken).ConfigureAwait(false) >= perNationalId)
        {
            LogRateLimited(_logger, "tckn", null);
            throw new TooManyRequestsException();
        }

        if (ipAddress is not null)
        {
            var perIp = await _parameters.GetIntegerAsync(ParameterCatalog.RegistrationLimitPerIp, cancellationToken).ConfigureAwait(false);
            if (await _store.CountAttemptsFromIpSinceAsync(ipAddress, since, cancellationToken).ConfigureAwait(false) >= perIp)
            {
                LogRateLimited(_logger, "ip", null);
                throw new TooManyRequestsException();
            }
        }
    }

    private async Task<RegistrationChannels> AllowedChannelsAsync(CancellationToken cancellationToken)
    {
        var channels = RegistrationChannels.None;
        foreach (var item in await _parameters.GetListAsync(ParameterCatalog.VerificationChannels, cancellationToken).ConfigureAwait(false))
        {
            channels |= item switch
            {
                "email" => RegistrationChannels.Email,
                "sms" => RegistrationChannels.Sms,
                _ => RegistrationChannels.None,
            };
        }

        return channels == RegistrationChannels.None ? RegistrationChannels.Email : channels;
    }

    /// <summary>
    /// Uc bilginin eslesmesi ve kullanilabilir kanal (SYG-KMLK-013, 017). Kullanilabilir
    /// kanal yoksa eslesme yokmus gibi davranilir.
    /// </summary>
    private async Task<(long PersonId, RegistrationChannels Channels)?> MatchAsync(
        RegistrationCandidate? candidate,
        StartRegistration request,
        RegistrationChannels allowed,
        CancellationToken cancellationToken)
    {
        if (candidate is null || !candidate.HasActiveEmployment)
        {
            return null;
        }

        var person = candidate.Person;
        if (person.BirthDate != request.BirthDate
            || person.Email is null
            || !string.Equals(person.Email, request.Email.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Giris kimligi kurumsal e-postadir (KR-073): adres kabul edilen alan adinda ve
        // kisiye tekil olmali; degilse kisi uye olamaz (ADR-0006 §4).
        var domains = await _parameters.GetListAsync(ParameterCatalog.AcceptedEmailDomains, cancellationToken).ConfigureAwait(false);
        var domain = person.Email[(person.Email.LastIndexOf('@') + 1)..];
        if (person.IsEmailShared || !domains.Contains(domain, StringComparer.OrdinalIgnoreCase))
        {
            return null;
        }

        var channels = RegistrationChannels.None;
        if (allowed.HasFlag(RegistrationChannels.Email))
        {
            channels |= RegistrationChannels.Email;
        }

        if (allowed.HasFlag(RegistrationChannels.Sms) && person.MobilePhone is not null)
        {
            channels |= RegistrationChannels.Sms;
        }

        return channels == RegistrationChannels.None ? null : (person.Id, channels);
    }

    private async Task<RegistrationAttempt> FindActiveAsync(Guid registrationId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var attempt = await _store.FindAttemptAsync(registrationId, cancellationToken).ConfigureAwait(false);
        if (attempt is null || now >= attempt.ExpiresAt || attempt.Status is RegistrationStatus.Verified or RegistrationStatus.Completed)
        {
            throw new NotFoundException(ExpiredMessageFor(attempt));
        }

        return attempt;
    }

    [LoggerMessage(EventId = 3700, Level = LogLevel.Information, Message = "Uyelik denemesi baslatildi {RegistrationId}: eslesme {IsMatch}.")]
    private static partial void LogStarted(ILogger logger, Guid registrationId, bool isMatch);

    [LoggerMessage(EventId = 3701, Level = LogLevel.Warning, Message = "Uyelik hiz siniri asildi ({Limit}); deneme {RegistrationId} (SYG-KMLK-059).")]
    private static partial void LogRateLimited(ILogger logger, string limit, Guid? registrationId);

    [LoggerMessage(EventId = 3702, Level = LogLevel.Information, Message = "Uyelik kodu dogrulandi {RegistrationId}: mevcut hesap {AccountExists}.")]
    private static partial void LogVerified(ILogger logger, Guid registrationId, bool accountExists);

    [LoggerMessage(EventId = 3703, Level = LogLevel.Information, Message = "Uyelik tamamlandi {RegistrationId}: kisi {PersonId} icin hesap olusturuldu.")]
    private static partial void LogCompleted(ILogger logger, Guid registrationId, long personId);

    [LoggerMessage(EventId = 3704, Level = LogLevel.Information, Message = "Parola sifirlandi {RegistrationId}: hesap {AccountId}; acik oturumlar kapatildi.")]
    private static partial void LogPasswordReset(ILogger logger, Guid registrationId, long accountId);
}

/// <summary>Uyelik baslatma istegi.</summary>
/// <param name="NationalId">TCKN (bicimi ve sagla algoritmasi API katmaninda dogrulanmis).</param>
/// <param name="BirthDate">Dogum tarihi.</param>
/// <param name="Email">Kurumsal e-posta.</param>
/// <param name="Purpose">Uyelik veya parola sifirlama (SYG-KMLK-047).</param>
public sealed record StartRegistration(
    string NationalId,
    DateOnly BirthDate,
    string Email,
    VerificationPurpose Purpose = VerificationPurpose.Registration)
{
    /// <inheritdoc />
    public override string ToString() => "StartRegistration";
}

/// <summary>Uyelik baslatildi.</summary>
/// <param name="RegistrationId">Denemenin dis kimligi.</param>
/// <param name="Channels">Sunulan kanallar.</param>
/// <param name="ExpiresAt">Denemenin gecerlilik sonu.</param>
public sealed record RegistrationStarted(Guid RegistrationId, RegistrationChannels Channels, DateTimeOffset ExpiresAt);

/// <summary>Kod istendi.</summary>
/// <param name="CodeExpiresAt">Kodun gecerlilik sonu; ekrandaki geri sayim icin (SYG-KMLK-028).</param>
public sealed record CodeRequested(DateTimeOffset CodeExpiresAt);

/// <summary>Kod dogrulama sonucu.</summary>
/// <param name="Result">Sonuc.</param>
/// <param name="AccountExists">Kisinin zaten hesabi var mi; yalnizca dogrulamadan sonra anlamlidir.</param>
public sealed record RegistrationVerification(VerificationResult Result, bool AccountExists);
