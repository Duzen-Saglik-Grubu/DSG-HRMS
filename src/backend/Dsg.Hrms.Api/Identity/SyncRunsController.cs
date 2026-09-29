using System.Text.Json;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Personnel.Sync;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Identity;

/// <summary>
/// Personel senkronizasyonunun durumu ve elle tetiklenmesi (SYG-KMLK-072).
/// </summary>
/// <remarks>
/// Iki uc da yetki gerektirir; yetkisiz cagri <c>403</c> doner. Elle tetikleme calismayi
/// BEKLEMEZ (<c>202</c>): sonuc son calisma bilgisinden okunur.
/// </remarks>
[ApiController]
[Route("api/v1/identity/sync-runs")]
[Produces("application/json")]
public sealed class SyncRunsController : ControllerBase
{
    private readonly IPersonnelSyncStatus _status;
    private readonly IPersonnelSyncTrigger _trigger;

    /// <summary>Yeni ornek olusturur.</summary>
    public SyncRunsController(IPersonnelSyncStatus status, IPersonnelSyncTrigger trigger)
    {
        _status = status;
        _trigger = trigger;
    }

    /// <summary>Son calisma bilgisi.</summary>
    /// <response code="200">Durum. Hic calisma yoksa <c>lastRun</c> bostur.</response>
    /// <response code="403">Yetki yok (<c>identity.sync.view</c>).</response>
    [HttpGet("latest")]
    [HasPermission(Permissions.SyncView)]
    [ProducesResponseType<SyncStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<SyncStatusResponse> LatestAsync(CancellationToken cancellationToken)
    {
        var run = await _status.GetLastRunAsync(cancellationToken);

        return new SyncStatusResponse(
            _status.IsEnabled,
            run is null
                ? null
                : new SyncRunResponse(
                    Camel(run.Status.ToString()),
                    run.StartedAt,
                    run.FinishedAt,
                    run.FailureReason is null ? null : Camel(run.FailureReason.Value.ToString())));
    }

    /// <summary>Senkronizasyonu elle baslatir.</summary>
    /// <response code="202">Calisma sira aldi (veya zaten sirada bekliyordu).</response>
    /// <response code="403">Yetki yok (<c>identity.sync.create</c>).</response>
    /// <response code="422">Senkronizasyon bu ortamda tanimli degil.</response>
    [HttpPost]
    [HasPermission(Permissions.SyncCreate)]
    [ProducesResponseType<SyncRequestedResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public IActionResult StartRun()
    {
        return _trigger.Request() switch
        {
            ManualSyncRequest.Disabled => throw new BusinessRuleException("Personel senkronizasyonu bu ortamda tanımlı değil."),
            ManualSyncRequest.AlreadyQueued => Accepted(new SyncRequestedResponse(AlreadyQueued: true)),
            _ => Accepted(new SyncRequestedResponse(AlreadyQueued: false)),
        };
    }

    private static string Camel(string value) => JsonNamingPolicy.CamelCase.ConvertName(value);
}

/// <summary>Senkronizasyon durumu.</summary>
/// <param name="Enabled">LOGO baglantisi tanimli mi; degilse senkronizasyon bilincli olarak kapalidir.</param>
/// <param name="LastRun">Son calisma; hic yoksa bos.</param>
public sealed record SyncStatusResponse(bool Enabled, SyncRunResponse? LastRun);

/// <summary>Bir calismanin ozeti.</summary>
/// <param name="Status">Sonuc (<c>running</c>, <c>succeeded</c>, <c>completedWithWarnings</c>, <c>failed</c>).</param>
/// <param name="StartedAt">Baslangic.</param>
/// <param name="FinishedAt">Bitis; suruyorsa bos.</param>
/// <param name="FailureReason">Basarisizlik nedeni.</param>
public sealed record SyncRunResponse(string Status, DateTimeOffset StartedAt, DateTimeOffset? FinishedAt, string? FailureReason);

/// <summary>Elle calisma istegi alindi.</summary>
/// <param name="AlreadyQueued">Sirada zaten bekleyen bir istek vardi.</param>
public sealed record SyncRequestedResponse(bool AlreadyQueued);
