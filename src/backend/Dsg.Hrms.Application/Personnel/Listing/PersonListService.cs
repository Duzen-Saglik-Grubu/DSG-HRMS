using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Common.Paging;
using Dsg.Hrms.Application.Personnel.Access;
using Dsg.Hrms.Application.Settings;

namespace Dsg.Hrms.Application.Personnel.Listing;

/// <summary>Calisma durumu suzgeci (SYG-PERS-027).</summary>
public enum EmploymentStatusFilter
{
    /// <summary>En az bir aktif sicili olan kisiler (varsayilan).</summary>
    Active = 1,

    /// <summary>Aktif sicili olmayan ve gorunur kalma suresi dolmamis kisiler.</summary>
    Terminated = 2,

    /// <summary>Ikisi birden.</summary>
    All = 3,
}

/// <summary>Liste siralamasi (SYG-PERS-031). Turkce alfabeye gore (ICU <c>tr-TR</c>).</summary>
public enum PersonSort
{
    /// <summary>Ad, sonra soyad (varsayilan).</summary>
    FirstName = 1,

    /// <summary>Soyad, sonra ad.</summary>
    LastName = 2,
}

/// <summary>Liste sorgusu.</summary>
/// <param name="Search">Hizli arama terimi (ad, soyad, onceki soyadi, sicil; izinle TCKN).</param>
/// <param name="Status">Calisma durumu.</param>
/// <param name="Sort">Siralama.</param>
/// <param name="Descending">Azalan siralama.</param>
/// <param name="Page">Sayfa.</param>
public sealed record PersonListCriteria(string? Search, EmploymentStatusFilter Status, PersonSort Sort, bool Descending, PageRequest Page)
{
    /// <summary>Suzgecler (SYG-PERS-024).</summary>
    public PersonListFilters Filters { get; init; } = PersonListFilters.None;
}

/// <summary>Bir secenek: deger ve ekranda gorunen ad.</summary>
/// <param name="Value">Suzgece gonderilen deger.</param>
/// <param name="Label">Ad.</param>
public sealed record FilterOption(string Value, string Label);

/// <summary>
/// Suzgec secenekleri ve kullanicinin kullanabilecegi korumali suzgecler (SYG-PERS-024, 026).
/// </summary>
/// <param name="Companies">Firmalar.</param>
/// <param name="Cities">Sehirler.</param>
/// <param name="Branches">Subeler.</param>
/// <param name="Departments">Birimler.</param>
/// <param name="Titles">Unvanlar.</param>
/// <param name="Duties">Gorevler (pasifler dahil: gecmis kayitlarda gecer).</param>
/// <param name="Statuses">Statuler.</param>
/// <param name="Occupations">SGK meslekleri.</param>
/// <param name="Educations">Egitim durumlari.</param>
/// <param name="CanFilterByNationalId">TCKN suzgeci sunulur mu.</param>
/// <param name="CanFilterBySensitive">Adres (ve sonra kan grubu, acil durum) suzgeci sunulur mu.</param>
public sealed record PersonFilterOptions(
    IReadOnlyList<FilterOption> Companies,
    IReadOnlyList<FilterOption> Cities,
    IReadOnlyList<FilterOption> Branches,
    IReadOnlyList<FilterOption> Departments,
    IReadOnlyList<FilterOption> Titles,
    IReadOnlyList<FilterOption> Duties,
    IReadOnlyList<FilterOption> Statuses,
    IReadOnlyList<FilterOption> Occupations,
    IReadOnlyList<FilterOption> Educations,
    bool CanFilterByNationalId,
    bool CanFilterBySensitive)
{
    /// <summary>Varsayilan sayfa boyutu (`PRM-GRN-02`).</summary>
    public int DefaultPageSize { get; init; }

    /// <summary>
    /// En buyuk sayfa boyutu (`PRM-GRN-03`): ekran sayfa boyutu seceneklerini bunu asmayacak
    /// sekilde sunar (SYG-PERS-029).
    /// </summary>
    public int MaxPageSize { get; init; }
}

/// <summary>Suzgec seceneklerinin veritabani tarafi.</summary>
public interface IPersonFilterOptionsQuery
{
    /// <summary>Secim listelerinin degerleri (korumali alanlar haric).</summary>
    Task<PersonFilterOptions> GetAsync(CancellationToken cancellationToken);
}

/// <summary>Listedeki kisi karti (SYG-PERS-022, 023).</summary>
/// <param name="PersonId">Dis kimlik.</param>
/// <param name="FirstName">Ad (LOGO'daki gibi).</param>
/// <param name="LastName">Soyad.</param>
/// <param name="IsActive">En az bir aktif sicili var mi.</param>
/// <param name="EmploymentCount">Sicil sayisi; 2 ve uzeri kartta "X sicil" isaretidir.</param>
/// <param name="Primary">Esas istihdam (KR-126): aktiflerden ise giris tarihi en yeni olan.</param>
public sealed record PersonListItem(
    Guid PersonId,
    string FirstName,
    string LastName,
    bool IsActive,
    int EmploymentCount,
    PrimaryEmployment? Primary);

/// <summary>Kartta gosterilen esas istihdam.</summary>
/// <param name="RegistryCode">Sicil.</param>
/// <param name="CompanyName">Firma.</param>
/// <param name="BranchName">Sube.</param>
/// <param name="DepartmentName">Birim.</param>
/// <param name="TitleName">Unvan; referans veride karsiligi yoksa bos.</param>
/// <param name="DutyName">Gorev.</param>
public sealed record PrimaryEmployment(
    string RegistryCode,
    string CompanyName,
    string? BranchName,
    string? DepartmentName,
    string? TitleName,
    string? DutyName);

/// <summary>
/// Personel listesi (SYG-PERS-022, 023, 027, 028, 029, 030, 031, 058).
/// </summary>
/// <remarks>
/// <para>
/// Sayfa boyutu verilmezse parametredeki varsayilandir (24); parametredeki en buyuk degeri asan
/// istek <c>400</c> alir. Kodda 100 tavani ayrica vardir (ADR-0010 §4).
/// </para>
/// <para>
/// Her listeleme erisim kaydina yazilir: arama terimi (maskeli), sayi ve ekranda gosterilen
/// kisilerin kimlikleri (ADR-0019 §5; SYG-PERS-058).
/// </para>
/// </remarks>
public sealed class PersonListService
{
    /// <summary>Erisim kaydindaki veri turu.</summary>
    public const string EntityName = "Person";

    /// <summary>Korumali suzgecin degeri yerine erisim kaydina yazilan isaret.</summary>
    public const string ProtectedFilterMarker = "(kullanıldı)";

    private readonly ICurrentUserScope _scope;
    private readonly IPersonnelQuery _query;
    private readonly IAccessLogger _accessLogger;
    private readonly ISystemParameters _parameters;
    private readonly IPersonFilterOptionsQuery _options;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonListService(ICurrentUserScope scope, IPersonnelQuery query, IAccessLogger accessLogger, ISystemParameters parameters, IPersonFilterOptionsQuery options)
    {
        _scope = scope;
        _query = query;
        _accessLogger = accessLogger;
        _parameters = parameters;
        _options = options;
    }

    /// <summary>
    /// Suzgec secenekleri; korumali suzgecler yalnizca izni olana sunulur (SYG-PERS-026). Secenekler
    /// kurum tanimlaridir, kisisel veri degildir; erisim kaydi uretilmez.
    /// </summary>
    public async Task<PersonFilterOptions> GetFilterOptionsAsync(CancellationToken cancellationToken)
    {
        var viewer = await _scope.GetAsync(cancellationToken).ConfigureAwait(false);
        var options = await _options.GetAsync(cancellationToken).ConfigureAwait(false);
        var (defaultPageSize, maxPageSize) = await PageSizesAsync(cancellationToken).ConfigureAwait(false);
        return options with
        {
            CanFilterByNationalId = PersonListFilters.CanUse(viewer, FilterProtection.NationalId),
            CanFilterBySensitive = PersonListFilters.CanUse(viewer, FilterProtection.Sensitive),
            DefaultPageSize = defaultPageSize,
            MaxPageSize = maxPageSize,
        };
    }

    /// <summary>Varsayilan ve en buyuk sayfa boyutu; varsayilan en buyugu asmaz.</summary>
    private async Task<(int Default, int Max)> PageSizesAsync(CancellationToken cancellationToken)
    {
        var max = await _parameters.GetIntegerAsync(ParameterCatalog.MaxPageSize, cancellationToken).ConfigureAwait(false);
        var preferred = await _parameters.GetIntegerAsync(ParameterCatalog.DefaultPageSize, cancellationToken).ConfigureAwait(false);
        return (Math.Min(preferred, max), max);
    }

    /// <summary>Listeyi doner ve erisimi kaydeder.</summary>
    public async Task<PagedResult<PersonListItem>> ListAsync(
        string? search,
        EmploymentStatusFilter status,
        PersonSort sort,
        bool descending,
        int page,
        int? pageSize,
        CancellationToken cancellationToken) =>
        await ListAsync(search, status, sort, descending, page, pageSize, PersonListFilters.None, cancellationToken).ConfigureAwait(false);

    /// <summary>Suzgecli listeyi doner ve erisimi kaydeder.</summary>
    /// <exception cref="InvalidRequestException">
    /// Kullanicinin goremedigi bir alanin suzgeci kullanildiysa (SYG-PERS-026): aksi halde suzgec gizli
    /// degeri sorgu sonucundan cikarirdi (KR-116 madde 3).
    /// </exception>
    public async Task<PagedResult<PersonListItem>> ListAsync(
        string? search,
        EmploymentStatusFilter status,
        PersonSort sort,
        bool descending,
        int page,
        int? pageSize,
        PersonListFilters filters,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filters);

        var (defaultPageSize, maxPageSize) = await PageSizesAsync(cancellationToken).ConfigureAwait(false);
        var size = pageSize ?? defaultPageSize;
        if (size > maxPageSize)
        {
            throw new InvalidRequestException($"Sayfa boyutu en fazla {maxPageSize} olabilir.");
        }

        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var viewer = await _scope.GetAsync(cancellationToken).ConfigureAwait(false);

        var used = filters.Used().ToList();
        if (used.FirstOrDefault(f => !PersonListFilters.CanUse(viewer, f.Protection)) is { Name: { } forbidden })
        {
            throw new InvalidRequestException($"Bu alana göre süzme yetkiniz yok: {forbidden}.");
        }

        var criteria = new PersonListCriteria(term, status, sort, descending, new PageRequest(page, size)) { Filters = filters };
        var result = await _query.ListAsync(viewer, criteria, cancellationToken).ConfigureAwait(false);

        // Korumali suzgecin DEGERI erisim kaydina yazilmaz, yalnizca kullanildigi (SYG-PERS-059, B-01):
        // deger kisi kimlikleriyle ayni kayitta dursaydi gizli degeri ele verirdi.
        var logged = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["q"] = term,
            ["status"] = status.ToString(),
            ["page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        foreach (var (name, protection, value) in used)
        {
            logged[name] = protection == FilterProtection.None ? value : ProtectedFilterMarker;
        }

        await _accessLogger.LogAsync(
            AccessRecord.List(EntityName, result.TotalCount, logged) with { SubjectIds = [.. result.Items.Select(i => i.PersonId)] },
            cancellationToken).ConfigureAwait(false);

        return result;
    }
}
