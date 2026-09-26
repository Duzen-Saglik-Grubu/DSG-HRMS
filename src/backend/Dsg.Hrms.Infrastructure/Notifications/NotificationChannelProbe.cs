using System.Collections.Concurrent;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Notifications;
using Dsg.Hrms.Domain.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace Dsg.Hrms.Infrastructure.Notifications;

/// <summary>
/// <see cref="INotificationChannelProbe"/> uygulamasi.
/// </summary>
/// <remarks>
/// Sonuc <see cref="CacheDuration"/> boyunca onbellekte tutulur: saglik ucu sik yoklanir
/// ve her yoklamada SMTP oturumu acmak veya NetGSM'i sorgulamak gereksiz yuk olurdu.
/// </remarks>
public sealed class NotificationChannelProbe : INotificationChannelProbe
{
    /// <summary>Onbellek suresi.</summary>
    public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDateTimeProvider _clock;
    private readonly ConcurrentDictionary<NotificationChannel, (ChannelProbeResult Result, DateTimeOffset At)> _cache = new();

    /// <summary>Yeni ornek olusturur.</summary>
    public NotificationChannelProbe(IServiceScopeFactory scopeFactory, IDateTimeProvider clock)
    {
        _scopeFactory = scopeFactory;
        _clock = clock;
    }

    /// <inheritdoc />
    public async Task<ChannelProbeResult> ProbeAsync(NotificationChannel channel, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(channel, out var cached) && _clock.UtcNow - cached.At < CacheDuration)
        {
            return cached.Result;
        }

        await using var scope = _scopeFactory.CreateAsyncScope();

        var result = channel == NotificationChannel.Email
            ? FromSmtp(await scope.ServiceProvider.GetRequiredService<SmtpEmailSender>().CheckAsync(cancellationToken).ConfigureAwait(false))
            : FromNetGsm(await scope.ServiceProvider.GetRequiredService<NetGsmSmsSender>().CheckAsync(cancellationToken).ConfigureAwait(false));

        _cache[channel] = (result, _clock.UtcNow);
        return result;
    }

    private static ChannelProbeResult FromSmtp(SendResult check) => check switch
    {
        { Outcome: SendOutcome.Sent } => new(ChannelState.Healthy, check.ResultCode),
        { ResultCode: "not-configured" } => new(ChannelState.NotConfigured, null),
        { Outcome: SendOutcome.ConfigurationError } => new(ChannelState.Misconfigured, check.ResultCode),
        _ => new(ChannelState.Unreachable, check.ResultCode),
    };

    private static ChannelProbeResult FromNetGsm(NetGsmStatus status) => status switch
    {
        { IsConfigured: false } => new(ChannelState.NotConfigured, null),
        { IsReachable: false, ResultCode: "network" } => new(ChannelState.Unreachable, "network"),
        { IsReachable: false } => new(ChannelState.Misconfigured, status.ResultCode),
        { HeaderDefined: false } => new(ChannelState.Misconfigured, "header-not-defined"),
        _ => new(ChannelState.Healthy, status.ResultCode),
    };
}
