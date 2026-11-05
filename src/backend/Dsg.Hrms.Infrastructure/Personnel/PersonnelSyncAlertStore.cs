using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Notifications;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Personnel;

/// <summary>
/// Senkronizasyon uyarilarinin deposu (SYG-PERS-012). Senkronizasyonun kendi baglamlarindan
/// bagimsiz, kisa omurlu bir baglam kullanir: calismanin oturumu geri alinmis olsa da bildirim yazilir.
/// </summary>
public sealed class PersonnelSyncAlertStore(IDbContextFactory<HrmsDbContext> factory) : IPersonnelSyncAlertStore
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SyncAlertRecipient>> ListRecipientsAsync(
        IReadOnlyCollection<string> roleCodes,
        InboxItemKind kind,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(roleCodes);

        if (roleCodes.Count == 0)
        {
            return [];
        }

        var context = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            var codes = roleCodes.ToList();

            return await (from userRole in context.Set<UserRole>()
                          join role in context.Set<Role>() on userRole.RoleId equals role.Id
                          join account in context.Set<UserAccount>() on userRole.UserAccountId equals account.Id
                          join person in context.Set<Person>() on account.PersonId equals person.Id
                          where codes.Contains(role.Code)
                                && account.Status == AccountStatus.Active
                                && !context.Set<InboxItem>().Any(item =>
                                    item.RecipientUserId == account.Id && item.Kind == kind && item.ReadAt == null)
                          select new { account.Id, account.PersonId, person.Email })
                .Distinct()
                .Select(r => new SyncAlertRecipient(r.Id, r.PersonId, r.Email))
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task AddAsync(IReadOnlyCollection<InboxItem> items, CancellationToken cancellationToken)
    {
        var context = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        await using (context.ConfigureAwait(false))
        {
            context.AddRange(items);
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
