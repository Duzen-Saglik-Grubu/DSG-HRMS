using Dsg.Hrms.Application.Personnel.Access;
using Dsg.Hrms.Application.Personnel.Cards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Dsg.Hrms.Api.Personnel;

/// <summary>
/// Kisi fotografi (SYG-PERS-040, 041; ADR-0021 §5).
/// </summary>
/// <remarks>
/// <para>
/// Kart ile ayni kapsam ve gorunur kalma kurali gecerlidir: kapsam disi, bulunmayan ve fotografi
/// olmayan kisi ayni <c>404</c> govdesini alir (SYG-PERS-051). Fotograf ayrica erisim kaydi
/// uretmez; kaydi yazilan kart veya liste goruntulemesinin parcasidir.
/// </para>
/// <para>
/// ETag, LOGO'daki orijinalin ozetidir. <c>private</c> ortak onbelleklerde (vekil) saklanmayi
/// engeller; <c>no-cache</c> tarayicinin her gosterimde ozeti sormasini saglar. Boylece LOGO'da
/// degisen fotograf bir sonraki gosterimde gelir, degismeyen yeniden indirilmez.
/// </para>
/// <para>
/// Personelin fotograf yuklemesi icin uc yoktur (SYG-PERS-040); kaynak LOGO'dur.
/// </para>
/// </remarks>
[ApiController]
[Route("api/v1/personnel/persons")]
[Authorize]
public sealed class PersonPhotosController : ControllerBase
{
    private const string ContentType = "image/jpeg";

    private readonly PersonCardService _cards;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonPhotosController(PersonCardService cards)
    {
        _cards = cards;
    }

    /// <summary>Kullanicinin kendi fotografi. Izin gerektirmez.</summary>
    /// <response code="200">Fotograf (JPEG, uzun kenar en fazla 400 piksel).</response>
    /// <response code="304">Tarayicidaki surum guncel.</response>
    /// <response code="404">Fotograf yok.</response>
    [HttpGet("me/photo")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, ContentType)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetOwnAsync(CancellationToken cancellationToken) =>
        Photo(await _cards.GetOwnPhotoAsync(cancellationToken));

    /// <summary>Kisinin fotografi.</summary>
    /// <response code="200">Fotograf (JPEG, uzun kenar en fazla 400 piksel).</response>
    /// <response code="304">Tarayicidaki surum guncel.</response>
    /// <response code="404">Kisi yok, kapsam disinda veya fotografi yok (ayirt edilemez).</response>
    [HttpGet("{personId:guid}/photo")]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, ContentType)]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetAsync(Guid personId, CancellationToken cancellationToken) =>
        Photo(await _cards.GetPhotoAsync(personId, cancellationToken));

    /// <summary><c>If-None-Match</c> ozetle eslesirse cerceve <c>304</c> doner.</summary>
    private FileContentResult Photo(PhotoRecord photo)
    {
        Response.Headers.CacheControl = "private, no-cache";
        return File(photo.Content, ContentType, lastModified: null, entityTag: new EntityTagHeaderValue($"\"{photo.SourceHash}\""));
    }
}
