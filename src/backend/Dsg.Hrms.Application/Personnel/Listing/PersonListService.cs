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
public sealed record PersonListCriteria(string? Search, EmploymentStatusFilter Status, PersonSort Sort, bool Descending, PageRequest Page);

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

    private readonly ICurrentUserScope _scope;
    private readonly IPersonnelQuery _query;
    private readonly IAccessLogger _accessLogger;
    private readonly ISystemParameters _parameters;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonListService(ICurrentUserScope scope, IPersonnelQuery query, IAccessLogger accessLogger, ISystemParameters parameters)
    {
        _scope = scope;
        _query = query;
        _accessLogger = accessLogger;
        _parameters = parameters;
    }

    /// <summary>Listeyi doner ve erisimi kaydeder.</summary>
    public async Task<PagedResult<PersonListItem>> ListAsync(
        string? search,
        EmploymentStatusFilter status,
        PersonSort sort,
        bool descending,
        int page,
        int? pageSize,
        CancellationToken cancellationToken)
    {
        var maxPageSize = await _parameters.GetIntegerAsync(ParameterCatalog.MaxPageSize, cancellationToken).ConfigureAwait(false);
        var size = pageSize ?? Math.Min(
            await _parameters.GetIntegerAsync(ParameterCatalog.DefaultPageSize, cancellationToken).ConfigureAwait(false),
            maxPageSize);
        if (size > maxPageSize)
        {
            throw new InvalidRequestException($"Sayfa boyutu en fazla {maxPageSize} olabilir.");
        }

        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var viewer = await _scope.GetAsync(cancellationToken).ConfigureAwait(false);
        var result = await _query.ListAsync(viewer, new PersonListCriteria(term, status, sort, descending, new PageRequest(page, size)), cancellationToken)
            .ConfigureAwait(false);

        var filters = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["q"] = term,
            ["status"] = status.ToString(),
            ["page"] = page.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };
        await _accessLogger.LogAsync(
            AccessRecord.List(EntityName, result.TotalCount, filters) with { SubjectIds = [.. result.Items.Select(i => i.PersonId)] },
            cancellationToken).ConfigureAwait(false);

        return result;
    }
}
