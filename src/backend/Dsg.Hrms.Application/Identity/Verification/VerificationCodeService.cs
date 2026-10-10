using System.Globalization;
using System.Security.Cryptography;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Notifications;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace Dsg.Hrms.Application.Identity.Verification;

/// <summary>
/// Dogrulama kodu uretir, gonderir ve dogrular (SYG-KMLK-019, 022…027, 059).
/// </summary>
/// <remarks>
/// Uyelik, parola sifirlama ve iki adimli dogrulama akislari bu servisi kullanir; kodun
/// hangi hedefe gidecegine (SYG-KMLK-017) akis karar verir. Servis hedefi ne gunluge
/// ne yanita yazar (SYG-KMLK-018).
/// </remarks>
public sealed partial class VerificationCodeService
{
    /// <summary>Kod gonderim hiz sinirinin penceresi (<c>PRM-KML-17</c>: "kisi basina / 15 dakika").</summary>
    public static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(15);

    private readonly IVerificationCodeStore _store;
    private readonly IVerificationCodeHasher _hasher;
    private readonly INotificationDispatch _queue;
    private readonly ISystemParameters _parameters;
    private readonly IDateTimeProvider _clock;
    private readonly ISecurityEventLog _events;
    private readonly ILogger<VerificationCodeService> _logger;

    /// <summary>Yeni ornek olusturur.</summary>
    public VerificationCodeService(
        IVerificationCodeStore store,
        IVerificationCodeHasher hasher,
        INotificationDispatch queue,
        ISystemParameters parameters,
        IDateTimeProvider clock,
        ISecurityEventLog events,
        ILogger<VerificationCodeService> logger)
    {
        _store = store;
        _hasher = hasher;
        _queue = queue;
        _parameters = parameters;
        _clock = clock;
        _events = events;
        _logger = logger;
    }

    /// <summary>
    /// Yeni kod uretir ve gonderim kuyruguna alir. Kisinin ayni amactaki acik kodlari
    /// gecersiz kilinir (SYG-KMLK-019: kanal degisimi ve tekrar gonderim).
    /// </summary>
    /// <param name="request">Istek. <see cref="VerificationRequest.Recipient"/> akis tarafindan secilir.</param>
    /// <param name="cancellationToken">Iptal.</param>
    public async Task<IssueResult> IssueAsync(VerificationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!_hasher.IsConfigured)
        {
            throw new InvalidOperationException("Dogrulama kodu ozet anahtari (Identity:CodeHashKey) tanimli degil.");
        }

        var now = _clock.UtcNow;

        // Hiz siniri kanal degisimi ve tekrar gonderim dahil TUM gonderimleri sayar
        // (SYG-KMLK-019, 028): siniri asmanin yolu kanal degistirmek olmamali.
        var limit = await _parameters.GetIntegerAsync(ParameterCatalog.CodeSendLimit, cancellationToken).ConfigureAwait(false);
        var recent = await _store.CountIssuedSinceAsync(request.PersonId, now - RateLimitWindow, cancellationToken).ConfigureAwait(false);
        if (recent >= limit)
        {
            LogRateLimited(_logger, request.PersonId, request.Purpose, limit);
            return IssueResult.RateLimited;
        }

        var length = await _parameters.GetIntegerAsync(ParameterCatalog.VerificationCodeLength, cancellationToken).ConfigureAwait(false);
        var lifetime = await _parameters.GetIntegerAsync(ParameterCatalog.VerificationCodeLifetimeMinutes, cancellationToken).ConfigureAwait(false);
        var maxAttempts = await _parameters.GetIntegerAsync(ParameterCatalog.MaxVerificationAttempts, cancellationToken).ConfigureAwait(false);

        foreach (var open in await _store.GetOpenAsync(request.PersonId, request.Purpose, cancellationToken).ConfigureAwait(false))
        {
            open.Invalidate();
        }

        var code = Generate(length);
        var entity = VerificationCode.Issue(request.PersonId, request.Purpose, request.Channel, now.AddMinutes(lifetime), maxAttempts);
        entity.SetHash(_hasher.Hash(entity.PublicId, code));
        _store.Add(entity);
        _events.Record(SecurityEventType.VerificationCodeSent, null, request.PersonId, $"{PurposeCode(request.Purpose)}:{ChannelCode(request.Channel)}");
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var message = request.Channel == VerificationChannel.Email
            ? new OutboundMessage(NotificationChannel.Email, ToNotificationPurpose(request.Purpose), request.PersonId, request.Recipient,
                VerificationMessages.EmailSubject(request.Purpose), VerificationMessages.EmailBody(code, request.Purpose, lifetime), now)
            : new OutboundMessage(NotificationChannel.Sms, ToNotificationPurpose(request.Purpose), request.PersonId, request.Recipient,
                null, VerificationMessages.Sms(code, request.Purpose, lifetime), now);

        if (!_queue.TryEnqueue(message))
        {
            // Kod kaydedildi ama iletilemeyecek: kullanici "tekrar gonder" ile yeni kod ister.
            LogQueueFull(_logger, request.PersonId);
        }

        LogIssued(_logger, entity.PublicId, request.PersonId, request.Purpose, request.Channel);
        return IssueResult.Issued(entity.PublicId, entity.ExpiresAt);
    }

    /// <summary>
    /// Girilen kodu dogrular. Dogrulanan kod ayni islemde gecersizlesir; eszamanli iki
    /// istekten yalnizca biri <see cref="VerificationResult.Verified"/> alir (SYG-KMLK-027).
    /// </summary>
    public Task<VerificationResult> VerifyAsync(Guid codeId, string? code, CancellationToken cancellationToken) =>
        VerifyCoreAsync(codeId, code, expected: null, cancellationToken);

    /// <summary>
    /// Girilen kodu, kodun verilen kisiye ve amaca ait oldugunu da denetleyerek dogrular
    /// (SYG-KMLK-080). Baska bir kisinin veya baska bir akisin kodu
    /// <see cref="VerificationResult.NotUsable"/> doner; o koda deneme sayilmaz ve kod
    /// degismez.
    /// </summary>
    /// <param name="codeId">Kodun dis kimligi.</param>
    /// <param name="code">Girilen kod.</param>
    /// <param name="personId">Kodun ait olmasi gereken kisi.</param>
    /// <param name="purpose">Kodun beklenen amaci.</param>
    /// <param name="cancellationToken">Iptal.</param>
    public Task<VerificationResult> VerifyAsync(
        Guid codeId,
        string? code,
        long personId,
        VerificationPurpose purpose,
        CancellationToken cancellationToken) =>
        VerifyCoreAsync(codeId, code, (personId, purpose), cancellationToken);

    private async Task<VerificationResult> VerifyCoreAsync(
        Guid codeId,
        string? code,
        (long PersonId, VerificationPurpose Purpose)? expected,
        CancellationToken cancellationToken)
    {
        var entity = await _store.FindAsync(codeId, cancellationToken).ConfigureAwait(false);
        if (entity is null)
        {
            return VerificationResult.NotUsable;
        }

        if (expected is { } owner && (entity.PersonId != owner.PersonId || entity.Purpose != owner.Purpose))
        {
            LogForeignCode(_logger, codeId);
            return VerificationResult.NotUsable;
        }

        // Bicimsiz girdi de yanlis deneme sayilir; ayri bir yanit kod uzunlugu hakkinda
        // bilgi vermez ve deneme hakkini harcamadan tahmin yapilmasina izin vermez.
        var input = code?.Trim() ?? string.Empty;
        var matches = input.Length > 0 && input.All(char.IsAsciiDigit) && _hasher.Matches(entity.PublicId, input, entity.CodeHash);

        var result = entity.Verify(matches, _clock.UtcNow);
        _events.Record(
            result == VerificationResult.Verified ? SecurityEventType.VerificationSucceeded : SecurityEventType.VerificationFailed,
            null,
            entity.PersonId,
            $"{PurposeCode(entity.Purpose)}:{ResultCode(result)}");

        try
        {
            await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (VerificationConflictException)
        {
            // Ayni kod icin eszamanli baska bir istek once kaydetti. Bu istegin sonucu
            // gecersizdir; aksi halde tek kullanimlik kod iki kez kullanilabilirdi.
            LogConflict(_logger, codeId);
            return VerificationResult.NotUsable;
        }

        LogVerified(_logger, codeId, result);
        return result;
    }

    /// <summary>Kriptografik olarak guvenli uretecle, basindaki sifirlar korunarak kod uretir (SYG-KMLK-022).</summary>
    public static string Generate(int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(length, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(length, 9);

        var upper = (int)Math.Pow(10, length);
        return RandomNumberGenerator.GetInt32(upper).ToString(new string('0', length), CultureInfo.InvariantCulture);
    }

    /// <summary>Kimlik olayinda amacin kodu (SYG-KMLK-060).</summary>
    public static string PurposeCode(VerificationPurpose purpose) => purpose switch
    {
        VerificationPurpose.Registration => "registration",
        VerificationPurpose.PasswordReset => "password-reset",
        VerificationPurpose.TwoFactorSetup => "two-factor-setup",
        _ => "two-factor",
    };

    /// <summary>Kimlik olayinda dogrulama sonucunun kodu (SYG-KMLK-060).</summary>
    public static string ResultCode(VerificationResult result) => result switch
    {
        VerificationResult.Verified => "verified",
        VerificationResult.Mismatch => "mismatch",
        VerificationResult.Expired => "expired",
        VerificationResult.AttemptsExceeded => "attempts-exceeded",
        _ => "not-usable",
    };

    private static string ChannelCode(VerificationChannel channel) => channel == VerificationChannel.Email ? "email" : "sms";

    private static NotificationPurpose ToNotificationPurpose(VerificationPurpose purpose) => purpose switch
    {
        VerificationPurpose.Registration => NotificationPurpose.RegistrationCode,
        VerificationPurpose.PasswordReset => NotificationPurpose.PasswordResetCode,
        VerificationPurpose.TwoFactorSetup => NotificationPurpose.TwoFactorSetupCode,
        _ => NotificationPurpose.TwoFactorCode,
    };

    [LoggerMessage(EventId = 3500, Level = LogLevel.Information,
        Message = "Dogrulama kodu uretildi {CodeId}: kisi {PersonId}, {Purpose}, {Channel}.")]
    private static partial void LogIssued(ILogger logger, Guid codeId, long personId, VerificationPurpose purpose, VerificationChannel channel);

    [LoggerMessage(EventId = 3501, Level = LogLevel.Warning,
        Message = "Kod gonderim siniri asildi: kisi {PersonId}, {Purpose}, sinir {Limit} / 15 dk (SYG-KMLK-059).")]
    private static partial void LogRateLimited(ILogger logger, long personId, VerificationPurpose purpose, int limit);

    [LoggerMessage(EventId = 3502, Level = LogLevel.Error,
        Message = "Bildirim kuyrugu dolu; kisi {PersonId} icin kod iletilemeyecek.")]
    private static partial void LogQueueFull(ILogger logger, long personId);

    [LoggerMessage(EventId = 3503, Level = LogLevel.Information,
        Message = "Dogrulama denemesi {CodeId}: {Result}.")]
    private static partial void LogVerified(ILogger logger, Guid codeId, VerificationResult result);

    [LoggerMessage(EventId = 3504, Level = LogLevel.Warning,
        Message = "Dogrulama kodu {CodeId} icin eszamanli istek; bu istek reddedildi (SYG-KMLK-027).")]
    private static partial void LogConflict(ILogger logger, Guid codeId);

    [LoggerMessage(EventId = 3505, Level = LogLevel.Warning,
        Message = "Dogrulama kodu {CodeId} baska bir kisiye veya akisa ait; kullanilmadi (SYG-KMLK-080).")]
    private static partial void LogForeignCode(ILogger logger, Guid codeId);
}

/// <summary>Kod uretim istegi.</summary>
/// <param name="PersonId">Kisi.</param>
/// <param name="Purpose">Amac.</param>
/// <param name="Channel">Kanal.</param>
/// <param name="Recipient">Kanalin hedefi (kisinin LOGO'dan gelen e-postasi veya cep telefonu).</param>
public sealed record VerificationRequest(
    long PersonId,
    VerificationPurpose Purpose,
    VerificationChannel Channel,
    [property: Common.Security.PersonalData(Common.Security.PersonalDataKind.Unspecified)] string Recipient)
{
    /// <inheritdoc />
    public override string ToString() => $"{PersonId} {Purpose} {Channel}";
}

/// <summary>Kod uretim sonucu.</summary>
/// <param name="IsRateLimited">Hiz siniri asildi mi (<c>429</c>).</param>
/// <param name="CodeId">Uretilen kodun dis kimligi.</param>
/// <param name="ExpiresAt">Gecerliligin bittigi an; ekrandaki geri sayim icin (SYG-KMLK-028).</param>
public sealed record IssueResult(bool IsRateLimited, Guid? CodeId, DateTimeOffset? ExpiresAt)
{
    /// <summary>Hiz siniri asildi; kod uretilmedi.</summary>
    public static IssueResult RateLimited { get; } = new(true, null, null);

    /// <summary>Kod uretildi.</summary>
    public static IssueResult Issued(Guid codeId, DateTimeOffset expiresAt) => new(false, codeId, expiresAt);
}
