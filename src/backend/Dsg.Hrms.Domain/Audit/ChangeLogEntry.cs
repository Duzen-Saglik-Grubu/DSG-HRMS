namespace Dsg.Hrms.Domain.Audit;

/// <summary>
/// Bir is kaydinda yapilan degisikligin denetim izi (ADR-0009 §2).
/// </summary>
/// <remarks>
/// <para>
/// <b>Bu kayit degistirilemez.</b> Uygulama bu tabloya yalnizca ekleme yapar;
/// guncelleme ve silme ucu noktasi yoktur. Ayrica veritabani duzeyinde bir tetikleyici
/// UPDATE ve DELETE islemlerini reddeder — uygulamadaki bir hata veya dogrudan
/// veritabani erisimi denetim izini bozamaz.
/// </para>
/// <para>
/// <see cref="Domain.Common.Entity"/> tabanindan TURETILMEZ: denetim kaydinin
/// kendisinin denetim alanlari, yumusak silme isareti veya dis kimligi olmaz.
/// Denetim izi, denetlenen seyin kurallarina tabi degildir.
/// </para>
/// </remarks>
public sealed class ChangeLogEntry
{
    /// <summary>Birincil anahtar.</summary>
    public long Id { get; private set; }

    /// <summary>Degisikligin gerceklestigi an (UTC).</summary>
    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>
    /// Islemi yapan kullanici hesabinin kimligi.
    /// Arka plan islerinde ve sistem islemlerinde <c>null</c>.
    /// </summary>
    public long? UserAccountId { get; init; }

    /// <summary>Degisen varligin adi (ornegin <c>Person</c>).</summary>
    public required string EntityName { get; init; }

    /// <summary>
    /// Degisen kaydin dis kimligi (<c>PublicId</c>).
    /// </summary>
    /// <remarks>
    /// Ic birincil anahtar degil dis kimlik kullanilir: dis kimlik istemci tarafinda
    /// uretildigi icin kayit veritabanina yazilmadan ONCE bellidir. Boylece ekleme
    /// islemleri de tek islemde, ikinci bir kaydetme adimi olmadan kaydedilebilir.
    /// </remarks>
    public Guid EntityId { get; init; }

    /// <summary>Islem turu.</summary>
    public AuditOperation Operation { get; init; }

    /// <summary>
    /// Degisen alanlarin eski ve yeni degerleri (JSON).
    /// </summary>
    /// <remarks>
    /// Kisisel veri iceren alanlar <b>maskelenmis</b> olarak yazilir; sir niteligindeki
    /// alanlarin degeri hic yazilmaz (`KR-059`). Denetim izi "neyin degistigini"
    /// gostermek icindir, veriyi ikinci bir yerde saklamak icin degil.
    /// </remarks>
    public required string Changes { get; init; }

    /// <summary>
    /// Uygulama logu ile iliskilendirme kimligi.
    /// </summary>
    /// <remarks>
    /// Bir denetim kaydindan yola cikarak ayni istegin teknik gunluk satirlarina
    /// ulasilabilmesini saglar; bir olay incelemesinin ilk adimi budur.
    /// </remarks>
    public string? TraceId { get; init; }

    /// <summary>Istegin geldigi IP adresi.</summary>
    public string? IpAddress { get; init; }
}
