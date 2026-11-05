using Dsg.Hrms.Api.Identity;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Personnel.Access;
using Dsg.Hrms.Application.Personnel.EmergencyContacts;
using Dsg.Hrms.Domain.Personnel;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Personnel;

/// <summary>
/// Kisinin acil durum kisileri (PG-PERS-44; SYG-PERS-043). Yalnizca <c>personnel.person.update</c>
/// (IK ve Sistem Yoneticisi); degisiklik denetim izine yazilir (SYG-PERS-066).
/// </summary>
/// <remarks>
/// Okuma ayri bir uc degildir: kayitlar kartin korumali bolumunde gelir ve ayni gorunurluk
/// kuralina tabidir (SYG-PERS-052). Kapsam disi veya olmayan kisi kart ile ayni <c>404</c>'u alir
/// (SYG-PERS-051).
/// </remarks>
[ApiController]
[Route("api/v1/personnel/persons/{personId:guid}/emergency-contacts")]
[Produces("application/json")]
public sealed class EmergencyContactsController : ControllerBase
{
    private readonly EmergencyContactService _contacts;

    /// <summary>Yeni ornek olusturur.</summary>
    public EmergencyContactsController(EmergencyContactService contacts)
    {
        _contacts = contacts;
    }

    /// <summary>Kisiye acil durum kisisi ekler.</summary>
    /// <response code="201">Eklenen kayit.</response>
    /// <response code="400">Alan eksik veya gecersiz (telefon SYG-KMLK-009).</response>
    /// <response code="403">Yetki yok (<c>personnel.person.update</c>).</response>
    /// <response code="404">Kisi yok ya da kapsam disi.</response>
    [HttpPost]
    [HasPermission(Permissions.PersonUpdate)]
    [ProducesResponseType<EmergencyContactResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddAsync(Guid personId, EmergencyContactRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contact = await _contacts.AddAsync(personId, request.ToDetails(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, EmergencyContactResponse.From(contact));
    }

    /// <summary>Acil durum kisisinin bilgilerini degistirir.</summary>
    /// <response code="200">Guncel kayit.</response>
    /// <response code="400">Alan eksik veya gecersiz.</response>
    /// <response code="403">Yetki yok (<c>personnel.person.update</c>).</response>
    /// <response code="404">Kisi veya kayit yok ya da kapsam disi.</response>
    [HttpPut("{contactId:guid}")]
    [HasPermission(Permissions.PersonUpdate)]
    [ProducesResponseType<EmergencyContactResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeAsync(Guid personId, Guid contactId, EmergencyContactRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contact = await _contacts.ChangeAsync(personId, contactId, request.ToDetails(), cancellationToken);
        return Ok(EmergencyContactResponse.From(contact));
    }

    /// <summary>Acil durum kisisini siler (kayit yumusak silinir).</summary>
    /// <response code="204">Silindi.</response>
    /// <response code="403">Yetki yok (<c>personnel.person.update</c>).</response>
    /// <response code="404">Kisi veya kayit yok ya da kapsam disi.</response>
    [HttpDelete("{contactId:guid}")]
    [HasPermission(Permissions.PersonUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(Guid personId, Guid contactId, CancellationToken cancellationToken)
    {
        await _contacts.DeleteAsync(personId, contactId, cancellationToken);
        return NoContent();
    }
}

/// <summary>Acil durum kisisi ekleme ve degistirme istegi; dort alan da zorunludur.</summary>
/// <param name="Relationship">Yakinligi (en fazla 50 karakter).</param>
/// <param name="FirstName">Ad (en fazla 100 karakter).</param>
/// <param name="LastName">Soyad (en fazla 100 karakter).</param>
/// <param name="MobilePhone">Cep telefonu; 5 ile baslayan 10 hane, bastaki 0 veya 90 kabul edilir (SYG-KMLK-009).</param>
public sealed record EmergencyContactRequest(string? Relationship, string? FirstName, string? LastName, string? MobilePhone)
{
    /// <summary>Dogrulanmis bicim; dogrulayici gecerliligi once denetler.</summary>
    internal EmergencyContactDetails ToDetails() =>
        EmergencyContactDetails.Parse(Relationship, FirstName, LastName, MobilePhone).Details!;
}

/// <summary>Acil durum kisisi.</summary>
/// <param name="Id">Dis kimlik.</param>
/// <param name="Relationship">Yakinligi.</param>
/// <param name="FirstName">Ad.</param>
/// <param name="LastName">Soyad.</param>
/// <param name="MobilePhone">Cep telefonu, <c>5XXXXXXXXX</c>.</param>
public sealed record EmergencyContactResponse(Guid Id, string Relationship, string FirstName, string LastName, string MobilePhone)
{
    /// <summary>Okunan kayittan yanit.</summary>
    public static EmergencyContactResponse From(EmergencyContactRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return new(record.Id, record.Relationship, record.FirstName, record.LastName, record.MobilePhone);
    }
}

/// <summary>Acil durum kisisi isteginin dogrulamasi (SYG-PERS-043): hata ilgili alanin altinda doner.</summary>
public sealed class EmergencyContactRequestValidator : AbstractValidator<EmergencyContactRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public EmergencyContactRequestValidator()
    {
        RuleFor(r => r).Custom((request, context) =>
        {
            var (_, errors) = EmergencyContactDetails.Parse(request.Relationship, request.FirstName, request.LastName, request.MobilePhone);
            foreach (var (field, message) in errors)
            {
                context.AddFailure(field, message);
            }
        });
    }
}
