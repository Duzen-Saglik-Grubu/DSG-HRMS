using System.Text.Json.Serialization;
using Dsg.Hrms.Api.Identity;
using Dsg.Hrms.Application.Common.Reports;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Personnel.Export;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Personnel;

/// <summary>
/// Personel listesinin disa aktarilmasi (PG-PERS-29, 30, 42; SYG-PERS-032, 033; ADR-0014).
/// </summary>
/// <remarks>
/// <para>
/// Iki uc da <c>personnel.person.export</c> ister; goruntuleme izni disa aktarma hakki vermez.
/// Suzgecler liste ucuyla aynidir; dosyadaki kisiler ekrandaki listeyle ayni kume ve siradadir,
/// sayfalama yoktur.
/// </para>
/// <para>
/// Her aktarim erisim kaydina yazilir (SYG-PERS-033).
/// </para>
/// </remarks>
[ApiController]
[Route("api/v1/personnel/persons/export")]
public sealed class PersonExportController : ControllerBase
{
    private readonly PersonExportService _export;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonExportController(PersonExportService export)
    {
        _export = export;
    }

    /// <summary>
    /// Kullanicinin secebilecegi alanlar (PG-PERS-42). Gorunurluk politikasindan gelir: adres ve
    /// kan grubu yalnizca <c>personnel.person.view_sensitive</c> iznine sunulur.
    /// </summary>
    /// <response code="200">Alanlar, dosyadaki sirada.</response>
    /// <response code="403">Disa aktarma izni yok.</response>
    [HttpGet("fields")]
    [HasPermission(Permissions.PersonExport)]
    [Produces("application/json")]
    [ProducesResponseType<IReadOnlyList<PersonExportFieldResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IReadOnlyList<PersonExportFieldResponse>> FieldsAsync(CancellationToken cancellationToken) =>
        [.. (await _export.GetFieldsAsync(cancellationToken)).Select(PersonExportFieldResponse.From)];

    /// <summary>
    /// Suzgece uyan butun kisileri Excel veya PDF olarak indirir. Ad ve soyad her satirda vardir;
    /// diger sutunlar <c>field</c> ile secilir. En fazla 50.000 kisi.
    /// </summary>
    /// <response code="200">Dosya.</response>
    /// <response code="400">Gecersiz istek, izin disi alan veya suzgec, Excel'de fotograf ya da satir siniri asildi.</response>
    /// <response code="403">Disa aktarma izni yok.</response>
    [HttpGet]
    [HasPermission(Permissions.PersonExport)]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/pdf")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    public async Task<FileContentResult> ExportAsync([FromQuery] PersonExportRequestModel request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var file = await _export.ExportAsync(
            new PersonExportRequest(
                request.Format == ExportFormatKind.Pdf ? ReportFormat.Pdf : ReportFormat.Excel,
                [.. request.Fields.Select(f => (PersonExportField)f)],
                request.Q,
                request.ToStatusFilter(),
                request.ToSort(),
                request.IsDescending(),
                request.ToFilters()),
            cancellationToken);

        // Kisisel veri iceren dosya ara bellege alinmaz (ADR-0014 §4).
        Response.Headers.CacheControl = "no-store";
        return File(file.Content, file.ContentType, file.FileName);
    }
}

/// <summary>Disa aktarma istegi: liste suzgecleri, bicim ve alanlar.</summary>
public sealed class PersonExportRequestModel : PersonFilterRequest
{
    /// <summary>Dosya bicimi.</summary>
    [FromQuery(Name = "format")]
    public ExportFormatKind Format { get; init; } = ExportFormatKind.Excel;

    /// <summary>Ad ve soyada ek olarak aktarilacak alanlar (coklu).</summary>
    [FromQuery(Name = "field")]
    public PersonExportFieldName[] Fields { get; init; } = [];

    /// <inheritdoc />
    public override string ToString() => nameof(PersonExportRequestModel);
}

/// <summary>Disa aktarma isteginin dogrulamasi.</summary>
public sealed class PersonExportRequestModelValidator : PersonFilterRequestValidator<PersonExportRequestModel>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public PersonExportRequestModelValidator()
    {
        RuleFor(r => r.Format).IsInEnum().WithMessage("Geçersiz dosya biçimi.");
        RuleForEach(r => r.Fields).IsInEnum().WithMessage("Geçersiz alan.");
        RuleFor(r => r.Fields)
            .Must(fields => fields.Distinct().Count() == fields.Length)
            .WithMessage("Bir alan birden fazla seçilmiş.");
    }
}

/// <summary>Secilebilir alan.</summary>
/// <param name="Field">Istekte <c>field</c> olarak gonderilen ad.</param>
/// <param name="Label">Basliktaki ad.</param>
/// <param name="PdfOnly">Yalnizca PDF'te secilebilir (fotograf).</param>
public sealed record PersonExportFieldResponse(PersonExportFieldName Field, string Label, bool PdfOnly)
{
    /// <summary>Uygulama katmanindaki tanimdan yanit kurar.</summary>
    public static PersonExportFieldResponse From(PersonExportFieldDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new((PersonExportFieldName)definition.Field, definition.Header, definition.PdfOnly);
    }
}

/// <summary>Dosya bicimi.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<ExportFormatKind>))]
public enum ExportFormatKind
{
    /// <summary>Excel (<c>.xlsx</c>).</summary>
    [JsonStringEnumMemberName("excel")]
    Excel = 1,

    /// <summary>PDF.</summary>
    [JsonStringEnumMemberName("pdf")]
    Pdf = 2,
}

/// <summary>Disa aktarilabilen alan; degerler uygulama katmanindaki <see cref="PersonExportField"/> ile aynidir.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PersonExportFieldName>))]
public enum PersonExportFieldName
{
    /// <summary>Sicil numarasi.</summary>
    [JsonStringEnumMemberName("registryCode")]
    RegistryCode = PersonExportField.RegistryCode,

    /// <summary>TCKN (izin yoksa maskeli).</summary>
    [JsonStringEnumMemberName("nationalId")]
    NationalId = PersonExportField.NationalId,

    /// <summary>Cinsiyet.</summary>
    [JsonStringEnumMemberName("gender")]
    Gender = PersonExportField.Gender,

    /// <summary>Dogum tarihi.</summary>
    [JsonStringEnumMemberName("birthDate")]
    BirthDate = PersonExportField.BirthDate,

    /// <summary>Yas.</summary>
    [JsonStringEnumMemberName("age")]
    Age = PersonExportField.Age,

    /// <summary>Onceki soyadi.</summary>
    [JsonStringEnumMemberName("previousSurname")]
    PreviousSurname = PersonExportField.PreviousSurname,

    /// <summary>Medeni hal.</summary>
    [JsonStringEnumMemberName("maritalStatus")]
    MaritalStatus = PersonExportField.MaritalStatus,

    /// <summary>Egitim.</summary>
    [JsonStringEnumMemberName("education")]
    Education = PersonExportField.Education,

    /// <summary>Kan grubu.</summary>
    [JsonStringEnumMemberName("bloodGroup")]
    BloodGroup = PersonExportField.BloodGroup,

    /// <summary>E-posta.</summary>
    [JsonStringEnumMemberName("email")]
    Email = PersonExportField.Email,

    /// <summary>Telefon.</summary>
    [JsonStringEnumMemberName("phone")]
    Phone = PersonExportField.MobilePhone,

    /// <summary>Adres.</summary>
    [JsonStringEnumMemberName("address")]
    Address = PersonExportField.Address,

    /// <summary>Calisma durumu.</summary>
    [JsonStringEnumMemberName("employmentStatus")]
    EmploymentStatus = PersonExportField.EmploymentStatus,

    /// <summary>Firma.</summary>
    [JsonStringEnumMemberName("company")]
    Company = PersonExportField.Company,

    /// <summary>Sehir.</summary>
    [JsonStringEnumMemberName("city")]
    City = PersonExportField.City,

    /// <summary>Sube.</summary>
    [JsonStringEnumMemberName("branch")]
    Branch = PersonExportField.Branch,

    /// <summary>Birim.</summary>
    [JsonStringEnumMemberName("department")]
    Department = PersonExportField.Department,

    /// <summary>Unvan.</summary>
    [JsonStringEnumMemberName("title")]
    Title = PersonExportField.Title,

    /// <summary>Gorev.</summary>
    [JsonStringEnumMemberName("duty")]
    Duty = PersonExportField.Duty,

    /// <summary>Statu.</summary>
    [JsonStringEnumMemberName("statusCode")]
    StatusCode = PersonExportField.StatusCode,

    /// <summary>SGK meslek kodu.</summary>
    [JsonStringEnumMemberName("occupationCode")]
    OccupationCode = PersonExportField.OccupationCode,

    /// <summary>SGK meslek.</summary>
    [JsonStringEnumMemberName("occupationName")]
    OccupationName = PersonExportField.OccupationName,

    /// <summary>Ise giris tarihi.</summary>
    [JsonStringEnumMemberName("hireDate")]
    HireDate = PersonExportField.HireDate,

    /// <summary>Isten cikis tarihi.</summary>
    [JsonStringEnumMemberName("terminationDate")]
    TerminationDate = PersonExportField.TerminationDate,

    /// <summary>Kidem suresi.</summary>
    [JsonStringEnumMemberName("seniority")]
    Seniority = PersonExportField.Seniority,

    /// <summary>Fotograf (yalnizca PDF).</summary>
    [JsonStringEnumMemberName("photo")]
    Photo = PersonExportField.Photo,
}
