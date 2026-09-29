using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Identity;

/// <summary>Eylem yetkisinin veritabani tarafi (ADR-0007 §1).</summary>
public sealed class AccessControlStore : IAccessControlStore
{
    private readonly HrmsDbContext _context;

    /// <summary>Yeni ornek olusturur.</summary>
    public AccessControlStore(HrmsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetPermissionsAsync(long userAccountId, CancellationToken cancellationToken) =>
        await (
            from userRole in _context.Set<UserRole>()
            join permission in _context.Set<RoleGrant>() on userRole.RoleId equals permission.RoleId
            where userRole.UserAccountId == userAccountId
            select permission.Permission)
            .Distinct()
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public Task<Role?> FindRoleAsync(string code, CancellationToken cancellationToken) =>
        _context.Set<Role>().SingleOrDefaultAsync(role => role.Code == code, cancellationToken);

    /// <inheritdoc />
    public async Task<bool> HasRoleAsync(long userAccountId, long roleId, CancellationToken cancellationToken) =>
        await _context.Set<UserRole>().AnyAsync(item => item.UserAccountId == userAccountId && item.RoleId == roleId, cancellationToken).ConfigureAwait(false)
        || _context.ChangeTracker.Entries<UserRole>().Any(entry =>
            entry.State == EntityState.Added && entry.Entity.UserAccountId == userAccountId && entry.Entity.RoleId == roleId);

    /// <inheritdoc />
    public void Add(object entity) => _context.Add(entity);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}
