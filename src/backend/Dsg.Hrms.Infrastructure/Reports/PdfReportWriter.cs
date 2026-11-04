using System.Globalization;
using System.Reflection;
using Dsg.Hrms.Application.Common.Reports;
using SkiaSharp;

namespace Dsg.Hrms.Infrastructure.Reports;

/// <summary>
/// PDF yazicisi (SkiaSharp, MIT; KR-131, ADR-0014 §1).
/// </summary>
/// <remarks>
/// <para>
/// Yatay A4. Ilk sayfada zorunlu ust bilgi ve KVKK uyarisi (ADR-0014 §3, §6); tablo basligi
/// her sayfada tekrarlanir; her sayfanin altinda "Sayfa X / Y" yazar.
/// </para>
/// <para>
/// Yazi tipi (Noto Sans, OFL) uygulamaya gomuludur: Linux konteynerinde sistem yazi tipi yoktur
/// ve Turkce karakterler gomulu yazi tipi olmadan bozulur (ADR-0014 §1).
/// </para>
/// <para>
/// Sutun genisligi icerige gore hesaplanir; sigmayan metin hucre icinde satir atlar. Fotograf
/// sutunu sabit genisliktedir; goruntu oranini koruyarak kucuk kareye sigdirilir.
/// </para>
/// </remarks>
public sealed class PdfReportWriter : IReportWriter
{
    private const float PageWidth = 842f;
    private const float PageHeight = 595f;
    private const float Margin = 28f;
    private const float Padding = 3f;
    private const float PhotoSize = 30f;
    private const float FooterHeight = 16f;
    private const float MaxNaturalWidth = 220f;
    private const float MaxWordWidth = 110f;
    private const float MinFontSize = 5.5f;
    private const float Slack = 1f;
    private const int MeasuredRows = 2000;

    private static readonly SKColor Ink = new(0x1F, 0x2A, 0x44);
    private static readonly SKColor Muted = new(0x5B, 0x67, 0x80);
    private static readonly SKColor Rule = new(0xD9, 0xDF, 0xEA);
    private static readonly SKColor HeaderFill = new(0xE8, 0xEE, 0xF7);
    private static readonly SKColor WarningFill = new(0xFD, 0xEC, 0xEA);
    private static readonly SKColor WarningInk = new(0x8A, 0x1C, 0x1C);

    private static readonly Lazy<SKTypeface> RegularFace = new(() => Load("Dsg.Hrms.Infrastructure.Fonts.Regular"));
    private static readonly Lazy<SKTypeface> BoldFace = new(() => Load("Dsg.Hrms.Infrastructure.Fonts.Bold"));

    /// <inheritdoc />
    public ReportFormat Format => ReportFormat.Pdf;

    /// <inheritdoc />
    public string ContentType => "application/pdf";

    /// <inheritdoc />
    public string Extension => "pdf";

    /// <inheritdoc />
    public byte[] Write(ReportDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.AdditionalSheets.Count > 0)
        {
            throw new NotSupportedException("PDF yazicisi ek sayfali raporu yazmaz; ek sayfalar yalnizca Excel'dedir.");
        }

        // Sutun cogaldikca yazi kuculur; sozcukler bolunmeden sigana kadar kademeli denenir.
        var size = document.Columns.Count switch
        {
            <= 8 => 8.5f,
            <= 14 => 7.5f,
            _ => 7f,
        };
        var layout = new Layout(document, size);
        while (!layout.Fits && size > MinFontSize)
        {
            layout.Dispose();
            size -= 0.5f;
            layout = new Layout(document, size);
        }

        using var owned = layout;

        using var stream = new MemoryStream();
        var metadata = new SKDocumentPdfMetadata
        {
            Title = document.Title,
            Creator = "DSG-HRMS",
            Producer = "DSG-HRMS",
        };
        using (var pdf = SKDocument.CreatePdf(stream, metadata))
        {
            var pages = layout.Paginate();
            for (var p = 0; p < pages.Count; p++)
            {
                var canvas = pdf.BeginPage(PageWidth, PageHeight);
                var top = p == 0 ? layout.DrawFirstPageHeader(canvas) : layout.DrawRunningHeader(canvas);
                layout.DrawTable(canvas, top, pages[p]);
                layout.DrawFooter(canvas, p + 1, pages.Count);
                pdf.EndPage();
            }

            pdf.Close();
        }

        return stream.ToArray();
    }

    private static SKTypeface Load(string resource)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Gomulu yazi tipi bulunamadi: {resource}");
        return SKTypeface.FromStream(stream)
            ?? throw new InvalidOperationException($"Yazi tipi okunamadi: {resource}");
    }

    /// <summary>Hucrenin metni; fotograf icin <c>null</c>.</summary>
    private static string Text(object? value) => value switch
    {
        DateOnly date => date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture),
        int number => number.ToString(CultureInfo.GetCultureInfo("tr-TR")),
        string text => text,
        _ => string.Empty,
    };

    /// <summary>Metni genislige gore satirlara boler; tek basina sigmayan sozcugu harf harf boler.</summary>
    private static List<string> Wrap(string text, SKFont font, float width)
    {
        var lines = new List<string>();
        foreach (var paragraph in text.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n'))
        {
            var current = string.Empty;
            foreach (var word in paragraph.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                var candidate = current.Length == 0 ? word : $"{current} {word}";
                if (font.MeasureText(candidate) <= width)
                {
                    current = candidate;
                    continue;
                }

                if (current.Length > 0)
                {
                    lines.Add(current);
                }

                current = word;
                while (current.Length > 1 && font.MeasureText(current) > width)
                {
                    var fit = current.Length - 1;
                    while (fit > 1 && font.MeasureText(current[..fit]) > width)
                    {
                        fit--;
                    }

                    lines.Add(current[..fit]);
                    current = current[fit..];
                }
            }

            lines.Add(current);
        }

        return lines;
    }

    /// <summary>Bir sayfaya dusen satirlar.</summary>
    private sealed record PageSlice(int Start, int Count);

    /// <summary>Olculer, yazi tipleri ve cizim.</summary>
    private sealed class Layout : IDisposable
    {
        private readonly ReportDocument _document;
        private readonly SKFont _cell;
        private readonly SKFont _header;
        private readonly SKFont _small;
        private readonly SKFont _smallBold;
        private readonly SKFont _title;
        private readonly SKPaint _ink = new() { Color = Ink, IsAntialias = true };
        private readonly SKPaint _muted = new() { Color = Muted, IsAntialias = true };
        private readonly SKPaint _warningInk = new() { Color = WarningInk, IsAntialias = true };
        private readonly SKPaint _rule = new() { Color = Rule, StrokeWidth = 0.5f, IsAntialias = true };
        private readonly SKPaint _headerFill = new() { Color = HeaderFill, IsAntialias = true };
        private readonly SKPaint _warningFill = new() { Color = WarningFill, IsAntialias = true };
        private readonly float[] _widths;
        private readonly float _lineHeight;
        private readonly float _headerHeight;
        private readonly List<List<string>[]> _cells;
        private readonly float[] _rowHeights;
        private readonly List<string>[] _headerLines;

        public Layout(ReportDocument document, float size)
        {
            _document = document;
            _cell = new SKFont(RegularFace.Value, size);
            _header = new SKFont(BoldFace.Value, size);
            _small = new SKFont(RegularFace.Value, 8f);
            _smallBold = new SKFont(BoldFace.Value, 8f);
            _title = new SKFont(BoldFace.Value, 15f);
            _lineHeight = size * 1.3f;

            _widths = ColumnWidths();
            var headers = document.Columns.Select((c, i) => Wrap(c.Header, _header, _widths[i] - (2 * Padding))).ToArray();
            _headerHeight = (headers.Max(h => h.Count) * _lineHeight) + (2 * Padding);
            _headerLines = headers;

            _cells = [.. document.Rows.Select(row => document.Columns
                .Select((c, i) => c.Kind == ReportColumnKind.Photo ? [] : Wrap(Text(row.Values[i]), _cell, _widths[i] - (2 * Padding)))
                .ToArray())];
            _rowHeights = [.. _cells.Select((lines, r) =>
            {
                var text = (lines.Length == 0 ? 1 : Math.Max(1, lines.Max(l => l.Count))) * _lineHeight;
                var photo = document.Columns.Any(c => c.Kind == ReportColumnKind.Photo) ? PhotoSize : 0f;
                return Math.Max(text, photo) + (2 * Padding);
            })];
        }

        private static float Available => PageWidth - (2 * Margin);

        private static float Bottom => PageHeight - Margin - FooterHeight;

        /// <summary>
        /// En kucuk genislikler sayfaya sigiyor mu; sigmiyorsa yazici daha kucuk yazi tipini dener.
        /// </summary>
        public bool Fits { get; private set; }

        /// <summary>
        /// Dogal genislik: baslik ve (ilk satirlarda) en uzun deger. En kucuk genislik sozcugu
        /// bolmeyen genisliktir: tarih ve sayi butun deger, metin en uzun sozcuk (ust sinirli).
        /// Toplam sigmazsa her sutun en kucuk genisliginin ustundeki payi oraninda daralir; en
        /// kucukler de sigmazsa (en kucuk yazi tipinde) oranla daralir ve sozcuk harf harf bolunur.
        /// </summary>
        private float[] ColumnWidths()
        {
            var columns = _document.Columns;
            var natural = new float[columns.Count];
            var minimum = new float[columns.Count];
            for (var i = 0; i < columns.Count; i++)
            {
                if (columns[i].Kind == ReportColumnKind.Photo)
                {
                    // Kare fotograf; baslik daha genisse baslik bolunmesin.
                    natural[i] = minimum[i] = Math.Max(PhotoSize, _header.MeasureText(columns[i].Header)) + (2 * Padding) + Slack;
                    continue;
                }

                var widest = _header.MeasureText(columns[i].Header);
                var longestWord = columns[i].Header.Split(' ').Max(w => _header.MeasureText(w));
                foreach (var row in _document.Rows.Take(MeasuredRows))
                {
                    var text = Text(row.Values[i]);
                    widest = Math.Max(widest, _cell.MeasureText(text));
                    foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                    {
                        longestWord = Math.Max(longestWord, _cell.MeasureText(word));
                    }
                }

                // Olcum yuvarlamasi tam sigan metni bir sonraki satira atmasin diye kucuk pay.
                natural[i] = Math.Min(widest, MaxNaturalWidth) + (2 * Padding) + Slack;
                minimum[i] = columns[i].Kind is ReportColumnKind.Date or ReportColumnKind.Number
                    ? natural[i]
                    : Math.Min(longestWord, MaxWordWidth) + (2 * Padding) + Slack;
            }

            var minTotal = minimum.Sum();
            Fits = minTotal <= Available;

            var total = natural.Sum();
            if (total <= Available)
            {
                // Bos kalan genislik metin sutunlarina oraninda dagitilir; tablo sayfayi doldurur.
                var flexible = columns.Select((c, i) => c.Kind == ReportColumnKind.Text ? natural[i] : 0f).Sum();
                var extra = Available - total;
                return [.. natural.Select((w, i) => columns[i].Kind != ReportColumnKind.Text || flexible == 0 ? w : w + (extra * w / flexible))];
            }

            if (!Fits)
            {
                return [.. minimum.Select(w => w * Available / minTotal)];
            }

            var slack = natural.Select((w, i) => w - minimum[i]).ToArray();
            var ratio = (Available - minTotal) / slack.Sum();
            return [.. minimum.Select((w, i) => w + (slack[i] * ratio))];
        }

        /// <summary>Satirlari sayfalara boler; her sayfaya en az bir satir.</summary>
        public List<PageSlice> Paginate()
        {
            var pages = new List<PageSlice>();
            var top = MeasureFirstPageHeader();
            var start = 0;
            do
            {
                var y = top + _headerHeight;
                var count = 0;
                while (start + count < _rowHeights.Length && (count == 0 || y + _rowHeights[start + count] <= Bottom))
                {
                    y += _rowHeights[start + count];
                    count++;
                }

                pages.Add(new PageSlice(start, count));
                start += count;
                top = RunningHeaderHeight;
            }
            while (start < _rowHeights.Length);

            return pages;
        }

        private static float RunningHeaderHeight => Margin + 18f;

        private float MeasureFirstPageHeader() => FirstPageHeader(null);

        public float DrawFirstPageHeader(SKCanvas canvas) => FirstPageHeader(canvas);

        /// <summary>Ilk sayfanin ust bilgisi; <paramref name="canvas"/> bossa yalnizca olcer.</summary>
        private float FirstPageHeader(SKCanvas? canvas)
        {
            var y = Margin + 9f;
            canvas?.DrawText(_document.Organization, Margin, y, SKTextAlign.Left, _smallBold, _muted);
            y += 20f;
            canvas?.DrawText(_document.Title, Margin, y, SKTextAlign.Left, _title, _ink);
            y += 12f;

            const float labelWidth = 120f;
            foreach (var info in _document.Info)
            {
                var lines = Wrap(info.Value, _small, Available - labelWidth);
                y += 11f;
                canvas?.DrawText(info.Label, Margin, y, SKTextAlign.Left, _smallBold, _ink);
                foreach (var line in lines)
                {
                    canvas?.DrawText(line, Margin + labelWidth, y, SKTextAlign.Left, _small, _ink);
                    y += 11f;
                }

                y -= 11f;
            }

            y += 8f;
            if (_document.ContainsPersonalData)
            {
                var lines = Wrap(ReportDocument.PersonalDataWarning, _smallBold, Available - 16f);
                var height = (lines.Count * 11f) + 10f;
                canvas?.DrawRoundRect(new SKRect(Margin, y, Margin + Available, y + height), 4f, 4f, _warningFill);
                var line = y + 5f + 8f;
                foreach (var text in lines)
                {
                    canvas?.DrawText(text, Margin + 8f, line, SKTextAlign.Left, _smallBold, _warningInk);
                    line += 11f;
                }

                y += height + 10f;
            }

            return y;
        }

        /// <summary>Ikinci ve sonraki sayfalarin ust satiri.</summary>
        public float DrawRunningHeader(SKCanvas canvas)
        {
            canvas.DrawText($"{_document.Title} · {_document.Organization}", Margin, Margin + 9f, SKTextAlign.Left, _smallBold, _muted);
            return RunningHeaderHeight;
        }

        public void DrawFooter(SKCanvas canvas, int page, int pages)
        {
            var y = PageHeight - Margin + 4f;
            canvas.DrawLine(Margin, y - 12f, Margin + Available, y - 12f, _rule);
            canvas.DrawText(_document.Title, Margin, y, SKTextAlign.Left, _small, _muted);
            var text = $"Sayfa {page.ToString(CultureInfo.InvariantCulture)} / {pages.ToString(CultureInfo.InvariantCulture)}";
            canvas.DrawText(text, Margin + Available - _small.MeasureText(text), y, SKTextAlign.Left, _small, _muted);
        }

        public void DrawTable(SKCanvas canvas, float top, PageSlice slice)
        {
            var baseline = -_cell.Metrics.Ascent;

            // Baslik satiri
            canvas.DrawRect(new SKRect(Margin, top, Margin + Available, top + _headerHeight), _headerFill);
            var x = Margin;
            for (var c = 0; c < _widths.Length; c++)
            {
                var y = top + Padding + baseline;
                foreach (var line in _headerLines[c])
                {
                    canvas.DrawText(line, x + Padding, y, SKTextAlign.Left, _header, _ink);
                    y += _lineHeight;
                }

                x += _widths[c];
            }

            var rowTop = top + _headerHeight;
            if (_rowHeights.Length == 0)
            {
                canvas.DrawText("Süzgece uyan kayıt yok.", Margin + Padding, rowTop + Padding + baseline, SKTextAlign.Left, _cell, _muted);
                return;
            }

            for (var r = slice.Start; r < slice.Start + slice.Count; r++)
            {
                x = Margin;
                for (var c = 0; c < _widths.Length; c++)
                {
                    if (_document.Columns[c].Kind == ReportColumnKind.Photo)
                    {
                        DrawPhoto(canvas, _document.Rows[r].Values[c] as byte[], x + Padding, rowTop + Padding);
                    }
                    else
                    {
                        var y = rowTop + Padding + baseline;
                        foreach (var line in _cells[r][c])
                        {
                            canvas.DrawText(line, x + Padding, y, SKTextAlign.Left, _cell, _ink);
                            y += _lineHeight;
                        }
                    }

                    x += _widths[c];
                }

                rowTop += _rowHeights[r];
                canvas.DrawLine(Margin, rowTop, Margin + Available, rowTop, _rule);
            }
        }

        /// <summary>Kucuk fotograf: oran korunur, kareye ortalanir. Cozulemeyen goruntu bos birakilir.</summary>
        private static void DrawPhoto(SKCanvas canvas, byte[]? content, float left, float top)
        {
            if (content is null || content.Length == 0)
            {
                return;
            }

            using var image = SKImage.FromEncodedData(content);
            if (image is null)
            {
                return;
            }

            var scale = Math.Min(PhotoSize / image.Width, PhotoSize / image.Height);
            var width = image.Width * scale;
            var height = image.Height * scale;
            var target = SKRect.Create(left + ((PhotoSize - width) / 2), top + ((PhotoSize - height) / 2), width, height);
            canvas.DrawImage(image, target, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        }

        public void Dispose()
        {
            _cell.Dispose();
            _header.Dispose();
            _small.Dispose();
            _smallBold.Dispose();
            _title.Dispose();
            _ink.Dispose();
            _muted.Dispose();
            _warningInk.Dispose();
            _rule.Dispose();
            _headerFill.Dispose();
            _warningFill.Dispose();
        }
    }
}
