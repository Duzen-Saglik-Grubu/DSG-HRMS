using System.Text.Json.Serialization;
using Dsg.Hrms.Api.Identity;
using Dsg.Hrms.Application.Common.Paging;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Settings;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Settings;

/// <summary>
/// En kucuk parametre ekrani: yalnizca T3 parametreleri (SYG-KMLK-076).
/// </summary>
/// <remarks>
/// <para>
/// Deger turune ve araligina gore dogrulanir; gecersiz deger kaydedilmez. Degisiklik en gec
/// 1 dakika icinde etkili olur ve eski/yeni degerle denetim izine yazilir (SYG-KMLK-075).
/// </para>
/// <para>
/// Sir niteligindeki parametreler YALNIZCA YAZILABILIR: deger yanitta hicbir zaman donmez,
/// yalnizca tanimli olup olmadigi soylenir (<c>KR-071</c>). Parametre ekraninin tamami Y4
/// kapsamindadir ve bu ucu genisletir.
/// </para>
/// </remarks>
[ApiController]
[Route("api/v1/system/parameters")]
[Produces("application/json")]
public sealed class SystemParametersController : ControllerBase
{
    private readonly ISystemParameterEditor _editor;

    /// <summary>Yeni ornek olusturur.</summary>
    public SystemParametersController(ISystemParameterEditor editor)
    {
        _editor = editor;
    }

    /// <summary>T3 parametrelerini katalog sirasiyla, gecerli degerleriyle listeler.</summary>
    /// <remarks>Tum listeleme uclari gibi sayfalidir (ADR-0010 §4); katalog tek sayfaya sigar.</remarks>
    /// <response code="200">Parametreler.</response>
    /// <response code="400">Gecersiz sayfa.</response>
    /// <response code="403">Yetki yok (<c>system.parameter.view</c>).</response>
    [HttpGet]
    [HasPermission(Permissions.ParameterView)]
    [ProducesResponseType<PagedResponse<ParameterResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<PagedResponse<ParameterResponse>> ListAsync([FromQuery] ParameterListRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var parameters = await _editor.ListAsync(cancellationToken);
        var page = new PageRequest(request.Page, request.PageSize);
        var result = new PagedResult<ParameterView>([.. parameters.Skip(page.Skip).Take(page.PageSize)], page.Page, page.PageSize, parameters.Count);
        return PagedResponse.From(result, ToResponse);
    }

    /// <summary>Parametrenin degerini degistirir.</summary>
    /// <response code="204">Kaydedildi; en gec 1 dakika icinde etkili olur.</response>
    /// <response code="400">Deger bos.</response>
    /// <response code="403">Yetki yok (<c>system.parameter.update</c>).</response>
    /// <response code="404">Parametre katalogda yok.</response>
    /// <response code="422">Deger turune veya araligina uymuyor; hicbir sey kaydedilmedi.</response>
    [HttpPut("{key}")]
    [HasPermission(Permissions.ParameterUpdate)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateAsync(string key, UpdateParameterRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _editor.UpdateAsync(key, request.Value, cancellationToken);
        return NoContent();
    }

    private static ParameterResponse ToResponse(ParameterView view) => new(
        view.Key,
        view.Description,
        view.Type switch
        {
            ParameterType.Number => ParameterKind.Number,
            ParameterType.Toggle => ParameterKind.Toggle,
            ParameterType.List => ParameterKind.List,
            ParameterType.Secret => ParameterKind.Secret,
            _ => ParameterKind.Text,
        },
        view.Min,
        view.Max,
        view.Type == ParameterType.Secret ? null : view.Value,
        view.IsSet,
        view.Source switch
        {
            ParameterSource.Default => ParameterSourceKind.Default,
            ParameterSource.Configuration => ParameterSourceKind.Configuration,
            ParameterSource.Database => ParameterSourceKind.Database,
            _ => ParameterSourceKind.None,
        });
}

/// <summary>Parametre.</summary>
/// <param name="Key">Katalog kimligi (orn. <c>PRM-KML-03</c>).</param>
/// <param name="Description">Aciklama (istemci kendi Turkce etiketini kullanir).</param>
/// <param name="Type">Tur.</param>
/// <param name="Min">Tam sayi icin alt sinir.</param>
/// <param name="Max">Tam sayi icin ust sinir.</param>
/// <param name="Value">Gecerli deger. Sir parametrede DAIMA bos (<c>KR-071</c>).</param>
/// <param name="IsSet">Deger herhangi bir kaynakta tanimli mi.</param>
/// <param name="Source">Degerin geldigi kaynak.</param>
public sealed record ParameterResponse(
    string Key,
    string Description,
    ParameterKind Type,
    int? Min,
    int? Max,
    string? Value,
    bool IsSet,
    ParameterSourceKind Source);

/// <summary>Parametre turu.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ParameterKind>))]
public enum ParameterKind
{
    /// <summary>Tam sayi.</summary>
    [JsonStringEnumMemberName("number")]
    Number = 1,

    /// <summary>Acik/kapali.</summary>
    [JsonStringEnumMemberName("toggle")]
    Toggle = 2,

    /// <summary>Metin.</summary>
    [JsonStringEnumMemberName("text")]
    Text = 3,

    /// <summary>Virgulle ayrilmis liste.</summary>
    [JsonStringEnumMemberName("list")]
    List = 4,

    /// <summary>Sir; yalnizca yazilabilir.</summary>
    [JsonStringEnumMemberName("secret")]
    Secret = 5,
}

/// <summary>Degerin kaynagi.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ParameterSourceKind>))]
public enum ParameterSourceKind
{
    /// <summary>Tanimli degil.</summary>
    [JsonStringEnumMemberName("none")]
    None = 0,

    /// <summary>Katalog varsayilani.</summary>
    [JsonStringEnumMemberName("default")]
    Default = 1,

    /// <summary>Yapilandirma veya ortam degiskeni.</summary>
    [JsonStringEnumMemberName("configuration")]
    Configuration = 2,

    /// <summary>Parametre ekranindan girilen deger.</summary>
    [JsonStringEnumMemberName("database")]
    Database = 3,
}

/// <summary>Parametre listesi istegi.</summary>
public sealed class ParameterListRequest
{
    /// <summary>Sayfa (1'den baslar).</summary>
    [FromQuery(Name = "page")]
    public int Page { get; init; } = 1;

    /// <summary>Sayfa boyutu (1–100).</summary>
    [FromQuery(Name = "pageSize")]
    public int PageSize { get; init; } = PagedResult<object>.MaxPageSize;
}

/// <summary>Parametre listesi istegi dogrulamasi.</summary>
public sealed class ParameterListRequestValidator : AbstractValidator<ParameterListRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public ParameterListRequestValidator()
    {
        RuleFor(r => r.Page).GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası 1 veya daha büyük olmalıdır.");
        RuleFor(r => r.PageSize)
            .InclusiveBetween(1, PagedResult<object>.MaxPageSize)
            .WithMessage($"Sayfa boyutu 1 ile {PagedResult<object>.MaxPageSize} arasında olmalıdır.");
    }
}

/// <summary>Parametre degisikligi.</summary>
/// <param name="Value">Yeni deger; turune gore dogrulanir.</param>
public sealed record UpdateParameterRequest(string Value)
{
    /// <inheritdoc />
    /// <remarks>Deger bir sir olabilir; nesnenin metin hali icerik tasimaz.</remarks>
    public override string ToString() => nameof(UpdateParameterRequest);
}

/// <summary>Parametre degisikligi dogrulamasi. Tur ve aralik denetimi katalogdadir.</summary>
public sealed class UpdateParameterRequestValidator : AbstractValidator<UpdateParameterRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public UpdateParameterRequestValidator()
    {
        RuleFor(r => r.Value)
            .NotNull().WithMessage("Değer girin.")
            .MaximumLength(2000).WithMessage("Değer en fazla 2000 karakter olabilir.");
    }
}
