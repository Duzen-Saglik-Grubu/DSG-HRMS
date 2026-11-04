using System.Globalization;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Common.Paging;
using Dsg.Hrms.Application.Common.Reports;
using Dsg.Hrms.Application.Common.Security;
using Dsg.Hrms.Application.Personnel.Access;
using Dsg.Hrms.Application.Personnel.Listing;
using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Application.Personnel.Export;

/// <summary>Disa aktarma istegi.</summary>
/// <param name="Format">Dosya bicimi.</param>
/// <param name="Fields">Secilen alanlar; ad ve soyad her zaman vardir.</param>
/// <param name="Search">Hizli arama terimi.</param>
/// <param name="Status">Calisma durumu.</param>
/// <param name="Sort">Siralama.</param>
/// <param name="Descending">Azalan siralama.</param>
/// <param name="Filters">Suzgecler.</param>
public sealed record PersonExportRequest(
    ReportFormat Format,
    IReadOnlyCollection<PersonExportField> Fields,
    string? Search,
    EmploymentStatusFilter Status,
    PersonSort Sort,
    bool Descending,
    PersonListFilters Filters);

/// <summary>
/// Personel listesinin disa aktarilmasi (SYG-PERS-032, 033; ADR-0014, ADR-0022 §4).
/// </summary>
/// <remarks>
/// <para>
/// Satir kisidir; kisiler ekrandaki listeyle ayni kume ve siradadir (<see cref="IPersonnelQuery.ExportAsync"/>).
/// Istihdam alanlari esas istihdamdan gelir (KR-130).
/// </para>
/// <para>
/// Yetki: uc <c>personnel.person.export</c> ister. Secilebilir alanlar ve kullanilabilir
/// suzgecler gorunurluk politikasindan gelir (ADR-0019 §2); izin disi alan veya suzgec
/// istegi <c>400</c> alir. Tam TCKN'yi goremeyenin dosyasinda TCKN maskelidir.
/// </para>
/// <para>
/// Kayit: her aktarim erisim kaydina <c>Export</c> olarak yazilir (bicim, alanlar, suzgec,
/// sayi, kisiler). Korumali suzgecin degeri yazilmaz (SYG-PERS-059). Kan grubu aktarildiysa
/// her kisi icin ayrica <c>SpecialCategoryView</c> yazilir.
/// </para>
/// </remarks>
public sealed class PersonExportService
{
    /// <summary>Tek aktarimdaki en fazla satir (ADR-0014 §5).</summary>
    public const int RowLimit = 50_000;

    /// <summary>Raporun adi; dosyada ve erisim kaydinda.</summary>
    public const string ReportTitle = "Personel Listesi";

    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    private readonly ICurrentUserScope _scope;
    private readonly IPersonnelQuery _query;
    private readonly IAccessLogger _accessLogger;
    private readonly IDateTimeProvider _clock;
    private readonly IPersonnelSyncStatus _sync;
    private readonly IPersonFilterOptionsQuery _options;
    private readonly IReadOnlyList<IReportWriter> _writers;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonExportService(
        ICurrentUserScope scope,
        IPersonnelQuery query,
        IAccessLogger accessLogger,
        IDateTimeProvider clock,
        IPersonnelSyncStatus sync,
        IPersonFilterOptionsQuery options,
        IEnumerable<IReportWriter> writers)
    {
        _scope = scope;
        _query = query;
        _accessLogger = accessLogger;
        _clock = clock;
        _sync = sync;
        _options = options;
        _writers = [.. writers];
    }

    /// <summary>Kullanicinin secebilecegi alanlar (PG-PERS-42).</summary>
    public async Task<IReadOnlyList<PersonExportFieldDefinition>> GetFieldsAsync(CancellationToken cancellationToken)
    {
        var viewer = await _scope.GetAsync(cancellationToken).ConfigureAwait(false);
        return PersonExportFields.Available(viewer);
    }

    /// <summary>Dosyayi uretir ve aktarimi kaydeder.</summary>
    /// <exception cref="InvalidRequestException">
    /// Izin disi alan veya suzgec, Excel'de fotograf ya da satir siniri asildiginda.
    /// </exception>
    public async Task<ReportFile> ExportAsync(PersonExportRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var viewer = await _scope.GetAsync(cancellationToken).ConfigureAwait(false);
        var writer = _writers.FirstOrDefault(w => w.Format == request.Format)
            ?? throw new InvalidRequestException("Geçersiz dosya biçimi.");

        var available = PersonExportFields.Available(viewer).Select(d => d.Field).ToHashSet();
        var forbidden = request.Fields.Where(f => !available.Contains(f)).ToList();
        if (forbidden.Count > 0)
        {
            // Gorulemeyen alan: politika disi istek; secenek ekranda zaten sunulmaz (ADR-0019 §2).
            throw new InvalidRequestException($"Bu alanı dışa aktarma yetkiniz yok: {Header(forbidden[0])}.");
        }

        var columns = PersonExportFields.All.Where(d => request.Fields.Contains(d.Field)).ToList();
        if (request.Format != ReportFormat.Pdf && columns.Any(d => d.PdfOnly))
        {
            throw new InvalidRequestException("Fotoğraf yalnızca PDF'e aktarılabilir.");
        }

        var used = request.Filters.Used().ToList();
        if (used.FirstOrDefault(f => !PersonListFilters.CanUse(viewer, f.Protection)) is { Name: { } forbiddenFilter })
        {
            throw new InvalidRequestException($"Bu alana göre süzme yetkiniz yok: {forbiddenFilter}.");
        }

        var term = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();
        var criteria = new PersonListCriteria(term, request.Status, request.Sort, request.Descending, new PageRequest(1, 1))
        {
            Filters = request.Filters,
        };
        var needs = new PersonExportNeeds(
            Address: columns.Any(d => d.Field == PersonExportField.Address),
            BloodGroup: columns.Any(d => d.Field == PersonExportField.BloodGroup),
            Photo: columns.Any(d => d.Field == PersonExportField.Photo));

        var result = await _query.ExportAsync(viewer, criteria, needs, RowLimit, cancellationToken).ConfigureAwait(false);
        if (result.TotalCount > RowLimit)
        {
            throw new InvalidRequestException(
                $"Süzgece uyan {result.TotalCount.ToString("N0", Turkish)} kişi var; bir seferde en fazla {RowLimit.ToString("N0", Turkish)} kişi aktarılabilir. Süzgeci daraltın.");
        }

        var now = _clock.UtcNow;
        var document = new ReportDocument(
            ReportTitle,
            await InfoAsync(viewer, request, term, used, result.TotalCount, now, cancellationToken).ConfigureAwait(false),
            [
                new ReportColumn(PersonExportFields.FirstNameHeader, ReportColumnKind.Text),
                new ReportColumn(PersonExportFields.LastNameHeader, ReportColumnKind.Text),
                .. columns.Select(d => new ReportColumn(d.Header, d.Kind)),
            ],
            [.. result.Rows.Select(r => Row(r, columns, viewer))]);

        var content = writer.Write(document);

        await LogAsync(request, term, used, columns, result, cancellationToken).ConfigureAwait(false);

        var stamp = CorporateTime.ToLocal(now).ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture);
        return new ReportFile($"personel-listesi-{stamp}.{writer.Extension}", writer.ContentType, content);
    }

    /// <summary>Bir kisinin satiri, secilen alanlarin sirasinda.</summary>
    private ReportRow Row(PersonExportRecord record, IReadOnlyList<PersonExportFieldDefinition> columns, PersonnelViewer viewer)
    {
        var person = record.Person;
        var primary = record.Primary;
        var current = primary?.Current;
        var today = _clock.Today;
        var fullNationalId = PersonFieldVisibility.CanSeeFullNationalId(viewer);

        object? Value(PersonExportField field) => field switch
        {
            PersonExportField.RegistryCode => primary?.RegistryCode,
            PersonExportField.NationalId => fullNationalId ? person.NationalId : Mask.NationalIdForDisplay(person.NationalId),
            PersonExportField.Gender => person.Profile.Gender switch
            {
                Gender.Female => "Kadın",
                Gender.Male => "Erkek",
                _ => null,
            },
            PersonExportField.BirthDate => person.BirthDate,
            PersonExportField.Age => Age.On(person.BirthDate, today),
            PersonExportField.PreviousSurname => person.Profile.PreviousSurname,
            PersonExportField.MaritalStatus => person.Profile.MaritalStatus switch
            {
                MaritalStatus.Married => "Evli",
                MaritalStatus.NotMarried => "Bekâr",
                _ => null,
            },
            PersonExportField.Education => person.Profile.EducationName,
            PersonExportField.BloodGroup => person.BloodGroup,
            PersonExportField.Email => person.Email,
            PersonExportField.MobilePhone => person.MobilePhone,
            PersonExportField.Address => person.Address,
            PersonExportField.EmploymentStatus => person.Employments.Any(e => e.IsActive) ? "Aktif" : "Ayrıldı",
            PersonExportField.Company => current?.CompanyName ?? primary?.CompanyName,
            PersonExportField.City => current?.City,
            PersonExportField.Branch => current?.BranchName,
            PersonExportField.Department => current?.DepartmentName,
            PersonExportField.Title => current?.TitleName,
            PersonExportField.Duty => primary?.CurrentDuty?.Name,
            PersonExportField.StatusCode => current?.StatusCode,
            PersonExportField.OccupationCode => current?.OccupationCode,
            PersonExportField.OccupationName => current?.OccupationName,
            PersonExportField.HireDate => primary?.HireDate,
            PersonExportField.TerminationDate => primary?.TerminationDate,
            PersonExportField.Seniority => primary is null ? null : SeniorityText(Seniority.Calculate(primary.HireDate, primary.TerminationDate, today)),
            PersonExportField.Photo => record.Photo,
            _ => null,
        };

        return new ReportRow([person.FirstName, person.LastName, .. columns.Select(d => Value(d.Field))]);
    }

    /// <summary>Kartla ayni yazim: "X Yıl Y Ay Z Gün" (SYG-PERS-039).</summary>
    private static string SeniorityText(Seniority seniority) =>
        string.Create(Turkish, $"{seniority.Years} Yıl {seniority.Months} Ay {seniority.Days} Gün");

    /// <summary>Zorunlu ust bilgi (ADR-0014 §3).</summary>
    private async Task<IReadOnlyList<ReportInfo>> InfoAsync(
        PersonnelViewer viewer,
        PersonExportRequest request,
        string? term,
        IReadOnlyList<(string Name, FilterProtection Protection, string? Value)> used,
        int count,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var own = await _query.FindOwnAsync(viewer, cancellationToken).ConfigureAwait(false);
        var freshness = await _sync.GetFreshnessAsync(cancellationToken).ConfigureAwait(false);
        var summary = await FilterSummaryAsync(request, term, used, cancellationToken).ConfigureAwait(false);

        return
        [
            new ReportInfo("Rapor", ReportTitle),
            new ReportInfo("Üretim zamanı", Timestamp(now)),
            new ReportInfo(
                "Veri anlık görüntüsü",
                freshness.LastSucceededAt is { } synced
                    ? $"{Timestamp(synced)} (son başarılı LOGO senkronizasyonu)"
                    : "Başarılı LOGO senkronizasyonu yok"),
            new ReportInfo("Üreten", own is null ? "—" : $"{own.FirstName} {own.LastName}"),
            new ReportInfo("Süzgeçler", summary),
            new ReportInfo("Kayıt sayısı", count.ToString("N0", Turkish)),
        ];
    }

    private static string Timestamp(DateTimeOffset instant) =>
        CorporateTime.ToLocal(instant).ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture);

    /// <summary>
    /// Suzgeclerin okunur ozeti: kimlikler yerine adlar. Korumali suzgecin degeri dosyaya da
    /// yazilmaz; dosya kurum disina ciktiginda deger kisi listesiyle yan yana durmaz.
    /// </summary>
    private async Task<string> FilterSummaryAsync(
        PersonExportRequest request,
        string? term,
        IReadOnlyList<(string Name, FilterProtection Protection, string? Value)> used,
        CancellationToken cancellationToken)
    {
        var parts = new List<string>
        {
            "Çalışma durumu: " + request.Status switch
            {
                EmploymentStatusFilter.Terminated => "Ayrılan",
                EmploymentStatusFilter.All => "Tümü",
                _ => "Aktif",
            },
        };

        if (term is not null)
        {
            parts.Add($"Arama: \"{term}\"");
        }

        if (used.Count > 0)
        {
            var options = await _options.GetAsync(cancellationToken).ConfigureAwait(false);
            var f = request.Filters;

            static string Names(IEnumerable<string> values, IReadOnlyList<FilterOption> options) =>
                string.Join(", ", values.Select(v => options.FirstOrDefault(o => string.Equals(o.Value, v, StringComparison.OrdinalIgnoreCase))?.Label ?? v));

            foreach (var (name, protection, _) in used)
            {
                var (label, value) = name switch
                {
                    "companyId" => ("Firma", Names(f.CompanyIds.Select(id => id.ToString()), options.Companies)),
                    "city" => ("Şehir", string.Join(", ", f.Cities)),
                    "branchId" => ("Şube", Names(f.BranchIds.Select(id => id.ToString()), options.Branches)),
                    "departmentId" => ("Birim", Names(f.DepartmentIds.Select(id => id.ToString()), options.Departments)),
                    "titleCode" => ("Ünvan", Names(f.TitleCodes, options.Titles)),
                    "dutyId" => ("Görev", Names(f.DutyIds.Select(id => id.ToString()), options.Duties)),
                    "statusCode" => ("Statü", string.Join(", ", f.StatusCodes)),
                    "occupationCode" => ("SGK meslek", Names(f.OccupationCodes, options.Occupations)),
                    "education" => ("Eğitim", string.Join(", ", f.Educations)),
                    "maritalStatus" => ("Medeni hâl", string.Join(", ", f.MaritalStatuses.Select(m => m == MaritalStatus.Married ? "Evli" : "Bekâr"))),
                    "gender" => ("Cinsiyet", string.Join(", ", f.Genders.Select(g => g == Gender.Female ? "Kadın" : "Erkek"))),
                    "firstName" => ("Ad", Quoted(f.FirstName)),
                    "lastName" => ("Soyad", Quoted(f.LastName)),
                    "previousSurname" => ("Önceki soyadı", Quoted(f.PreviousSurname)),
                    "registryCode" => ("Sicil", Quoted(f.RegistryCode)),
                    "nationalId" => ("TCKN", Quoted(f.NationalId)),
                    "email" => ("E-posta", Quoted(f.Email)),
                    "phone" => ("Telefon", Quoted(f.Phone)),
                    "address" => ("Adres", Quoted(f.Address)),
                    "birthDate" => ("Doğum tarihi", DateRangeText(f.BirthDate)),
                    "hireDate" => ("İşe giriş tarihi", DateRangeText(f.HireDate)),
                    "terminationDate" => ("İşten çıkış tarihi", DateRangeText(f.TerminationDate)),
                    "age" => ("Yaş", NumberRangeText(f.Age)),
                    "seniorityYears" => ("Kıdem (yıl)", NumberRangeText(f.SeniorityYears)),
                    _ => (name, string.Empty),
                };

                parts.Add($"{label}: {(protection == FilterProtection.None ? value : PersonListService.ProtectedFilterMarker)}");
            }
        }

        return string.Join("; ", parts);
    }

    private static string Quoted(string? value) => $"\"{value?.Trim()}\" içinde geçen";

    private static string DateRangeText(DateRange? range) => range switch
    {
        { From: { } from, To: { } to } => $"{from:dd.MM.yyyy} – {to:dd.MM.yyyy}",
        { From: { } from } => $"{from:dd.MM.yyyy} ve sonrası",
        { To: { } to } => $"{to:dd.MM.yyyy} ve öncesi",
        _ => string.Empty,
    };

    private static string NumberRangeText(NumberRange? range) => range switch
    {
        { Min: { } min, Max: { } max } => string.Create(CultureInfo.InvariantCulture, $"{min} – {max}"),
        { Min: { } min } => string.Create(CultureInfo.InvariantCulture, $"en az {min}"),
        { Max: { } max } => string.Create(CultureInfo.InvariantCulture, $"en çok {max}"),
        _ => string.Empty,
    };

    /// <summary>Erisim kaydi (SYG-PERS-033, 059).</summary>
    private async Task LogAsync(
        PersonExportRequest request,
        string? term,
        IReadOnlyList<(string Name, FilterProtection Protection, string? Value)> used,
        IReadOnlyList<PersonExportFieldDefinition> columns,
        PersonExportResult result,
        CancellationToken cancellationToken)
    {
        var logged = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["format"] = request.Format.ToString(),
            ["fields"] = "FirstName,LastName" + string.Concat(columns.Select(d => "," + d.Field)),
            ["q"] = term,
            ["status"] = request.Status.ToString(),
            ["sort"] = request.Descending ? $"{request.Sort}:desc" : request.Sort.ToString(),
        };
        foreach (var (name, protection, value) in used)
        {
            logged[name] = protection == FilterProtection.None ? value : PersonListService.ProtectedFilterMarker;
        }

        List<Guid> subjects = [.. result.Rows.Select(r => r.Person.PublicId)];
        await _accessLogger.LogAsync(
            AccessRecord.Export(PersonListService.EntityName, result.Rows.Count, logged) with { SubjectIds = subjects },
            cancellationToken).ConfigureAwait(false);

        // Kan grubu ozel nitelikli veridir: degeri dosyaya giren her kisi icin ayri kayit (SYG-PERS-059).
        if (columns.Any(d => d.Field == PersonExportField.BloodGroup))
        {
            foreach (var row in result.Rows.Where(r => r.Person.BloodGroup is not null))
            {
                await _accessLogger.LogAsync(
                    AccessRecord.SpecialCategoryView(PersonListService.EntityName, row.Person.PublicId),
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

    private static string Header(PersonExportField field) =>
        PersonExportFields.All.FirstOrDefault(d => d.Field == field)?.Header ?? field.ToString();
}
