namespace Dsg.Hrms.Domain.Audit;

/// <summary>
/// Kisisel veriye erisimin kaydi (ADR-0009 §3).
/// </summary>
/// <remarks>
/// <para>
/// Degisiklik kaydi (<see cref="ChangeLogEntry"/>) tek basina yeterli degildir: bir
/// kullanicinin 400 personelin ozluk dosyasini goruntulemesi hicbir degisiklik
/// uretmez, ancak ciddi bir olaydir. Bu kayit o soruyu cevaplar.
/// </para>
/// <para>
/// <b>Bu kayit da degistirilemez</b> (KR-060): uygulama yalnizca ekleme yapar,
/// veritabani tetikleyicileri guncelleme ve silmeyi reddeder.
/// </para>
/// </remarks>
public sealed class AccessLogEntry
{
    /// <summary>Birincil anahtar.</summary>
    public long Id { get; private set; }

    /// <summary>Erisimin gerceklestigi an (UTC).</summary>
    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>Veriye erisen kullanici hesabinin kimligi.</summary>
    public long? UserAccountId { get; init; }

    /// <summary>Erisim turu.</summary>
    public AccessType AccessType { get; init; }

    /// <summary>Erisilen veri turu (ornegin <c>Person</c>).</summary>
    public required string EntityName { get; init; }

    /// <summary>
    /// Tek kayda erisimde, erisilen kaydin dis kimligi. Listeleme ve disa
    /// aktarmada <c>null</c>.
    /// </summary>
    public Guid? EntityId { get; init; }

    /// <summary>
    /// Erisilen kayit sayisi.
    /// </summary>
    /// <remarks>
    /// Disa aktarma kayitlarinda <b>kac kisinin verisinin</b> disari ciktigi bilgisi,
    /// olasi bir sizinti incelemesinin ilk sorusudur (ADR-0009 §3).
    /// </remarks>
    public int RecordCount { get; init; }

    /// <summary>
    /// Uygulanan suzgecler (JSON).
    /// </summary>
    /// <remarks>
    /// Suzgec degerleri <b>maskelenmis</b> yazilir: arama kutusuna girilen bir T.C.
    /// kimlik numarasi aksi hâlde erisim kaydina duz metin duserdi (KR-059).
    /// </remarks>
    public required string Filters { get; init; }

    /// <summary>Uygulama logu ile iliskilendirme kimligi.</summary>
    public string? TraceId { get; init; }

    /// <summary>Istegin geldigi IP adresi.</summary>
    public string? IpAddress { get; init; }
}
