using Dsg.Hrms.Application.Common.Reports;
using Dsg.Hrms.Application.Personnel.Access;

namespace Dsg.Hrms.Application.Personnel.Export;

/// <summary>
/// Disa aktarilabilen kisi alanlari (SYG-PERS-032, PG-PERS-42). Ad ve soyad her satirda vardir;
/// secilmez. Istihdam alanlari kartin esas istihdamindan gelir (KR-130).
/// </summary>
public enum PersonExportField
{
    /// <summary>Esas istihdamin sicili.</summary>
    RegistryCode = 1,

    /// <summary>TCKN; tam TCKN'yi goremeyene maskeli (SYG-PERS-053).</summary>
    NationalId = 2,

    /// <summary>Cinsiyet.</summary>
    Gender = 3,

    /// <summary>Dogum tarihi.</summary>
    BirthDate = 4,

    /// <summary>Yas (SYG-PERS-035).</summary>
    Age = 5,

    /// <summary>Onceki soyadi.</summary>
    PreviousSurname = 6,

    /// <summary>Medeni hal.</summary>
    MaritalStatus = 7,

    /// <summary>Egitim.</summary>
    Education = 8,

    /// <summary>Kan grubu (ozel nitelikli; <c>view_sensitive</c>).</summary>
    BloodGroup = 9,

    /// <summary>Kurumsal e-posta.</summary>
    Email = 10,

    /// <summary>Cep telefonu.</summary>
    MobilePhone = 11,

    /// <summary>Adres (<c>view_sensitive</c>).</summary>
    Address = 12,

    /// <summary>Aktif veya ayrildi.</summary>
    EmploymentStatus = 13,

    /// <summary>Firma.</summary>
    Company = 14,

    /// <summary>Subenin sehri.</summary>
    City = 15,

    /// <summary>Sube.</summary>
    Branch = 16,

    /// <summary>Birim.</summary>
    Department = 17,

    /// <summary>Unvan.</summary>
    Title = 18,

    /// <summary>Gorev.</summary>
    Duty = 19,

    /// <summary>Statu.</summary>
    StatusCode = 20,

    /// <summary>SGK meslek kodu.</summary>
    OccupationCode = 21,

    /// <summary>SGK meslek adi.</summary>
    OccupationName = 22,

    /// <summary>Ise giris tarihi.</summary>
    HireDate = 23,

    /// <summary>Isten cikis tarihi.</summary>
    TerminationDate = 24,

    /// <summary>Kidem suresi (SYG-PERS-039).</summary>
    Seniority = 25,

    /// <summary>Kucuk fotograf; yalnizca PDF (ADR-0022 §4).</summary>
    Photo = 26,
}

/// <summary>Alanin tanimi.</summary>
/// <param name="Field">Alan.</param>
/// <param name="Header">Basliktaki ad (kartla ayni).</param>
/// <param name="Kind">Deger turu.</param>
/// <param name="Sensitive">Korumali alan mi (adres, kan grubu): kume isleminde yalnizca izinle (ADR-0019 §2).</param>
/// <param name="PdfOnly">Yalnizca PDF'te secilebilir (fotograf).</param>
public sealed record PersonExportFieldDefinition(
    PersonExportField Field,
    string Header,
    ReportColumnKind Kind,
    bool Sensitive = false,
    bool PdfOnly = false);

/// <summary>
/// Alan katalogu ve gorunurluk karari (ADR-0019 §2, dorduncu kullanim yeri: disa aktarma alanlari).
/// </summary>
/// <remarks>
/// Disa aktarma bir kume islemidir: "kisinin kendisi" kurali uygulanmaz, yalnizca izne bakilir
/// (<see cref="PersonFieldVisibility.CanUseSensitiveInSets"/>). Kendi adresini goren personel
/// baskalarinin adresini disari aktaramaz.
/// </remarks>
public static class PersonExportFields
{
    /// <summary>Ad sutununun basligi; her satirda vardir.</summary>
    public const string FirstNameHeader = "Ad";

    /// <summary>Soyad sutununun basligi; her satirda vardir.</summary>
    public const string LastNameHeader = "Soyad";

    /// <summary>Katalog, dosyadaki sutun sirasinda.</summary>
    public static IReadOnlyList<PersonExportFieldDefinition> All { get; } =
    [
        new(PersonExportField.RegistryCode, "Sicil numarası", ReportColumnKind.Text),
        new(PersonExportField.NationalId, "TCKN", ReportColumnKind.Text),
        new(PersonExportField.Gender, "Cinsiyet", ReportColumnKind.Text),
        new(PersonExportField.BirthDate, "Doğum tarihi", ReportColumnKind.Date),
        new(PersonExportField.Age, "Yaş", ReportColumnKind.Number),
        new(PersonExportField.PreviousSurname, "Önceki soyadı", ReportColumnKind.Text),
        new(PersonExportField.MaritalStatus, "Medeni hâl", ReportColumnKind.Text),
        new(PersonExportField.Education, "Eğitim", ReportColumnKind.Text),
        new(PersonExportField.BloodGroup, "Kan grubu", ReportColumnKind.Text, Sensitive: true),
        new(PersonExportField.Email, "E-posta", ReportColumnKind.Text),
        new(PersonExportField.MobilePhone, "Telefon", ReportColumnKind.Text),
        new(PersonExportField.Address, "Adres", ReportColumnKind.Text, Sensitive: true),
        new(PersonExportField.EmploymentStatus, "Çalışma durumu", ReportColumnKind.Text),
        new(PersonExportField.Company, "Firma", ReportColumnKind.Text),
        new(PersonExportField.City, "Şehir", ReportColumnKind.Text),
        new(PersonExportField.Branch, "Şube", ReportColumnKind.Text),
        new(PersonExportField.Department, "Birim", ReportColumnKind.Text),
        new(PersonExportField.Title, "Ünvanı", ReportColumnKind.Text),
        new(PersonExportField.Duty, "Görevi", ReportColumnKind.Text),
        new(PersonExportField.StatusCode, "Statü", ReportColumnKind.Text),
        new(PersonExportField.OccupationCode, "SGK meslek kodu", ReportColumnKind.Text),
        new(PersonExportField.OccupationName, "SGK meslek", ReportColumnKind.Text),
        new(PersonExportField.HireDate, "İşe giriş tarihi", ReportColumnKind.Date),
        new(PersonExportField.TerminationDate, "İşten çıkış tarihi", ReportColumnKind.Date),
        new(PersonExportField.Seniority, "Kıdem süresi", ReportColumnKind.Text),
        new(PersonExportField.Photo, "Fotoğraf", ReportColumnKind.Photo, PdfOnly: true),
    ];

    /// <summary>Kullanicinin secebilecegi alanlar.</summary>
    public static IReadOnlyList<PersonExportFieldDefinition> Available(PersonnelViewer viewer)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        var sensitive = PersonFieldVisibility.CanUseSensitiveInSets(viewer);
        return [.. All.Where(d => !d.Sensitive || sensitive)];
    }
}
