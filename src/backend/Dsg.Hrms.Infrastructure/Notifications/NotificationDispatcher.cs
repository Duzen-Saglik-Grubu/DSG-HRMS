using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Notifications;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Notifications;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dsg.Hrms.Infrastructure.Notifications;

/// <summary>
/// Kuyruktaki iletileri gonderir ve her gonderimi kaydeder (ADR-0012 §5, §6; SYG-KMLK-079).
/// </summary>
/// <remarks>
/// <para>
/// <b>Yeniden deneme</b> yalnizca gecici hatalarda yapilir (ag, zaman asimi, 5xx, NetGSM
/// <c>80</c>), en fazla iki kez: 2 ve 10 saniye sonra. Toplam sure SYG-KMLK-079'daki
/// 60 saniyenin altinda kalir. Kalici ve yapilandirma hatalari yeniden denenmez.
/// </para>
/// <para>
/// Ileti govdesi HICBIR gunluge yazilmaz (SYG-KMLK-026); alici maskelidir.
/// </para>
/// </remarks>
public sealed partial class NotificationDispatcher : BackgroundService
{
    /// <summary>Varsayilan yeniden deneme beklemeleri.</summary>
    public static readonly IReadOnlyList<TimeSpan> DefaultRetryDelays = [TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10)];

    private readonly InMemoryNotificationDispatch _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly NotificationOptions _options;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<NotificationDispatcher> _logger;

    /// <summary>Yeni ornek olusturur.</summary>
    public NotificationDispatcher(
        InMemoryNotificationDispatch queue,
        IServiceScopeFactory scopeFactory,
        NotificationOptions options,
        IDateTimeProvider clock,
        ILogger<NotificationDispatcher> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _options = options;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>Bildirim istisnasi nedeniyle gonderilmeyen iletinin sonuc kodu.</summary>
    public const string ExemptResultCode = "Exempt";

    /// <summary>Yeniden deneme beklemeleri. Testlerde kisaltilir.</summary>
    public IReadOnlyList<TimeSpan> RetryDelays { get; init; } = DefaultRetryDelays;

    /// <summary>Tek bir iletiyi gonderir ve sonucu kaydeder.</summary>
    public async Task ProcessAsync(OutboundMessage message, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        await using var scope = _scopeFactory.CreateAsyncScope();

        if (!_options.Permits(message.Recipient))
        {
            LogSuppressed(_logger, message.Channel, message.Purpose, message.MaskedRecipient, _options.DeliveryMode);
            await RecordAsync(scope, message, DeliveryStatus.Suppressed, _options.DeliveryMode.ToString(), null, 0).ConfigureAwait(false);
            return;
        }

        if (await IsExemptAsync(scope.ServiceProvider, message, cancellationToken).ConfigureAwait(false))
        {
            LogExempt(_logger, message.Channel, message.Purpose, message.PersonId);
            await RecordAsync(scope, message, DeliveryStatus.Suppressed, ExemptResultCode, null, 0).ConfigureAwait(false);
            return;
        }

        var attempts = 0;
        SendResult result;
        while (true)
        {
            attempts++;
            result = await SendAsync(scope.ServiceProvider, message, cancellationToken).ConfigureAwait(false);

            if (result.Outcome != SendOutcome.TransientFailure || attempts > RetryDelays.Count)
            {
                break;
            }

            LogRetrying(_logger, message.Channel, message.MaskedRecipient, result.ResultCode, attempts);
            await Task.Delay(RetryDelays[attempts - 1], cancellationToken).ConfigureAwait(false);
        }

        switch (result.Outcome)
        {
            case SendOutcome.Sent:
                LogSent(_logger, message.Channel, message.Purpose, message.MaskedRecipient, result.ExternalId, attempts);
                break;
            case SendOutcome.ConfigurationError:
                // ADR-0012 §5: kimlik bilgisi veya gonderici adi hatasi; kimse kod alamaz.
                LogMisconfigured(_logger, message.Channel, result.ResultCode);
                break;
            default:
                LogFailed(_logger, message.Channel, message.Purpose, message.MaskedRecipient, result.ResultCode, attempts);
                break;
        }

        var status = result.Outcome == SendOutcome.Sent ? DeliveryStatus.Sent : DeliveryStatus.Failed;
        await RecordAsync(scope, message, status, result.ResultCode, result.ExternalId, attempts).ConfigureAwait(false);
    }

    /// <summary>
    /// Kisi bu kanalda bildirim istisnasi kapsaminda mi (KR-056, SYG-KMLK-062).
    /// </summary>
    /// <remarks>
    /// Islemsel iletiler (kod, sifirlama, davet) PRM-BLD-03 acikken istisnaya HIC sorulmaz:
    /// istisnadaki kisi kod alamasaydi sisteme giremezdi. Istisna kaynagi kayitli degilse (Y1
    /// Bildirim Merkezi gelmeden once) istisna yoktur.
    /// </remarks>
    private static async Task<bool> IsExemptAsync(IServiceProvider services, OutboundMessage message, CancellationToken cancellationToken)
    {
        var exemptions = services.GetService<INotificationExemptions>();
        if (exemptions is null)
        {
            return false;
        }

        if (message.Purpose.IsTransactional())
        {
            var parameters = services.GetRequiredService<ISystemParameters>();
            if (await parameters.GetBooleanAsync(ParameterCatalog.TransactionalMessagesBypassExemption, cancellationToken).ConfigureAwait(false))
            {
                return false;
            }
        }

        return await exemptions.IsExemptAsync(message.PersonId, message.Channel, message.QueuedAt, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var message in _queue.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    await ProcessAsync(message, stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
#pragma warning disable CA1031 // Tek bir iletinin hatasi dagiticiyi DURDURMAMALIDIR.
                catch (Exception ex)
#pragma warning restore CA1031
                {
                    LogUnexpected(_logger, ex, message.Channel);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Uygulama kapaniyor; kuyrukta kalan iletiler kaybolur (bkz. InMemoryNotificationDispatch).
        }
    }

    private static async Task<SendResult> SendAsync(IServiceProvider services, OutboundMessage message, CancellationToken cancellationToken) =>
        message.Channel == NotificationChannel.Email
            ? await services.GetRequiredService<IEmailSender>()
                .SendAsync(message.Recipient, message.Subject ?? string.Empty, message.MessageBody, cancellationToken).ConfigureAwait(false)
            : await services.GetRequiredService<ISmsSender>()
                .SendAsync(message.Recipient, message.MessageBody, Guid.CreateVersion7(), cancellationToken).ConfigureAwait(false);

    private async Task RecordAsync(
        AsyncServiceScope scope,
        OutboundMessage message,
        DeliveryStatus status,
        string? resultCode,
        string? externalId,
        int attempts)
    {
        try
        {
            var context = scope.ServiceProvider.GetRequiredService<HrmsDbContext>();
            context.Add(NotificationDelivery.Record(
                message.Channel, message.Purpose, message.PersonId, message.MaskedRecipient,
                status, resultCode, externalId, attempts, message.QueuedAt, _clock.UtcNow));

            // Kayit, uygulama kapanirken de yazilmaya calisilir: gonderilmis bir iletinin
            // kaydi kaybolmamali.
            await context.SaveChangesAsync(CancellationToken.None).ConfigureAwait(false);
        }
#pragma warning disable CA1031 // Kayit hatasi gonderimi geri almaz; gunluge yazilir.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogRecordFailed(_logger, ex, message.Channel, status);
        }
    }

    [LoggerMessage(EventId = 3600, Level = LogLevel.Information,
        Message = "Ileti gonderildi: {Channel} {Purpose} -> {Recipient}, dis kimlik {ExternalId}, deneme {Attempts}.")]
    private static partial void LogSent(ILogger logger, NotificationChannel channel, NotificationPurpose purpose, string recipient, string? externalId, int attempts);

    [LoggerMessage(EventId = 3601, Level = LogLevel.Information,
        Message = "Ileti gonderilmedi (kip {Mode}): {Channel} {Purpose} -> {Recipient}.")]
    private static partial void LogSuppressed(ILogger logger, NotificationChannel channel, NotificationPurpose purpose, string recipient, DeliveryMode mode);

    [LoggerMessage(EventId = 3602, Level = LogLevel.Warning,
        Message = "Ileti gonderimi gecici hata verdi ({ResultCode}); yeniden denenecek: {Channel} -> {Recipient}, deneme {Attempts}.")]
    private static partial void LogRetrying(ILogger logger, NotificationChannel channel, string recipient, string? resultCode, int attempts);

    [LoggerMessage(EventId = 3603, Level = LogLevel.Error,
        Message = "Ileti gonderilemedi ({ResultCode}): {Channel} {Purpose} -> {Recipient}, deneme {Attempts}.")]
    private static partial void LogFailed(ILogger logger, NotificationChannel channel, NotificationPurpose purpose, string recipient, string? resultCode, int attempts);

    [LoggerMessage(EventId = 3604, Level = LogLevel.Critical,
        Message = "{Channel} gonderim yapilandirmasi hatali ({ResultCode}); hicbir ileti gonderilemiyor. Parametreleri denetleyin (PRM-ENT-01…06).")]
    private static partial void LogMisconfigured(ILogger logger, NotificationChannel channel, string? resultCode);

    [LoggerMessage(EventId = 3605, Level = LogLevel.Error,
        Message = "Gonderim kaydi yazilamadi: {Channel}, {Status}.")]
    private static partial void LogRecordFailed(ILogger logger, Exception exception, NotificationChannel channel, DeliveryStatus status);

    [LoggerMessage(EventId = 3606, Level = LogLevel.Error,
        Message = "Ileti islenirken beklenmeyen hata: {Channel}.")]
    private static partial void LogUnexpected(ILogger logger, Exception exception, NotificationChannel channel);

    [LoggerMessage(EventId = 3607, Level = LogLevel.Information, Message = "Ileti gonderilmedi (bildirim istisnasi): {Channel} {Purpose}, kisi {PersonId} (KR-056).")]
    private static partial void LogExempt(ILogger logger, NotificationChannel channel, NotificationPurpose purpose, long? personId);
}
