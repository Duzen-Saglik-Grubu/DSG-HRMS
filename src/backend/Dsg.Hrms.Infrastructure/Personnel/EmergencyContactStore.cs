using Dsg.Hrms.Application.Personnel.EmergencyContacts;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Personnel;

/// <summary>Acil durum kisisi deposu (SYG-PERS-043).</summary>
public sealed class EmergencyContactStore(HrmsDbContext context) : IEmergencyContactStore
{
    /// <inheritdoc />
    public Task<EmergencyContact?> FindAsync(long personId, Guid contactId, CancellationToken cancellationToken) =>
        context.Set<EmergencyContact>().SingleOrDefaultAsync(c => c.PersonId == personId && c.PublicId == contactId, cancellationToken);

    /// <inheritdoc />
    public void Add(EmergencyContact contact) => context.Add(contact);

    /// <inheritdoc />
    public void Remove(EmergencyContact contact) => context.Remove(contact);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
