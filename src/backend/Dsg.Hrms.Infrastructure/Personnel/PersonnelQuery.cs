using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Paging;
using Dsg.Hrms.Application.Personnel.Access;
using Dsg.Hrms.Application.Personnel.Listing;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Organization;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Domain.Reference;
using Dsg.Hrms.Infrastructure.Data;
using Dsg.Hrms.Infrastructure.Personnel.Protection;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Personnel;

/// <summary>
/// Kisi okumalarinin tek giris noktasi (ADR-0019 §1, SYG-PERS-050). Kapsam suzgeci her
/// sorguya burada eklenir.
/// </summary>
public sealed class PersonnelQuery(HrmsDbContext context, ISystemParameters parameters, IDateTimeProvider clock, PersonalDataProtector protector) : IPersonnelQuery
{
    /// <inheritdoc />
    public async Task<PersonRecord?> FindAsync(Guid personId, PersonnelViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        // Gorunur kalma suresi dolan ayrilan kisinin karti herkes icin 404'tur (SYG-PERS-028).
        var persons = await VisibleAsync(InScope(viewer), cancellationToken).ConfigureAwait(false);
        return await FirstAsync(persons.Where(person => person.PublicId == personId), viewer, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PagedResult<PersonListItem>> ListAsync(PersonnelViewer viewer, PersonListCriteria criteria, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentNullException.ThrowIfNull(criteria);

        var persons = await MatchingAsync(viewer, criteria, cancellationToken).ConfigureAwait(false);
        var total = await persons.CountAsync(cancellationToken).ConfigureAwait(false);

        var page = await Ordered(persons, criteria)
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
        var withPhoto = (await context.Set<PersonPhoto>()
            .Where(photo => personIds.Contains(photo.PersonId))
            .Select(photo => photo.PersonId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false)).ToHashSet();

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
                        primary is null ? null : duties.GetValueOrDefault(primary.Id)?.Name))
            {
                HasPhoto = withPhoto.Contains(p.Id),
            };
        }).ToList();

        return new PagedResult<PersonListItem>(items, Math.Max(1, criteria.Page.Page), criteria.Page.PageSize, total);
    }

    /// <inheritdoc />
    public async Task<PersonExportResult> ExportAsync(PersonnelViewer viewer, PersonListCriteria criteria, PersonExportNeeds needs, int limit, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentNullException.ThrowIfNull(criteria);
        ArgumentNullException.ThrowIfNull(needs);

        var persons = await MatchingAsync(viewer, criteria, cancellationToken).ConfigureAwait(false);
        var total = await persons.CountAsync(cancellationToken).ConfigureAwait(false);
        if (total > limit)
        {
            return new PersonExportResult(total, []);
        }

        var people = await Ordered(persons, criteria)
            .Select(p => new
            {
                p.Id,
                p.PublicId,
                p.FirstName,
                p.LastName,
                p.NationalId,
                p.BirthDate,
                p.Email,
                p.MobilePhone,
                p.Gender,
                p.PreviousSurname,
                p.MaritalStatus,
                p.EducationName,
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        long[] personIds = [.. people.Select(p => p.Id)];
        var employments = (await context.Set<Employment>()
            .AsNoTracking()
            .Include(e => e.Company)
            .Where(e => personIds.Contains(e.PersonId))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false)).ToLookup(e => e.PersonId);

        var primaries = people
            .Select(p => (p.Id, Primary: Employment.Primary(employments[p.Id])))
            .Where(x => x.Primary is not null)
            .ToDictionary(x => x.Id, x => x.Primary!);
        long[] primaryIds = [.. primaries.Values.Select(e => e.Id)];
        var assignments = await CurrentAssignmentsAsync(primaryIds, cancellationToken).ConfigureAwait(false);
        var duties = await CurrentDutiesAsync(primaryIds, cancellationToken).ConfigureAwait(false);

        var sensitive = needs.Address || needs.BloodGroup
            ? await context.Set<PersonSensitive>()
                .AsNoTracking()
                .Where(s => personIds.Contains(s.PersonId))
                .Select(s => new SensitiveValues(s.PersonId, s.Address, s.BloodGroup))
                .ToDictionaryAsync(s => s.PersonId, cancellationToken)
                .ConfigureAwait(false)
            : [];
        var bloodGroupNames = needs.BloodGroup
            ? await context.Set<ReferenceDefinition>()
                .AsNoTracking()
                .Where(d => d.Type == ReferenceType.BloodGroup)
                .ToDictionaryAsync(d => d.Code, d => d.Name, StringComparer.Ordinal, cancellationToken)
                .ConfigureAwait(false)
            : [];
        var photos = needs.Photo
            ? await context.Set<PersonPhoto>()
                .AsNoTracking()
                .Where(photo => personIds.Contains(photo.PersonId))
                .ToDictionaryAsync(photo => photo.PersonId, photo => photo.Content, cancellationToken)
                .ConfigureAwait(false)
            : [];

        var rows = people.Select(p =>
        {
            List<EmploymentRecord> own = [.. employments[p.Id]
                .Select(e => new EmploymentRecord(e.Id, e.RegistryCode, e.Company.Name, e.HireDate, e.TerminationDate, e.IsActive))];
            var primary = primaries.GetValueOrDefault(p.Id) is { } entity
                ? own.Single(e => e.Id == entity.Id) with
                {
                    Current = assignments.GetValueOrDefault(entity.Id),
                    CurrentDuty = duties.GetValueOrDefault(entity.Id),
                }
                : null;
            var values = sensitive.GetValueOrDefault(p.Id);

            // Cozulemeyen kan grubu (anahtar kaybi, baska satirdan kopya) kartta oldugu gibi
            // "kayitli degil" sayilir.
            string? bloodGroup = null;
            if (needs.BloodGroup && values?.BloodGroup is { } cipher
                && protector.TryUnprotect(cipher, ProtectedField.BloodGroup(p.PublicId)) is { } code)
            {
                bloodGroup = bloodGroupNames.GetValueOrDefault(code);
            }

            var person = new PersonRecord(
                p.Id, p.PublicId, p.FirstName, p.LastName, p.NationalId, p.BirthDate, p.Email, p.MobilePhone,
                own, new PersonProfile(p.Gender, p.PreviousSurname, p.MaritalStatus, p.EducationName),
                needs.Address ? values?.Address : null)
            {
                HasPhoto = photos.ContainsKey(p.Id),
                BloodGroup = bloodGroup,
            };
            return new PersonExportRecord(person, primary, photos.GetValueOrDefault(p.Id));
        }).ToList();

        return new PersonExportResult(total, rows);
    }

    private sealed record SensitiveValues(long PersonId, string? Address, string? BloodGroup);

    /// <summary>
    /// Listenin ve disa aktarmanin ortak kumesi: kapsam, gorunur kalma suresi, calisma durumu, hizli
    /// arama ve suzgecler. Iki yer ayni kumeyi kullanir; dosyadaki kisiler ekrandaki listeyle aynidir.
    /// </summary>
    private async Task<IQueryable<Person>> MatchingAsync(PersonnelViewer viewer, PersonListCriteria criteria, CancellationToken cancellationToken)
    {
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

        return ApplyFilters(persons, criteria.Filters, activeOnly: criteria.Status == EmploymentStatusFilter.Active);
    }

    /// <summary>Liste siralamasi (SYG-PERS-031); esitlikte ic kimlik, sayfalar kararli kalir.</summary>
    private static IOrderedQueryable<Person> Ordered(IQueryable<Person> persons, PersonListCriteria criteria) =>
        ((criteria.Sort, criteria.Descending) switch
        {
            (PersonSort.LastName, false) => persons.OrderBy(p => p.LastName).ThenBy(p => p.FirstName),
            (PersonSort.LastName, true) => persons.OrderByDescending(p => p.LastName).ThenByDescending(p => p.FirstName),
            (_, true) => persons.OrderByDescending(p => p.FirstName).ThenByDescending(p => p.LastName),
            _ => persons.OrderBy(p => p.FirstName).ThenBy(p => p.LastName),
        }).ThenBy(p => p.Id);

    /// <summary>
    /// Suzgecler (SYG-PERS-024, 025). Her suzgec ayri kosuldur ve birlikte (VE) uygulanir; coklu secimde
    /// secilenlerden herhangi biri yeter. Istihdam suzgecleri kisinin HERHANGI BIR sicilinin guncel
    /// doneminde (baslangici en yeni kayit) eslesir (KR-126). "Aktif" secildiginde yalnizca aktif
    /// siciller aranir (SYG-PERS-022, ADR-0019 §6); aksi halde eski bir sicili Tunus'ta olan, bugun
    /// baska subede calisan kisi Tunus suzgecine girerdi (#338). Cikis tarihi suzgeci bunun
    /// disindadir: yalnizca ayrilmis sicilde anlamlidir. Gorunurluk denetimi servistedir; bu metot
    /// yalnizca izinli istekle cagrilir.
    /// </summary>
    private IQueryable<Person> ApplyFilters(IQueryable<Person> persons, PersonListFilters f, bool activeOnly)
    {
        var today = clock.Today;
        var assignments = context.Set<EmploymentAssignment>();
        var current = assignments.Where(a => a.ValidFrom == assignments.Where(x => x.EmploymentId == a.EmploymentId).Max(x => x.ValidFrom));
        var employments = context.Set<Employment>().Where(e => !activeOnly || e.IsActive);

        IQueryable<Person> WithAssignment(IQueryable<Person> source, IQueryable<EmploymentAssignment> matching)
        {
            var ids = from a in matching join e in employments on a.EmploymentId equals e.Id select e.PersonId;
            return source.Where(p => ids.Contains(p.Id));
        }

        if (f.CompanyIds.Count > 0)
        {
            var ids = context.Set<Company>().Where(c => f.CompanyIds.Contains(c.PublicId)).Select(c => c.Id);
            persons = WithAssignment(persons, current.Where(a => ids.Contains(a.CompanyId)));
        }

        if (f.BranchIds.Count > 0)
        {
            var ids = context.Set<Branch>().Where(b => f.BranchIds.Contains(b.PublicId)).Select(b => (long?)b.Id);
            persons = WithAssignment(persons, current.Where(a => ids.Contains(a.BranchId)));
        }

        if (f.Cities.Count > 0)
        {
            var ids = context.Set<Branch>().Where(b => b.City != null && f.Cities.Contains(b.City)).Select(b => (long?)b.Id);
            persons = WithAssignment(persons, current.Where(a => ids.Contains(a.BranchId)));
        }

        if (f.DepartmentIds.Count > 0)
        {
            var ids = context.Set<Department>().Where(d => f.DepartmentIds.Contains(d.PublicId)).Select(d => (long?)d.Id);
            persons = WithAssignment(persons, current.Where(a => ids.Contains(a.DepartmentId)));
        }

        if (f.TitleCodes.Count > 0)
        {
            persons = WithAssignment(persons, current.Where(a => a.TitleCode != null && f.TitleCodes.Contains(a.TitleCode)));
        }

        if (f.StatusCodes.Count > 0)
        {
            persons = WithAssignment(persons, current.Where(a => a.StatusCode != null && f.StatusCodes.Contains(a.StatusCode)));
        }

        if (f.OccupationCodes.Count > 0)
        {
            persons = WithAssignment(persons, current.Where(a => a.OccupationCode != null && f.OccupationCodes.Contains(a.OccupationCode)));
        }

        if (f.DutyIds.Count > 0)
        {
            var duties = context.Set<EmploymentDuty>();
            var definitionIds = context.Set<ReferenceDefinition>().Where(d => f.DutyIds.Contains(d.PublicId)).Select(d => d.Id);
            var personIds =
                from d in duties
                where d.ValidFrom == duties.Where(x => x.EmploymentId == d.EmploymentId).Max(x => x.ValidFrom)
                      && definitionIds.Contains(d.DutyDefinitionId)
                join e in employments on d.EmploymentId equals e.Id
                select e.PersonId;
            persons = persons.Where(p => personIds.Contains(p.Id));
        }

        if (f.Educations.Count > 0)
        {
            persons = persons.Where(p => p.EducationName != null && f.Educations.Contains(p.EducationName));
        }

        if (f.MaritalStatuses.Count > 0)
        {
            persons = persons.Where(p => p.MaritalStatus != null && f.MaritalStatuses.Contains(p.MaritalStatus.Value));
        }

        if (f.Genders.Count > 0)
        {
            persons = persons.Where(p => p.Gender != null && f.Genders.Contains(p.Gender.Value));
        }

        if (Folded(f.FirstName) is { } firstName)
        {
            persons = persons.Where(p => HrmsDbFunctions.Fold(p.FirstName).Contains(firstName));
        }

        if (Folded(f.LastName) is { } lastName)
        {
            persons = persons.Where(p => HrmsDbFunctions.Fold(p.LastName).Contains(lastName));
        }

        if (Folded(f.PreviousSurname) is { } previousSurname)
        {
            persons = persons.Where(p => p.PreviousSurname != null && HrmsDbFunctions.Fold(p.PreviousSurname).Contains(previousSurname));
        }

        if (Trimmed(f.RegistryCode) is { } registryCode)
        {
            persons = persons.Where(p => p.Employments.Any(e => (!activeOnly || e.IsActive) && e.RegistryCode.Contains(registryCode)));
        }

        if (Trimmed(f.NationalId) is { } nationalId)
        {
            persons = persons.Where(p => p.NationalId.Contains(nationalId));
        }

        if (Trimmed(f.Email)?.ToLowerInvariant() is { } email)
        {
            persons = persons.Where(p => p.Email != null && p.Email.Contains(email));
        }

        if (Trimmed(f.Phone) is { } phone)
        {
            // Numara basinda 0 olmadan tutulur; kullanici "0532..." yazsa da eslesir.
            var digits = new string([.. phone.Where(char.IsAsciiDigit)]).TrimStart('0');
            persons = persons.Where(p => p.MobilePhone != null && p.MobilePhone.Contains(digits));
        }

        if (Folded(f.Address) is { } address)
        {
            var ids = context.Set<PersonSensitive>().Where(s => s.Address != null && HrmsDbFunctions.Fold(s.Address).Contains(address)).Select(s => s.PersonId);
            persons = persons.Where(p => ids.Contains(p.Id));
        }

        if (f.BirthDate is { } birth)
        {
            if (birth.From is { } from)
            {
                persons = persons.Where(p => p.BirthDate >= from);
            }

            if (birth.To is { } to)
            {
                persons = persons.Where(p => p.BirthDate <= to);
            }
        }

        if (f.HireDate is { } hire)
        {
            persons = persons.Where(p => p.Employments.Any(e => (!activeOnly || e.IsActive) &&
                (hire.From == null || e.HireDate >= hire.From) && (hire.To == null || e.HireDate <= hire.To)));
        }

        if (f.TerminationDate is { } termination)
        {
            persons = persons.Where(p => p.Employments.Any(e => e.TerminationDate != null &&
                (termination.From == null || e.TerminationDate >= termination.From) && (termination.To == null || e.TerminationDate <= termination.To)));
        }

        if (f.Age is { } age)
        {
            // Yas >= min  <=>  dogum <= bugun - min yil; yas <= max  <=>  dogum > bugun - (max + 1) yil.
            if (age.Min is { } min)
            {
                var latest = today.AddYears(-min);
                persons = persons.Where(p => p.BirthDate <= latest);
            }

            if (age.Max is { } max)
            {
                var earliest = today.AddYears(-(max + 1));
                persons = persons.Where(p => p.BirthDate > earliest);
            }
        }

        if (f.SeniorityYears is { } seniority)
        {
            // Kidem bitisi: aktifte bugun, ayrilanda cikis gunu dahil (SYG-PERS-039).
            var min = seniority.Min;
            var max = seniority.Max;
            persons = persons.Where(p => p.Employments.Any(e => (!activeOnly || e.IsActive) &&
                (min == null || e.HireDate.AddYears(min.Value) <= (e.TerminationDate == null ? today : e.TerminationDate.Value.AddDays(1))) &&
                (max == null || e.HireDate.AddYears(max.Value + 1) > (e.TerminationDate == null ? today : e.TerminationDate.Value.AddDays(1)))));
        }

        return persons;
    }

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Folded(string? value) => string.IsNullOrWhiteSpace(value) ? null : SearchText.Fold(value);

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
    public async Task<PhotoRecord?> FindPhotoAsync(Guid personId, PersonnelViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        // Kart ile ayni kapsam ve gorunur kalma kurali (SYG-PERS-041, 028).
        var persons = await VisibleAsync(InScope(viewer), cancellationToken).ConfigureAwait(false);
        return await persons
            .Where(person => person.PublicId == personId)
            .Join(context.Set<PersonPhoto>(), person => person.Id, photo => photo.PersonId, (_, photo) => new PhotoRecord(photo.Content, photo.SourceHash, photo.ThumbnailVersion))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<PhotoRecord?> FindOwnPhotoAsync(PersonnelViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        if (viewer.PersonId is not { } own)
        {
            return null;
        }

        return await context.Set<PersonPhoto>()
            .AsNoTracking()
            .Where(photo => photo.PersonId == own)
            .Select(photo => new PhotoRecord(photo.Content, photo.SourceHash, photo.ThumbnailVersion))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public Task<PersonRecord?> FindOwnAsync(PersonnelViewer viewer, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(viewer);

        return viewer.PersonId is { } own
            ? FirstAsync(context.Set<Person>().Where(person => person.Id == own), viewer, cancellationToken)
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

    private async Task<PersonRecord?> FirstAsync(IQueryable<Person> query, PersonnelViewer viewer, CancellationToken cancellationToken)
    {
        var record = await ReadAsync(query, cancellationToken).ConfigureAwait(false);
        if (record is null)
        {
            return null;
        }

        // Kan grubu yalnizca gorebilen bakista cozulur; gormeyen icin sifreli deger hic okunmaz.
        if (PersonFieldVisibility.CanSeeSensitive(viewer, record.Id))
        {
            record = record with
            {
                BloodGroup = await BloodGroupAsync(record, cancellationToken).ConfigureAwait(false),
                EmergencyContacts = await EmergencyContactsAsync(record.Id, cancellationToken).ConfigureAwait(false),
            };
        }

        if (record.Employments.Count == 0)
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

    /// <summary>
    /// Kan grubunu cozer ve referans veriden adini bulur (ADR-0020 §1, ADR-0022 §1). Cozulemeyen
    /// deger (anahtar kaybi, baska satirdan kopya) "kayitli degil" gibi davranir; bir sonraki
    /// senkronizasyon LOGO'daki kodu yeniden yazar.
    /// </summary>
    private async Task<string?> BloodGroupAsync(PersonRecord record, CancellationToken cancellationToken)
    {
        var value = await context.Set<PersonSensitive>()
            .AsNoTracking()
            .Where(s => s.PersonId == record.Id)
            .Select(s => s.BloodGroup)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (value is null || protector.TryUnprotect(value, ProtectedField.BloodGroup(record.PublicId)) is not { } code)
        {
            return null;
        }

        return await context.Set<ReferenceDefinition>()
            .AsNoTracking()
            .Where(d => d.Type == ReferenceType.BloodGroup && d.Code == code)
            .Select(d => d.Name)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <summary>Kisinin silinmemis acil durum kisileri, eklenme sirasiyla (SYG-PERS-043).</summary>
    private async Task<IReadOnlyList<EmergencyContactRecord>> EmergencyContactsAsync(long personId, CancellationToken cancellationToken) =>
        await (
            from c in context.Set<EmergencyContact>().AsNoTracking()
            where c.PersonId == personId
            join r in context.Set<ReferenceDefinition>() on c.RelationshipDefinitionId equals r.Id
            orderby c.CreatedAt, c.Id
            select new EmergencyContactRecord(c.PublicId, r.PublicId, r.Name, c.FirstName, c.LastName, c.MobilePhone))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

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
                context.Set<PersonSensitive>().Where(s => s.PersonId == person.Id).Select(s => s.Address).FirstOrDefault())
            {
                HasPhoto = context.Set<PersonPhoto>().Any(photo => photo.PersonId == person.Id),
            })
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
}
