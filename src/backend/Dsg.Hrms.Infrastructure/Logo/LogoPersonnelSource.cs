using System.Data.Common;
using Dsg.Hrms.Application.Personnel.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Dsg.Hrms.Infrastructure.Logo;

/// <summary>
/// <see cref="ILogoPersonnelSource"/> uygulamasi (ADR-0003).
/// </summary>
/// <remarks>
/// Bu sinif YALNIZCA okur. Hicbir metodu LOGO'da veri, nesne veya yetki degistirmez;
/// yetki denetimi bile yazma denemesiyle degil, <c>HAS_PERMS_BY_NAME</c> sorgusuyla
/// yapilir.
/// </remarks>
public sealed partial class LogoPersonnelSource : ILogoPersonnelSource
{
    /// <summary>Tablo duzeyinde denetlenen yazma yetkileri.</summary>
    private static readonly string[] ObjectWritePermissions = ["INSERT", "UPDATE", "DELETE", "ALTER"];

    /// <summary>Veritabani duzeyinde denetlenen yazma yetkileri.</summary>
    private static readonly string[] DatabaseWritePermissions = ["INSERT", "UPDATE", "DELETE", "ALTER", "EXECUTE"];

    private readonly LogoDbContext _context;
    private readonly ILogger<LogoPersonnelSource> _logger;

    /// <summary>Yeni ornek olusturur.</summary>
    public LogoPersonnelSource(LogoDbContext context, ILogger<LogoPersonnelSource> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<LogoAccessCheckResult> VerifyReadOnlyAccessAsync(CancellationToken cancellationToken)
    {
        // Sorgu metni yalnizca bu siniftaki SABITLERDEN uretilir; disaridan girdi almaz.
        var checks = LogoSchema.Tables
            .SelectMany(table => ObjectWritePermissions.Select(permission =>
                $"SELECT '{table}:{permission}' AS Permission, HAS_PERMS_BY_NAME('{table}', 'OBJECT', '{permission}') AS Granted"))
            .Concat(DatabaseWritePermissions.Select(permission =>
                $"SELECT 'DATABASE:{permission}' AS Permission, HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', '{permission}') AS Granted"));
        var sql = string.Join(" UNION ALL ", checks);

        var rows = await QueryAsync(() => _context.Database.SqlQueryRaw<PermissionRow>(sql).ToListAsync(cancellationToken))
            .ConfigureAwait(false);

        // HAS_PERMS_BY_NAME, nesne yoksa NULL doner; bu yetki degil sema sorunudur ve
        // sema denetiminde yakalanir.
        var granted = rows.Where(r => r.Granted == 1).Select(r => r.Permission).Order(StringComparer.Ordinal).ToList();
        return new LogoAccessCheckResult(granted.Count == 0, granted);
    }

    /// <inheritdoc />
    public async Task<LogoSchemaCheckResult> VerifySchemaAsync(CancellationToken cancellationToken)
    {
        var tableList = string.Join(", ", LogoSchema.Tables.Select(t => $"'{t}'"));
        var sql =
            "SELECT TABLE_NAME AS TableName, COLUMN_NAME AS ColumnName, DATA_TYPE AS DataType " +
            $"FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME IN ({tableList})";

        var actual = await QueryAsync(() => _context.Database.SqlQueryRaw<ColumnRow>(sql).ToListAsync(cancellationToken))
            .ConfigureAwait(false);

        var byName = actual
            .GroupBy(c => (c.TableName.ToUpperInvariant(), c.ColumnName.ToUpperInvariant()))
            .ToDictionary(g => g.Key, g => g.First().DataType);

        var problems = new List<string>();
        foreach (var expected in LogoSchema.Columns)
        {
            if (!byName.TryGetValue((expected.Table.ToUpperInvariant(), expected.Column.ToUpperInvariant()), out var dataType))
            {
                problems.Add($"{expected.Table}.{expected.Column}: bulunamadi");
            }
            else if (!string.Equals(dataType, expected.DataType, StringComparison.OrdinalIgnoreCase))
            {
                problems.Add($"{expected.Table}.{expected.Column}: tip {dataType}, beklenen {expected.DataType}");
            }
        }

        return new LogoSchemaCheckResult(problems.Count == 0, problems);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<LogoPersonnelRecord>> GetAllAsync(CancellationToken cancellationToken)
    {
        var (persons, contacts, firms) = await QueryAsync(async () =>
        {
            var p = await _context.Persons.ToListAsync(cancellationToken).ConfigureAwait(false);

            // Siralama onemlidir: bir kartta birden fazla e-posta varsa senkronizasyon
            // ILKINI kullanir. Kaynak sirasi (LREF) her calismada ayni kaydi secer.
            var c = await _context.Contacts
                .Where(x => x.CardRef != null
                            && (x.Type == LogoSchema.EmailType || x.Type == LogoSchema.MobilePhoneType)
                            && x.Value != null && x.Value.Trim() != string.Empty)
                .OrderBy(x => x.CardRef).ThenBy(x => x.LogicalRef)
                .ToListAsync(cancellationToken).ConfigureAwait(false);

            var f = await _context.Firms.Where(x => x.Number != null).ToListAsync(cancellationToken).ConfigureAwait(false);
            return (p, c, f);
        }).ConfigureAwait(false);

        var firmNames = firms
            .GroupBy(f => f.Number!.Value)
            .ToDictionary(g => g.Key, g => g.Select(f => f.Name).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)));

        var contactsByCard = contacts.ToLookup(c => c.CardRef!.Value);

        var records = new List<LogoPersonnelRecord>(persons.Count);
        var skipped = 0;

        foreach (var person in persons)
        {
            var code = person.Code?.Trim();

            // Sicil ve ise giris tarihi 26.09.2026 itibariyla tum kartlarda doludur.
            // Bos gelirse kart atlanir ve gunluge yazilir; tahminle doldurulmaz.
            if (string.IsNullOrEmpty(code) || person.HireDate is null)
            {
                skipped++;
                continue;
            }

            var cardContacts = contactsByCard[person.LogicalRef];
            var firmNumber = person.FirmNumber ?? 0;

            records.Add(new LogoPersonnelRecord(
                LogoRef: person.LogicalRef,
                RegistryCode: code,
                NationalId: string.IsNullOrWhiteSpace(person.NationalId) ? null : person.NationalId.Trim(),
                FirstName: person.FirstName,
                LastName: person.LastName,
                BirthDate: ToDate(person.BirthDate),
                HireDate: DateOnly.FromDateTime(person.HireDate.Value),
                TerminationDate: ToDate(person.TerminationDate),
                FirmNumber: firmNumber,
                FirmName: firmNames.GetValueOrDefault(firmNumber),
                Emails: [.. cardContacts.Where(c => c.Type == LogoSchema.EmailType).Select(c => c.Value!)],
                MobilePhones: [.. cardContacts.Where(c => c.Type == LogoSchema.MobilePhoneType).Select(c => c.Value!)]));
        }

        if (skipped > 0)
        {
            LogSkippedCards(_logger, skipped);
        }

        return records;
    }

    private static DateOnly? ToDate(DateTime? value) => value is null ? null : DateOnly.FromDateTime(value.Value);

    /// <summary>
    /// Baglanti ve sorgu hatalarini <see cref="LogoUnavailableException"/>'a donusturur.
    /// </summary>
    /// <remarks>
    /// Uygulama katmani SQL Server'a ozgu istisna tiplerini bilmez; yalnizca "LOGO'ya
    /// erisilemedi" olgusunu bilir (SYG-KMLK-011).
    /// </remarks>
    private static async Task<T> QueryAsync<T>(Func<Task<T>> query)
    {
        try
        {
            return await query().ConfigureAwait(false);
        }
        catch (DbException ex)
        {
            throw new LogoUnavailableException("LOGO veritabanina erisilemedi.", ex);
        }
        catch (TimeoutException ex)
        {
            throw new LogoUnavailableException("LOGO veritabani zaman asimina ugradi.", ex);
        }
    }

    [LoggerMessage(EventId = 3200, Level = LogLevel.Warning,
        Message = "LOGO'da sicil kodu veya ise giris tarihi bos {Count} kart atlandi.")]
    private static partial void LogSkippedCards(ILogger logger, int count);

    /// <summary>Yetki sorgusunun satiri.</summary>
    private sealed class PermissionRow
    {
        public string Permission { get; init; } = string.Empty;

        public int? Granted { get; init; }
    }

    /// <summary>Sema sorgusunun satiri.</summary>
    private sealed class ColumnRow
    {
        public string TableName { get; init; } = string.Empty;

        public string ColumnName { get; init; } = string.Empty;

        public string DataType { get; init; } = string.Empty;
    }
}
