using Dsg.Hrms.Application.Personnel.Sync;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Personnel;

/// <summary>
/// Personel listesi ve kisi kartindaki "Son senkronizasyon" bilgisi (SYG-PERS-010, PG-PERS-07).
/// </summary>
/// <remarks>
/// Giris yapmis her kullaniciya aciktir: kendi kartini goren izinsiz kullanici da verinin ne
/// kadar guncel oldugunu bilmelidir. Yalnizca zaman ve durum doner; sayilar, uyarilar ve hata
/// nedeni <c>personnel.sync.view</c> izniyle <c>/api/v1/identity/sync-runs/latest</c> ucundadir.
/// </remarks>
[ApiController]
[Route("api/v1/personnel/sync-freshness")]
[Produces("application/json")]
[Authorize]
public sealed class SyncFreshnessController : ControllerBase
{
    private readonly IPersonnelSyncStatus _status;

    /// <summary>Yeni ornek olusturur.</summary>
    public SyncFreshnessController(IPersonnelSyncStatus status)
    {
        _status = status;
    }

    /// <summary>Verinin guncelligi.</summary>
    /// <response code="200">Guncellik bilgisi.</response>
    [HttpGet]
    [ProducesResponseType<SyncFreshnessResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<SyncFreshnessResponse> GetAsync(CancellationToken cancellationToken)
    {
        var freshness = await _status.GetFreshnessAsync(cancellationToken);
        return new SyncFreshnessResponse(_status.IsEnabled, freshness.LastSucceededAt, freshness.LastRunFailed);
    }
}

/// <summary>Personel verisinin guncelligi.</summary>
/// <param name="Enabled">LOGO baglantisi tanimli mi; degilse senkronizasyon bilincli olarak kapalidir ve bilgi gosterilmez.</param>
/// <param name="LastSucceededAt">Son basarili calismanin bitisi; hic yoksa bos.</param>
/// <param name="LastRunFailed">Son biten calisma basarisiz (veya kismen tamamlandi); ekranda uyari isareti.</param>
public sealed record SyncFreshnessResponse(bool Enabled, DateTimeOffset? LastSucceededAt, bool LastRunFailed);
