using ClosedXML.Excel;
using Dsg.Hrms.Application.Common.Reports;

namespace Dsg.Hrms.Infrastructure.Reports;

/// <summary>
/// Excel yazicisi (ClosedXML, MIT; ADR-0014 §1, §6).
/// </summary>
/// <remarks>
/// <para>
/// Ilk sayfa "Rapor bilgisi"dir: zorunlu ust bilgi ve KVKK uyarisi (ADR-0014 §3, §6). Ikinci
/// sayfa duz listedir: baslik satiri dondurulur, otomatik suzgec aciktir. Ek sayfalar
/// (<see cref="ReportDocument.AdditionalSheets"/>) ayni bicimde arkasindan gelir.
/// </para>
/// <para>
/// Metin sutunlari (TCKN, sicil, telefon dahil) metin olarak ve metin bicimiyle yazilir; Excel
/// bastaki sifiri silmez, uzun sayiyi bilimsel gosterime cevirmez. Metin degeri formul olarak
/// yazilmaz: "=" ile baslayan bir ad hucrede duz metindir.
/// </para>
/// </remarks>
public sealed class ExcelReportWriter : IReportWriter
{
    private const string DateFormat = "dd.mm.yyyy";
    private const double MaxColumnWidth = 60;

    /// <inheritdoc />
    public ReportFormat Format => ReportFormat.Excel;

    /// <inheritdoc />
    public string ContentType => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <inheritdoc />
    public string Extension => "xlsx";

    /// <inheritdoc />
    public byte[] Write(ReportDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        using var workbook = new XLWorkbook();
        WriteInfo(workbook.AddWorksheet("Rapor bilgisi"), document);
        WriteData(workbook.AddWorksheet(SheetName(document.DataSheetName ?? document.Title)), document.Columns, document.Rows);
        foreach (var sheet in document.AdditionalSheets)
        {
            WriteData(workbook.AddWorksheet(SheetName(sheet.Name)), sheet.Columns, sheet.Rows);
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void WriteInfo(IXLWorksheet sheet, ReportDocument document)
    {
        sheet.Cell(1, 1).SetValue(document.Organization).Style.Font.SetBold().Font.SetFontSize(12);
        sheet.Cell(2, 1).SetValue(document.Title).Style.Font.SetBold().Font.SetFontSize(16);

        var row = 4;
        foreach (var info in document.Info)
        {
            sheet.Cell(row, 1).SetValue(info.Label).Style.Font.SetBold();
            sheet.Cell(row, 2).SetValue(info.Value).Style.Alignment.SetWrapText();
            row++;
        }

        if (document.ContainsPersonalData)
        {
            row++;
            var warning = sheet.Range(row, 1, row, 2).Merge();
            warning.Value = ReportDocument.PersonalDataWarning;
            warning.Style.Font.SetBold().Font.SetFontColor(XLColor.FromHtml("#8A1C1C"));
            warning.Style.Alignment.SetWrapText().Alignment.SetVertical(XLAlignmentVerticalValues.Top);
            warning.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#FDECEA"));
            sheet.Row(row).Height = 48;
        }

        sheet.Column(1).Width = 24;
        sheet.Column(2).Width = 100;
        sheet.Column(2).Style.Alignment.SetVertical(XLAlignmentVerticalValues.Top);
    }

    private static void WriteData(IXLWorksheet sheet, IReadOnlyList<ReportColumn> columns, IReadOnlyList<ReportRow> rows)
    {
        for (var c = 0; c < columns.Count; c++)
        {
            var header = sheet.Cell(1, c + 1);
            header.SetValue(columns[c].Header);
            header.Style.Font.SetBold();
            header.Style.Fill.SetBackgroundColor(XLColor.FromHtml("#E8EEF7"));
        }

        for (var r = 0; r < rows.Count; r++)
        {
            var values = rows[r].Values;
            for (var c = 0; c < columns.Count; c++)
            {
                var cell = sheet.Cell(r + 2, c + 1);
                switch (values[c])
                {
                    case DateOnly date:
                        cell.SetValue(date.ToDateTime(TimeOnly.MinValue));
                        break;
                    case int number:
                        cell.SetValue(number);
                        break;
                    case string text:
                        cell.SetValue(text);
                        break;
                    default:
                        // Bos deger veya Excel'de yeri olmayan tur (fotograf): hucre bos kalir.
                        break;
                }
            }
        }

        var lastRow = Math.Max(1, rows.Count + 1);
        for (var c = 0; c < columns.Count; c++)
        {
            var column = sheet.Column(c + 1);
            var body = sheet.Range(2, c + 1, lastRow, c + 1);
            switch (columns[c].Kind)
            {
                case ReportColumnKind.Date:
                    body.Style.DateFormat.SetFormat(DateFormat);
                    break;
                case ReportColumnKind.Number:
                    body.Style.NumberFormat.SetNumberFormatId(1);
                    break;
                default:
                    // "@" metin bicimi: kullanici hucreyi duzenlese de deger sayiya donmez.
                    body.Style.NumberFormat.SetFormat("@");
                    break;
            }

            column.AdjustToContents(1, Math.Min(lastRow, 500));
            if (column.Width > MaxColumnWidth)
            {
                column.Width = MaxColumnWidth;
            }
        }

        sheet.SheetView.FreezeRows(1);
        sheet.Range(1, 1, lastRow, columns.Count).SetAutoFilter();
    }

    /// <summary>Excel sayfa adi en fazla 31 karakterdir ve bazi isaretleri almaz.</summary>
    private static string SheetName(string title)
    {
        var cleaned = new string([.. title.Where(ch => ch is not ('\\' or '/' or '?' or '*' or '[' or ']' or ':'))]);
        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }
}
