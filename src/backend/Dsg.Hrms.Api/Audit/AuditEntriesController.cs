using System.Text.Json.Serialization;
using Dsg.Hrms.Api.Identity;
using Dsg.Hrms.Application.Audit;
using Dsg.Hrms.Application.Identity.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Audit;

/// <summary>
/// Denetim kayitlari (PG-PERS-28, 30; SYG-PERS-069; ADR-0022 §2). Yalnizca <c>audit.log.view</c>;
/// salt okunur. Goruntuleme erisim kaydina yazilir.
/// </summary>
[ApiController]
[Route("api/v1/audit/entries")]
[Produces("application/json")]
public sealed class AuditEntriesController : ControllerBase
{
    private readonly AuditLogService _audit;

    /// <summary>Yeni ornek olusturur.</summary>
    public AuditEntriesController(AuditLogService audit)
    {
        _audit = audit;
    }

    /// <summary>
    /// Degisiklik, erisim, disa aktarma ve guvenlik olayi kayitlarini yeniden eskiye, sayfali
    /// listeler. Maskeli degerler maskeli kalir.
    /// </summary>
    /// <response code="200">Kayitlar.</response>
    /// <response code="400">Sayfa, tarih araligi veya derinlik gecersiz.</response>
    /// <response code="403">Yetki yok (<c>audit.log.view</c>).</response>
    [HttpGet]
    [HasPermission(Permissions.AuditLogView)]
    [ProducesResponseType<PagedResponse<AuditEntryResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<PagedResponse<AuditEntryResponse>> ListAsync([FromQuery] AuditEntryListRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await _audit.ListAsync(
            new AuditLogRequest(
                request.From,
                request.To,
                request.Type is { } type ? (AuditLogKind)type : null,
                request.ActorId,
                request.SubjectId,
                request.Page,
                request.PageSize),
            cancellationToken);

        return PagedResponse.From(result, AuditEntryResponse.From);
    }
}

/// <summary>Liste istegi.</summary>
public sealed class AuditEntryListRequest
{
    /// <summary>Ilk gun (YYYY-AA-GG, Europe/Istanbul), dahil.</summary>
    [FromQuery(Name = "from")]
    public DateOnly? From { get; init; }

    /// <summary>Son gun (YYYY-AA-GG, Europe/Istanbul), dahil.</summary>
    [FromQuery(Name = "to")]
    public DateOnly? To { get; init; }

    /// <summary>Kayit turu; verilmezse dordu.</summary>
    [FromQuery(Name = "type")]
    public AuditEntryKind? Type { get; init; }

    /// <summary>Islemi yapan kisinin dis kimligi.</summary>
    [FromQuery(Name = "actorId")]
    public Guid? ActorId { get; init; }

    /// <summary>Verisi goruntulenen kisinin dis kimligi.</summary>
    [FromQuery(Name = "subjectId")]
    public Guid? SubjectId { get; init; }

    /// <summary>Sayfa (1'den baslar).</summary>
    [FromQuery(Name = "page")]
    public int Page { get; init; } = 1;

    /// <summary>Sayfa boyutu (en fazla 100).</summary>
    [FromQuery(Name = "pageSize")]
    public int PageSize { get; init; } = 25;
}

/// <summary>Denetim kaydi turu.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<AuditEntryKind>))]
public enum AuditEntryKind
{
    /// <summary>Degisiklik.</summary>
    [JsonStringEnumMemberName("change")]
    Change = AuditLogKind.Change,

    /// <summary>Erisim (disa aktarma haric).</summary>
    [JsonStringEnumMemberName("access")]
    Access = AuditLogKind.Access,

    /// <summary>Disa aktarma.</summary>
    [JsonStringEnumMemberName("export")]
    Export = AuditLogKind.Export,

    /// <summary>Guvenlik olayi.</summary>
    [JsonStringEnumMemberName("security")]
    Security = AuditLogKind.Security,
}

/// <summary>Kayitta adiyla gosterilen kisi.</summary>
/// <param name="PersonId">Kisinin dis kimligi.</param>
/// <param name="FullName">Ad soyad.</param>
public sealed record AuditPersonResponse(Guid PersonId, string FullName);

/// <summary>Bir denetim kaydi.</summary>
/// <param name="Id">Satir kimligi (ornek <c>change-42</c>).</param>
/// <param name="Type">Tur.</param>
/// <param name="OccurredAt">Zaman (UTC).</param>
/// <param name="Action">Islem kodu: <c>Insert</c>/<c>Update</c>/<c>Delete</c>; erisim turu (<c>View</c>, <c>List</c>, <c>Export</c> ...); guvenlik olayi turu (<c>SignInSucceeded</c> ...).</param>
/// <param name="Actor">Islemi yapan; arka plan isleminde bos ("Sistem").</param>
/// <param name="EntityName">Kayit turu.</param>
/// <param name="EntityId">Kaydin dis kimligi; yoksa bos.</param>
/// <param name="Subject">Verisi goruntulenen veya olayin konusu olan kisi; yoksa bos.</param>
/// <param name="RecordCount">Erisilen kayit sayisi (erisim ve disa aktarma).</param>
/// <param name="SubjectCount">Verisi gosterilen kisi sayisi (liste ve disa aktarma).</param>
/// <param name="Details">Kayittaki ayrinti; maskeli degerler maskeli kalir.</param>
/// <param name="TraceId">Uygulama gunlugu ile iliskilendirme kimligi.</param>
/// <param name="IpAddress">Istegin geldigi IP adresi.</param>
public sealed record AuditEntryResponse(
    string Id,
    AuditEntryKind Type,
    DateTimeOffset OccurredAt,
    string Action,
    AuditPersonResponse? Actor,
    string EntityName,
    Guid? EntityId,
    AuditPersonResponse? Subject,
    int? RecordCount,
    int SubjectCount,
    string? Details,
    string? TraceId,
    string? IpAddress)
{
    /// <summary>Uygulama satirindan yanit uretir.</summary>
    public static AuditEntryResponse From(AuditLogItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        return new(
            item.Id,
            (AuditEntryKind)item.Kind,
            item.OccurredAt,
            item.Action,
            item.Actor is { } actor ? new AuditPersonResponse(actor.PersonId, actor.FullName) : null,
            item.EntityName,
            item.EntityId,
            item.Subject is { } subject ? new AuditPersonResponse(subject.PersonId, subject.FullName) : null,
            item.RecordCount,
            item.SubjectCount,
            item.Details,
            item.TraceId,
            item.IpAddress);
    }
}
