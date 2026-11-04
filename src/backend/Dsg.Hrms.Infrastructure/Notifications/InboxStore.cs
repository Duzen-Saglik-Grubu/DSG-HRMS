using Dsg.Hrms.Application.Notifications.Inbox;
using Dsg.Hrms.Domain.Notifications;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Notifications;

/// <summary>Bildirim kutusu deposu (SYG-PERS-046).</summary>
public sealed class InboxStore(HrmsDbContext context) : IInboxStore
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<InboxItem>> ListAsync(long userId, int take, CancellationToken cancellationToken) =>
        await context.Set<InboxItem>()
            .AsNoTracking()
            .Where(item => item.RecipientUserId == userId)
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public Task<int> CountUnreadAsync(long userId, CancellationToken cancellationToken) =>
        context.Set<InboxItem>().CountAsync(item => item.RecipientUserId == userId && item.ReadAt == null, cancellationToken);

    /// <inheritdoc />
    public Task<InboxItem?> FindAsync(long userId, Guid itemId, CancellationToken cancellationToken) =>
        context.Set<InboxItem>().SingleOrDefaultAsync(item => item.RecipientUserId == userId && item.PublicId == itemId, cancellationToken);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
