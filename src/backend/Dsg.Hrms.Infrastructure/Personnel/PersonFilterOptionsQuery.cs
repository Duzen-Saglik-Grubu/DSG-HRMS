using Dsg.Hrms.Application.Personnel.Listing;
using Dsg.Hrms.Domain.Organization;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Domain.Reference;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Personnel;

/// <summary>
/// Suzgec secim listelerinin degerleri (SYG-PERS-024). Yalnizca kurum tanimlari ve kullanilan
/// degerler; kisisel veri icermez.
/// </summary>
public sealed class PersonFilterOptionsQuery(HrmsDbContext context) : IPersonFilterOptionsQuery
{
    /// <inheritdoc />
    public async Task<PersonFilterOptions> GetAsync(CancellationToken cancellationToken)
    {
        var companies = await context.Set<Company>().AsNoTracking().OrderBy(c => c.Name)
            .Select(c => new FilterOption(c.PublicId.ToString(), c.Name)).ToListAsync(cancellationToken).ConfigureAwait(false);
        var branches = await context.Set<Branch>().AsNoTracking().OrderBy(b => b.Name)
            .Select(b => new FilterOption(b.PublicId.ToString(), b.Name)).ToListAsync(cancellationToken).ConfigureAwait(false);
        var cities = await context.Set<Branch>().AsNoTracking().Where(b => b.City != null).Select(b => b.City!).Distinct().OrderBy(c => c)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var departments = await context.Set<Department>().AsNoTracking().OrderBy(d => d.Name)
            .Select(d => new FilterOption(d.PublicId.ToString(), d.Name)).ToListAsync(cancellationToken).ConfigureAwait(false);
        var titles = await context.Set<ReferenceDefinition>().AsNoTracking().Where(d => d.Type == ReferenceType.Title).OrderBy(d => d.SortOrder)
            .Select(d => new FilterOption(d.Code, d.Name)).ToListAsync(cancellationToken).ConfigureAwait(false);
        var duties = await context.Set<ReferenceDefinition>().AsNoTracking().Where(d => d.Type == ReferenceType.Duty).OrderBy(d => d.Name)
            .Select(d => new FilterOption(d.PublicId.ToString(), d.Name)).ToListAsync(cancellationToken).ConfigureAwait(false);
        var statuses = await context.Set<EmploymentAssignment>().AsNoTracking().Where(a => a.StatusCode != null).Select(a => a.StatusCode!).Distinct().OrderBy(s => s)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var occupations = await context.Set<EmploymentAssignment>().AsNoTracking().Where(a => a.OccupationCode != null)
            .Select(a => new { Code = a.OccupationCode!, a.OccupationName }).Distinct()
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var educations = await context.Set<Person>().AsNoTracking().Where(p => p.EducationName != null).Select(p => p.EducationName!).Distinct().OrderBy(e => e)
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new PersonFilterOptions(
            companies,
            [.. cities.Select(c => new FilterOption(c, c))],
            branches,
            departments,
            titles,
            duties,
            [.. statuses.Select(s => new FilterOption(s, s))],
            [.. occupations.GroupBy(o => o.Code).Select(g => new FilterOption(g.Key, g.Select(o => o.OccupationName).FirstOrDefault(n => n != null) ?? g.Key)).OrderBy(o => o.Label, StringComparer.Create(new System.Globalization.CultureInfo("tr-TR"), false))],
            [.. educations.Select(e => new FilterOption(e, e))],
            CanFilterByNationalId: false,
            CanFilterBySensitive: false);
    }
}
