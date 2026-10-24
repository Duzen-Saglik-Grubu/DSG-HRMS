using Dsg.Hrms.Application.Personnel.Access;
using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Application.Personnel.Listing;

/// <summary>Bir suzgecin hangi gorme hakkini gerektirdigi (ADR-0019 §2).</summary>
public enum FilterProtection
{
    /// <summary>Kaydi gorebilen herkes kullanabilir.</summary>
    None = 0,

    /// <summary>Tam TCKN'yi gorme izni (<c>view_national_id</c>).</summary>
    NationalId = 1,

    /// <summary>Korumali alanlari gorme izni (<c>view_sensitive</c>); kume isleminde "kisinin kendisi" sayilmaz.</summary>
    Sensitive = 2,
}

/// <summary>Liste suzgecleri (SYG-PERS-024). Bos birakilan suzgec uygulanmaz.</summary>
/// <remarks>
/// Coklu secimli suzgecte secilenlerden HERHANGI BIRINE uyan kayitlar gelir; farkli suzgecler
/// BIRLIKTE (VE) uygulanir (SYG-PERS-025). Istihdam suzgecleri kisinin herhangi bir sicilinin guncel
/// doneminde eslesir (<c>KR-126</c>).
/// </remarks>
public sealed record PersonListFilters
{
    /// <summary>Bos suzgec kumesi.</summary>
    public static PersonListFilters None { get; } = new();

    /// <summary>Firmalar (dis kimlik).</summary>
    public IReadOnlyList<Guid> CompanyIds { get; init; } = [];

    /// <summary>Sehirler (subenin sehri).</summary>
    public IReadOnlyList<string> Cities { get; init; } = [];

    /// <summary>Subeler (dis kimlik).</summary>
    public IReadOnlyList<Guid> BranchIds { get; init; } = [];

    /// <summary>Birimler (dis kimlik).</summary>
    public IReadOnlyList<Guid> DepartmentIds { get; init; } = [];

    /// <summary>Unvan kodlari.</summary>
    public IReadOnlyList<string> TitleCodes { get; init; } = [];

    /// <summary>Gorev tanimlari (dis kimlik).</summary>
    public IReadOnlyList<Guid> DutyIds { get; init; } = [];

    /// <summary>Statuler.</summary>
    public IReadOnlyList<string> StatusCodes { get; init; } = [];

    /// <summary>SGK meslek kodlari.</summary>
    public IReadOnlyList<string> OccupationCodes { get; init; } = [];

    /// <summary>Egitim adlari.</summary>
    public IReadOnlyList<string> Educations { get; init; } = [];

    /// <summary>Medeni haller.</summary>
    public IReadOnlyList<MaritalStatus> MaritalStatuses { get; init; } = [];

    /// <summary>Cinsiyetler.</summary>
    public IReadOnlyList<Gender> Genders { get; init; } = [];

    /// <summary>Ad (icinde gecen, Turkce karaktersiz).</summary>
    public string? FirstName { get; init; }

    /// <summary>Soyad (icinde gecen, Turkce karaktersiz).</summary>
    public string? LastName { get; init; }

    /// <summary>Onceki soyadi (icinde gecen, Turkce karaktersiz).</summary>
    public string? PreviousSurname { get; init; }

    /// <summary>Sicil (icinde gecen; herhangi bir sicil).</summary>
    public string? RegistryCode { get; init; }

    /// <summary>TCKN (icinde gecen); <see cref="FilterProtection.NationalId"/>.</summary>
    public string? NationalId { get; init; }

    /// <summary>Kurumsal e-posta (icinde gecen).</summary>
    public string? Email { get; init; }

    /// <summary>Cep telefonu (icinde gecen).</summary>
    public string? Phone { get; init; }

    /// <summary>Adres (icinde gecen, Turkce karaktersiz); <see cref="FilterProtection.Sensitive"/>.</summary>
    public string? Address { get; init; }

    /// <summary>Dogum tarihi araligi (dahil).</summary>
    public DateRange? BirthDate { get; init; }

    /// <summary>Ise giris tarihi araligi (dahil; herhangi bir sicil).</summary>
    public DateRange? HireDate { get; init; }

    /// <summary>Isten cikis tarihi araligi (dahil; herhangi bir sicil).</summary>
    public DateRange? TerminationDate { get; init; }

    /// <summary>Yas araligi (yil, dahil).</summary>
    public NumberRange? Age { get; init; }

    /// <summary>Kidem araligi (tam yil, dahil; herhangi bir sicil, SYG-PERS-039 kurali).</summary>
    public NumberRange? SeniorityYears { get; init; }

    /// <summary>
    /// Kullanilan suzgeclerin adlari ve korumalari. Erisim kaydi ve gorunurluk denetimi bu listeden
    /// beslenir; korumali bir suzgec yeni eklendiginde korumasi burada yazilir.
    /// </summary>
    public IEnumerable<(string Name, FilterProtection Protection, string? Value)> Used()
    {
        static string? Join<T>(IReadOnlyList<T> items) => items.Count == 0 ? null : string.Join(',', items);

        (string, FilterProtection, string?)[] all =
        [
            ("companyId", FilterProtection.None, Join(CompanyIds)),
            ("city", FilterProtection.None, Join(Cities)),
            ("branchId", FilterProtection.None, Join(BranchIds)),
            ("departmentId", FilterProtection.None, Join(DepartmentIds)),
            ("titleCode", FilterProtection.None, Join(TitleCodes)),
            ("dutyId", FilterProtection.None, Join(DutyIds)),
            ("statusCode", FilterProtection.None, Join(StatusCodes)),
            ("occupationCode", FilterProtection.None, Join(OccupationCodes)),
            ("education", FilterProtection.None, Join(Educations)),
            ("maritalStatus", FilterProtection.None, Join(MaritalStatuses)),
            ("gender", FilterProtection.None, Join(Genders)),
            ("firstName", FilterProtection.None, FirstName),
            ("lastName", FilterProtection.None, LastName),
            ("previousSurname", FilterProtection.None, PreviousSurname),
            ("registryCode", FilterProtection.None, RegistryCode),
            ("nationalId", FilterProtection.NationalId, NationalId),
            ("email", FilterProtection.None, Email),
            ("phone", FilterProtection.None, Phone),
            ("address", FilterProtection.Sensitive, Address),
            ("birthDate", FilterProtection.None, BirthDate?.ToString()),
            ("hireDate", FilterProtection.None, HireDate?.ToString()),
            ("terminationDate", FilterProtection.None, TerminationDate?.ToString()),
            ("age", FilterProtection.None, Age?.ToString()),
            ("seniorityYears", FilterProtection.None, SeniorityYears?.ToString()),
        ];

        return all.Where(f => !string.IsNullOrWhiteSpace(f.Item3));
    }

    /// <summary>Kullanicinin bu korumadaki suzgeci kullanip kullanamayacagi (kume islemi: yalnizca izin).</summary>
    public static bool CanUse(PersonnelViewer viewer, FilterProtection protection) => protection switch
    {
        FilterProtection.NationalId => PersonFieldVisibility.CanSearchByNationalId(viewer),
        FilterProtection.Sensitive => PersonFieldVisibility.CanUseSensitiveInSets(viewer),
        _ => true,
    };
}

/// <summary>Tarih araligi (iki uc da dahil; biri bos olabilir).</summary>
/// <param name="From">Baslangic.</param>
/// <param name="To">Bitis.</param>
public sealed record DateRange(DateOnly? From, DateOnly? To)
{
    /// <inheritdoc />
    public override string ToString() => $"{From:yyyy-MM-dd}..{To:yyyy-MM-dd}";
}

/// <summary>Sayi araligi (iki uc da dahil; biri bos olabilir).</summary>
/// <param name="Min">En az.</param>
/// <param name="Max">En cok.</param>
public sealed record NumberRange(int? Min, int? Max)
{
    /// <inheritdoc />
    public override string ToString() => $"{Min}..{Max}";
}
