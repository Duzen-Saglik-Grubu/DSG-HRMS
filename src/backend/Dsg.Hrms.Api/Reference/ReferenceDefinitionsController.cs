using System.Text.Json.Serialization;
using Dsg.Hrms.Api.Identity;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Reference;
using Dsg.Hrms.Domain.Reference;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Reference;

/// <summary>
/// Referans tanimlari (ADR-0022 §1; SYG-PERS-072): unvan, gorev ve kan grubu.
/// </summary>
/// <remarks>
/// Tanimlar kisisel veri degildir; erisim kaydi uretilmez. Degisiklikler denetim izine yazilir.
/// Kan grubu tanimlari sistem tanimidir ve degistirilemez (<c>KR-129</c>): istek <c>422</c> alir.
/// </remarks>
[ApiController]
[Route("api/v1/reference/definitions")]
[Produces("application/json")]
public sealed class ReferenceDefinitionsController : ControllerBase
{
    private readonly ReferenceDefinitionService _definitions;

    /// <summary>Yeni ornek olusturur.</summary>
    public ReferenceDefinitionsController(ReferenceDefinitionService definitions)
    {
        _definitions = definitions;
    }

    /// <summary>Turun tanimlari; varsayilan olarak yalnizca aktifler (yeni secim icin).</summary>
    /// <response code="200">Tanimlar, siraya gore.</response>
    /// <response code="403">Yetki yok (<c>reference.definition.view</c>).</response>
    [HttpGet]
    [HasPermission(Permissions.ReferenceView)]
    [ProducesResponseType<IReadOnlyList<ReferenceDefinitionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IReadOnlyList<ReferenceDefinitionResponse>> ListAsync(
        [FromQuery] ReferenceTypeKind type,
        [FromQuery] bool includeInactive,
        CancellationToken cancellationToken) =>
        [.. (await _definitions.ListAsync((ReferenceType)type, includeInactive, cancellationToken)).Select(ReferenceDefinitionResponse.From)];

    /// <summary>Yeni tanim ekler. Unvan kodu zorunludur (LOGO kodu); gorev kodu verilmezse uretilir.</summary>
    /// <response code="201">Eklenen tanim.</response>
    /// <response code="400">Ad bos.</response>
    /// <response code="403">Yetki yok (<c>reference.definition.update</c>).</response>
    /// <response code="422">Kod kullaniliyor, unvan kodu yok veya tur kan grubu.</response>
    [HttpPost]
    [HasPermission(Permissions.ReferenceUpdate)]
    [ProducesResponseType<ReferenceDefinitionResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> CreateAsync(CreateReferenceDefinitionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var definition = await _definitions.CreateAsync((ReferenceType)request.Type, request.Code, request.Name, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ReferenceDefinitionResponse.From(definition));
    }

    /// <summary>Tanimin adini degistirir.</summary>
    /// <response code="204">Ad degisti.</response>
    /// <response code="400">Ad bos.</response>
    /// <response code="403">Yetki yok (<c>reference.definition.update</c>).</response>
    /// <response code="404">Tanim yok.</response>
    /// <response code="422">Sistem tanimi.</response>
    [HttpPut("{id:guid}/name")]
    [HasPermission(Permissions.ReferenceUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RenameAsync(Guid id, RenameReferenceDefinitionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _definitions.RenameAsync(id, request.Name, cancellationToken);
        return NoContent();
    }

    /// <summary>Tanimi pasife alir; gecmis kayitlarda adiyla gorunmeye devam eder.</summary>
    /// <response code="204">Tanim pasif.</response>
    /// <response code="403">Yetki yok (<c>reference.definition.update</c>).</response>
    /// <response code="404">Tanim yok.</response>
    /// <response code="422">Sistem tanimi veya zaten pasif.</response>
    [HttpPost("{id:guid}/deactivation")]
    [HasPermission(Permissions.ReferenceUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        await _definitions.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}

/// <summary>Referans tanim turu.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ReferenceTypeKind>))]
public enum ReferenceTypeKind
{
    /// <summary>Unvan.</summary>
    [JsonStringEnumMemberName("title")]
    Title = 1,

    /// <summary>Gorev.</summary>
    [JsonStringEnumMemberName("duty")]
    Duty = 2,

    /// <summary>Kan grubu (sistem tanimi).</summary>
    [JsonStringEnumMemberName("bloodGroup")]
    BloodGroup = 3,
}

/// <summary>Referans tanimi.</summary>
/// <param name="Id">Dis kimlik.</param>
/// <param name="Type">Tur.</param>
/// <param name="Code">Kod.</param>
/// <param name="Name">Ad.</param>
/// <param name="SortOrder">Sira.</param>
/// <param name="IsActive">Yeni secimde listelenir mi.</param>
/// <param name="IsSystem">Sistem tanimi mi; oyleyse degistirilemez.</param>
public sealed record ReferenceDefinitionResponse(
    Guid Id,
    ReferenceTypeKind Type,
    string Code,
    string Name,
    int SortOrder,
    bool IsActive,
    bool IsSystem)
{
    /// <summary>Alan modelinden yanit kurar.</summary>
    public static ReferenceDefinitionResponse From(ReferenceDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new(definition.PublicId, (ReferenceTypeKind)definition.Type, definition.Code, definition.Name, definition.SortOrder, definition.IsActive, definition.IsSystem);
    }
}

/// <summary>Yeni tanim istegi.</summary>
/// <param name="Type">Tur (unvan veya gorev).</param>
/// <param name="Code">Kod; unvanda zorunlu, gorevde istege bagli.</param>
/// <param name="Name">Ad.</param>
public sealed record CreateReferenceDefinitionRequest(ReferenceTypeKind Type, string? Code, string Name);

/// <summary>Ad degistirme istegi.</summary>
/// <param name="Name">Yeni ad.</param>
public sealed record RenameReferenceDefinitionRequest(string Name);

/// <summary>Yeni tanim isteginin dogrulamasi.</summary>
public sealed class CreateReferenceDefinitionRequestValidator : AbstractValidator<CreateReferenceDefinitionRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public CreateReferenceDefinitionRequestValidator()
    {
        RuleFor(r => r.Type).IsInEnum().WithMessage("Tanım türünü seçin.");
        RuleFor(r => r.Name).NotEmpty().WithMessage("Adı girin.").MaximumLength(200).WithMessage("Ad en fazla 200 karakter olabilir.");
        RuleFor(r => r.Code).MaximumLength(20).WithMessage("Kod en fazla 20 karakter olabilir.");
    }
}

/// <summary>Ad degistirme isteginin dogrulamasi.</summary>
public sealed class RenameReferenceDefinitionRequestValidator : AbstractValidator<RenameReferenceDefinitionRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public RenameReferenceDefinitionRequestValidator()
    {
        RuleFor(r => r.Name).NotEmpty().WithMessage("Adı girin.").MaximumLength(200).WithMessage("Ad en fazla 200 karakter olabilir.");
    }
}
