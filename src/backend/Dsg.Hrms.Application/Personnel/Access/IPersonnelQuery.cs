using Dsg.Hrms.Application.Common.Paging;
using Dsg.Hrms.Application.Personnel.Listing;
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

    /// <summary>
    /// Istihdamin atama ve gorev donemleri, yeniden eskiye (SYG-PERS-020). <paramref name="asOf"/>
    /// verilirse yalnizca o tarihi kapsayan donemler. Kapsam denetimi cagirandadir.
    /// </summary>
    Task<History.EmploymentHistory> HistoryAsync(long employmentId, DateOnly? asOf, CancellationToken cancellationToken);

    /// <summary>
    /// Kapsamdaki kisileri suzer, siralar ve sayfalar (SYG-PERS-022, 027, 028, 030, 031). Gorunur kalma
    /// suresi dolmus ayrilan kisi listede yoktur.
    /// </summary>
    Task<PagedResult<PersonListItem>> ListAsync(PersonnelViewer viewer, PersonListCriteria criteria, CancellationToken cancellationToken);

    /// <summary>
    /// Disa aktarma (SYG-PERS-032): listeyle ayni kapsam, gorunur kalma kurali, arama, suzgec ve
    /// siralama; sayfalama yoktur. Eslesen kisi sayisi <paramref name="limit"/> degerini asarsa
    /// satir okunmaz, yalnizca sayi doner. Korumali degerler yalnizca <paramref name="needs"/>
    /// isterse okunur; gorunurluk denetimi cagiran servisindir.
    /// </summary>
    /// <remarks><see cref="PersonListCriteria.Page"/> kullanilmaz.</remarks>
    Task<PersonExportResult> ExportAsync(PersonnelViewer viewer, PersonListCriteria criteria, PersonExportNeeds needs, int limit, CancellationToken cancellationToken);

    /// <summary>
    /// Kisinin fotografi (SYG-PERS-041); kart ile ayni kapsam ve gorunur kalma kurali. Kapsam
    /// disindaysa, kisi yoksa veya fotografi yoksa <c>null</c>.
    /// </summary>
    Task<PhotoRecord?> FindPhotoAsync(Guid personId, PersonnelViewer viewer, CancellationToken cancellationToken);

    /// <summary>Kullanicinin kendi fotografi; yoksa <c>null</c>.</summary>
    Task<PhotoRecord?> FindOwnPhotoAsync(PersonnelViewer viewer, CancellationToken cancellationToken);
}

/// <summary>Disa aktarmada okunacak korumali ve agir degerler.</summary>
/// <param name="Address">Adres.</param>
/// <param name="BloodGroup">Kan grubu (cozulur).</param>
/// <param name="Photo">Kucuk fotograf.</param>
public sealed record PersonExportNeeds(bool Address, bool BloodGroup, bool Photo);

/// <summary>Disa aktarma sonucu.</summary>
/// <param name="TotalCount">Eslesen kisi sayisi.</param>
/// <param name="Rows">Satirlar; sinir asildiysa bos.</param>
public sealed record PersonExportResult(int TotalCount, IReadOnlyList<PersonExportRecord> Rows);

/// <summary>Disa aktarilan kisi.</summary>
/// <param name="Person">Kisi; adres ve kan grubu yalnizca istendiyse dolu.</param>
/// <param name="Primary">Esas istihdam (KR-130), guncel nitelikleri ve goreviyle; sicili yoksa <c>null</c>.</param>
/// <param name="Photo">Kucuk fotograf (JPEG); istenmediyse veya yoksa <c>null</c>.</param>
#pragma warning disable CA1819 // Goruntu baytlari kopyalanmadan rapora yazilir.
public sealed record PersonExportRecord(PersonRecord Person, EmploymentRecord? Primary, byte[]? Photo);
#pragma warning restore CA1819

/// <summary>Saklanan fotograf.</summary>
/// <param name="Content">Kucultulmus JPEG.</param>
/// <param name="SourceHash">LOGO'daki orijinalin SHA-256 ozeti.</param>
/// <param name="ThumbnailVersion">Goruntuyu ureten kucultme kuralinin surumu.</param>
#pragma warning disable CA1819 // Goruntu baytlari kopyalanmadan yanita yazilir.
public sealed record PhotoRecord(byte[] Content, string SourceHash, int ThumbnailVersion)
{
    /// <summary>
    /// Tarayici onbellegi icin surum: kaynak ozeti ve kucultme surumu. Yalnizca ozet kullanilsaydi
    /// kural degisince (#315) ozet ayni kalir, tarayici <c>304</c> alir ve eski goruntuyu
    /// gostermeye devam ederdi.
    /// </summary>
    public string EntityTag => $"{SourceHash}-{ThumbnailVersion}";
}
#pragma warning restore CA1819

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
    string? Address)
{
    /// <summary>Kisinin saklanan fotografi var mi (SYG-PERS-040).</summary>
    public bool HasPhoto { get; init; }

    /// <summary>
    /// Kan grubunun adi (ornek "A Rh+"; <c>KR-129</c>). Yalnizca korumali alanlari gorebilen
    /// bakista (<c>view_sensitive</c> veya kisinin kendisi) cozulur; digerlerinde ve kayitli
    /// degilse <c>null</c> (SYG-PERS-052, ADR-0020).
    /// </summary>
    public string? BloodGroup { get; init; }

    /// <summary>
    /// Acil durum kisileri, eklenme sirasiyla (SYG-PERS-043). Yalnizca korumali alanlari gorebilen
    /// bakista okunur; digerlerinde bos (SYG-PERS-052).
    /// </summary>
    public IReadOnlyList<EmergencyContactRecord> EmergencyContacts { get; init; } = [];
}

/// <summary>Acil durum kisisi (SYG-PERS-043).</summary>
/// <param name="Id">Kaydin dis kimligi.</param>
/// <param name="RelationshipId">Yakinlik taniminin dis kimligi (referans veri, <c>KR-135</c>).</param>
/// <param name="Relationship">Yakinlik taniminin adi; tanim pasife alinmis olsa da gorunur.</param>
/// <param name="FirstName">Ad.</param>
/// <param name="LastName">Soyad.</param>
/// <param name="MobilePhone">Cep telefonu, <c>5XXXXXXXXX</c>.</param>
public sealed record EmergencyContactRecord(Guid Id, Guid RelationshipId, string Relationship, string FirstName, string LastName, string MobilePhone);

/// <summary>Istihdamin okunan hali.</summary>
/// <param name="Id">Ic kimlik (disariya verilmez).</param>
/// <param name="RegistryCode">Sicil numarasi.</param>
/// <param name="CompanyName">Firma.</param>
/// <param name="HireDate">Ise giris tarihi.</param>
/// <param name="TerminationDate">Isten cikis tarihi; aktifse <c>null</c>.</param>
/// <param name="IsActive">Istihdam suruyor mu.</param>
public sealed record EmploymentRecord(
    long Id,
    string RegistryCode,
    string CompanyName,
    DateOnly HireDate,
    DateOnly? TerminationDate,
    bool IsActive)
{
    /// <summary>Istihdamin guncel tarih aralikli nitelikleri; T1 oncesi veya hic gorulmediyse <c>null</c>.</summary>
    public AssignmentRecord? Current { get; init; }

    /// <summary>Istihdamin guncel gorevi (acik kayit, yoksa en son kayit); girilmediyse <c>null</c>.</summary>
    public DutyRecord? CurrentDuty { get; init; }
}

/// <summary>Bir gorev donemi (SYG-PERS-042).</summary>
/// <param name="Id">Gorev kaydinin dis kimligi.</param>
/// <param name="Name">Gorev.</param>
/// <param name="ValidFrom">Baslangic.</param>
/// <param name="ValidTo">Bitis (haric); acikse <c>null</c>.</param>
public sealed record DutyRecord(Guid Id, string Name, DateOnly ValidFrom, DateOnly? ValidTo);

/// <summary>
/// Bir donemin nitelikleri, adlariyla (SYG-PERS-038). Guncel kayit: acik kayit, yoksa en son kayit.
/// </summary>
/// <param name="ValidFrom">Donemin baslangici.</param>
/// <param name="ValidTo">Donemin bitisi (haric); acikse <c>null</c>.</param>
/// <param name="CompanyName">Firma.</param>
/// <param name="BranchName">Sube.</param>
/// <param name="City">Subenin sehri.</param>
/// <param name="DepartmentName">Birim.</param>
/// <param name="TitleCode">Unvan kodu (LOGO).</param>
/// <param name="TitleName">Unvan adi (referans veri); karsiligi yoksa <c>null</c> ve ekranda bos (PG-PERS-21).</param>
/// <param name="StatusCode">Statu.</param>
/// <param name="OccupationCode">SGK meslek kodu.</param>
/// <param name="OccupationName">SGK meslek adi.</param>
public sealed record AssignmentRecord(
    DateOnly ValidFrom,
    DateOnly? ValidTo,
    string CompanyName,
    string? BranchName,
    string? City,
    string? DepartmentName,
    string? TitleCode,
    string? TitleName,
    string? StatusCode,
    string? OccupationCode,
    string? OccupationName);
