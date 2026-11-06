using Dsg.Hrms.Api.Identity;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Personnel.Cards;
using Dsg.Hrms.Application.Personnel.Marriage;
using Dsg.Hrms.Domain.Personnel;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Personnel;

/// <summary>
/// Evlilik tarihi ve evlilik yardimi (PG-PERS-19; SYG-PERS-036). Yalnizca
/// <c>personnel.person.update</c> (IK ve Sistem Yoneticisi) ve medeni hali Evli olan kiside;
/// degisiklik denetim izine yazilir. Okuma kartin "marriage" bolumundendir.
/// </summary>
/// <remarks>
/// Kapsam disi veya olmayan kisi kart ile ayni <c>404</c>'u alir (SYG-PERS-051). Medeni hali Evli
/// olmayan kiside ve kural disi tarihte <c>422</c> doner.
/// </remarks>
[ApiController]
[Route("api/v1/personnel/persons/{personId:guid}/marriage")]
[Produces("application/json")]
public sealed class MarriageController : ControllerBase
{
    private readonly MarriageService _marriage;

    /// <summary>Yeni ornek olusturur.</summary>
    public MarriageController(MarriageService marriage)
    {
        _marriage = marriage;
    }

    /// <summary>Evlilik bilgisini girer veya degistirir.</summary>
    /// <response code="200">Guncel bilgi.</response>
    /// <response code="400">Alan eksik.</response>
    /// <response code="403">Yetki yok (<c>personnel.person.update</c>).</response>
    /// <response code="404">Kisi yok ya da kapsam disi.</response>
    /// <response code="422">Medeni hal Evli degil; tarih bugunden sonra veya dogum tarihinden once.</response>
    [HttpPut]
    [HasPermission(Permissions.PersonUpdate)]
    [ProducesResponseType<MarriageResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<MarriageResponse> SetAsync(Guid personId, MarriageRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var marriage = await _marriage.SetAsync(personId, new MarriageDetails(request.MarriageDate!.Value, request.AllowanceReceived!.Value), cancellationToken);
        return new MarriageResponse(marriage.MarriageDate, marriage.AllowanceReceived, Editable: true);
    }

    /// <summary>Evlilik bilgisini siler (kayit yumusak silinir).</summary>
    /// <response code="204">Silindi.</response>
    /// <response code="403">Yetki yok (<c>personnel.person.update</c>).</response>
    /// <response code="404">Kisi veya evlilik bilgisi yok ya da kapsam disi.</response>
    /// <response code="422">Medeni hal Evli degil.</response>
    [HttpDelete]
    [HasPermission(Permissions.PersonUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeleteAsync(Guid personId, CancellationToken cancellationToken)
    {
        await _marriage.DeleteAsync(personId, cancellationToken);
        return NoContent();
    }
}

/// <summary>Evlilik bilgisi istegi.</summary>
/// <param name="MarriageDate">Evlilik tarihi.</param>
/// <param name="AllowanceReceived">Evlilik yardimi alindi mi.</param>
public sealed record MarriageRequest(DateOnly? MarriageDate, bool? AllowanceReceived);

/// <summary>Evlilik bilgisi (karttaki bolum).</summary>
/// <param name="MarriageDate">Evlilik tarihi; kayitli degilse bos ("Kayitli degil").</param>
/// <param name="AllowanceReceived">Evlilik yardimi alindi mi; kayitli degilse bos.</param>
/// <param name="Editable">Kullanici bu bilgiyi girebilir mi: giris izni ve medeni hal Evli.</param>
public sealed record MarriageResponse(DateOnly? MarriageDate, bool? AllowanceReceived, bool Editable)
{
    /// <summary>Karttaki bolumden yanit uretir.</summary>
    public static MarriageResponse From(MarriageCard card)
    {
        ArgumentNullException.ThrowIfNull(card);
        return new(card.Record?.MarriageDate, card.Record?.AllowanceReceived, card.Editable);
    }
}

/// <summary>Evlilik bilgisi isteginin dogrulamasi.</summary>
public sealed class MarriageRequestValidator : AbstractValidator<MarriageRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public MarriageRequestValidator()
    {
        RuleFor(r => r.MarriageDate).NotNull().WithMessage("Evlilik tarihini girin.");
        RuleFor(r => r.AllowanceReceived).NotNull().WithMessage("Evlilik yardımı alınıp alınmadığını seçin.");
    }
}
