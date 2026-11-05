using Dsg.Hrms.Domain.Personnel.Sync;

namespace Dsg.Hrms.Application.Personnel.Quality;

/// <summary>Veri kalitesi uyarisinin turu (SYG-PERS-070). Siralama raporun sirasidir.</summary>
public enum DataQualityIssue
{
    /// <summary>Kartta TCKN yok; kisi olusturulmadi (SYG-PERS-016).</summary>
    MissingNationalId = 1,

    /// <summary>Karttaki TCKN gecersiz; kisi olusturulmadi.</summary>
    InvalidNationalId = 2,

    /// <summary>Kartta dogum tarihi yok; kisi olusturulmadi.</summary>
    MissingBirthDate = 3,

    /// <summary>Kisinin ne e-postasi ne cep telefonu var.</summary>
    NoContact = 4,

    /// <summary>Kartta gecersiz bicimli cep telefonu var.</summary>
    InvalidMobilePhone = 5,

    /// <summary>E-posta baska bir aktif kisiyle paylasiliyor.</summary>
    SharedEmail = 6,

    /// <summary>Istihdamin unvani bos.</summary>
    MissingTitle = 7,

    /// <summary>Istihdamin acik gorevi yok.</summary>
    MissingDuty = 8,
}

/// <summary>Uye olamama nedenleri (SYG-PERS-071; SYG-KMLK-013, 017).</summary>
[Flags]
public enum MembershipBlocker
{
    /// <summary>Neden yok.</summary>
    None = 0,

    /// <summary>Kurumsal e-postasi yok.</summary>
    NoEmail = 1,

    /// <summary>E-postasi kabul edilen alan adlarinda degil (<c>PRM-KML-01</c>).</summary>
    DomainNotAccepted = 2,

    /// <summary>E-postasi baska bir aktif kisiyle paylasiliyor.</summary>
    SharedEmail = 4,
}

/// <summary>Raporda kartin veya kisinin kurumsal konumu.</summary>
/// <param name="RegistryCode">Sicil kodu; IK karti LOGO'da bununla bulur.</param>
/// <param name="PersonId">Kisinin dis kimligi; kisi olusturulmadiysa <c>null</c>.</param>
/// <param name="FirstName">Ad.</param>
/// <param name="LastName">Soyad.</param>
/// <param name="Company">Firma.</param>
/// <param name="Branch">Sube.</param>
/// <param name="Department">Birim.</param>
public sealed record DataQualitySubject(
    string RegistryCode,
    Guid? PersonId,
    string? FirstName,
    string? LastName,
    string? Company,
    string? Branch,
    string? Department);

/// <summary>Raporun bir satiri.</summary>
/// <param name="Issue">Uyari turu.</param>
/// <param name="Subject">Kart veya kisi.</param>
public sealed record DataQualityItem(DataQualityIssue Issue, DataQualitySubject Subject);

/// <summary>Hesabi olmayan aktif kisi; uyelik kosulu uygulama katmaninda degerlendirilir.</summary>
/// <param name="Subject">Kisi ve esas istihdami.</param>
/// <param name="Email">E-posta; yalnizca alan adi denetimi icin, rapora YAZILMAZ.</param>
/// <param name="IsEmailShared">E-posta paylasiliyor mu.</param>
public sealed record UnregisteredPerson(DataQualitySubject Subject, string? Email, bool IsEmailShared);

/// <summary>Senkronizasyon calismasinin ozeti.</summary>
/// <param name="Status">Sonuc.</param>
/// <param name="StartedAt">Baslangic.</param>
/// <param name="FinishedAt">Bitis.</param>
/// <param name="RecordsRead">Okunan kart sayisi.</param>
/// <param name="WarningCount">Uretilen uyari sayisi.</param>
public sealed record DataQualityRun(SyncStatus Status, DateTimeOffset StartedAt, DateTimeOffset? FinishedAt, int RecordsRead, int WarningCount);

/// <summary>Veri kalitesi raporunun veritabani tarafi (SYG-PERS-070, 071).</summary>
public interface IDataQualityQuery
{
    /// <summary>En son biten calisma (sonucu ne olursa olsun); hic yoksa <c>null</c>.</summary>
    Task<DataQualityRun?> LastFinishedRunAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Kart duzeyindeki uyarilarin alindigi calisma: veriyi aktaran son calisma (basarili, uyarili
    /// veya kismen tamamlanan); hic yoksa <c>null</c>.
    /// </summary>
    Task<DataQualityRun?> LastWarningsRunAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Aktif istihdamlardaki butun uyarilar, rapor sirasinda: tur, firma, sicil. Kart duzeyindeki
    /// uyarilar <see cref="LastWarningsRunAsync"/> calismasindan, digerleri guncel veriden gelir.
    /// </summary>
    Task<IReadOnlyList<DataQualityItem>> ListIssuesAsync(CancellationToken cancellationToken);

    /// <summary>Aktif istihdami olan ve hesabi bulunmayan kisiler; firma ve sicile gore sirali.</summary>
    Task<IReadOnlyList<UnregisteredPerson>> ListUnregisteredAsync(CancellationToken cancellationToken);
}
