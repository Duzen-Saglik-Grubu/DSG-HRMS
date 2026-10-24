using System.Text.Json.Serialization;
using Dsg.Hrms.Api.Identity;
using Dsg.Hrms.Application.Common.Paging;
using Dsg.Hrms.Application.Personnel.Cards;
using Dsg.Hrms.Application.Personnel.Listing;
using Dsg.Hrms.Domain.Personnel;
using FluentValidation;
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
    private readonly PersonListService _list;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonsController(PersonCardService cards, PersonListService list)
    {
        _cards = cards;
        _list = list;
    }

    /// <summary>
    /// Personel listesi: kart izgarasi ve tablo icin (SYG-PERS-022, 023, 027–031). Varsayilan olarak
    /// yalnizca aktif personel; kapsam disindaki kisiler listede yoktur.
    /// </summary>
    /// <response code="200">Sayfalanmis liste.</response>
    /// <response code="400">Gecersiz sayfa, siralama veya sayfa boyutu parametredeki en buyuk degerden fazla.</response>
    [HttpGet]
    [ProducesResponseType<PagedResponse<PersonListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<PagedResponse<PersonListItemResponse>> ListAsync([FromQuery] PersonListRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await _list.ListAsync(
            request.Q,
            (EmploymentStatusFilter)request.Status,
            request.Sort == PersonSortField.LastName ? PersonSort.LastName : PersonSort.FirstName,
            request.Order == SortOrder.Desc,
            request.Page,
            request.PageSize,
            request.ToFilters(),
            cancellationToken);

        return PagedResponse.From(result, PersonListItemResponse.From);
    }

    /// <summary>
    /// Suzgec secenekleri ve kullanicinin kullanabilecegi korumali suzgecler (SYG-PERS-024, 026).
    /// </summary>
    /// <response code="200">Secenekler.</response>
    [HttpGet("filter-options")]
    [ProducesResponseType<PersonFilterOptionsResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<PersonFilterOptionsResponse> FilterOptionsAsync(CancellationToken cancellationToken) =>
        PersonFilterOptionsResponse.From(await _list.GetFilterOptionsAsync(cancellationToken));

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

/// <summary>Liste istegi.</summary>
public sealed class PersonListRequest
{
    /// <summary>Hizli arama: ad, soyad, sicil; izinle TCKN. Turkce karakterden bagimsiz.</summary>
    [FromQuery(Name = "q")]
    public string? Q { get; init; }

    /// <summary>Calisma durumu (varsayilan aktif).</summary>
    [FromQuery(Name = "status")]
    public EmploymentStatusKind Status { get; init; } = EmploymentStatusKind.Active;

    /// <summary>Sayfa (1'den baslar).</summary>
    [FromQuery(Name = "page")]
    public int Page { get; init; } = 1;

    /// <summary>Sayfa boyutu; verilmezse parametredeki varsayilan (24).</summary>
    [FromQuery(Name = "pageSize")]
    public int? PageSize { get; init; }

    /// <summary>Siralama alani.</summary>
    [FromQuery(Name = "sort")]
    public PersonSortField Sort { get; init; } = PersonSortField.FirstName;

    /// <summary>Siralama yonu.</summary>
    [FromQuery(Name = "order")]
    public SortOrder Order { get; init; } = SortOrder.Asc;

    /// <summary>Firmalar (coklu; secilenlerden herhangi biri).</summary>
    [FromQuery(Name = "companyId")]
    public Guid[] CompanyIds { get; init; } = [];

    /// <summary>Sehirler (coklu).</summary>
    [FromQuery(Name = "city")]
    public string[] Cities { get; init; } = [];

    /// <summary>Subeler (coklu).</summary>
    [FromQuery(Name = "branchId")]
    public Guid[] BranchIds { get; init; } = [];

    /// <summary>Birimler (coklu).</summary>
    [FromQuery(Name = "departmentId")]
    public Guid[] DepartmentIds { get; init; } = [];

    /// <summary>Unvan kodlari (coklu).</summary>
    [FromQuery(Name = "titleCode")]
    public string[] TitleCodes { get; init; } = [];

    /// <summary>Gorev tanimlari (coklu).</summary>
    [FromQuery(Name = "dutyId")]
    public Guid[] DutyIds { get; init; } = [];

    /// <summary>Statuler (coklu).</summary>
    [FromQuery(Name = "statusCode")]
    public string[] StatusCodes { get; init; } = [];

    /// <summary>SGK meslek kodlari (coklu).</summary>
    [FromQuery(Name = "occupationCode")]
    public string[] OccupationCodes { get; init; } = [];

    /// <summary>Egitim (coklu).</summary>
    [FromQuery(Name = "education")]
    public string[] Educations { get; init; } = [];

    /// <summary>Medeni hal (coklu: married, notMarried).</summary>
    [FromQuery(Name = "maritalStatus")]
    public MaritalStatusKind[] MaritalStatuses { get; init; } = [];

    /// <summary>Cinsiyet (coklu: male, female).</summary>
    [FromQuery(Name = "gender")]
    public GenderKind[] Genders { get; init; } = [];

    /// <summary>Ad (icinde gecen).</summary>
    [FromQuery(Name = "firstName")]
    public string? FirstName { get; init; }

    /// <summary>Soyad (icinde gecen).</summary>
    [FromQuery(Name = "lastName")]
    public string? LastName { get; init; }

    /// <summary>Onceki soyadi (icinde gecen).</summary>
    [FromQuery(Name = "previousSurname")]
    public string? PreviousSurname { get; init; }

    /// <summary>Sicil (icinde gecen).</summary>
    [FromQuery(Name = "registryCode")]
    public string? RegistryCode { get; init; }

    /// <summary>TCKN (icinde gecen); <c>personnel.person.view_national_id</c> izni gerekir.</summary>
    [FromQuery(Name = "nationalId")]
    public string? NationalId { get; init; }

    /// <summary>Kurumsal e-posta (icinde gecen).</summary>
    [FromQuery(Name = "email")]
    public string? Email { get; init; }

    /// <summary>Cep telefonu (icinde gecen).</summary>
    [FromQuery(Name = "phone")]
    public string? Phone { get; init; }

    /// <summary>Adres (icinde gecen); <c>personnel.person.view_sensitive</c> izni gerekir.</summary>
    [FromQuery(Name = "address")]
    public string? Address { get; init; }

    /// <summary>Dogum tarihi, baslangic (dahil).</summary>
    [FromQuery(Name = "birthDateFrom")]
    public DateOnly? BirthDateFrom { get; init; }

    /// <summary>Dogum tarihi, bitis (dahil).</summary>
    [FromQuery(Name = "birthDateTo")]
    public DateOnly? BirthDateTo { get; init; }

    /// <summary>Ise giris tarihi, baslangic (dahil).</summary>
    [FromQuery(Name = "hireDateFrom")]
    public DateOnly? HireDateFrom { get; init; }

    /// <summary>Ise giris tarihi, bitis (dahil).</summary>
    [FromQuery(Name = "hireDateTo")]
    public DateOnly? HireDateTo { get; init; }

    /// <summary>Isten cikis tarihi, baslangic (dahil).</summary>
    [FromQuery(Name = "terminationDateFrom")]
    public DateOnly? TerminationDateFrom { get; init; }

    /// <summary>Isten cikis tarihi, bitis (dahil).</summary>
    [FromQuery(Name = "terminationDateTo")]
    public DateOnly? TerminationDateTo { get; init; }

    /// <summary>Yas, en az.</summary>
    [FromQuery(Name = "ageMin")]
    public int? AgeMin { get; init; }

    /// <summary>Yas, en cok.</summary>
    [FromQuery(Name = "ageMax")]
    public int? AgeMax { get; init; }

    /// <summary>Kidem (tam yil), en az.</summary>
    [FromQuery(Name = "seniorityMin")]
    public int? SeniorityMin { get; init; }

    /// <summary>Kidem (tam yil), en cok.</summary>
    [FromQuery(Name = "seniorityMax")]
    public int? SeniorityMax { get; init; }

    /// <summary>Istegi uygulama katmaninin suzgec kumesine cevirir.</summary>
    public PersonListFilters ToFilters() => new()
    {
        CompanyIds = CompanyIds,
        Cities = Cities,
        BranchIds = BranchIds,
        DepartmentIds = DepartmentIds,
        TitleCodes = TitleCodes,
        DutyIds = DutyIds,
        StatusCodes = StatusCodes,
        OccupationCodes = OccupationCodes,
        Educations = Educations,
        MaritalStatuses = [.. MaritalStatuses.Select(m => m == MaritalStatusKind.Married ? MaritalStatus.Married : MaritalStatus.NotMarried)],
        Genders = [.. Genders.Select(g => g == GenderKind.Female ? Gender.Female : Gender.Male)],
        FirstName = FirstName,
        LastName = LastName,
        PreviousSurname = PreviousSurname,
        RegistryCode = RegistryCode,
        NationalId = NationalId,
        Email = Email,
        Phone = Phone,
        Address = Address,
        BirthDate = BirthDateFrom is null && BirthDateTo is null ? null : new DateRange(BirthDateFrom, BirthDateTo),
        HireDate = HireDateFrom is null && HireDateTo is null ? null : new DateRange(HireDateFrom, HireDateTo),
        TerminationDate = TerminationDateFrom is null && TerminationDateTo is null ? null : new DateRange(TerminationDateFrom, TerminationDateTo),
        Age = AgeMin is null && AgeMax is null ? null : new NumberRange(AgeMin, AgeMax),
        SeniorityYears = SeniorityMin is null && SeniorityMax is null ? null : new NumberRange(SeniorityMin, SeniorityMax),
    };

    /// <inheritdoc />
    public override string ToString() => nameof(PersonListRequest);
}

/// <summary>Suzgec secenekleri (SYG-PERS-024, 026).</summary>
/// <param name="Companies">Firmalar.</param>
/// <param name="Cities">Sehirler.</param>
/// <param name="Branches">Subeler.</param>
/// <param name="Departments">Birimler.</param>
/// <param name="Titles">Unvanlar.</param>
/// <param name="Duties">Gorevler.</param>
/// <param name="Statuses">Statuler.</param>
/// <param name="Occupations">SGK meslekleri.</param>
/// <param name="Educations">Egitim durumlari.</param>
/// <param name="CanFilterByNationalId">TCKN suzgeci sunulur mu.</param>
/// <param name="CanFilterBySensitive">Adres suzgeci sunulur mu.</param>
/// <param name="DefaultPageSize">Varsayilan sayfa boyutu (PRM-GRN-02).</param>
/// <param name="MaxPageSize">En buyuk sayfa boyutu (PRM-GRN-03); sayfa boyutu secenekleri bunu asmaz (SYG-PERS-029).</param>
public sealed record PersonFilterOptionsResponse(
    IReadOnlyList<FilterOptionResponse> Companies,
    IReadOnlyList<FilterOptionResponse> Cities,
    IReadOnlyList<FilterOptionResponse> Branches,
    IReadOnlyList<FilterOptionResponse> Departments,
    IReadOnlyList<FilterOptionResponse> Titles,
    IReadOnlyList<FilterOptionResponse> Duties,
    IReadOnlyList<FilterOptionResponse> Statuses,
    IReadOnlyList<FilterOptionResponse> Occupations,
    IReadOnlyList<FilterOptionResponse> Educations,
    bool CanFilterByNationalId,
    bool CanFilterBySensitive,
    int DefaultPageSize,
    int MaxPageSize)
{
    /// <summary>Uygulama katmanindan yanit kurar.</summary>
    public static PersonFilterOptionsResponse From(PersonFilterOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        static IReadOnlyList<FilterOptionResponse> Map(IReadOnlyList<FilterOption> items) => [.. items.Select(i => new FilterOptionResponse(i.Value, i.Label))];
        return new(Map(options.Companies), Map(options.Cities), Map(options.Branches), Map(options.Departments), Map(options.Titles),
            Map(options.Duties), Map(options.Statuses), Map(options.Occupations), Map(options.Educations),
            options.CanFilterByNationalId, options.CanFilterBySensitive, options.DefaultPageSize, options.MaxPageSize);
    }
}

/// <summary>Bir secenek.</summary>
/// <param name="Value">Suzgece gonderilen deger.</param>
/// <param name="Label">Ad.</param>
public sealed record FilterOptionResponse(string Value, string Label);

/// <summary>Calisma durumu suzgeci.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<EmploymentStatusKind>))]
public enum EmploymentStatusKind
{
    /// <summary>En az bir aktif sicili olanlar.</summary>
    [JsonStringEnumMemberName("active")]
    Active = 1,

    /// <summary>Ayrilanlar (gorunur kalma suresi icindekiler).</summary>
    [JsonStringEnumMemberName("terminated")]
    Terminated = 2,

    /// <summary>Tumu.</summary>
    [JsonStringEnumMemberName("all")]
    All = 3,
}

/// <summary>Liste siralama alani.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<PersonSortField>))]
public enum PersonSortField
{
    /// <summary>Ad, sonra soyad.</summary>
    [JsonStringEnumMemberName("firstName")]
    FirstName = 1,

    /// <summary>Soyad, sonra ad.</summary>
    [JsonStringEnumMemberName("lastName")]
    LastName = 2,
}

/// <summary>Liste isteginin dogrulamasi.</summary>
public sealed class PersonListRequestValidator : AbstractValidator<PersonListRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public PersonListRequestValidator()
    {
        RuleFor(r => r.Page).GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası 1 veya daha büyük olmalıdır.");
        RuleFor(r => r.PageSize)
            .InclusiveBetween(1, PagedResult<object>.MaxPageSize)
            .When(r => r.PageSize is not null)
            .WithMessage($"Sayfa boyutu 1 ile {PagedResult<object>.MaxPageSize} arasında olmalıdır.");
        RuleFor(r => r.Q).MaximumLength(100).WithMessage("Arama metni en fazla 100 karakter olabilir.");
        RuleFor(r => r.Status).IsInEnum().WithMessage("Geçersiz çalışma durumu.");
        RuleFor(r => r.Sort).IsInEnum().WithMessage("Geçersiz sıralama alanı.");
        RuleFor(r => r.Order).IsInEnum().WithMessage("Geçersiz sıralama yönü.");
        RuleForEach(r => r.MaritalStatuses).IsInEnum().WithMessage("Geçersiz medeni hâl.");
        RuleForEach(r => r.Genders).IsInEnum().WithMessage("Geçersiz cinsiyet.");
        RuleFor(r => r.AgeMin).InclusiveBetween(0, 120).When(r => r.AgeMin is not null).WithMessage("Yaş 0 ile 120 arasında olmalıdır.");
        RuleFor(r => r.AgeMax).InclusiveBetween(0, 120).When(r => r.AgeMax is not null).WithMessage("Yaş 0 ile 120 arasında olmalıdır.");
        RuleFor(r => r.SeniorityMin).InclusiveBetween(0, 80).When(r => r.SeniorityMin is not null).WithMessage("Kıdem 0 ile 80 yıl arasında olmalıdır.");
        RuleFor(r => r.SeniorityMax).InclusiveBetween(0, 80).When(r => r.SeniorityMax is not null).WithMessage("Kıdem 0 ile 80 yıl arasında olmalıdır.");
        RuleForEach(r => new[] { r.FirstName, r.LastName, r.PreviousSurname, r.RegistryCode, r.NationalId, r.Email, r.Phone, r.Address })
            .MaximumLength(100).WithMessage("Süzgeç metni en fazla 100 karakter olabilir.")
            .OverridePropertyName("filters");
    }
}

/// <summary>Listedeki kisi karti (SYG-PERS-023).</summary>
/// <param name="PersonId">Dis kimlik.</param>
/// <param name="FirstName">Ad.</param>
/// <param name="LastName">Soyad.</param>
/// <param name="IsActive">En az bir aktif sicili var mi.</param>
/// <param name="EmploymentCount">Sicil sayisi; 2 ve uzeri kartta "X sicil" isaretidir (KR-126).</param>
/// <param name="RegistryCode">Esas istihdamin sicili.</param>
/// <param name="CompanyName">Firma.</param>
/// <param name="BranchName">Sube.</param>
/// <param name="DepartmentName">Birim.</param>
/// <param name="TitleName">Unvan.</param>
/// <param name="DutyName">Gorev.</param>
public sealed record PersonListItemResponse(
    Guid PersonId,
    string FirstName,
    string LastName,
    bool IsActive,
    int EmploymentCount,
    string? RegistryCode,
    string? CompanyName,
    string? BranchName,
    string? DepartmentName,
    string? TitleName,
    string? DutyName)
{
    /// <summary>Uygulama katmanindaki kayittan yanit kurar.</summary>
    public static PersonListItemResponse From(PersonListItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        var p = item.Primary;
        return new(item.PersonId, item.FirstName, item.LastName, item.IsActive, item.EmploymentCount,
            p?.RegistryCode, p?.CompanyName, p?.BranchName, p?.DepartmentName, p?.TitleName, p?.DutyName);
    }
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
/// <param name="Sensitive">Korumali alanlar; yalnizca gorme hakki olana (<c>view_sensitive</c> veya kisinin kendisi) yazilir, digerlerinin yanitinda alan HIC YOKTUR (SYG-PERS-052).</param>
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
                new SeniorityResponse(e.Seniority.Years, e.Seniority.Months, e.Seniority.Days, e.Seniority.Text),
                e.Current is { } c
                    ? new WorkLifeResponse(c.ValidFrom, c.City, c.BranchName, c.DepartmentName, c.TitleCode, c.TitleName, c.StatusCode, c.OccupationCode, c.OccupationName)
                    : null,
                e.Duty is { } d ? new DutyResponse(d.Id, d.Name, d.ValidFrom, d.ValidTo) : null))],
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
/// <param name="WorkLife">Calisma hayati: istihdamin guncel nitelikleri (SYG-PERS-038); T1 oncesi veya hic gorulmediyse bos.</param>
/// <param name="Duty">Guncel gorev (SYG-PERS-042); girilmediyse bos.</param>
public sealed record EmploymentCardResponse(
    string RegistryCode,
    string CompanyName,
    DateOnly HireDate,
    DateOnly? TerminationDate,
    bool IsActive,
    SeniorityResponse Seniority,
    WorkLifeResponse? WorkLife,
    DutyResponse? Duty);

/// <summary>Calisma hayati bolumu (SYG-PERS-038). Firma istihdamin kendisindedir.</summary>
/// <param name="Since">Bu niteliklerin gecerli oldugu donemin baslangici.</param>
/// <param name="City">Sehir (subenin sehri).</param>
/// <param name="BranchName">Sube.</param>
/// <param name="DepartmentName">Birim.</param>
/// <param name="TitleCode">Unvan kodu (LOGO, 1–4).</param>
/// <param name="TitleName">Unvan ("Unvani" etiketiyle gosterilir); referans veride karsiligi yoksa bos (PG-PERS-21).</param>
/// <param name="StatusCode">Statu.</param>
/// <param name="OccupationCode">SGK meslek kodu.</param>
/// <param name="OccupationName">SGK meslek.</param>
public sealed record WorkLifeResponse(
    DateOnly Since,
    string? City,
    string? BranchName,
    string? DepartmentName,
    string? TitleCode,
    string? TitleName,
    string? StatusCode,
    string? OccupationCode,
    string? OccupationName);

/// <summary>Kidem suresi (SYG-PERS-039).</summary>
/// <param name="Years">Tam yil.</param>
/// <param name="Months">Yildan arta kalan tam ay.</param>
/// <param name="Days">Aydan arta kalan gun.</param>
/// <param name="Text">Ekran metni: "X Yil Y Ay Z Gun"; sifir olan bas birimler yazilmaz.</param>
public sealed record SeniorityResponse(int Years, int Months, int Days, string Text);
