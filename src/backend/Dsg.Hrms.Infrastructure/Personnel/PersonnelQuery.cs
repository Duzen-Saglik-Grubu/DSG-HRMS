using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Paging;
using Dsg.Hrms.Application.Personnel.Access;
using Dsg.Hrms.Application.Personnel.Listing;
using Dsg.Hrms.Application.Settings;
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
public sealed class PersonnelQuery(HrmsDbContext context, ISystemParameters parameters, IDateTimeProvider clock) : IPersonnelQuery
{
    /// <inheritdoc />
    public async Task<PersonRecord?> FindAsync(Guid personId, PersonnelViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        // Gorunur kalma suresi dolan ayrilan kisinin karti herkes icin 404'tur (SYG-PERS-028).
        var persons = await VisibleAsync(InScope(viewer), cancellationToken).ConfigureAwait(false);
        return await FirstAsync(persons.Where(person => person.PublicId == personId), cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PagedResult<PersonListItem>> ListAsync(PersonnelViewer viewer, PersonListCriteria criteria, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentNullException.ThrowIfNull(criteria);

        var persons = await VisibleAsync(InScope(viewer), cancellationToken).ConfigureAwait(false);

        persons = criteria.Status switch
        {
            EmploymentStatusFilter.Active => persons.Where(p => p.Employments.Any(e => e.IsActive)),
            EmploymentStatusFilter.Terminated => persons.Where(p => !p.Employments.Any(e => e.IsActive)),
            _ => persons,
        };

        if (criteria.Search is { } term)
        {
            persons = Search(persons, term, PersonFieldVisibility.CanSearchByNationalId(viewer));
        }

        var total = await persons.CountAsync(cancellationToken).ConfigureAwait(false);

        var ordered = (criteria.Sort, criteria.Descending) switch
        {
            (PersonSort.LastName, false) => persons.OrderBy(p => p.LastName).ThenBy(p => p.FirstName),
            (PersonSort.LastName, true) => persons.OrderByDescending(p => p.LastName).ThenByDescending(p => p.FirstName),
            (_, true) => persons.OrderByDescending(p => p.FirstName).ThenByDescending(p => p.LastName),
            _ => persons.OrderBy(p => p.FirstName).ThenBy(p => p.LastName),
        };

        var page = await ordered
            .ThenBy(p => p.Id)
            .Skip(criteria.Page.Skip)
            .Take(criteria.Page.PageSize)
            .Select(p => new { p.Id, p.PublicId, p.FirstName, p.LastName })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        long[] personIds = [.. page.Select(p => p.Id)];
        var employments = await context.Set<Employment>()
            .AsNoTracking()
            .Include(e => e.Company)
            .Where(e => personIds.Contains(e.PersonId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var byPerson = employments.ToLookup(e => e.PersonId);

        // Kartta gosterilen esas istihdam (KR-126); nitelikleri yalnizca bu sayfa icin okunur.
        var primaries = page
            .Select(p => (p.Id, Primary: Employment.Primary(byPerson[p.Id])))
            .Where(x => x.Primary is not null)
            .ToDictionary(x => x.Id, x => x.Primary!);
        long[] primaryIds = [.. primaries.Values.Select(e => e.Id)];
        var assignments = await CurrentAssignmentsAsync(primaryIds, cancellationToken).ConfigureAwait(false);
        var duties = await CurrentDutiesAsync(primaryIds, cancellationToken).ConfigureAwait(false);

        var items = page.Select(p =>
        {
            var primary = primaries.GetValueOrDefault(p.Id);
            var current = primary is null ? null : assignments.GetValueOrDefault(primary.Id);
            return new PersonListItem(
                p.PublicId,
                p.FirstName,
                p.LastName,
                byPerson[p.Id].Any(e => e.IsActive),
                byPerson[p.Id].Count(),
                primary is null
                    ? null
                    : new PrimaryEmployment(
                        primary.RegistryCode,
                        current?.CompanyName ?? primary.Company.Name,
                        current?.BranchName,
                        current?.DepartmentName,
                        current?.TitleName,
                        primary is null ? null : duties.GetValueOrDefault(primary.Id)?.Name));
        }).ToList();

        return new PagedResult<PersonListItem>(items, Math.Max(1, criteria.Page.Page), criteria.Page.PageSize, total);
    }

    /// <summary>
    /// Hizli arama (SYG-PERS-030): ad, soyad ve gosterildigi kosulda onceki soyadi Turkce karaktersiz
    /// arama anahtarinda; sicil numarasinda; izin varsa TCKN'de. Hepsi "icinde gecen" aramadir.
    /// </summary>
    private static IQueryable<Person> Search(IQueryable<Person> persons, string term, bool nationalId)
    {
        var folded = SearchText.Fold(term);
        var digits = term.All(char.IsAsciiDigit);

        return nationalId && digits
            ? persons.Where(p => p.SearchKey.Contains(folded) || p.Employments.Any(e => e.RegistryCode.Contains(term)) || p.NationalId.Contains(term))
            : persons.Where(p => p.SearchKey.Contains(folded) || p.Employments.Any(e => e.RegistryCode.Contains(term)));
    }

    /// <summary>
    /// Ayrilan kisinin gorunur kalma suresi (PG-PERS-38, KR-114, KR-126): en az bir aktif sicili olan
    /// kisi her zaman gorunur; olmayan, son cikis tarihinden itibaren parametredeki sure boyunca. Bos
    /// parametre suresizdir.
    /// </summary>
    private async Task<IQueryable<Person>> VisibleAsync(IQueryable<Person> persons, CancellationToken cancellationToken)
    {
        var years = await parameters.GetOptionalIntegerAsync(ParameterCatalog.TerminatedVisibilityYears, cancellationToken).ConfigureAwait(false);
        if (years is null)
        {
            return persons;
        }

        var cutoff = clock.Today.AddYears(-years.Value);
        return persons.Where(p => p.Employments.Any(e => e.IsActive) || p.Employments.Max(e => e.TerminationDate) >= cutoff);
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
