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
}
