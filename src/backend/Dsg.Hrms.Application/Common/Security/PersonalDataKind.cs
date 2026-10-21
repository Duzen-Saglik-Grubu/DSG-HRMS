namespace Dsg.Hrms.Application.Common.Security;

/// <summary>
/// Maskeleme kuralini belirleyen kisisel veri turu (ADR-0009 §4).
/// </summary>
public enum PersonalDataKind
{
    /// <summary>
    /// Turu bilinmeyen kisisel veri. <b>Tamamen</b> maskelenir.
    /// </summary>
    /// <remarks>
    /// Varsayilan deger kasitli olarak en kisitlayici secenektir: bir gelistirici
    /// <c>[PersonalData]</c> yazip turu belirtmeyi unutursa sonuc sizinti degil,
    /// fazla maskeleme olur.
    /// </remarks>
    Unspecified = 0,

    /// <summary>T.C. Kimlik Numarasi.</summary>
    NationalId = 1,

    /// <summary>Telefon numarasi.</summary>
    Phone = 2,

    /// <summary>E-posta adresi.</summary>
    Email = 3,

    /// <summary>IBAN.</summary>
    Iban = 4,

    /// <summary>
    /// Kullanicinin yazdigi serbest arama metni. Icindeki uzun rakam dizileri (TCKN,
    /// telefon) ve e-posta adresleri maskelenir; ad gibi duz metin okunur kalir (#244, B-07).
    /// </summary>
    SearchText = 5,
}
