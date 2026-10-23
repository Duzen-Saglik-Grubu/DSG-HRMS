using Dsg.Hrms.Domain.Organization;
using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Application.Personnel.Sync;

/// <summary>
/// Senkronizasyonun T1 adimi (SYG-PERS-003, 013): kisinin genel bilgileri, adresi, sube ve
/// birim tanimlari. Saf mantiktir; veritabanina dokunmaz.
/// </summary>
/// <remarks>
/// <para>
/// Kisi ve istihdam adimi (<see cref="PersonnelSynchronizer"/>) once kaydedilir; bu adim
/// basarisiz olursa ilk adimin degisiklikleri kalir (AN-07). Boylece hesabin istihdam bitince
/// kapanmasi (SYG-KMLK-078) bu adimdan etkilenmez.
/// </para>
/// <para>
/// Bir kisinin birden fazla karti olabilir. Genel bilgiler ve adres kisinin <b>esas
/// istihdaminin</b> kartindan alinir (<see cref="Employment.Primary"/>): liste kartinda
/// gorunen sicille ayni kural (<c>KR-126</c>).
/// </para>
/// </remarks>
public static class PersonnelDetailsSynchronizer
{
    /// <summary>T1 verisini uygular.</summary>
    public static DetailsOutcome Synchronize(LogoPersonnelDetails details, PersonnelDetailsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(details);
        ArgumentNullException.ThrowIfNull(snapshot);

        var companies = snapshot.Companies.ToDictionary(c => c.LogoFirmNumber);
        var newBranches = new List<Branch>();
        var newDepartments = new List<Department>();
        var unitsChanged = 0;

        var branches = snapshot.Branches.ToDictionary(b => (b.CompanyId, b.LogoNumber));
        foreach (var record in details.Branches)
        {
            if (!companies.TryGetValue(record.FirmNumber, out var company))
            {
                continue;
            }

            if (branches.TryGetValue((company.Id, record.Number), out var branch))
            {
                unitsChanged += branch.Apply(record.Name, record.City) ? 1 : 0;
            }
            else
            {
                branch = Branch.FromLogo(company.Id, record.Number, record.Name, record.City);
                branches.Add((company.Id, record.Number), branch);
                newBranches.Add(branch);
            }
        }

        var departments = snapshot.Departments.ToDictionary(d => (d.CompanyId, d.LogoNumber));
        foreach (var record in details.Departments)
        {
            if (!companies.TryGetValue(record.FirmNumber, out var company))
            {
                continue;
            }

            if (departments.TryGetValue((company.Id, record.Number), out var department))
            {
                unitsChanged += department.Rename(record.Name) ? 1 : 0;
            }
            else
            {
                department = Department.FromLogo(company.Id, record.Number, record.Name);
                departments.Add((company.Id, record.Number), department);
                newDepartments.Add(department);
            }
        }

        var profiles = details.Profiles.GroupBy(p => p.LogoRef).ToDictionary(g => g.Key, g => g.First());
        var employmentsByPerson = snapshot.Employments.ToLookup(e => e.PersonId);
        var sensitiveByPerson = snapshot.Sensitive.ToDictionary(s => s.PersonId);
        var newSensitive = new List<PersonSensitive>();
        var personsUpdated = 0;

        foreach (var person in snapshot.Persons)
        {
            var primary = Employment.Primary(employmentsByPerson[person.Id]);
            if (primary is null || !profiles.TryGetValue(primary.LogoRef, out var profile))
            {
                continue;
            }

            var changed = person.ApplyProfile(ToProfile(profile));

            if (sensitiveByPerson.TryGetValue(person.Id, out var sensitive))
            {
                changed |= sensitive.ApplyAddress(profile.Address);
            }
            else if (!string.IsNullOrWhiteSpace(profile.Address))
            {
                sensitive = PersonSensitive.For(person.Id);
                sensitive.ApplyAddress(profile.Address);
                newSensitive.Add(sensitive);
                changed = true;
            }

            personsUpdated += changed ? 1 : 0;
        }

        return new DetailsOutcome(newSensitive, newBranches, newDepartments, personsUpdated, unitsChanged + newBranches.Count + newDepartments.Count);
    }

    /// <summary>LOGO kodlarini alan modeline cevirir.</summary>
    /// <remarks>
    /// Cinsiyet: 1 erkek, 2 kadin, diger tanimsiz. Medeni hal: 1 evli, diger her deger bekar;
    /// aile satiri yoksa tanimsiz (PG-PERS-19, toplantı kaydı §6.1).
    /// </remarks>
    public static PersonProfile ToProfile(LogoProfileRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        Gender? gender = record.Sex switch
        {
            1 => Gender.Male,
            2 => Gender.Female,
            _ => null,
        };
        MaritalStatus? maritalStatus = record.MaritalStatus switch
        {
            null => null,
            1 => MaritalStatus.Married,
            _ => MaritalStatus.NotMarried,
        };

        return new PersonProfile(gender, record.PreviousSurname, maritalStatus, record.EducationName);
    }
}

/// <summary>T1 adiminin yukledigi mevcut durum.</summary>
/// <param name="Companies">Firmalar.</param>
/// <param name="Persons">Kisiler (izlenen).</param>
/// <param name="Employments">Istihdamlar.</param>
/// <param name="Sensitive">Korumali alan satirlari (izlenen).</param>
/// <param name="Branches">Subeler (izlenen).</param>
/// <param name="Departments">Birimler (izlenen).</param>
public sealed record PersonnelDetailsSnapshot(
    IReadOnlyList<Company> Companies,
    IReadOnlyList<Person> Persons,
    IReadOnlyList<Employment> Employments,
    IReadOnlyList<PersonSensitive> Sensitive,
    IReadOnlyList<Branch> Branches,
    IReadOnlyList<Department> Departments);

/// <summary>T1 adiminin sonucu.</summary>
/// <param name="NewSensitive">Eklenecek korumali alan satirlari.</param>
/// <param name="NewBranches">Eklenecek subeler.</param>
/// <param name="NewDepartments">Eklenecek birimler.</param>
/// <param name="PersonsUpdated">Genel bilgisi veya adresi degisen kisi sayisi.</param>
/// <param name="UnitsChanged">Eklenen veya degisen sube ve birim sayisi.</param>
public sealed record DetailsOutcome(
    IReadOnlyList<PersonSensitive> NewSensitive,
    IReadOnlyList<Branch> NewBranches,
    IReadOnlyList<Department> NewDepartments,
    int PersonsUpdated,
    int UnitsChanged);
