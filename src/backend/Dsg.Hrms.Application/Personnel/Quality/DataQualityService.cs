using System.Globalization;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Common.Paging;
using Dsg.Hrms.Application.Common.Reports;
using Dsg.Hrms.Application.Personnel.Access;
using Dsg.Hrms.Application.Settings;

namespace Dsg.Hrms.Application.Personnel.Quality;

/// <summary>Rapor ekraninin verisi.</summary>
/// <param name="LastRun">En son biten senkronizasyon.</param>
/// <param name="WarningsRun">Kart duzeyindeki uyarilarin alindigi calisma.</param>
/// <param name="Counts">Her uyari turunun sayisi (suzgecten bagimsiz).</param>
/// <param name="IneligibleCount">Uye olamayacak kisi sayisi.</param>
/// <param name="Items">Suzgece uyan uyarilar, sayfalanmis.</param>
public sealed record DataQualityReport(
    DataQualityRun? LastRun,
    DataQualityRun? WarningsRun,
    IReadOnlyDictionary<DataQualityIssue, int> Counts,
    int IneligibleCount,
    PagedResult<DataQualityItem> Items);

/// <summary>Uye olamayacak kisi (SYG-PERS-071).</summary>
/// <param name="Subject">Kisi ve esas istihdami.</param>
/// <param name="Reasons">Nedenler.</param>
public sealed record IneligiblePerson(DataQualitySubject Subject, MembershipBlocker Reasons);

/// <summary>
/// Veri kalitesi raporu (PG-PERS-32, 33; SYG-PERS-016, 070, 071; ADR-0003 §5).
/// </summary>
/// <remarks>
/// <para>
/// Rapor yalnizca aktif istihdamlari kapsar. IK karti LOGO'da sicil koduyla bulur; raporda ve
/// dosyada TCKN, e-posta ve telefon YAZILMAZ, uyarinin turu yeterlidir (veri minimizasyonu).
/// </para>
/// <para>
/// Yetki uclardadir: gormek <c>personnel.quality.view</c>, aktarmak <c>personnel.quality.export</c>.
/// Bu izinler yalnizca tam kapsamli rollerdedir (SYG-PERS-055); rapor kapsam suzgeci uygulamaz.
/// Goruntuleme <c>List</c>, aktarim <c>Export</c> erisim kaydi uretir; satirdaki kisiler kayda girer.
/// </para>
/// </remarks>
public sealed class DataQualityService
{
    /// <summary>Erisim kaydindaki varlik adi.</summary>
    public const string EntityName = "PersonDataQuality";

    /// <summary>Raporun adi; dosyada ve erisim kaydinda.</summary>
    public const string ReportTitle = "Veri Kalitesi Raporu";

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    private readonly ICurrentUserScope _scope;
    private readonly IDataQualityQuery _query;
    private readonly IPersonnelQuery _persons;
    private readonly ISystemParameters _parameters;
    private readonly IAccessLogger _accessLogger;
    private readonly IDateTimeProvider _clock;
    private readonly IReadOnlyList<IReportWriter> _writers;

    /// <summary>Yeni ornek olusturur.</summary>
    public DataQualityService(
        ICurrentUserScope scope,
        IDataQualityQuery query,
        IPersonnelQuery persons,
        ISystemParameters parameters,
        IAccessLogger accessLogger,
        IDateTimeProvider clock,
        IEnumerable<IReportWriter> writers)
    {
        _scope = scope;
        _query = query;
        _persons = persons;
        _parameters = parameters;
        _accessLogger = accessLogger;
        _clock = clock;
        _writers = [.. writers];
    }

    /// <summary>Uyari turunun gorunen adi.</summary>
    public static string Label(DataQualityIssue issue) => issue switch
    {
        DataQualityIssue.MissingNationalId => "TCKN yok",
        DataQualityIssue.InvalidNationalId => "TCKN geçersiz",
        DataQualityIssue.MissingBirthDate => "Doğum tarihi yok",
        DataQualityIssue.NoContact => "İletişim bilgisi yok",
        DataQualityIssue.InvalidMobilePhone => "Geçersiz telefon biçimi",
        DataQualityIssue.SharedEmail => "Paylaşılan e-posta",
        DataQualityIssue.MissingTitle => "Ünvanı boş",
        DataQualityIssue.MissingDuty => "Görevi boş",
        _ => issue.ToString(),
    };

    /// <summary>Uye olamama nedenlerinin okunur yazimi.</summary>
    public static string Reasons(MembershipBlocker reasons)
    {
        var parts = new List<string>(3);
        if (reasons.HasFlag(MembershipBlocker.NoEmail))
        {
            parts.Add("Kurumsal e-postası yok");
        }

        if (reasons.HasFlag(MembershipBlocker.DomainNotAccepted))
        {
            parts.Add("E-postası kabul edilen alan adlarında değil");
        }

        if (reasons.HasFlag(MembershipBlocker.SharedEmail))
        {
            parts.Add("E-postası başka bir kişiyle paylaşılıyor");
        }

        return string.Join("; ", parts);
    }

    /// <summary>Rapor ekrani: son calisma, tur sayilari ve suzgece uyan uyarilar.</summary>
    public async Task<DataQualityReport> GetAsync(DataQualityIssue? issue, int page, int pageSize, CancellationToken cancellationToken)
    {
        EnsurePage(page, pageSize);

        var all = await _query.ListIssuesAsync(cancellationToken).ConfigureAwait(false);
        var ineligible = await IneligibleAsync(cancellationToken).ConfigureAwait(false);
        var counts = Enum.GetValues<DataQualityIssue>().ToDictionary(t => t, t => all.Count(i => i.Issue == t));

        var matching = issue is null ? all : [.. all.Where(i => i.Issue == issue)];
        var items = new PagedResult<DataQualityItem>([.. matching.Skip((page - 1) * pageSize).Take(pageSize)], page, pageSize, matching.Count);

        await _accessLogger.LogAsync(
            AccessRecord.List(EntityName, items.Items.Count, new Dictionary<string, string?> { ["issue"] = issue?.ToString() })
                with
            { SubjectIds = Subjects(items.Items.Select(i => i.Subject)) },
            cancellationToken).ConfigureAwait(false);

        return new DataQualityReport(
            await _query.LastFinishedRunAsync(cancellationToken).ConfigureAwait(false),
            await _query.LastWarningsRunAsync(cancellationToken).ConfigureAwait(false),
            counts,
            ineligible.Count,
            items);
    }

    /// <summary>Uye olamayacak kisiler (SYG-PERS-071).</summary>
    public async Task<PagedResult<IneligiblePerson>> ListIneligibleAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        EnsurePage(page, pageSize);

        var all = await IneligibleAsync(cancellationToken).ConfigureAwait(false);
        var result = new PagedResult<IneligiblePerson>([.. all.Skip((page - 1) * pageSize).Take(pageSize)], page, pageSize, all.Count);

        await _accessLogger.LogAsync(
            AccessRecord.List(EntityName, result.Items.Count, new Dictionary<string, string?> { ["section"] = "ineligible" })
                with
            { SubjectIds = Subjects(result.Items.Select(i => i.Subject)) },
            cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Raporu Excel'e aktarir: rapor bilgisi, "Uyarılar" (tur suzgeciyle) ve "Üye olamayacak
    /// kişiler" sayfalari. Aktarim kaydedilir (SYG-PERS-033).
    /// </summary>
    public async Task<ReportFile> ExportAsync(DataQualityIssue? issue, CancellationToken cancellationToken)
    {
        var writer = _writers.FirstOrDefault(w => w.Format == ReportFormat.Excel)
            ?? throw new InvalidOperationException("Excel yazicisi kayitli degil.");

        var all = await _query.ListIssuesAsync(cancellationToken).ConfigureAwait(false);
        var items = issue is null ? all : [.. all.Where(i => i.Issue == issue)];
        var ineligible = await IneligibleAsync(cancellationToken).ConfigureAwait(false);
        var now = _clock.UtcNow;

        var document = new ReportDocument(
            ReportTitle,
            await InfoAsync(issue, items.Count, ineligible.Count, now, cancellationToken).ConfigureAwait(false),
            [new ReportColumn("Uyarı", ReportColumnKind.Text), .. SubjectColumns],
            [.. items.Select(i => new ReportRow([Label(i.Issue), .. SubjectValues(i.Subject)]))])
        {
            DataSheetName = "Uyarılar",
            AdditionalSheets =
            [
                new ReportSheet(
                    "Üye olamayacak kişiler",
                    [.. SubjectColumns, new ReportColumn("Neden", ReportColumnKind.Text)],
                    [.. ineligible.Select(p => new ReportRow([.. SubjectValues(p.Subject), Reasons(p.Reasons)]))]),
            ],
        };

        var content = writer.Write(document);

        var subjects = Subjects(items.Select(i => i.Subject).Concat(ineligible.Select(p => p.Subject)));
        await _accessLogger.LogAsync(
            AccessRecord.Export(EntityName, items.Count + ineligible.Count, new Dictionary<string, string?>
            {
                ["format"] = ReportFormat.Excel.ToString(),
                ["issue"] = issue?.ToString(),
            }) with
            { SubjectIds = subjects },
            cancellationToken).ConfigureAwait(false);

        var stamp = CorporateTime.ToLocal(now).ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        return new ReportFile($"veri-kalitesi-{stamp}.{writer.Extension}", writer.ContentType, content);
    }

    private static readonly ReportColumn[] SubjectColumns =
    [
        new("Sicil", ReportColumnKind.Text),
        new("Ad", ReportColumnKind.Text),
        new("Soyad", ReportColumnKind.Text),
        new("Firma", ReportColumnKind.Text),
        new("Şube", ReportColumnKind.Text),
        new("Birim", ReportColumnKind.Text),
    ];

    private static object?[] SubjectValues(DataQualitySubject s) =>
        [s.RegistryCode, s.FirstName, s.LastName, s.Company, s.Branch, s.Department];

    private static List<Guid> Subjects(IEnumerable<DataQualitySubject> subjects) =>
        [.. subjects.Select(s => s.PersonId).OfType<Guid>().Distinct()];

    /// <summary>
    /// T3'un uyelik kosulu (SYG-KMLK-013, 017; RegistrationService ile ayni kural): kurumsal
    /// e-posta var, kabul edilen alan adinda ve kisiye tekil. Dogum tarihi kosulu burada yoktur:
    /// dogum tarihi olmayan kart kisi olarak olusturulmaz ve uyarilarda yer alir.
    /// </summary>
    private async Task<List<IneligiblePerson>> IneligibleAsync(CancellationToken cancellationToken)
    {
        var domains = await _parameters.GetListAsync(ParameterCatalog.AcceptedEmailDomains, cancellationToken).ConfigureAwait(false);
        var unregistered = await _query.ListUnregisteredAsync(cancellationToken).ConfigureAwait(false);

        var result = new List<IneligiblePerson>();
        foreach (var person in unregistered)
        {
            var reasons = MembershipBlocker.None;
            if (person.Email is null)
            {
                reasons |= MembershipBlocker.NoEmail;
            }
            else if (!domains.Contains(person.Email[(person.Email.LastIndexOf('@') + 1)..], StringComparer.OrdinalIgnoreCase))
            {
                reasons |= MembershipBlocker.DomainNotAccepted;
            }

            if (person.Email is not null && person.IsEmailShared)
            {
                reasons |= MembershipBlocker.SharedEmail;
            }

            if (reasons != MembershipBlocker.None)
            {
                result.Add(new IneligiblePerson(person.Subject, reasons));
            }
        }

        return result;
    }

    /// <summary>Zorunlu ust bilgi (ADR-0014 §3).</summary>
    private async Task<IReadOnlyList<ReportInfo>> InfoAsync(
        DataQualityIssue? issue,
        int itemCount,
        int ineligibleCount,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var viewer = await _scope.GetAsync(cancellationToken).ConfigureAwait(false);
        var own = await _persons.FindOwnAsync(viewer, cancellationToken).ConfigureAwait(false);
        var run = await _query.LastWarningsRunAsync(cancellationToken).ConfigureAwait(false);

        return
        [
            new ReportInfo("Rapor", ReportTitle),
            new ReportInfo("Üretim zamanı", Timestamp(now)),
            new ReportInfo(
                "Veri anlık görüntüsü",
                run?.FinishedAt is { } finished
                    ? $"{Timestamp(finished)} (kart uyarılarının alındığı LOGO senkronizasyonu); diğer uyarılar güncel veriden"
                    : "Tamamlanmış LOGO senkronizasyonu yok"),
            new ReportInfo("Üreten", own is null ? "—" : $"{own.FirstName} {own.LastName}"),
            new ReportInfo("Süzgeçler", "Yalnızca aktif istihdamlar; uyarı türü: " + (issue is { } t ? Label(t) : "Tümü")),
            new ReportInfo("Kayıt sayısı", $"{itemCount.ToString("N0", Turkish)} uyarı; {ineligibleCount.ToString("N0", Turkish)} üye olamayacak kişi"),
        ];
    }

    private static string Timestamp(DateTimeOffset instant) =>
        CorporateTime.ToLocal(instant).ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);

    private static void EnsurePage(int page, int pageSize)
    {
        if (page < 1 || pageSize < 1 || pageSize > PagedResult<DataQualityItem>.MaxPageSize)
        {
            throw new InvalidRequestException($"Sayfa 1 veya büyük, sayfa boyutu 1–{PagedResult<DataQualityItem>.MaxPageSize} olmalıdır.");
        }
    }
}
