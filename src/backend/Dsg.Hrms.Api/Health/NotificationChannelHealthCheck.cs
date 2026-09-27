using Dsg.Hrms.Application.Notifications;
using Dsg.Hrms.Domain.Notifications;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dsg.Hrms.Api.Health;

/// <summary>
/// E-posta ve SMS kanallarinin sagligi (#85). Hicbir ileti gondermez.
/// </summary>
/// <remarks>
/// <para>
/// <c>/health/ready</c> kapsaminda DEGILDIR, ayri bir uc noktada (<c>/health/notifications</c>)
/// sunulur: SMTP veya NetGSM kesintisi uygulamanin trafik almasini engellememeli.
/// </para>
/// <list type="table">
///   <item><term>Healthy</term><description>Kimlik dogrulamasi basarili; ya da kanal bilincli olarak tanimli degil.</description></item>
///   <item><term>Degraded</term><description>Sunucuya ulasilamadi (gecici olabilir).</description></item>
///   <item><term>Unhealthy</term><description>Parola, kullanici veya gonderici adi hatali: hic kimse kod alamaz.</description></item>
/// </list>
/// </remarks>
public sealed class NotificationChannelHealthCheck : IHealthCheck
{
    private readonly INotificationChannelProbe _probe;
    private readonly NotificationChannel _channel;

    /// <summary>Yeni ornek olusturur.</summary>
    public NotificationChannelHealthCheck(INotificationChannelProbe probe, NotificationChannel channel)
    {
        _probe = probe;
        _channel = channel;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var result = await _probe.ProbeAsync(_channel, cancellationToken).ConfigureAwait(false);

        return result.State switch
        {
            ChannelState.NotConfigured => HealthCheckResult.Healthy($"{_channel} kanali tanimli degil."),
            ChannelState.Healthy => HealthCheckResult.Healthy(),
            ChannelState.Misconfigured => HealthCheckResult.Unhealthy($"{_channel} yapilandirmasi hatali: {result.ResultCode}."),
            _ => HealthCheckResult.Degraded($"{_channel} sunucusuna ulasilamadi: {result.ResultCode}."),
        };
    }
}
