using ClosedXML.Excel;
using Dsg.Hrms.Application.Common.Reports;
using Dsg.Hrms.Infrastructure.Reports;
using SkiaSharp;
using UglyToad.PdfPig;

namespace Dsg.Hrms.Infrastructure.Tests.Reports;

/// <summary>
/// Excel ve PDF yazicilari (ADR-0014 §1, §3, §6; SYG-PERS-032). Veriler SENTETIKTIR.
/// </summary>
public sealed class ReportWriterTests
{
    private static ReportDocument Document(int rows = 2, bool photo = false)
    {
        List<ReportColumn> columns =
        [
            new("Ad", ReportColumnKind.Text),
            new("Soyad", ReportColumnKind.Text),
            new("TCKN", ReportColumnKind.Text),
            new("Doğum tarihi", ReportColumnKind.Date),
            new("Yaş", ReportColumnKind.Number),
        ];
        if (photo)
        {
            columns.Add(new ReportColumn("Fotoğraf", ReportColumnKind.Photo));
        }

        var picture = photo ? Jpeg() : null;
        return new ReportDocument(
            "Personel Listesi",
            [new ReportInfo("Üretim zamanı", "04.11.2026 10:15"), new ReportInfo("Kayıt sayısı", rows.ToString(System.Globalization.CultureInfo.InvariantCulture))],
            columns,
            [.. Enumerable.Range(0, rows).Select(i => new ReportRow(
                [
                    i == 0 ? "ŞÜKRÜ ĞÖÇİ" : "=1+1",
                    "IŞIK",
                    "01234567890",
                    new DateOnly(1985, 4, 12),
                    41,
                    .. photo ? new object?[] { picture } : [],
                ]))]);
    }

    [Fact]
    public void Excel_keeps_national_id_as_text_dates_as_dates_and_formulas_as_plain_text()
    {
        using var stream = new MemoryStream(new ExcelReportWriter().Write(Document()));
        using var workbook = new XLWorkbook(stream);
        var data = workbook.Worksheet(2);

        // TCKN, bastaki sifir ve butun haneleriyle metin (ADR-0014 §6).
        var nationalId = data.Cell(2, 3);
        nationalId.DataType.ShouldBe(XLDataType.Text);
        nationalId.GetString().ShouldBe("01234567890");
        nationalId.Style.NumberFormat.Format.ShouldBe("@");

        var birth = data.Cell(2, 4);
        birth.DataType.ShouldBe(XLDataType.DateTime);
        birth.GetDateTime().ShouldBe(new DateTime(1985, 4, 12));
        birth.Style.DateFormat.Format.ShouldBe("dd.mm.yyyy");

        data.Cell(2, 5).GetValue<int>().ShouldBe(41);

        // "=" ile baslayan deger formul degil duz metindir.
        data.Cell(3, 1).HasFormula.ShouldBeFalse();
        data.Cell(3, 1).GetString().ShouldBe("=1+1");

        data.Cell(2, 1).GetString().ShouldBe("ŞÜKRÜ ĞÖÇİ");
    }

    [Fact]
    public void Excel_freezes_header_turns_on_auto_filter_and_starts_with_report_info_and_kvkk_warning()
    {
        using var stream = new MemoryStream(new ExcelReportWriter().Write(Document()));
        using var workbook = new XLWorkbook(stream);

        var info = workbook.Worksheet(1);
        info.Name.ShouldBe("Rapor bilgisi");
        info.Cell(2, 1).GetString().ShouldBe("Personel Listesi");
        info.CellsUsed().Select(c => c.GetString()).ShouldContain("Üretim zamanı");
        info.CellsUsed().Select(c => c.GetString()).ShouldContain(ReportDocument.PersonalDataWarning);

        var data = workbook.Worksheet(2);
        data.Name.ShouldBe("Personel Listesi");
        data.SheetView.SplitRow.ShouldBe(1);
        data.AutoFilter.IsEnabled.ShouldBeTrue();
        data.Cell(1, 3).GetString().ShouldBe("TCKN");
    }

    [Fact]
    public void Pdf_embeds_the_font_and_keeps_turkish_characters()
    {
        var bytes = new PdfReportWriter().Write(Document());

        using var pdf = PdfDocument.Open(bytes);
        var text = string.Concat(pdf.GetPages().Select(p => p.Text));
        text.ShouldContain("ŞÜKRÜ");
        text.ShouldContain("ĞÖÇİ");
        text.ShouldContain("IŞIK");
        text.ShouldContain("Doğum tarihi");
        text.ShouldContain("12.04.1985");
        text.ShouldContain("Kişisel Verilerin Korunması Kanunu");
        text.ShouldContain("Sayfa 1 / 1");

        // Yazi tipi gomulu; okuyucunun sistemindeki yazi tipine dayanilmaz.
        pdf.GetPage(1).Letters.Select(l => l.FontName).ShouldAllBe(name => name != null && name.Contains("NotoSans", StringComparison.Ordinal));
    }

    [Fact]
    public void Pdf_repeats_the_header_on_every_page_and_numbers_the_pages()
    {
        using var pdf = PdfDocument.Open(new PdfReportWriter().Write(Document(rows: 120)));

        pdf.NumberOfPages.ShouldBeGreaterThan(2);
        foreach (var page in pdf.GetPages())
        {
            page.Text.ShouldContain("TCKN");
            page.Text.ShouldContain($"Sayfa {page.Number} / {pdf.NumberOfPages}");
            page.Width.ShouldBeGreaterThan(page.Height); // yatay A4
        }
    }

    [Fact]
    public void Pdf_draws_small_photos()
    {
        using var pdf = PdfDocument.Open(new PdfReportWriter().Write(Document(photo: true)));

        pdf.GetPage(1).GetImages().Count().ShouldBe(2);

        // Fotograf sutunu basligi sigdirir; "Fotoğraf" iki satira bolunmez.
        pdf.GetPage(1).GetWords().Select(w => w.Text).ShouldContain("Fotoğraf");
    }

    [Fact]
    public void Pdf_with_no_rows_still_has_the_header_and_says_so()
    {
        using var pdf = PdfDocument.Open(new PdfReportWriter().Write(Document(rows: 0)));

        pdf.NumberOfPages.ShouldBe(1);
        pdf.GetPage(1).Text.ShouldContain("Süzgece uyan kayıt yok.");
    }

    private static byte[] Jpeg()
    {
        using var bitmap = new SKBitmap(40, 60);
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.SteelBlue);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 80);
        return data.ToArray();
    }
}
