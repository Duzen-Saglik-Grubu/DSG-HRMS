namespace Dsg.Hrms.Domain.Audit;

/// <summary>
/// Denetim izine kaydedilen islem turu (ADR-0009 §2).
/// </summary>
public enum AuditOperation
{
    /// <summary>Yeni kayit olusturuldu.</summary>
    Insert = 1,

    /// <summary>Mevcut kayit guncellendi.</summary>
    Update = 2,

    /// <summary>
    /// Kayit silindi.
    /// </summary>
    /// <remarks>
    /// Yumusak silme (ADR-0004 §5) veritabani acisindan bir guncellemedir; denetim
    /// izinde ise <b>silme</b> olarak gorunur. Denetim izini okuyan kisi teknik
    /// ayrintiyla degil, is anlamiyla ilgilenir.
    /// </remarks>
    Delete = 3,
}
