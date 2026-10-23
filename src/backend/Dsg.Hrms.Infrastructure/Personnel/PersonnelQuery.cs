using Dsg.Hrms.Application.Personnel.Access;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Personnel;

/// <summary>
/// Kisi okumalarinin tek giris noktasi (ADR-0019 §1, SYG-PERS-050). Kapsam suzgeci her
/// sorguya burada eklenir.
/// </summary>
public sealed class PersonnelQuery(HrmsDbContext context) : IPersonnelQuery
{
    /// <inheritdoc />
    public Task<PersonRecord?> FindAsync(Guid personId, PersonnelViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        return FirstAsync(InScope(viewer).Where(person => person.PublicId == personId), cancellationToken);
    }

    /// <inheritdoc />
    public Task<PersonRecord?> FindOwnAsync(PersonnelViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        return viewer.PersonId is { } own
            ? FirstAsync(context.Set<Person>().Where(person => person.Id == own), cancellationToken)
            : Task.FromResult<PersonRecord?>(null);
    }

    /// <summary>
    /// Kapsam suzgeci. <c>Self</c> kapsaminda yalnizca kullanicinin kendi kaydi; kisi kaydi
    /// olmayan bir bakis (arka plan) hicbir kayit gormez.
    /// </summary>
    private IQueryable<Person> InScope(PersonnelViewer viewer)
    {
        var persons = context.Set<Person>().AsNoTracking();

        return viewer.Scope switch
        {
            PersonnelScope.All => persons,
            _ => viewer.PersonId is { } own ? persons.Where(person => person.Id == own) : persons.Where(_ => false),
        };
    }

    private async Task<PersonRecord?> FirstAsync(IQueryable<Person> query, CancellationToken cancellationToken) =>
        await query
            .AsNoTracking()
            .Select(person => new PersonRecord(
                person.Id,
                person.PublicId,
                person.FirstName,
                person.LastName,
                person.NationalId,
                person.BirthDate,
                person.Email,
                person.MobilePhone,
                person.Employments
                    .Select(e => new EmploymentRecord(e.RegistryCode, e.Company.Name, e.HireDate, e.TerminationDate, e.IsActive))
                    .ToList(),
                new PersonProfile(person.Gender, person.PreviousSurname, person.MaritalStatus, person.EducationName),
                context.Set<PersonSensitive>().Where(s => s.PersonId == person.Id).Select(s => s.Address).FirstOrDefault()))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
}
