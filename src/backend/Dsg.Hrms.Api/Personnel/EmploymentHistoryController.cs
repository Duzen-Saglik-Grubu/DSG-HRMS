using Dsg.Hrms.Application.Personnel.Access;
using Dsg.Hrms.Application.Personnel.History;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Personnel;

/// <summary>
/// Istihdam tarihcesi ve tarihli sorgu (PG-PERS-12; SYG-PERS-020). Yetki ve kapsam kartla
/// aynidir; kisi kendi tarihcesini izinsiz gorur. Goruntuleme erisim kaydina yazilir.
/// </summary>
/// <remarks>
/// Kart gibi oturum ister ama izin istemez: kapsam sorgudadir; kapsam disi kisi <c>404</c> alir
/// (SYG-PERS-051).
/// </remarks>
[ApiController]
[Route("api/v1/personnel/persons/{personId:guid}/employments/{registryCode}/history")]
[Produces("application/json")]
[Authorize]
public sealed class EmploymentHistoryController : ControllerBase
{
    private readonly EmploymentHistoryService _history;

    /// <summary>Yeni ornek olusturur.</summary>
    public EmploymentHistoryController(EmploymentHistoryService history)
    {
        _history = history;
    }

    /// <summary>
    /// Istihdamin atama (firma, sube, birim, unvan) ve gorev donemleri, yeniden eskiye. Tarih
    /// verilirse yalnizca o tarihte gecerli donemler doner; T1 oncesi bir tarih icin listeler
    /// bostur ("kayit yok").
    /// </summary>
    /// <param name="personId">Kisinin dis kimligi.</param>
    /// <param name="registryCode">Sicil numarasi.</param>
    /// <param name="date">Tarihli sorgu (YYYY-AA-GG); verilmezse butun tarihce.</param>
    /// <param name="cancellationToken">Iptal.</param>
    /// <response code="200">Tarihce.</response>
    /// <response code="400">Tarih gecersiz.</response>
    /// <response code="404">Kisi veya istihdam yok ya da kapsam disi (ayirt edilemez).</response>
    [HttpGet]
    [ProducesResponseType<EmploymentHistoryResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<EmploymentHistoryResponse> GetAsync(Guid personId, string registryCode, [FromQuery] DateOnly? date, CancellationToken cancellationToken) =>
        EmploymentHistoryResponse.From(date, await _history.GetAsync(personId, registryCode, date, cancellationToken));
}

/// <summary>Istihdam tarihcesi.</summary>
/// <param name="Date">Tarihli sorguda istenen tarih; butun tarihcede bos.</param>
/// <param name="Assignments">Atama donemleri, yeniden eskiye.</param>
/// <param name="Duties">Gorev donemleri, yeniden eskiye; silinmis kayitlar yoktur.</param>
public sealed record EmploymentHistoryResponse(
    DateOnly? Date,
    IReadOnlyList<AssignmentPeriodResponse> Assignments,
    IReadOnlyList<DutyResponse> Duties)
{
    /// <summary>Uygulama sonucundan yanit uretir.</summary>
    public static EmploymentHistoryResponse From(DateOnly? date, EmploymentHistory history)
    {
        ArgumentNullException.ThrowIfNull(history);

        return new(
            date,
            [.. history.Assignments.Select(AssignmentPeriodResponse.From)],
            [.. history.Duties.Select(d => new DutyResponse(d.Id, d.Name, d.ValidFrom, d.ValidTo))]);
    }
}

/// <summary>Bir atama donemi.</summary>
/// <param name="ValidFrom">Baslangic.</param>
/// <param name="ValidTo">Bitis (haric); acikse bos.</param>
/// <param name="CompanyName">Firma.</param>
/// <param name="BranchName">Sube.</param>
/// <param name="DepartmentName">Birim.</param>
/// <param name="TitleName">Unvan (referans veri); karsiligi yoksa bos.</param>
public sealed record AssignmentPeriodResponse(
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    string CompanyName,
    string? BranchName,
    string? DepartmentName,
    string? TitleName)
{
    /// <summary>Donem kaydindan yanit uretir.</summary>
    public static AssignmentPeriodResponse From(AssignmentRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new(record.ValidFrom, record.ValidTo, record.CompanyName, record.BranchName, record.DepartmentName, record.TitleName);
    }
}
