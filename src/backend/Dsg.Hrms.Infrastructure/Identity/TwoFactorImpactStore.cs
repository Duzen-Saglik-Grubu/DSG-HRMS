using Dsg.Hrms.Application.Identity.Sessions;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Identity;

/// <summary>2FA etki sayimi (SYG-KMLK-035, 080).</summary>
public sealed class TwoFactorImpactStore : ITwoFactorImpactStore
{
    private readonly HrmsDbContext _context;

    /// <summary>Yeni ornek olusturur.</summary>
    public TwoFactorImpactStore(HrmsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task<int> CountWithTwoFactorPreferenceAsync(CancellationToken cancellationToken) =>
        _context.Set<UserAccount>().AsNoTracking()
            .Where(account => account.Status == AccountStatus.Active
                && account.TwoFactorEnabled
                && _context.Set<Employment>().Any(e => e.PersonId == account.PersonId && e.IsActive))
            .CountAsync(cancellationToken);
}
