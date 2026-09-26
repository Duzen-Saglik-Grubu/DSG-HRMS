using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Personnel.Sync;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dsg.Hrms.Api.Health;

/// <summary>
/// LOGO personel senkronizasyonunun sagligi (SYG-KMLK-010).
/// </summary>
/// <remarks>
/// <para>
/// <c>/health/ready</c> kapsaminda DEGILDIR, ayri bir uc noktada (<c>/health/sync</c>)
/// sunulur: LOGO erisilemediginde sistem son anlik goruntuyle calismaya devam eder
/// (SYG-KMLK-011). Hazir olma denetimine girseydi, LOGO'daki bir kesinti HRMS'i de
/// "hazir degil" gosterir ve yuk dengeleyici trafigi keserdi.
/// </para>
/// <list type="table">
///   <item><term>Healthy</term><description>Son calisma basarili ve guncel; ya da LOGO bilincli olarak tanimli degil.</description></item>
///   <item><term>Degraded</term><description>Hic calisma yok ya da son basarili calisma uc periyottan eski.</description></item>
///   <item><term>Unhealthy</term><description>Son calisma basarisiz.</description></item>
/// </list>
/// </remarks>
public sealed class PersonnelSyncHealthCheck : IHealthCheck
{
    private readonly IPersonnelSyncStatus _status;
    private readonly IDateTimeProvider _clock;
    private readonly ISystemParameters _parameters;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonnelSyncHealthCheck(IPersonnelSyncStatus status, IDateTimeProvider clock, ISystemParameters parameters)
    {
        _status = status;
        _clock = clock;
        _parameters = parameters;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!_status.IsEnabled)
        {
            return HealthCheckResult.Healthy("Senkronizasyon devre disi: LOGO baglantisi tanimli degil.");
        }

        var last = await _status.GetLastRunAsync(cancellationToken).ConfigureAwait(false);
        if (last is null)
        {
            return HealthCheckResult.Degraded("Henuz senkronizasyon calismasi yok.");
        }

        if (last.Status == SyncStatus.Failed)
        {
            return HealthCheckResult.Unhealthy($"Son senkronizasyon basarisiz: {last.FailureReason}.");
        }

        // Uc periyot: tek bir gecikme veya uzun suren bir calisma alarm uretmesin,
        // ama bir saati asan sessizlik fark edilsin.
        var interval = await _parameters
            .GetIntegerAsync(ParameterCatalog.SyncIntervalMinutes, cancellationToken)
            .ConfigureAwait(false);
        var staleAfter = TimeSpan.FromMinutes(interval * 3);
        var reference = last.FinishedAt ?? last.StartedAt;

        return _clock.UtcNow - reference > staleAfter
            ? HealthCheckResult.Degraded($"Son senkronizasyon {staleAfter.TotalMinutes:0} dakikadan eski.")
            : HealthCheckResult.Healthy();
    }
}
