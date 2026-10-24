using Dsg.Hrms.Application.Personnel.Access;
using Dsg.Hrms.Domain.Organization;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Domain.Reference;
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

    private async Task<PersonRecord?> FirstAsync(IQueryable<Person> query, CancellationToken cancellationToken)
    {
        var record = await ReadAsync(query, cancellationToken).ConfigureAwait(false);
        if (record is null || record.Employments.Count == 0)
        {
            return record;
        }

        long[] ids = [.. record.Employments.Select(e => e.Id)];
        var current = await CurrentAssignmentsAsync(ids, cancellationToken).ConfigureAwait(false);
        var duties = await CurrentDutiesAsync(ids, cancellationToken).ConfigureAwait(false);
        return record with
        {
            Employments = [.. record.Employments.Select(e => e with { Current = current.GetValueOrDefault(e.Id), CurrentDuty = duties.GetValueOrDefault(e.Id) })],
        };
    }

    /// <summary>
    /// Her istihdamin guncel donemi: acik kayit, yoksa en son kayit (SYG-PERS-038). Bitmis
    /// istihdamda kartta son bilinen nitelikler gorunur.
    /// </summary>
    private async Task<Dictionary<long, AssignmentRecord>> CurrentAssignmentsAsync(long[] employmentIds, CancellationToken cancellationToken)
    {
        var rows = await (
            from a in context.Set<EmploymentAssignment>().AsNoTracking()
            where employmentIds.Contains(a.EmploymentId)
            join c in context.Set<Company>() on a.CompanyId equals c.Id
            join b in context.Set<Branch>() on a.BranchId equals (long?)b.Id into branches
            from b in branches.DefaultIfEmpty()
            join d in context.Set<Department>() on a.DepartmentId equals (long?)d.Id into departments
            from d in departments.DefaultIfEmpty()
            join t in context.Set<ReferenceDefinition>().Where(x => x.Type == ReferenceType.Title) on a.TitleCode equals t.Code into titles
            from t in titles.DefaultIfEmpty()
                // Sol birlesimde eslesme yoksa EF alanlari SQL NULL olarak dondurur.
            select new
            {
                a.EmploymentId,
                Record = new AssignmentRecord(
                    a.ValidFrom, a.ValidTo, c.Name, b.Name, b.City, d.Name,
                    a.TitleCode, t.Name, a.StatusCode, a.OccupationCode, a.OccupationName),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .GroupBy(r => r.EmploymentId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.Record.ValidTo is null).ThenByDescending(r => r.Record.ValidFrom).First().Record);
    }

    /// <summary>Her istihdamin guncel gorevi: acik kayit, yoksa en son kayit; silinmisler haric.</summary>
    private async Task<Dictionary<long, DutyRecord>> CurrentDutiesAsync(long[] employmentIds, CancellationToken cancellationToken)
    {
        var rows = await (
            from d in context.Set<EmploymentDuty>().AsNoTracking()
            where employmentIds.Contains(d.EmploymentId)
            join r in context.Set<ReferenceDefinition>() on d.DutyDefinitionId equals r.Id
            select new { d.EmploymentId, Record = new DutyRecord(d.PublicId, r.Name, d.ValidFrom, d.ValidTo) })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .GroupBy(r => r.EmploymentId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(r => r.Record.ValidTo is null).ThenByDescending(r => r.Record.ValidFrom).First().Record);
    }

    private async Task<PersonRecord?> ReadAsync(IQueryable<Person> query, CancellationToken cancellationToken) =>
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
                    .Select(e => new EmploymentRecord(e.Id, e.RegistryCode, e.Company.Name, e.HireDate, e.TerminationDate, e.IsActive))
                    .ToList(),
                new PersonProfile(person.Gender, person.PreviousSurname, person.MaritalStatus, person.EducationName),
                context.Set<PersonSensitive>().Where(s => s.PersonId == person.Id).Select(s => s.Address).FirstOrDefault()))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
}
