namespace Dsg.Hrms.Domain.Audit;

/// <summary>
/// Erisim kaydina yazilan olay turu (ADR-0009 §3).
/// </summary>
/// <remarks>
/// Turler bilincli olarak ayristirilmistir: bir sizinti incelemesinde "400 kaydi
/// listeledi" ile "400 kaydi Excel'e aktardi" ayni agirlikta degildir.
/// </remarks>
public enum AccessType
{
    /// <summary>Tek bir kaydin kisisel verisi goruntulendi.</summary>
    View = 1,

    /// <summary>
    /// Ozel nitelikli kisisel veri goruntulendi (saglik raporu, engellilik bilgisi).
    /// </summary>
    /// <remarks>
    /// KVKK md. 6 kapsamindaki veriler ayri bir tur olarak tutulur; denetimde
    /// oncelikle bu kayitlar sorulur.
    /// </remarks>
    SpecialCategoryView = 2,

    /// <summary>Birden fazla kaydin listelendi.</summary>
    List = 3,

    /// <summary>Veri Excel veya PDF olarak disariya aktarildi.</summary>
    Export = 4,

    /// <summary>Toplu rapor uretildi.</summary>
    Report = 5,

    /// <summary>Ozluk dosyasi eki indirildi.</summary>
    FileDownload = 6,
}
