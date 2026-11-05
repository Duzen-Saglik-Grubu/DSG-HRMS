using System.Text.Json;
using Dsg.Hrms.Api.Identity;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Common.Paging;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Domain.Personnel.Sync;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Personnel;

/// <summary>
/// "Senkronizasyon durumu" ekrani (PG-PERS-05, 06; SYG-PERS-008, 009).
/// </summary>
/// <remarks>
/// Gormek <c>personnel.sync.view</c>, elle baslatmak <c>personnel.sync.create</c> ister. T3'teki
/// <c>/api/v1/identity/sync-runs</c> uclari kabul edilmis surumde (0.2.0) oldugu icin kalir;
/// personel islemleri <c>/api/v1/personnel/</c> altindadir (SYG-PERS-074).
/// </remarks>
[ApiController]
[Route("api/v1/personnel/sync-runs")]
[Produces("application/json")]
public sealed class PersonnelSyncRunsController : ControllerBase
{
    private readonly IPersonnelSyncStatus _status;
    private readonly IPersonnelSyncTrigger _trigger;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonnelSyncRunsController(IPersonnelSyncStatus status, IPersonnelSyncTrigger trigger)
    {
        _status = status;
        _trigger = trigger;
    }

    /// <summary>Son calismalar, en yeni once (SYG-PERS-008).</summary>
    /// <response code="200">Durum ve sayfalanmis calismalar.</response>
    /// <response code="400">Gecersiz sayfa veya sayfa boyutu.</response>
    /// <response code="403">Yetki yok (<c>personnel.sync.view</c>).</response>
    [HttpGet]
    [HasPermission(Permissions.SyncView)]
    [ProducesResponseType<SyncRunListResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<SyncRunListResponse> ListAsync([FromQuery] SyncRunListRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Page < 1 || request.PageSize < 1 || request.PageSize > PagedResult<PersonnelSyncRunDetail>.MaxPageSize)
        {
            throw new InvalidRequestException($"Sayfa 1 veya büyük, sayfa boyutu 1–{PagedResult<PersonnelSyncRunDetail>.MaxPageSize} olmalıdır.");
        }

        var latest = await _status.GetLastRunAsync(cancellationToken);
        var runs = await _status.ListRunsAsync(new PageRequest(request.Page, request.PageSize), cancellationToken);

        return new SyncRunListResponse(
            _status.IsEnabled,
            IsRunning(latest),
            PagedResponse.From(runs, run => SyncRunItemResponse.From(run, latest)));
    }

    /// <summary>Senkronizasyonu elle baslatir (SYG-PERS-009).</summary>
    /// <remarks>
    /// Calismayi BEKLEMEZ (<c>202</c>): sonuc listede gorunur. Calisma surerken istek reddedilir.
    /// </remarks>
    /// <response code="202">Calisma sira aldi (veya zaten sirada bekliyordu).</response>
    /// <response code="403">Yetki yok (<c>personnel.sync.create</c>).</response>
    /// <response code="409">Senkronizasyon su anda calisiyor.</response>
    /// <response code="422">Senkronizasyon bu ortamda tanimli degil.</response>
    [HttpPost]
    [HasPermission(Permissions.SyncCreate)]
    [ProducesResponseType<SyncStartResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> StartAsync(CancellationToken cancellationToken)
    {
        if (IsRunning(await _status.GetLastRunAsync(cancellationToken)))
        {
            throw new ConflictException("Senkronizasyon şu anda çalışıyor. Bitince yeniden başlatabilirsiniz.");
        }

        return _trigger.Request() switch
        {
            ManualSyncRequest.Disabled => throw new BusinessRuleException("Personel senkronizasyonu bu ortamda tanımlı değil."),
            ManualSyncRequest.AlreadyQueued => Accepted(new SyncStartResponse(AlreadyQueued: true)),
            _ => Accepted(new SyncStartResponse(AlreadyQueued: false)),
        };
    }

    /// <summary>
    /// En son baslayan calisma surerken senkronizasyon calisiyordur. Daha eski bir "suruyor" kaydi
    /// yarida kalmistir (uygulama calisma sirasinda durdu); yeni calismayi engellemez.
    /// </summary>
    private static bool IsRunning(PersonnelSyncRunSummary? latest) => latest?.Status == SyncStatus.Running;
}

/// <summary>Calisma listesi istegi.</summary>
public sealed class SyncRunListRequest
{
    /// <summary>Sayfa (1'den baslar).</summary>
    public int Page { get; init; } = 1;

    /// <summary>Sayfa boyutu (1–100).</summary>
    public int PageSize { get; init; } = 25;
}

/// <summary>"Senkronizasyon durumu" ekrani.</summary>
/// <param name="Enabled">LOGO baglantisi tanimli mi; degilse senkronizasyon bu ortamda kapalidir.</param>
/// <param name="Running">Su anda bir calisma suruyor mu.</param>
/// <param name="Runs">Calismalar, en yeni once.</param>
public sealed record SyncRunListResponse(bool Enabled, bool Running, PagedResponse<SyncRunItemResponse> Runs);

/// <summary>Bir calisma (SYG-PERS-008).</summary>
/// <param name="Id">Calismanin kimligi.</param>
/// <param name="Trigger">Tetikleyen: <c>scheduled</c> veya <c>manual</c>.</param>
/// <param name="Status">
/// Sonuc: <c>running</c>, <c>succeeded</c>, <c>completedWithWarnings</c>, <c>partiallyCompleted</c>,
/// <c>failed</c>; uygulama calisma sirasinda durduysa <c>interrupted</c>.
/// </param>
/// <param name="StartedAt">Baslangic.</param>
/// <param name="FinishedAt">Bitis; suruyorsa veya yarida kaldiysa bos.</param>
/// <param name="FailureReason">
/// Basarisizlik nedeni: <c>sourceUnavailable</c>, <c>sourceWritable</c>, <c>schemaDrift</c>,
/// <c>persistenceFailed</c>, <c>unexpected</c>.
/// </param>
/// <param name="RecordsRead">LOGO'dan okunan kart.</param>
/// <param name="PersonsCreated">Eklenen kisi.</param>
/// <param name="PersonsUpdated">Guncellenen kisi.</param>
/// <param name="EmploymentsCreated">Eklenen istihdam (sicil).</param>
/// <param name="EmploymentsUpdated">Guncellenen istihdam.</param>
/// <param name="EmploymentsDeactivated">Pasiflesen istihdam.</param>
/// <param name="WarningCount">Uretilen uyari.</param>
public sealed record SyncRunItemResponse(
    Guid Id,
    string Trigger,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    string? FailureReason,
    int RecordsRead,
    int PersonsCreated,
    int PersonsUpdated,
    int EmploymentsCreated,
    int EmploymentsUpdated,
    int EmploymentsDeactivated,
    int WarningCount)
{
    /// <summary>Uygulama katmaninin kaydini yanita cevirir.</summary>
    /// <param name="run">Calisma.</param>
    /// <param name="latest">En son baslayan calisma; ondan eski "suruyor" kaydi yarida kalmistir.</param>
    public static SyncRunItemResponse From(PersonnelSyncRunDetail run, PersonnelSyncRunSummary? latest)
    {
        ArgumentNullException.ThrowIfNull(run);

        var interrupted = run.Status == SyncStatus.Running && latest is not null && run.StartedAt < latest.StartedAt;

        return new SyncRunItemResponse(
            run.Id,
            Camel(run.Trigger.ToString()),
            interrupted ? "interrupted" : Camel(run.Status.ToString()),
            run.StartedAt,
            run.FinishedAt,
            run.FailureReason is null ? null : Camel(run.FailureReason.Value.ToString()),
            run.RecordsRead,
            run.PersonsCreated,
            run.PersonsUpdated,
            run.EmploymentsCreated,
            run.EmploymentsUpdated,
            run.EmploymentsDeactivated,
            run.WarningCount);
    }

    private static string Camel(string value) => JsonNamingPolicy.CamelCase.ConvertName(value);
}

/// <summary>Elle calisma istegi alindi.</summary>
/// <param name="AlreadyQueued">Sirada zaten bekleyen bir istek vardi.</param>
public sealed record SyncStartResponse(bool AlreadyQueued);
