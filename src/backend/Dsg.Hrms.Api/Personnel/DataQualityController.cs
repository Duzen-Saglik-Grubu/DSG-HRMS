using System.Text.Json;
using System.Text.Json.Serialization;
using Dsg.Hrms.Api.Identity;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Personnel.Quality;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Personnel;

/// <summary>
/// Veri kalitesi raporu (PG-PERS-32, 33; SYG-PERS-070, 071; ADR-0003 §5).
/// </summary>
/// <remarks>
/// Gormek <c>personnel.quality.view</c>, Excel'e aktarmak <c>personnel.quality.export</c> ister.
/// Yanitta TCKN, e-posta ve telefon yoktur; IK karti LOGO'da sicil koduyla bulur.
/// </remarks>
[ApiController]
[Route("api/v1/personnel/quality")]
public sealed class DataQualityController : ControllerBase
{
    private readonly DataQualityService _quality;

    /// <summary>Yeni ornek olusturur.</summary>
    public DataQualityController(DataQualityService quality)
    {
        _quality = quality;
    }

    /// <summary>Son senkronizasyon, uyari turu sayilari ve suzgece uyan uyarilar (SYG-PERS-070).</summary>
    /// <response code="200">Rapor.</response>
    /// <response code="400">Gecersiz sayfa veya sayfa boyutu.</response>
    /// <response code="403">Yetki yok (<c>personnel.quality.view</c>).</response>
    [HttpGet]
    [HasPermission(Permissions.QualityView)]
    [Produces("application/json")]
    [ProducesResponseType<DataQualityReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<DataQualityReportResponse> GetAsync([FromQuery] DataQualityRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var report = await _quality.GetAsync(request.Issue?.ToDomain(), request.Page, request.PageSize, cancellationToken);
        return new DataQualityReportResponse(
            SyncRunResponse.From(report.LastRun),
            SyncRunResponse.From(report.WarningsRun),
            [.. report.Counts.OrderBy(c => c.Key).Select(c => new DataQualityCountResponse(DataQualityNames.Issue(c.Key), c.Value))],
            report.IneligibleCount,
            PagedResponse.From(report.Items, DataQualityItemResponse.From));
    }

    /// <summary>Uye olamayacak kisiler: aktif, hesabi yok, uyelik kosulunu karsilamiyor (SYG-PERS-071).</summary>
    /// <response code="200">Sayfalanmis liste.</response>
    /// <response code="400">Gecersiz sayfa veya sayfa boyutu.</response>
    /// <response code="403">Yetki yok (<c>personnel.quality.view</c>).</response>
    [HttpGet("ineligible")]
    [HasPermission(Permissions.QualityView)]
    [Produces("application/json")]
    [ProducesResponseType<PagedResponse<IneligiblePersonResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<PagedResponse<IneligiblePersonResponse>> ListIneligibleAsync([FromQuery] IneligibleListRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await _quality.ListIneligibleAsync(request.Page, request.PageSize, cancellationToken);
        return PagedResponse.From(result, IneligiblePersonResponse.From);
    }

    /// <summary>
    /// Raporu Excel'e aktarir: "Uyarılar" (tur suzgeciyle) ve "Üye olamayacak kişiler" sayfalari.
    /// Aktarim erisim kaydina yazilir (SYG-PERS-033).
    /// </summary>
    /// <response code="200">Excel dosyasi.</response>
    /// <response code="403">Yetki yok (<c>personnel.quality.export</c>).</response>
    [HttpGet("export")]
    [HasPermission(Permissions.QualityExport)]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<FileContentResult> ExportAsync([FromQuery] DataQualityExportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var file = await _quality.ExportAsync(request.Issue?.ToDomain(), cancellationToken);

        // Kisisel veri iceren dosya ara bellege alinmaz (ADR-0014 §4).
        Response.Headers.CacheControl = "no-store";
        return File(file.Content, file.ContentType, file.FileName);
    }
}

/// <summary>Uyari turu.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<DataQualityIssueName>))]
public enum DataQualityIssueName
{
    /// <summary>Kartta TCKN yok.</summary>
    [JsonStringEnumMemberName("missingNationalId")]
    MissingNationalId = 1,

    /// <summary>Karttaki TCKN gecersiz.</summary>
    [JsonStringEnumMemberName("invalidNationalId")]
    InvalidNationalId = 2,

    /// <summary>Kartta dogum tarihi yok.</summary>
    [JsonStringEnumMemberName("missingBirthDate")]
    MissingBirthDate = 3,

    /// <summary>Ne e-posta ne cep telefonu var.</summary>
    [JsonStringEnumMemberName("noContact")]
    NoContact = 4,

    /// <summary>Gecersiz bicimli cep telefonu.</summary>
    [JsonStringEnumMemberName("invalidMobilePhone")]
    InvalidMobilePhone = 5,

    /// <summary>Paylasilan e-posta.</summary>
    [JsonStringEnumMemberName("sharedEmail")]
    SharedEmail = 6,

    /// <summary>Unvani bos.</summary>
    [JsonStringEnumMemberName("missingTitle")]
    MissingTitle = 7,

    /// <summary>Acik gorevi yok.</summary>
    [JsonStringEnumMemberName("missingDuty")]
    MissingDuty = 8,
}

/// <summary>Uye olamama nedeni.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MembershipBlockerName>))]
public enum MembershipBlockerName
{
    /// <summary>Kurumsal e-postasi yok.</summary>
    [JsonStringEnumMemberName("noEmail")]
    NoEmail = 1,

    /// <summary>E-postasi kabul edilen alan adlarinda degil.</summary>
    [JsonStringEnumMemberName("domainNotAccepted")]
    DomainNotAccepted = 2,

    /// <summary>E-postasi paylasiliyor.</summary>
    [JsonStringEnumMemberName("sharedEmail")]
    SharedEmail = 4,
}

/// <summary>Rapor istegi.</summary>
public sealed class DataQualityRequest
{
    /// <summary>Uyari turu; verilmezse hepsi.</summary>
    public DataQualityIssueName? Issue { get; init; }

    /// <summary>Sayfa (1'den baslar).</summary>
    public int Page { get; init; } = 1;

    /// <summary>Sayfa boyutu (1–100).</summary>
    public int PageSize { get; init; } = 25;
}

/// <summary>Uye olamayacak kisiler istegi.</summary>
public sealed class IneligibleListRequest
{
    /// <summary>Sayfa (1'den baslar).</summary>
    public int Page { get; init; } = 1;

    /// <summary>Sayfa boyutu (1–100).</summary>
    public int PageSize { get; init; } = 25;
}

/// <summary>Aktarim istegi.</summary>
public sealed class DataQualityExportRequest
{
    /// <summary>Uyari turu; verilmezse hepsi. "Üye olamayacak kişiler" sayfasi suzulmez.</summary>
    public DataQualityIssueName? Issue { get; init; }
}

/// <summary>Senkronizasyon calismasinin ozeti.</summary>
/// <param name="Status">Sonuc (<c>succeeded</c>, <c>completedWithWarnings</c>, <c>failed</c>, <c>partiallyCompleted</c>).</param>
/// <param name="StartedAt">Baslangic.</param>
/// <param name="FinishedAt">Bitis.</param>
/// <param name="RecordsRead">Okunan kart sayisi.</param>
/// <param name="WarningCount">Uretilen uyari sayisi.</param>
public sealed record SyncRunResponse(string Status, DateTimeOffset StartedAt, DateTimeOffset? FinishedAt, int RecordsRead, int WarningCount)
{
    /// <summary>Uygulama katmaninin ozetini yanita cevirir.</summary>
    public static SyncRunResponse? From(DataQualityRun? run) =>
        run is null
            ? null
            : new SyncRunResponse(JsonNamingPolicy.CamelCase.ConvertName(run.Status.ToString()), run.StartedAt, run.FinishedAt, run.RecordsRead, run.WarningCount);
}

/// <summary>Bir uyari turunun sayisi.</summary>
/// <param name="Issue">Tur.</param>
/// <param name="Count">Sayi.</param>
public sealed record DataQualityCountResponse(DataQualityIssueName Issue, int Count);

/// <summary>Rapor ekrani.</summary>
/// <param name="LastRun">En son biten senkronizasyon; hic yoksa bos.</param>
/// <param name="WarningsRun">Kart uyarilarinin alindigi calisma; hic yoksa bos.</param>
/// <param name="Counts">Her uyari turunun sayisi (suzgecten bagimsiz).</param>
/// <param name="IneligibleCount">Uye olamayacak kisi sayisi.</param>
/// <param name="Items">Suzgece uyan uyarilar.</param>
public sealed record DataQualityReportResponse(
    SyncRunResponse? LastRun,
    SyncRunResponse? WarningsRun,
    IReadOnlyList<DataQualityCountResponse> Counts,
    int IneligibleCount,
    PagedResponse<DataQualityItemResponse> Items);

/// <summary>Bir uyari.</summary>
/// <param name="Issue">Tur.</param>
/// <param name="RegistryCode">Sicil kodu.</param>
/// <param name="PersonId">Kisinin dis kimligi; kisi olusturulmadiysa bos.</param>
/// <param name="FirstName">Ad.</param>
/// <param name="LastName">Soyad.</param>
/// <param name="Company">Firma.</param>
/// <param name="Branch">Sube.</param>
/// <param name="Department">Birim.</param>
public sealed record DataQualityItemResponse(
    DataQualityIssueName Issue,
    string RegistryCode,
    Guid? PersonId,
    string? FirstName,
    string? LastName,
    string? Company,
    string? Branch,
    string? Department)
{
    /// <summary>Uygulama katmaninin satirini yanita cevirir.</summary>
    public static DataQualityItemResponse From(DataQualityItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var s = item.Subject;
        return new DataQualityItemResponse(DataQualityNames.Issue(item.Issue), s.RegistryCode, s.PersonId, s.FirstName, s.LastName, s.Company, s.Branch, s.Department);
    }
}

/// <summary>Uye olamayacak kisi.</summary>
/// <param name="PersonId">Kisinin dis kimligi.</param>
/// <param name="RegistryCode">Esas istihdamin sicil kodu.</param>
/// <param name="FirstName">Ad.</param>
/// <param name="LastName">Soyad.</param>
/// <param name="Company">Firma.</param>
/// <param name="Branch">Sube.</param>
/// <param name="Department">Birim.</param>
/// <param name="Reasons">Nedenler.</param>
public sealed record IneligiblePersonResponse(
    Guid PersonId,
    string RegistryCode,
    string? FirstName,
    string? LastName,
    string? Company,
    string? Branch,
    string? Department,
    IReadOnlyList<MembershipBlockerName> Reasons)
{
    /// <summary>Uygulama katmaninin satirini yanita cevirir.</summary>
    public static IneligiblePersonResponse From(IneligiblePerson person)
    {
        ArgumentNullException.ThrowIfNull(person);

        var s = person.Subject;
        return new IneligiblePersonResponse(
            s.PersonId!.Value,
            s.RegistryCode,
            s.FirstName,
            s.LastName,
            s.Company,
            s.Branch,
            s.Department,
            [.. Enum.GetValues<MembershipBlocker>().Where(b => b != MembershipBlocker.None && person.Reasons.HasFlag(b)).Select(b => (MembershipBlockerName)(int)b)]);
    }
}

/// <summary>API ve uygulama adlari arasindaki eslemeler.</summary>
internal static class DataQualityNames
{
    public static DataQualityIssue ToDomain(this DataQualityIssueName issue) => (DataQualityIssue)(int)issue;

    public static DataQualityIssueName Issue(DataQualityIssue issue) => (DataQualityIssueName)(int)issue;
}
