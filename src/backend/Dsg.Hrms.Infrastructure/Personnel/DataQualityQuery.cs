using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Personnel.Quality;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Organization;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Domain.Personnel.Sync;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Personnel;

/// <summary>
/// Veri kalitesi raporunun sorgulari (SYG-PERS-070, 071).
/// </summary>
/// <remarks>
/// Rapor aktif istihdamlarin tamamini tarar ve uygulama katmani sayfalar. Aktif istihdam sayisi
/// binler mertebesindedir; tur sayilari ve sayfalama tek okumayla cikar.
/// </remarks>
public sealed class DataQualityQuery(HrmsDbContext context, IDateTimeProvider clock) : IDataQualityQuery
{
    /// <summary>Kart duzeyindeki uyarilarin rapordaki karsiliklari.</summary>
    private static readonly Dictionary<SyncWarningCode, DataQualityIssue> CardIssues = new()
    {
        [SyncWarningCode.MissingNationalId] = DataQualityIssue.MissingNationalId,
        [SyncWarningCode.InvalidNationalId] = DataQualityIssue.InvalidNationalId,
        [SyncWarningCode.MissingBirthDate] = DataQualityIssue.MissingBirthDate,
        [SyncWarningCode.InvalidMobilePhone] = DataQualityIssue.InvalidMobilePhone,
    };

    private static readonly SyncStatus[] DataTransferred = [SyncStatus.Succeeded, SyncStatus.CompletedWithWarnings, SyncStatus.PartiallyCompleted];

    /// <inheritdoc />
    public Task<DataQualityRun?> LastFinishedRunAsync(CancellationToken cancellationToken) =>
        LastRunAsync(context.Set<PersonnelSyncRun>().Where(r => r.FinishedAt != null), cancellationToken);

    /// <inheritdoc />
    public Task<DataQualityRun?> LastWarningsRunAsync(CancellationToken cancellationToken) =>
        LastRunAsync(context.Set<PersonnelSyncRun>().Where(r => r.FinishedAt != null && DataTransferred.Contains(r.Status)), cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<DataQualityItem>> ListIssuesAsync(CancellationToken cancellationToken)
    {
        var employments = await ActiveEmploymentsAsync(cancellationToken).ConfigureAwait(false);
        var items = new List<DataQualityItem>();

        // Kart duzeyi: veriyi aktaran son calismanin, aktif kartlara ait uyarilari.
        var runId = await context.Set<PersonnelSyncRun>()
            .Where(r => r.FinishedAt != null && DataTransferred.Contains(r.Status))
            .OrderByDescending(r => r.Id)
            .Select(r => (long?)r.Id)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (runId is not null)
        {
            var codes = CardIssues.Keys.ToArray();
            var warnings = await context.Set<PersonnelSyncWarning>()
                .AsNoTracking()
                .Where(w => w.RunId == runId && w.IsActiveCard && codes.Contains(w.Code))
                .Select(w => new { w.Code, w.RegistryCode })
                .Distinct()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            items.AddRange(warnings.Select(w => new DataQualityItem(
                CardIssues[w.Code],
                employments.TryGetValue(w.RegistryCode, out var e) ? e.Subject : new DataQualitySubject(w.RegistryCode, null, null, null, null, null, null))));
        }

        // Kisi duzeyi: esas istihdamin satirinda, kisi basina bir kez (KR-130).
        foreach (var primary in PrimaryPerPerson(employments.Values))
        {
            if (primary.Email is null && primary.MobilePhone is null)
            {
                items.Add(new DataQualityItem(DataQualityIssue.NoContact, primary.Subject));
            }

            if (primary.IsEmailShared)
            {
                items.Add(new DataQualityItem(DataQualityIssue.SharedEmail, primary.Subject));
            }
        }

        // Istihdam duzeyi.
        foreach (var employment in employments.Values)
        {
            if (string.IsNullOrWhiteSpace(employment.TitleCode))
            {
                items.Add(new DataQualityItem(DataQualityIssue.MissingTitle, employment.Subject));
            }

            if (!employment.HasCurrentDuty)
            {
                items.Add(new DataQualityItem(DataQualityIssue.MissingDuty, employment.Subject));
            }
        }

        return
        [
            .. items
                .OrderBy(i => i.Issue)
                .ThenBy(i => i.Subject.Company ?? "￿", StringComparer.Ordinal)
                .ThenBy(i => i.Subject.RegistryCode, StringComparer.Ordinal),
        ];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<UnregisteredPerson>> ListUnregisteredAsync(CancellationToken cancellationToken)
    {
        var employments = await ActiveEmploymentsAsync(cancellationToken).ConfigureAwait(false);
        var registered = await context.Set<UserAccount>()
            .Select(a => a.PersonId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var withAccount = registered.ToHashSet();

        return
        [
            .. PrimaryPerPerson(employments.Values)
                .Where(e => !withAccount.Contains(e.PersonDbId))
                .OrderBy(e => e.Subject.Company ?? "￿", StringComparer.Ordinal)
                .ThenBy(e => e.Subject.RegistryCode, StringComparer.Ordinal)
                .Select(e => new UnregisteredPerson(e.Subject, e.Email, e.IsEmailShared)),
        ];
    }

    private static IEnumerable<ActiveEmployment> PrimaryPerPerson(IEnumerable<ActiveEmployment> employments) =>
        employments
            .GroupBy(e => e.PersonDbId)
            .Select(g => Employment.InPrimaryOrder(g, e => (true, e.HireDate, null, e.Subject.RegistryCode)).First());

    /// <summary>Aktif istihdamlar, guncel nitelikleri ve gorev durumuyla; sicil koduna gore.</summary>
    private async Task<Dictionary<string, ActiveEmployment>> ActiveEmploymentsAsync(CancellationToken cancellationToken)
    {
        var rows = await context.Set<Employment>()
            .AsNoTracking()
            .Where(e => e.IsActive)
            .Select(e => new
            {
                e.Id,
                e.PersonId,
                e.RegistryCode,
                e.HireDate,
                CompanyName = e.Company.Name,
                PersonPublicId = e.Person.PublicId,
                e.Person.FirstName,
                e.Person.LastName,
                e.Person.Email,
                e.Person.MobilePhone,
                e.Person.IsEmailShared,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var ids = rows.Select(r => r.Id).ToArray();

        var assignments = (await (
                from a in context.Set<EmploymentAssignment>().AsNoTracking()
                where ids.Contains(a.EmploymentId)
                join c in context.Set<Company>() on a.CompanyId equals c.Id
                join b in context.Set<Branch>() on a.BranchId equals (long?)b.Id into branches
                from b in branches.DefaultIfEmpty()
                join d in context.Set<Department>() on a.DepartmentId equals (long?)d.Id into departments
                from d in departments.DefaultIfEmpty()
                select new { a.EmploymentId, a.ValidFrom, a.ValidTo, Company = c.Name, Branch = b.Name, Department = d.Name, a.TitleCode })
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false))
            .GroupBy(a => a.EmploymentId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.ValidTo is null).ThenByDescending(a => a.ValidFrom).First());

        // Acik gorev: bitis tarihi yok veya ileride (valid_to haric, SYG-PERS-018); silinen kayit sayilmaz (sorgu suzgeci).
        var today = clock.Today;
        var withDuty = (await context.Set<EmploymentDuty>()
                .AsNoTracking()
                .Where(d => ids.Contains(d.EmploymentId) && (d.ValidTo == null || d.ValidTo > today))
                .Select(d => d.EmploymentId)
                .Distinct()
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false))
            .ToHashSet();

        return rows.ToDictionary(
            r => r.RegistryCode,
            r =>
            {
                var current = assignments.GetValueOrDefault(r.Id);
                return new ActiveEmployment(
                    r.PersonId,
                    r.HireDate,
                    new DataQualitySubject(r.RegistryCode, r.PersonPublicId, r.FirstName, r.LastName, current?.Company ?? r.CompanyName, current?.Branch, current?.Department),
                    current?.TitleCode,
                    withDuty.Contains(r.Id),
                    r.Email,
                    r.MobilePhone,
                    r.IsEmailShared);
            },
            StringComparer.Ordinal);
    }

    private static async Task<DataQualityRun?> LastRunAsync(IQueryable<PersonnelSyncRun> runs, CancellationToken cancellationToken) =>
        await runs
            .AsNoTracking()
            .OrderByDescending(r => r.Id)
            .Select(r => new DataQualityRun(r.Status, r.StartedAt, r.FinishedAt, r.RecordsRead, r.WarningCount))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    private sealed record ActiveEmployment(
        long PersonDbId,
        DateOnly HireDate,
        DataQualitySubject Subject,
        string? TitleCode,
        bool HasCurrentDuty,
        string? Email,
        string? MobilePhone,
        bool IsEmailShared);
}
