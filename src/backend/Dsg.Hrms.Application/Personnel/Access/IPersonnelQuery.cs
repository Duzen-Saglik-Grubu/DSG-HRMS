using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Application.Personnel.Access;

/// <summary>
/// Kisi okumalarinin TEK giris noktasi (ADR-0019 §1, SYG-PERS-050).
/// </summary>
/// <remarks>
/// <para>
/// Bilesen sorguya kapsam suzgecini ekler. Kapsam disi kayit "bulunamadi" olarak doner; uc
/// bunu var olmayan kayitla ayni <c>404</c>'e cevirir (SYG-PERS-051). EF genel sorgu suzgeci
/// kullanilmaz: senkronizasyon ve uyelik butun kisileri gormek zorundadir ve suzgec
/// <c>IgnoreQueryFilters</c> ile sessizce atlanabilirdi.
/// </para>
/// <para>
/// Donen kayit ham veridir (tam TCKN dahil). Alan gorunurlugu ve maskeleme cagiran servisin
/// isidir (<see cref="PersonFieldVisibility"/>); bilesen yaniti kurmaz.
/// </para>
/// </remarks>
public interface IPersonnelQuery
{
    /// <summary>Dis kimligiyle kisiyi bulur; kapsam disindaysa <c>null</c>.</summary>
    Task<PersonRecord?> FindAsync(Guid personId, PersonnelViewer viewer, CancellationToken cancellationToken);

    /// <summary>Kullanicinin kendi kisi kaydi; kisi kaydi yoksa <c>null</c>.</summary>
    Task<PersonRecord?> FindOwnAsync(PersonnelViewer viewer, CancellationToken cancellationToken);
}

/// <summary>Kisi kaydinin okunan hali.</summary>
/// <param name="Id">Ic kimlik (gorunurluk karari icin; disariya verilmez).</param>
/// <param name="PublicId">Dis kimlik.</param>
/// <param name="FirstName">Ad (LOGO'daki gibi).</param>
/// <param name="LastName">Soyad.</param>
/// <param name="NationalId">Tam TCKN.</param>
/// <param name="BirthDate">Dogum tarihi.</param>
/// <param name="Email">Kurumsal e-posta; yoksa <c>null</c>.</param>
/// <param name="MobilePhone">Cep telefonu; yoksa <c>null</c>.</param>
/// <param name="Employments">Istihdamlar.</param>
/// <param name="Profile">LOGO'dan gelen genel bilgiler (SYG-PERS-034).</param>
/// <param name="Address">Adres (korumali alan); kayitli degilse <c>null</c>.</param>
public sealed record PersonRecord(
    long Id,
    Guid PublicId,
    string FirstName,
    string LastName,
    string NationalId,
    DateOnly BirthDate,
    string? Email,
    string? MobilePhone,
    IReadOnlyList<EmploymentRecord> Employments,
    PersonProfile Profile,
    string? Address);

/// <summary>Istihdamin okunan hali.</summary>
/// <param name="RegistryCode">Sicil numarasi.</param>
/// <param name="CompanyName">Firma.</param>
/// <param name="HireDate">Ise giris tarihi.</param>
/// <param name="TerminationDate">Isten cikis tarihi; aktifse <c>null</c>.</param>
/// <param name="IsActive">Istihdam suruyor mu.</param>
public sealed record EmploymentRecord(
    string RegistryCode,
    string CompanyName,
    DateOnly HireDate,
    DateOnly? TerminationDate,
    bool IsActive);
