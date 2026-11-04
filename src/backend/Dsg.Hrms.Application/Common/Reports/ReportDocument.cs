namespace Dsg.Hrms.Application.Common.Reports;

/// <summary>Raporun dosya bicimi (ADR-0014 §1).</summary>
public enum ReportFormat
{
    /// <summary>Excel (<c>.xlsx</c>), duz liste.</summary>
    Excel = 1,

    /// <summary>PDF, yatay A4.</summary>
    Pdf = 2,
}

/// <summary>Bir sutundaki degerin turu; yazici bicimi bu ture gore secer.</summary>
public enum ReportColumnKind
{
    /// <summary>Metin. Excel'de metin olarak yazilir: TCKN, sicil ve telefonun bastaki sifiri korunur (ADR-0014 §6).</summary>
    Text = 1,

    /// <summary>Tarih (<see cref="DateOnly"/>); <c>GG.AA.YYYY</c>.</summary>
    Date = 2,

    /// <summary>Tam sayi.</summary>
    Number = 3,

    /// <summary>Kucuk fotograf (JPEG baytlari); yalnizca PDF'te (ADR-0022 §4).</summary>
    Photo = 4,
}

/// <summary>Rapor sutunu.</summary>
/// <param name="Header">Basliktaki ad.</param>
/// <param name="Kind">Deger turu.</param>
public sealed record ReportColumn(string Header, ReportColumnKind Kind);

/// <summary>
/// Raporun basindaki zorunlu bilgi satiri (ADR-0014 §3): rapor adi, uretim zamani, veri anlik
/// goruntusu, ureten kullanici, suzgecler, kayit sayisi.
/// </summary>
/// <param name="Label">Ad.</param>
/// <param name="Value">Deger.</param>
public sealed record ReportInfo(string Label, string Value);

/// <summary>Bir satir: sutun sirasinda degerler (<see cref="string"/>, <see cref="DateOnly"/>, <see cref="int"/>, JPEG <see cref="byte"/> dizisi veya <c>null</c>).</summary>
/// <param name="Values">Degerler.</param>
public sealed record ReportRow(IReadOnlyList<object?> Values);

/// <summary>Ek veri sayfasi (yalnizca Excel): ayni rapordaki ikinci bir liste.</summary>
/// <param name="Name">Sayfa adi.</param>
/// <param name="Columns">Sutunlar.</param>
/// <param name="Rows">Satirlar.</param>
public sealed record ReportSheet(string Name, IReadOnlyList<ReportColumn> Columns, IReadOnlyList<ReportRow> Rows);

/// <summary>
/// Bicimden bagimsiz rapor (ADR-0014 §2): ayni belge Excel'e de PDF'e de yazilir. Veri ve
/// bicim ayridir; yazici yalnizca bu modeli bilir.
/// </summary>
/// <param name="Title">Rapor adi.</param>
/// <param name="Info">Zorunlu bilgi satirlari.</param>
/// <param name="Columns">Sutunlar.</param>
/// <param name="Rows">Satirlar.</param>
public sealed record ReportDocument(
    string Title,
    IReadOnlyList<ReportInfo> Info,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<ReportRow> Rows)
{
    /// <summary>Ana veri sayfasinin adi (Excel); verilmezse rapor adi.</summary>
    public string? DataSheetName { get; init; }

    /// <summary>
    /// Ana listeden sonraki ek sayfalar. Yalnizca Excel yazar; PDF yazicisi ek sayfali belgeyi
    /// reddeder (ikinci listeyi sessizce dusurmemek icin).
    /// </summary>
    public IReadOnlyList<ReportSheet> AdditionalSheets { get; init; } = [];

    /// <summary>Kurum adi; her raporun basinda yazar.</summary>
    public string Organization { get; init; } = "Düzen Sağlık Grubu";

    /// <summary>
    /// Kisisel veri iceren raporda ilk sayfada KVKK uyarisi bulunur (ADR-0014 §6): dosya kurum
    /// disina ciktiginda acan kisi icerigin KVKK kapsaminda oldugunu gorur.
    /// </summary>
    public bool ContainsPersonalData { get; init; } = true;

    /// <summary>KVKK uyarisinin metni.</summary>
    public const string PersonalDataWarning =
        "Bu belge 6698 sayılı Kişisel Verilerin Korunması Kanunu kapsamında kişisel veri içerir. " +
        "Yalnızca görevinizin gerektirdiği amaçla kullanın; yetkisiz kişilerle paylaşmayın, " +
        "işiniz bitince güvenli biçimde silin.";
}

/// <summary>Uretilen dosya.</summary>
/// <param name="FileName">Indirilen dosyanin adi.</param>
/// <param name="ContentType">MIME turu.</param>
/// <param name="Content">Dosya baytlari.</param>
#pragma warning disable CA1819 // Dosya baytlari kopyalanmadan yanita yazilir.
public sealed record ReportFile(string FileName, string ContentType, byte[] Content);
#pragma warning restore CA1819

/// <summary>Raporu bir bicimde dosyaya yazar (ADR-0014 §2). Altyapi katmaninda her bicim icin bir uygulama vardir.</summary>
public interface IReportWriter
{
    /// <summary>Yazdigi bicim.</summary>
    ReportFormat Format { get; }

    /// <summary>MIME turu.</summary>
    string ContentType { get; }

    /// <summary>Dosya uzantisi (noktasiz).</summary>
    string Extension { get; }

    /// <summary>Belgeyi yazar.</summary>
    byte[] Write(ReportDocument document);
}
