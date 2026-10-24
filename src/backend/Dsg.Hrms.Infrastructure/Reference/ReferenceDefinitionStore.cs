using Dsg.Hrms.Application.Reference;
using Dsg.Hrms.Domain.Reference;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Reference;

/// <summary>Referans tanimlari deposu (ADR-0022 §1).</summary>
public sealed class ReferenceDefinitionStore(HrmsDbContext context) : IReferenceDefinitionStore
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ReferenceDefinition>> ListAsync(ReferenceType type, bool includeInactive, CancellationToken cancellationToken) =>
        await context.Set<ReferenceDefinition>()
            .AsNoTracking()
            .Where(d => d.Type == type && (includeInactive || d.IsActive))
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Code)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public Task<ReferenceDefinition?> FindAsync(Guid id, CancellationToken cancellationToken) =>
        context.Set<ReferenceDefinition>().SingleOrDefaultAsync(d => d.PublicId == id, cancellationToken);

    /// <inheritdoc />
    public Task<bool> CodeExistsAsync(ReferenceType type, string code, CancellationToken cancellationToken) =>
        context.Set<ReferenceDefinition>().AnyAsync(d => d.Type == type && d.Code == code, cancellationToken);

    /// <inheritdoc />
    public Task<int> CountAsync(ReferenceType type, CancellationToken cancellationToken) =>
        context.Set<ReferenceDefinition>().CountAsync(d => d.Type == type, cancellationToken);

    /// <inheritdoc />
    public void Add(ReferenceDefinition definition) => context.Add(definition);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
