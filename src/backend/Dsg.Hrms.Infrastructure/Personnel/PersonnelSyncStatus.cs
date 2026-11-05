using Dsg.Hrms.Application.Common.Paging;
using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Domain.Personnel.Sync;
using Dsg.Hrms.Infrastructure.Data;
using Dsg.Hrms.Infrastructure.Logo;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Personnel;

/// <summary><see cref="IPersonnelSyncStatus"/> uygulamasi.</summary>
public sealed class PersonnelSyncStatus : IPersonnelSyncStatus
{
    private readonly HrmsDbContext _context;
    private readonly LogoOptions _logo;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonnelSyncStatus(HrmsDbContext context, LogoOptions logo)
    {
        _context = context;
        _logo = logo;
    }

    /// <inheritdoc />
    public bool IsEnabled => _logo.IsConfigured;

    /// <inheritdoc />
    public async Task<PersonnelSyncRunSummary?> GetLastRunAsync(CancellationToken cancellationToken) =>
        await _context.Set<PersonnelSyncRun>()
            .AsNoTracking()
            .OrderByDescending(run => run.StartedAt)
            // Ayni anda baslamis iki kayit icin belirlenimci sira: sonra eklenen kazanir.
            .ThenByDescending(run => run.Id)
            .Select(run => new PersonnelSyncRunSummary(run.Status, run.StartedAt, run.FinishedAt, run.FailureReason))
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<PersonnelSyncFreshness> GetFreshnessAsync(CancellationToken cancellationToken)
    {
        var runs = _context.Set<PersonnelSyncRun>().AsNoTracking();

        var lastSucceededAt = await runs
            .Where(run => run.Status == SyncStatus.Succeeded || run.Status == SyncStatus.CompletedWithWarnings)
            .MaxAsync(run => run.FinishedAt, cancellationToken)
            .ConfigureAwait(false);

        var lastFinished = await runs
            .Where(run => run.Status != SyncStatus.Running)
            .OrderByDescending(run => run.StartedAt)
            .ThenByDescending(run => run.Id)
            .Select(run => (SyncStatus?)run.Status)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PersonnelSyncFreshness(
            lastSucceededAt,
            lastFinished is SyncStatus.Failed or SyncStatus.PartiallyCompleted);
    }

    /// <inheritdoc />
    public async Task<PagedResult<PersonnelSyncRunDetail>> ListRunsAsync(PageRequest page, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        var runs = _context.Set<PersonnelSyncRun>().AsNoTracking();
        var total = await runs.CountAsync(cancellationToken).ConfigureAwait(false);
        var items = await runs
            .OrderByDescending(run => run.StartedAt)
            .ThenByDescending(run => run.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(run => new PersonnelSyncRunDetail(
                run.PublicId,
                run.Trigger,
                run.Status,
                run.StartedAt,
                run.FinishedAt,
                run.FailureReason,
                run.RecordsRead,
                run.PersonsCreated,
                run.PersonsUpdated,
                run.EmploymentsCreated,
                run.EmploymentsUpdated,
                run.EmploymentsDeactivated,
                run.WarningCount))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PagedResult<PersonnelSyncRunDetail>(items, page.Page, page.PageSize, total);
    }
}
