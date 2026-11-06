using Dsg.Hrms.Application.Personnel.Marriage;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Personnel;

/// <summary>Evlilik bilgisi deposu (SYG-PERS-036).</summary>
public sealed class MarriageStore(HrmsDbContext context) : IMarriageStore
{
    /// <inheritdoc />
    public Task<PersonMarriage?> FindAsync(long personId, CancellationToken cancellationToken) =>
        context.Set<PersonMarriage>().SingleOrDefaultAsync(m => m.PersonId == personId, cancellationToken);

    /// <inheritdoc />
    public void Add(PersonMarriage marriage) => context.Add(marriage);

    /// <inheritdoc />
    public void Remove(PersonMarriage marriage) => context.Remove(marriage);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
