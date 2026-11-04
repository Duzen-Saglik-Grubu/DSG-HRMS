using System.Text.Json;
using Dsg.Hrms.Application.Notifications.Inbox;
using Dsg.Hrms.Domain.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Notifications;

/// <summary>
/// Uygulama ici bildirim kutusu (ADR-0022 §3; SYG-PERS-046). Herkes yalnizca kendi bildirimlerini
/// gorur; izin gerektirmez.
/// </summary>
/// <remarks>
/// Bildirim kisisel veri tasimaz: tur ve ilgili kaydin kimligi doner. Ilgili kaydi acmak o kaydin
/// kendi yetkisini ister.
/// </remarks>
[ApiController]
[Route("api/v1/notifications/inbox")]
[Produces("application/json")]
[Authorize]
public sealed class InboxController : ControllerBase
{
    private readonly InboxService _inbox;

    /// <summary>Yeni ornek olusturur.</summary>
    public InboxController(InboxService inbox)
    {
        _inbox = inbox;
    }

    /// <summary>En yeni bildirimler (en fazla 50), en yeni once.</summary>
    /// <response code="200">Bildirimler.</response>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<InboxItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<IReadOnlyList<InboxItemResponse>> ListAsync(CancellationToken cancellationToken) =>
        [.. (await _inbox.ListAsync(cancellationToken)).Select(InboxItemResponse.From)];

    /// <summary>Okunmamis bildirim sayisi (menude gosterilir).</summary>
    /// <response code="200">Sayi.</response>
    [HttpGet("unread-count")]
    [ProducesResponseType<UnreadCountResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<UnreadCountResponse> UnreadCountAsync(CancellationToken cancellationToken) =>
        new(await _inbox.CountUnreadAsync(cancellationToken));

    /// <summary>Bildirimi okundu olarak isaretler.</summary>
    /// <response code="200">Bildirim.</response>
    /// <response code="404">Bildirim yok veya baskasinin.</response>
    [HttpPost("{itemId:guid}/read")]
    [ProducesResponseType<InboxItemResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<InboxItemResponse> MarkReadAsync(Guid itemId, CancellationToken cancellationToken) =>
        InboxItemResponse.From(await _inbox.MarkReadAsync(itemId, cancellationToken));
}

/// <summary>Bildirim.</summary>
/// <param name="Id">Bildirimin dis kimligi.</param>
/// <param name="Kind">Tur (<c>correctionRequested</c>).</param>
/// <param name="SubjectId">Ilgili kaydin dis kimligi (ornegin duzeltme talebi).</param>
/// <param name="CreatedAt">Olusma ani.</param>
/// <param name="ReadAt">Okunma ani; okunmadiysa bos.</param>
public sealed record InboxItemResponse(Guid Id, string Kind, Guid SubjectId, DateTimeOffset CreatedAt, DateTimeOffset? ReadAt)
{
    /// <summary>Alan nesnesini yanita cevirir.</summary>
    public static InboxItemResponse From(InboxItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new InboxItemResponse(item.PublicId, JsonNamingPolicy.CamelCase.ConvertName(item.Kind.ToString()), item.SubjectId, item.CreatedAt, item.ReadAt);
    }
}

/// <summary>Okunmamis bildirim sayisi.</summary>
/// <param name="Count">Sayi.</param>
public sealed record UnreadCountResponse(int Count);
