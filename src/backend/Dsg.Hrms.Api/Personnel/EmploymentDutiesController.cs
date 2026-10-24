using Dsg.Hrms.Api.Identity;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Personnel.Duties;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Personnel;

/// <summary>
/// Kisiye gorev girisi (PG-PERS-43; SYG-PERS-042). Yalnizca <c>personnel.person.update</c>
/// (IK ve Sistem Yoneticisi); degisiklik denetim izine yazilir.
/// </summary>
/// <remarks>
/// Kapsam disi veya olmayan kisi kart ile ayni <c>404</c>'u alir (SYG-PERS-051). Cakisan donem
/// <c>409</c>, istihdam araligi disi tarih <c>422</c> doner (SYG-PERS-019).
/// </remarks>
[ApiController]
[Route("api/v1/personnel/persons/{personId:guid}/employments/{registryCode}/duties")]
[Produces("application/json")]
public sealed class EmploymentDutiesController : ControllerBase
{
    private readonly DutyService _duties;

    /// <summary>Yeni ornek olusturur.</summary>
    public EmploymentDutiesController(DutyService duties)
    {
        _duties = duties;
    }

    /// <summary>Istihdama gorev atar; onceki acik gorev yeni baslangicla kapanir.</summary>
    /// <response code="201">Gecerli gorev kaydi.</response>
    /// <response code="400">Gorev tanimi secilmedi.</response>
    /// <response code="403">Yetki yok (<c>personnel.person.update</c>).</response>
    /// <response code="404">Kisi veya istihdam yok ya da kapsam disi.</response>
    /// <response code="409">Baska bir gorev donemiyle cakisiyor veya acik gorevden once basliyor.</response>
    /// <response code="422">Tarih istihdam araligi disinda ya da tanim pasif.</response>
    [HttpPost]
    [HasPermission(Permissions.PersonUpdate)]
    [ProducesResponseType<DutyResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AssignAsync(Guid personId, string registryCode, AssignDutyRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var duty = await _duties.AssignAsync(personId, registryCode, request.DefinitionId, request.ValidFrom, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new DutyResponse(duty.Id, duty.Name, duty.ValidFrom, duty.ValidTo));
    }

    /// <summary>Gorev kaydini siler (yanlis girisi duzeltmek icin; kayit yumusak silinir).</summary>
    /// <response code="204">Silindi.</response>
    /// <response code="403">Yetki yok (<c>personnel.person.update</c>).</response>
    /// <response code="404">Kisi, istihdam veya gorev kaydi yok ya da kapsam disi.</response>
    [HttpDelete("{dutyId:guid}")]
    [HasPermission(Permissions.PersonUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid personId, string registryCode, Guid dutyId, CancellationToken cancellationToken)
    {
        await _duties.DeleteAsync(personId, registryCode, dutyId, cancellationToken);
        return NoContent();
    }
}

/// <summary>Gorev atama istegi.</summary>
/// <param name="DefinitionId">Gorev taniminin dis kimligi (referans veri, tur gorev).</param>
/// <param name="ValidFrom">Baslangic; verilmezse bugun.</param>
public sealed record AssignDutyRequest(Guid DefinitionId, DateOnly? ValidFrom);

/// <summary>Gorev kaydi.</summary>
/// <param name="Id">Dis kimlik.</param>
/// <param name="Name">Gorev.</param>
/// <param name="ValidFrom">Baslangic.</param>
/// <param name="ValidTo">Bitis (haric); acikse bos.</param>
public sealed record DutyResponse(Guid Id, string Name, DateOnly ValidFrom, DateOnly? ValidTo);

/// <summary>Gorev atama isteginin dogrulamasi.</summary>
public sealed class AssignDutyRequestValidator : AbstractValidator<AssignDutyRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public AssignDutyRequestValidator()
    {
        RuleFor(r => r.DefinitionId).NotEmpty().WithMessage("Görev seçin.");
    }
}
