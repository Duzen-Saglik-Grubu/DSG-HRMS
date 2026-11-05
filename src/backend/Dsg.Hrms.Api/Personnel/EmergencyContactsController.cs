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

    /// <summary>
    /// Yeni secim icin aktif yakinlik tanimlari (referans veri, <c>KR-135</c>). Giris izniyle okunur:
    /// referans veri izni olmayan bir rol de formu kullanabilir.
    /// </summary>
    /// <response code="200">Tanimlar, siraya gore.</response>
    /// <response code="403">Yetki yok (<c>personnel.person.update</c>).</response>
    [HttpGet("/api/v1/personnel/emergency-contact-relationships")]
    [HasPermission(Permissions.PersonUpdate)]
    [ProducesResponseType<IReadOnlyList<RelationshipOptionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IReadOnlyList<RelationshipOptionResponse>> ListRelationshipsAsync(CancellationToken cancellationToken) =>
        [.. (await _contacts.ListRelationshipsAsync(cancellationToken)).Select(d => new RelationshipOptionResponse(d.PublicId, d.Name))];

    /// <summary>Kisiye acil durum kisisi ekler.</summary>
    /// <response code="201">Eklenen kayit.</response>
    /// <response code="400">Alan eksik veya gecersiz (telefon SYG-KMLK-009).</response>
    /// <response code="403">Yetki yok (<c>personnel.person.update</c>).</response>
    /// <response code="404">Kisi yok ya da kapsam disi.</response>
    /// <response code="422">Yakinlik tanimi yok veya pasif.</response>
    [HttpPost]
    [HasPermission(Permissions.PersonUpdate)]
    [ProducesResponseType<EmergencyContactResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddAsync(Guid personId, EmergencyContactRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contact = await _contacts.AddAsync(personId, request.ToInput(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, EmergencyContactResponse.From(contact));
    }

    /// <summary>Acil durum kisisinin bilgilerini degistirir.</summary>
    /// <response code="200">Guncel kayit.</response>
    /// <response code="400">Alan eksik veya gecersiz.</response>
    /// <response code="403">Yetki yok (<c>personnel.person.update</c>).</response>
    /// <response code="404">Kisi veya kayit yok ya da kapsam disi.</response>
    /// <response code="422">Yeni secilen yakinlik tanimi yok veya pasif.</response>
    [HttpPut("{contactId:guid}")]
    [HasPermission(Permissions.PersonUpdate)]
    [ProducesResponseType<EmergencyContactResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeAsync(Guid personId, Guid contactId, EmergencyContactRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var contact = await _contacts.ChangeAsync(personId, contactId, request.ToInput(), cancellationToken);
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
/// <param name="RelationshipId">Yakinlik taniminin dis kimligi (aktif tanimlar: <c>GET /api/v1/personnel/emergency-contact-relationships</c>).</param>
/// <param name="FirstName">Ad (en fazla 100 karakter).</param>
/// <param name="LastName">Soyad (en fazla 100 karakter).</param>
/// <param name="MobilePhone">Cep telefonu; 5 ile baslayan 10 hane, bastaki 0 veya 90 kabul edilir (SYG-KMLK-009).</param>
public sealed record EmergencyContactRequest(Guid? RelationshipId, string? FirstName, string? LastName, string? MobilePhone)
{
    /// <summary>Dogrulanmis bicim; dogrulayici gecerliligi once denetler.</summary>
    internal EmergencyContactInput ToInput() =>
        EmergencyContactInput.Parse(RelationshipId, FirstName, LastName, MobilePhone).Input!;
}

/// <summary>Yakinlik secenegi.</summary>
/// <param name="Id">Tanimin dis kimligi.</param>
/// <param name="Name">Ad.</param>
public sealed record RelationshipOptionResponse(Guid Id, string Name);

/// <summary>Acil durum kisisi.</summary>
/// <param name="Id">Dis kimlik.</param>
/// <param name="RelationshipId">Yakinlik taniminin dis kimligi.</param>
/// <param name="Relationship">Yakinlik taniminin adi; tanim pasife alinmis olsa da gorunur.</param>
/// <param name="FirstName">Ad.</param>
/// <param name="LastName">Soyad.</param>
/// <param name="MobilePhone">Cep telefonu, <c>5XXXXXXXXX</c>.</param>
public sealed record EmergencyContactResponse(Guid Id, Guid RelationshipId, string Relationship, string FirstName, string LastName, string MobilePhone)
{
    /// <summary>Okunan kayittan yanit.</summary>
    public static EmergencyContactResponse From(EmergencyContactRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return new(record.Id, record.RelationshipId, record.Relationship, record.FirstName, record.LastName, record.MobilePhone);
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
            var (_, errors) = EmergencyContactInput.Parse(request.RelationshipId, request.FirstName, request.LastName, request.MobilePhone);
            foreach (var (field, message) in errors)
            {
                context.AddFailure(field, message);
            }
        });
    }
}
