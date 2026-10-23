namespace Dsg.Hrms.Domain.Personnel;

/// <summary>Cinsiyet (LOGO <c>LH_001_PERSON.SEX</c>: 1 erkek, 2 kadin).</summary>
public enum Gender
{
    /// <summary>Erkek.</summary>
    Male = 1,

    /// <summary>Kadin.</summary>
    Female = 2,
}

/// <summary>Medeni hal (LOGO aile/resim tablosu <c>STATUS</c>: 1 evli, diger bekar; PG-PERS-19).</summary>
public enum MaritalStatus
{
    /// <summary>Evli.</summary>
    Married = 1,

    /// <summary>Bekar (LOGO kodu 1 disindaki her deger).</summary>
    NotMarried = 2,
}

/// <summary>
/// Kisinin T1'de LOGO'dan gelen genel bilgileri (SYG-PERS-003, 034). T3 alanlarindan ayri
/// uygulanir: senkronizasyonda kisi ve istihdam adimi once kaydedilir (SYG-PERS-013).
/// </summary>
/// <param name="Gender">Cinsiyet; LOGO'da tanimsizsa <c>null</c>.</param>
/// <param name="PreviousSurname">Onceki soyadi; yalnizca kadin personelde tutulur.</param>
/// <param name="MaritalStatus">Medeni hal; LOGO'da satiri yoksa <c>null</c>.</param>
/// <param name="EducationName">Egitim (LOGO tanim adi); yoksa <c>null</c>.</param>
public sealed record PersonProfile(
    Gender? Gender,
    string? PreviousSurname,
    MaritalStatus? MaritalStatus,
    string? EducationName);
