using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Personnel.Duties;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Domain.Reference;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Dsg.Hrms.Infrastructure.Personnel;

/// <summary>Gorev girisi deposu (SYG-PERS-042).</summary>
public sealed class DutyStore(HrmsDbContext context) : IDutyStore
{
    /// <inheritdoc />
    public Task<Employment?> FindEmploymentAsync(long personId, string registryCode, CancellationToken cancellationToken) =>
        context.Set<Employment>().SingleOrDefaultAsync(e => e.PersonId == personId && e.RegistryCode == registryCode, cancellationToken);

    /// <inheritdoc />
    public async Task<IReadOnlyList<EmploymentDuty>> ListAsync(long employmentId, CancellationToken cancellationToken) =>
        await context.Set<EmploymentDuty>()
            .Where(d => d.EmploymentId == employmentId)
            .OrderBy(d => d.ValidFrom)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<(long Id, string Name)?> FindActiveDutyDefinitionAsync(Guid definitionId, CancellationToken cancellationToken)
    {
        var definition = await context.Set<ReferenceDefinition>()
            .AsNoTracking()
            .Where(d => d.PublicId == definitionId && d.Type == ReferenceType.Duty && d.IsActive)
            .Select(d => new { d.Id, d.Name })
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return definition is null ? null : (definition.Id, definition.Name);
    }

    /// <inheritdoc />
    public void Add(EmploymentDuty duty) => context.Add(duty);

    /// <inheritdoc />
    public void Remove(EmploymentDuty duty) => context.Remove(duty);

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.ExclusionViolation })
        {
            // SYG-PERS-019: cakisan aralik 500 degil 409 doner (ADR-0021 §2).
            throw new ConflictException("Görev, aynı istihdamdaki başka bir görev dönemiyle çakışıyor.");
        }
    }
}
