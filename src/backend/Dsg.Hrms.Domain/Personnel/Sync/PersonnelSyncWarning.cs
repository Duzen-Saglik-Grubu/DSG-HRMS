namespace Dsg.Hrms.Domain.Personnel.Sync;

/// <summary>
/// Senkronizasyonda tespit edilen veri kalitesi uyarisi (ADR-0003 §5).
/// </summary>
/// <remarks>
/// <para>
/// Uyarilar IK'nin LOGO'da duzeltme yapabilmesi icindir. Karti tanimlamak icin
/// <b>sicil kodu</b> kullanilir; TCKN, e-posta veya telefon uyari kaydina
/// YAZILMAZ. Duzeltmeyi yapacak kisi karta LOGO'da sicil koduyla ulasir.
/// </para>
/// </remarks>
public sealed class PersonnelSyncWarning
{
    private PersonnelSyncWarning()
    {
    }

    /// <summary>Veritabani ici birincil anahtar.</summary>
    public long Id { get; private set; }

    /// <summary>Calismanin kimligi.</summary>
    public long RunId { get; private set; }

    /// <summary>Uyari turu.</summary>
    public SyncWarningCode Code { get; private set; }

    /// <summary>Uyarinin ilgili oldugu kartin sicil kodu.</summary>
    public string RegistryCode { get; private set; } = string.Empty;

    /// <summary>Aciklama. Kisisel veri icermez.</summary>
    public string Detail { get; private set; } = string.Empty;

    /// <summary>Yeni uyari olusturur.</summary>
    public static PersonnelSyncWarning Create(SyncWarningCode code, string registryCode, string detail) =>
        new() { Code = code, RegistryCode = registryCode, Detail = detail };
}

/// <summary>Veri kalitesi uyari turleri.</summary>
public enum SyncWarningCode
{
    /// <summary>Kartta TCKN yok; kisi olusturulmadi (<c>KR-043</c>, SYG-KMLK-007).</summary>
    MissingNationalId = 1,

    /// <summary>TCKN sagla algoritmasini gecmiyor; kisi olusturulmadi (SYG-KMLK-007).</summary>
    InvalidNationalId = 2,

    /// <summary>Kartta dogum tarihi yok; kisi olusturulmadi.</summary>
    MissingBirthDate = 3,

    /// <summary>Kartta birden fazla e-posta var; ilki kullanildi.</summary>
    MultipleEmailsOnCard = 4,

    /// <summary>Kartta bicimi gecersiz e-posta var; kullanilmadi.</summary>
    InvalidEmail = 5,

    /// <summary>Cep telefonu gecersiz; kullanilmadi (SYG-KMLK-009).</summary>
    InvalidMobilePhone = 6,

    /// <summary>Ayni e-posta birden fazla kisiye tanimli (SYG-KMLK-008).</summary>
    SharedEmail = 7,

    /// <summary>Kisinin kartlari arasinda farkli dogum tarihi var.</summary>
    ConflictingBirthDate = 8,

    /// <summary>Kisinin kartlari arasinda farkli e-posta var.</summary>
    ConflictingEmail = 9,

    /// <summary>Kisinin kartlari arasinda farkli ad veya soyad var.</summary>
    ConflictingName = 10,

    /// <summary>Kartin firmasi LOGO firma listesinde yok.</summary>
    UnknownCompany = 11,

    /// <summary>HRMS'teki istihdamin karti LOGO'da artik bulunmuyor.</summary>
    MissingFromSource = 12,
}
