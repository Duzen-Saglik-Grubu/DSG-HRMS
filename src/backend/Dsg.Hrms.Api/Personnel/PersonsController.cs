using System.Text.Json.Serialization;
using Dsg.Hrms.Application.Personnel.Cards;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Personnel;

/// <summary>
/// Kisi karti ve "Profilim" (ADR-0019; SYG-PERS-044, 050, 051, 052, 053, 057).
/// </summary>
/// <remarks>
/// <para>
/// Uclar yalnizca oturum ister; kimin kimi gorecegi izin listesinden degil KAPSAMDAN gelir
/// (<c>personnel.person.view</c> varsa tum personel, yoksa yalnizca kendisi). Kapsam disi
/// kisi ile var olmayan kisi ayni <c>404</c> govdesini alir (SYG-PERS-051).
/// </para>
/// <para>
/// Her kart acilisi erisim kaydina yazilir (SYG-PERS-057).
/// </para>
/// </remarks>
[ApiController]
[Route("api/v1/personnel/persons")]
[Produces("application/json")]
[Authorize]
public sealed class PersonsController : ControllerBase
{
    private readonly PersonCardService _cards;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonsController(PersonCardService cards)
    {
        _cards = cards;
    }

    /// <summary>Kullanicinin kendi karti ("Profilim"). Izin gerektirmez.</summary>
    /// <response code="200">Kart.</response>
    /// <response code="404">Hesaba bagli kisi kaydi yok.</response>
    [HttpGet("me")]
    [ProducesResponseType<PersonCardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<PersonCardResponse> GetOwnAsync(CancellationToken cancellationToken) =>
        PersonCardResponse.From(await _cards.GetOwnAsync(cancellationToken));

    /// <summary>Kisinin karti.</summary>
    /// <response code="200">Kart.</response>
    /// <response code="404">Kisi yok veya kullanicinin kapsami disinda (ikisi ayirt edilemez).</response>
    [HttpGet("{personId:guid}")]
    [ProducesResponseType<PersonCardResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<PersonCardResponse> GetAsync(Guid personId, CancellationToken cancellationToken) =>
        PersonCardResponse.From(await _cards.GetAsync(personId, cancellationToken));
}

/// <summary>Kisi karti.</summary>
/// <param name="PersonId">Dis kimlik.</param>
/// <param name="FirstName">Ad (LOGO'daki gibi).</param>
/// <param name="LastName">Soyad.</param>
/// <param name="NationalId">TCKN; tam degeri gorme izni yoksa <c>123******01</c> bicimde maskeli.</param>
/// <param name="IsNationalIdMasked">TCKN maskeli mi.</param>
/// <param name="BirthDate">Dogum tarihi.</param>
/// <param name="Age">Yas.</param>
/// <param name="Email">Kurumsal e-posta; kayitli degilse bos.</param>
/// <param name="MobilePhone">Cep telefonu; kayitli degilse bos.</param>
/// <param name="IsSelf">Kart kullanicinin kendisine mi ait.</param>
/// <param name="Employments">Istihdamlar; once aktifler, ise giris tarihi yeniden eskiye.</param>
/// <param name="Gender">Cinsiyet; LOGO'da tanimsizsa bos.</param>
/// <param name="PreviousSurname">Onceki soyadi; yalnizca kadin personelde ve LOGO'da doluysa.</param>
/// <param name="MaritalStatus">Medeni hal; LOGO'da satiri yoksa bos.</param>
/// <param name="EducationName">Egitim; kayitli degilse bos.</param>
/// <param name="Sensitive">
/// Korumali alanlar. Yalnizca gorme hakki olan kullaniciya (<c>personnel.person.view_sensitive</c>
/// veya kisinin kendisi) yazilir; digerlerinin yanitinda alan HIC YOKTUR (SYG-PERS-052).
/// </param>
public sealed record PersonCardResponse(
    Guid PersonId,
    string FirstName,
    string LastName,
    string NationalId,
    bool IsNationalIdMasked,
    DateOnly BirthDate,
    int Age,
    string? Email,
    string? MobilePhone,
    bool IsSelf,
    IReadOnlyList<EmploymentCardResponse> Employments,
    GenderKind? Gender,
    string? PreviousSurname,
    MaritalStatusKind? MaritalStatus,
    string? EducationName,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] SensitiveResponse? Sensitive)
{
    /// <summary>Uygulama katmanindaki karttan yanit kurar.</summary>
    public static PersonCardResponse From(PersonCard card)
    {
        ArgumentNullException.ThrowIfNull(card);

        return new PersonCardResponse(
            card.PersonId,
            card.FirstName,
            card.LastName,
            card.NationalId,
            card.IsNationalIdMasked,
            card.BirthDate,
            card.Age,
            card.Email,
            card.MobilePhone,
            card.IsSelf,
            [.. card.Employments.Select(e => new EmploymentCardResponse(
                e.RegistryCode,
                e.CompanyName,
                e.HireDate,
                e.TerminationDate,
                e.IsActive,
                new SeniorityResponse(e.Seniority.Years, e.Seniority.Months, e.Seniority.Days, e.Seniority.Text)))],
            card.Gender switch
            {
                Domain.Personnel.Gender.Male => GenderKind.Male,
                Domain.Personnel.Gender.Female => GenderKind.Female,
                _ => null,
            },
            card.PreviousSurname,
            card.MaritalStatus switch
            {
                Domain.Personnel.MaritalStatus.Married => MaritalStatusKind.Married,
                Domain.Personnel.MaritalStatus.NotMarried => MaritalStatusKind.NotMarried,
                _ => null,
            },
            card.EducationName,
            card.Sensitive is { } sensitive ? new SensitiveResponse(sensitive.Address) : null);
    }
}

/// <summary>Kartin korumali alanlari (ADR-0019 §2).</summary>
/// <param name="Address">Adres; kayitli degilse bos ("Kayitli degil").</param>
public sealed record SensitiveResponse(string? Address);

/// <summary>Cinsiyet.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<GenderKind>))]
public enum GenderKind
{
    /// <summary>Erkek.</summary>
    [JsonStringEnumMemberName("male")]
    Male = 1,

    /// <summary>Kadin.</summary>
    [JsonStringEnumMemberName("female")]
    Female = 2,
}

/// <summary>Medeni hal.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MaritalStatusKind>))]
public enum MaritalStatusKind
{
    /// <summary>Evli.</summary>
    [JsonStringEnumMemberName("married")]
    Married = 1,

    /// <summary>Bekar.</summary>
    [JsonStringEnumMemberName("notMarried")]
    NotMarried = 2,
}

/// <summary>Kartta bir istihdam.</summary>
/// <param name="RegistryCode">Sicil numarasi.</param>
/// <param name="CompanyName">Firma.</param>
/// <param name="HireDate">Ise giris tarihi.</param>
/// <param name="TerminationDate">Isten cikis tarihi; aktifse bos.</param>
/// <param name="IsActive">Istihdam suruyor mu.</param>
/// <param name="Seniority">Kidem suresi.</param>
public sealed record EmploymentCardResponse(
    string RegistryCode,
    string CompanyName,
    DateOnly HireDate,
    DateOnly? TerminationDate,
    bool IsActive,
    SeniorityResponse Seniority);

/// <summary>Kidem suresi (SYG-PERS-039).</summary>
/// <param name="Years">Tam yil.</param>
/// <param name="Months">Yildan arta kalan tam ay.</param>
/// <param name="Days">Aydan arta kalan gun.</param>
/// <param name="Text">Ekran metni: "X Yil Y Ay Z Gun"; sifir olan bas birimler yazilmaz.</param>
public sealed record SeniorityResponse(int Years, int Months, int Days, string Text);
