using Dsg.Hrms.Application.Personnel.EmergencyContacts;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Domain.Reference;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Personnel;

/// <summary>Acil durum kisisi deposu (SYG-PERS-043; yakinlik tanimlari <c>KR-135</c>).</summary>
public sealed class EmergencyContactStore(HrmsDbContext context) : IEmergencyContactStore
{
    /// <inheritdoc />
    public Task<EmergencyContact?> FindAsync(long personId, Guid contactId, CancellationToken cancellationToken) =>
        context.Set<EmergencyContact>().SingleOrDefaultAsync(c => c.PersonId == personId && c.PublicId == contactId, cancellationToken);

    /// <inheritdoc />
    public Task<RelationshipDefinition?> FindRelationshipAsync(Guid definitionId, CancellationToken cancellationToken) =>
        Relationships()
            .Where(d => d.PublicId == definitionId)
            .Select(d => new RelationshipDefinition(d.Id, d.PublicId, d.Name, d.IsActive))
            .SingleOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<RelationshipDefinition>> ListActiveRelationshipsAsync(CancellationToken cancellationToken) =>
        await Relationships()
            .Where(d => d.IsActive)
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Name)
            .Select(d => new RelationshipDefinition(d.Id, d.PublicId, d.Name, d.IsActive))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public void Add(EmergencyContact contact) => context.Add(contact);

    /// <inheritdoc />
    public void Remove(EmergencyContact contact) => context.Remove(contact);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);

    private IQueryable<ReferenceDefinition> Relationships() =>
        context.Set<ReferenceDefinition>().AsNoTracking().Where(d => d.Type == ReferenceType.Relationship);
}
