using Dsg.Hrms.Api.Identity;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace Dsg.Hrms.Api.Settings;

/// <summary>
/// Kurumsal logo (PRM-GRN-01, SYG-KMLK-069, 076).
/// </summary>
/// <remarks>
/// Logonun okunmasi kimlik dogrulamasi istemez: giris ve uyelik ekranlari oturumdan once
/// acilir. Yuklenmemisse <c>404</c> doner ve istemci depodaki varsayilan logoyu kullanir.
/// </remarks>
[ApiController]
[Route("api/v1/system/logo")]
public sealed class BrandingController : ControllerBase
{
    private readonly BrandingService _branding;

    /// <summary>Yeni ornek olusturur.</summary>
    public BrandingController(BrandingService branding)
    {
        _branding = branding;
    }

    /// <summary>Yuklu logoyu dondurur.</summary>
    /// <response code="200">Logo (PNG veya JPEG).</response>
    /// <response code="304">Tarayicidaki surum guncel.</response>
    /// <response code="404">Logo yuklenmemis; varsayilan kullanilir.</response>
    [HttpGet]
    [ProducesResponseType<FileContentResult>(StatusCodes.Status200OK, "image/png", "image/jpeg")]
    [ProducesResponseType(StatusCodes.Status304NotModified)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
    {
        var logo = await _branding.GetLogoAsync(cancellationToken)
            ?? throw new NotFoundException("Logo yüklenmemiş.");

        // Onbellek her istekte dogrulanir: logo degistiginde eskisi gosterilmeye devam etmez.
        var etag = new EntityTagHeaderValue($"\"{logo.Sha256}\"");
        Response.Headers.CacheControl = "no-cache";
        Response.Headers.ETag = etag.ToString();

        if (Request.GetTypedHeaders().IfNoneMatch.Any(tag => tag.Compare(etag, useStrongComparison: true)))
        {
            return StatusCode(StatusCodes.Status304NotModified);
        }

        return File(logo.Content, logo.ContentType);
    }

    /// <summary>Logoyu yukler veya degistirir (PNG veya JPEG, en fazla 512 KB).</summary>
    /// <response code="204">Kaydedildi.</response>
    /// <response code="403">Yetki yok (<c>system.parameter.update</c>).</response>
    /// <response code="422">Dosya bos, cok buyuk veya PNG/JPEG degil.</response>
    [HttpPut]
    [HasPermission(Permissions.ParameterUpdate)]
    [RequestSizeLimit(BrandLogo.MaxSizeBytes + 64 * 1024)]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UploadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length > BrandLogo.MaxSizeBytes)
        {
            throw new BusinessRuleException($"Logo en fazla {BrandLogo.MaxSizeBytes / 1024} KB olabilir.");
        }

        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, cancellationToken);
        await _branding.SetLogoAsync(buffer.ToArray(), cancellationToken);
        return NoContent();
    }

    /// <summary>Yuklu logoyu kaldirir; varsayilan logoya donulur.</summary>
    /// <response code="204">Kaldirildi (yuklu degilse de).</response>
    /// <response code="403">Yetki yok (<c>system.parameter.update</c>).</response>
    [HttpDelete]
    [HasPermission(Permissions.ParameterUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveAsync(CancellationToken cancellationToken)
    {
        await _branding.RemoveLogoAsync(cancellationToken);
        return NoContent();
    }
}
