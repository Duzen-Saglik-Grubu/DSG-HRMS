using System.Text.Json;
using System.Text.Json.Serialization;
using Dsg.Hrms.Api.Identity;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Personnel.Corrections;
using Dsg.Hrms.Domain.Personnel;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Personnel;

/// <summary>
/// "IK'ya bildir" duzeltme talepleri (PG-PERS-37; ADR-0022 §3; SYG-PERS-045…049).
/// </summary>
/// <remarks>
/// <para>
/// <c>me</c> uclari izin gerektirmez: kisi yalnizca KENDI adina talep acar ve kendi acik
/// taleplerini gorur; istekte kisi kimligi yoktur. Talepleri listelemek
/// <c>personnel.correction.view</c>, kapatmak <c>personnel.correction.update</c> ister.
/// </para>
/// <para>
/// Onerilen deger yalnizca acik talepte doner; kapatilan talepte veritabaninda da yoktur.
/// </para>
/// </remarks>
[ApiController]
[Route("api/v1/personnel/corrections")]
[Produces("application/json")]
public sealed class CorrectionsController : ControllerBase
{
    /// <summary>IK'nin talepler sayfasi; e-postadaki baglanti buraya gider.</summary>
    public const string RequestsPagePath = "/personnel/corrections";

    private readonly CorrectionService _corrections;
    private readonly IConfiguration _configuration;

    /// <summary>Yeni ornek olusturur.</summary>
    public CorrectionsController(CorrectionService corrections, IConfiguration configuration)
    {
        _corrections = corrections;
        _configuration = configuration;
    }

    /// <summary>Kendi acik taleplerim ve formun secenekleri.</summary>
    /// <response code="200">Acik talebi olan alanlar ve kan grubu secenekleri.</response>
    /// <response code="404">Hesaba bagli kisi kaydi yok.</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType<OwnCorrectionStateResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<OwnCorrectionStateResponse> GetOwnAsync(CancellationToken cancellationToken)
    {
        var state = await _corrections.GetOwnStateAsync(cancellationToken);
        return new OwnCorrectionStateResponse(
            [.. state.OpenFields.Select(CorrectionNames.Field)],
            [.. state.BloodGroups.Select(o => new CorrectionOptionResponse(o.Value, o.Label))]);
    }

    /// <summary>Kendi kartimdaki bir alan icin IK'ya duzeltme talebi gonderir (SYG-PERS-045).</summary>
    /// <response code="201">Talep olustu; IK'ya bildirim gitti.</response>
    /// <response code="400">Alan secilmedi veya deger alana gore gecersiz.</response>
    /// <response code="404">Hesaba bagli kisi kaydi yok.</response>
    /// <response code="409">Bu alan icin acik bir talep zaten var (AN-08).</response>
    [HttpPost("me")]
    [Authorize]
    [ProducesResponseType<CorrectionSubmittedResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> SubmitOwnAsync(SubmitCorrectionRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Dogrulayici gecerliligi denetledi; burada yalnizca kanonik bicim alinir.
        var field = request.Field!.Value.ToDomain();
        var value = CorrectionValue.Normalize(field, request.Value).Value!;

        var id = await _corrections.SubmitOwnAsync(field, value, RequestsPageUri(), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new CorrectionSubmittedResponse(id));
    }

    /// <summary>Talepleri listeler, en yeni once (SYG-PERS-048).</summary>
    /// <response code="200">Sayfalanmis liste.</response>
    /// <response code="400">Gecersiz sayfa veya sayfa boyutu.</response>
    /// <response code="403">Yetki yok (<c>personnel.correction.view</c>).</response>
    [HttpGet]
    [HasPermission(Permissions.CorrectionView)]
    [ProducesResponseType<PagedResponse<CorrectionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<PagedResponse<CorrectionResponse>> ListAsync([FromQuery] CorrectionListRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var status = request.Status switch
        {
            CorrectionStatusFilter.New => CorrectionStatus.New,
            CorrectionStatusFilter.Completed => CorrectionStatus.Completed,
            _ => (CorrectionStatus?)null,
        };

        var result = await _corrections.ListAsync(status, request.Page, request.PageSize, cancellationToken);
        return PagedResponse.From(result, CorrectionResponse.From);
    }

    /// <summary>Tek talep; bildirimden acilir.</summary>
    /// <response code="200">Talep.</response>
    /// <response code="403">Yetki yok (<c>personnel.correction.view</c>).</response>
    /// <response code="404">Talep yok.</response>
    [HttpGet("{requestId:guid}")]
    [HasPermission(Permissions.CorrectionView)]
    [ProducesResponseType<CorrectionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<CorrectionResponse> GetAsync(Guid requestId, CancellationToken cancellationToken) =>
        CorrectionResponse.From(await _corrections.GetAsync(requestId, cancellationToken));

    /// <summary>Talebi "Tamamlandi" olarak kapatir; onerilen deger silinir (SYG-PERS-048).</summary>
    /// <response code="200">Kapatilan talep (degersiz).</response>
    /// <response code="403">Yetki yok (<c>personnel.correction.update</c>).</response>
    /// <response code="404">Talep yok.</response>
    /// <response code="409">Talep zaten tamamlandi.</response>
    [HttpPost("{requestId:guid}/complete")]
    [HasPermission(Permissions.CorrectionUpdate)]
    [ProducesResponseType<CorrectionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<CorrectionResponse> CompleteAsync(Guid requestId, CancellationToken cancellationToken) =>
        CorrectionResponse.From(await _corrections.CompleteAsync(requestId, cancellationToken));

    /// <summary>
    /// Talepler sayfasinin adresi. <c>Identity:PublicBaseUrl</c> tanimliysa o kullanilir; boylece
    /// istek basligiyla oynanarak e-postaya baska bir site yazdirilamaz (davet baglantisi gibi).
    /// </summary>
    private Uri RequestsPageUri()
    {
        var configured = _configuration["Identity:PublicBaseUrl"];
        var baseUrl = string.IsNullOrWhiteSpace(configured) ? $"{Request.Scheme}://{Request.Host}" : configured.TrimEnd('/');
        return new Uri(baseUrl + RequestsPagePath);
    }
}

/// <summary>Bildirilebilen alan.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CorrectionFieldName>))]
public enum CorrectionFieldName
{
    /// <summary>Cep telefonu.</summary>
    [JsonStringEnumMemberName("mobilePhone")]
    MobilePhone = 1,

    /// <summary>E-posta.</summary>
    [JsonStringEnumMemberName("email")]
    Email = 2,

    /// <summary>Adres.</summary>
    [JsonStringEnumMemberName("address")]
    Address = 3,

    /// <summary>Egitim.</summary>
    [JsonStringEnumMemberName("education")]
    Education = 4,

    /// <summary>Medeni hal (<c>married</c> veya <c>notMarried</c>).</summary>
    [JsonStringEnumMemberName("maritalStatus")]
    MaritalStatus = 5,

    /// <summary>Kan grubu (secenek kodu).</summary>
    [JsonStringEnumMemberName("bloodGroup")]
    BloodGroup = 6,
}

/// <summary>Talep listesi durum suzgeci.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CorrectionStatusFilter>))]
public enum CorrectionStatusFilter
{
    /// <summary>Yeni (varsayilan).</summary>
    [JsonStringEnumMemberName("new")]
    New = 1,

    /// <summary>Tamamlandi.</summary>
    [JsonStringEnumMemberName("completed")]
    Completed = 2,

    /// <summary>Hepsi.</summary>
    [JsonStringEnumMemberName("all")]
    All = 3,
}

/// <summary>Talep gonderimi.</summary>
/// <param name="Field">Alan.</param>
/// <param name="Value">
/// Dogru deger: telefon <c>5XXXXXXXXX</c> (bosluk ve on ek kabul edilir), medeni hal
/// <c>married</c>/<c>notMarried</c>, kan grubu secenek kodu; adres ve egitim en fazla 500 karakter.
/// </param>
public sealed record SubmitCorrectionRequest(CorrectionFieldName? Field, string? Value);

/// <summary>Talep gonderiminin dogrulamasi (SYG-PERS-045).</summary>
public sealed class SubmitCorrectionRequestValidator : AbstractValidator<SubmitCorrectionRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public SubmitCorrectionRequestValidator()
    {
        RuleFor(r => r.Field).NotNull().WithMessage("Düzeltilecek alanı seçin.");

        RuleFor(r => r.Value)
            .Custom((value, context) =>
            {
                if (context.InstanceToValidate.Field is not { } field)
                {
                    return;
                }

                if (CorrectionValue.Normalize(field.ToDomain(), value).Error is { } error)
                {
                    context.AddFailure(nameof(SubmitCorrectionRequest.Value), error);
                }
            });
    }
}

/// <summary>Talep listesi istegi.</summary>
public sealed class CorrectionListRequest
{
    /// <summary>Durum; verilmezse "Yeni".</summary>
    public CorrectionStatusFilter Status { get; init; } = CorrectionStatusFilter.New;

    /// <summary>Sayfa (1'den baslar).</summary>
    public int Page { get; init; } = 1;

    /// <summary>Sayfa boyutu (1–100).</summary>
    public int PageSize { get; init; } = 20;
}

/// <summary>Kisinin "IK'ya bildir" formu icin durumu.</summary>
/// <param name="OpenFields">Acik talebi olan alanlar.</param>
/// <param name="BloodGroups">Kan grubu secenekleri.</param>
public sealed record OwnCorrectionStateResponse(IReadOnlyList<CorrectionFieldName> OpenFields, IReadOnlyList<CorrectionOptionResponse> BloodGroups);

/// <summary>Secim listesi ogesi.</summary>
/// <param name="Value">Gonderilecek deger.</param>
/// <param name="Label">Gorunen ad.</param>
public sealed record CorrectionOptionResponse(string Value, string Label);

/// <summary>Olusan talep.</summary>
/// <param name="Id">Talebin dis kimligi.</param>
public sealed record CorrectionSubmittedResponse(Guid Id);

/// <summary>IK'nin gordugu talep.</summary>
/// <param name="Id">Talebin dis kimligi.</param>
/// <param name="PersonId">Kisinin dis kimligi (kisi kartina baglanti).</param>
/// <param name="FirstName">Ad.</param>
/// <param name="LastName">Soyad.</param>
/// <param name="Field">Alan.</param>
/// <param name="ProposedValue">Onerilen deger (kan grubunda adi, medeni halde <c>married</c>/<c>notMarried</c>); tamamlanan talepte bos.</param>
/// <param name="Status">Durum (<c>new</c>, <c>completed</c>).</param>
/// <param name="SubmittedAt">Gonderim ani.</param>
/// <param name="CompletedAt">Kapatilma ani.</param>
public sealed record CorrectionResponse(
    Guid Id,
    Guid PersonId,
    string FirstName,
    string LastName,
    CorrectionFieldName Field,
    string? ProposedValue,
    string Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? CompletedAt)
{
    /// <summary>Uygulama katmaninin kaydini yanita cevirir.</summary>
    public static CorrectionResponse From(CorrectionRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        return new CorrectionResponse(
            record.Id,
            record.PersonId,
            record.FirstName,
            record.LastName,
            CorrectionNames.Field(record.Field),
            record.ProposedValue,
            JsonNamingPolicy.CamelCase.ConvertName(record.Status.ToString()),
            record.SubmittedAt,
            record.CompletedAt);
    }
}

/// <summary>API ve alan adlari arasindaki eslemeler.</summary>
internal static class CorrectionNames
{
    public static CorrectionField ToDomain(this CorrectionFieldName field) => (CorrectionField)(int)field;

    public static CorrectionFieldName Field(CorrectionField field) => (CorrectionFieldName)(int)field;
}
