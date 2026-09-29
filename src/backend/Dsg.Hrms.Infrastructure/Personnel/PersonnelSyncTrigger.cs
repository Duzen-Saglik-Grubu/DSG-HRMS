using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Infrastructure.Logo;

namespace Dsg.Hrms.Infrastructure.Personnel;

/// <summary>
/// Elle calisma sinyali (SYG-KMLK-072). Zamanlayici bir sonraki periyodu beklerken bu sinyali
/// de bekler; sinyal gelince hemen "elle" calismayi baslatir.
/// </summary>
/// <remarks>
/// Sirada en fazla bir istek durur: arka arkaya basilan dugme ayni calismayi ister. Sinyal
/// uygulama belleginde tutulur; uygulama yeniden baslarsa sirada bekleyen istek kaybolur ve
/// periyodik calisma devam eder.
/// </remarks>
public sealed class PersonnelSyncTrigger(LogoOptions logo) : IPersonnelSyncTrigger, IDisposable
{
    private readonly SemaphoreSlim _signal = new(0, 1);

    /// <inheritdoc />
    public ManualSyncRequest Request()
    {
        ArgumentNullException.ThrowIfNull(logo);

        if (!logo.IsConfigured)
        {
            return ManualSyncRequest.Disabled;
        }

        try
        {
            _signal.Release();
            return ManualSyncRequest.Accepted;
        }
        catch (SemaphoreFullException)
        {
            return ManualSyncRequest.AlreadyQueued;
        }
    }

    /// <summary>
    /// Verilen sure kadar veya elle calisma istenene kadar bekler.
    /// </summary>
    /// <returns>Elle calisma istendiyse <c>true</c>.</returns>
    public Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken) =>
        _signal.WaitAsync(timeout, cancellationToken);

    /// <inheritdoc />
    public void Dispose() => _signal.Dispose();
}
