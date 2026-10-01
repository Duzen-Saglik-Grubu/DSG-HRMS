using Dsg.Hrms.Application.Identity.Sessions;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Identity;

/// <summary>2FA etki sayimi (SYG-KMLK-035).</summary>
public sealed class TwoFactorImpactStore : ITwoFactorImpactStore
{
    private readonly HrmsDbContext _context;

    /// <summary>Yeni ornek olusturur.</summary>
    public TwoFactorImpactStore(HrmsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task<int> CountWithoutChannelAsync(bool emailAllowed, bool smsAllowed, CancellationToken cancellationToken) =>
        (from account in _context.Set<UserAccount>().AsNoTracking()
         join person in _context.Set<Person>().AsNoTracking() on account.PersonId equals person.Id
         where account.Status == AccountStatus.Active
             && _context.Set<Employment>().Any(e => e.PersonId == person.Id && e.IsActive)
             && !(emailAllowed && person.Email != null)
             && !(smsAllowed && person.MobilePhone != null)
         select account.Id)
        .CountAsync(cancellationToken);
}
