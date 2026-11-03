using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Domain.Common;
using Dsg.Hrms.Domain.Organization;
using Dsg.Hrms.Domain.Personnel;
using static Dsg.Hrms.Application.Tests.Personnel.Sync.TestData;

namespace Dsg.Hrms.Application.Tests.Personnel.Sync;

/// <summary>
/// Senkronizasyonun T1 adimi: genel bilgiler, adres, sube ve birim tanimlari (SYG-PERS-003).
/// Veriler SENTETIKTIR.
/// </summary>
public sealed class PersonnelDetailsSynchronizerTests
{
    private readonly Company _company = WithId(Company.FromLogo(1, "Duzen Laboratuvarlar"), 1);

    [Fact]
    public void Profile_comes_from_the_primary_employment_card()
    {
        // KR-130: aktif istihdamlardan ise giris tarihi en eski olanin karti esastir.
        var person = Person(1);
        var older = Employ(person, "00001", logoRef: 10, hire: new DateOnly(2015, 1, 1));
        var newer = Employ(person, "00002", logoRef: 20, hire: new DateOnly(2022, 1, 1));
        var ended = Employ(person, "00003", logoRef: 30, hire: new DateOnly(2024, 1, 1), terminated: Today.AddDays(-30));

        PersonnelDetailsSynchronizer.Synchronize(
            Details(Profile(10, education: "Lise"), Profile(20, education: "Lisans"), Profile(30, education: "Doktora")),
            Snapshot([person], [older, newer, ended]));

        person.EducationName.ShouldBe("Lise");
    }

    [Fact]
    public void Earliest_active_hire_wins_even_when_its_registry_code_is_larger()
    {
        // KR-130: sira kod sirasina degil ise giris tarihine bakar.
        var person = Person(1);
        var later = Employ(person, "00001", logoRef: 10, hire: new DateOnly(2021, 3, 1));
        var earlier = Employ(person, "00009", logoRef: 90, hire: new DateOnly(2016, 9, 1));

        Employment.Primary([later, earlier]).ShouldBe(earlier);
    }

    [Fact]
    public void Active_employments_with_the_same_hire_date_take_the_smaller_registry_code()
    {
        var person = Person(1);
        var second = Employ(person, "00007", logoRef: 70, hire: new DateOnly(2020, 1, 1));
        var first = Employ(person, "00003", logoRef: 30, hire: new DateOnly(2020, 1, 1));

        Employment.Primary([second, first]).ShouldBe(first);
    }

    [Fact]
    public void Terminated_person_takes_the_card_with_the_latest_termination()
    {
        var person = Person(1);
        var first = Employ(person, "00001", logoRef: 10, hire: new DateOnly(2010, 1, 1), terminated: new DateOnly(2018, 1, 1));
        var last = Employ(person, "00002", logoRef: 20, hire: new DateOnly(2012, 1, 1), terminated: new DateOnly(2025, 1, 1));

        Employment.Primary([first, last]).ShouldBe(last);
    }

    [Theory]
    [InlineData((short)1, (short)1, Gender.Male, MaritalStatus.Married)]
    [InlineData((short)2, (short)2, Gender.Female, MaritalStatus.NotMarried)]
    [InlineData((short)0, (short)0, null, MaritalStatus.NotMarried)]
    [InlineData((short)2, null, Gender.Female, null)]
    public void Logo_codes_map_to_gender_and_marital_status(short sex, short? marital, Gender? gender, MaritalStatus? status)
    {
        // PG-PERS-19: medeni hal 1 evli, diger her deger bekar; aile satiri yoksa tanimsiz.
        var profile = PersonnelDetailsSynchronizer.ToProfile(new LogoProfileRecord(1, sex, null, marital, null, null));

        profile.Gender.ShouldBe(gender);
        profile.MaritalStatus.ShouldBe(status);
    }

    [Fact]
    public void Previous_surname_is_kept_only_for_women()
    {
        // PG-PERS-19: gosterilmeyecek bir deger saklanmaz.
        var woman = Person(1);
        var man = Person(2);

        PersonnelDetailsSynchronizer.Synchronize(
            Details(Profile(10, sex: 2, previousSurname: " Demir "), Profile(20, sex: 1, previousSurname: "Kaya")),
            Snapshot([woman, man], [Employ(woman, "00001", 10), Employ(man, "00002", 20)]));

        woman.PreviousSurname.ShouldBe("Demir");
        man.PreviousSurname.ShouldBeNull();
    }

    [Fact]
    public void Address_row_is_created_updated_and_cleared()
    {
        var person = Person(1);
        var employment = Employ(person, "00001", 10);

        var created = PersonnelDetailsSynchronizer.Synchronize(Details(Profile(10, address: "Ornek Mah. 1")), Snapshot([person], [employment]));
        var row = created.NewSensitive.Single();
        row.PersonId.ShouldBe(person.Id);
        row.Address.ShouldBe("Ornek Mah. 1");
        created.PersonsUpdated.ShouldBe(1);

        var updated = PersonnelDetailsSynchronizer.Synchronize(Details(Profile(10, address: "Yeni Mah. 2")), Snapshot([person], [employment], [row]));
        updated.NewSensitive.ShouldBeEmpty();
        row.Address.ShouldBe("Yeni Mah. 2");

        PersonnelDetailsSynchronizer.Synchronize(Details(Profile(10, address: "  ")), Snapshot([person], [employment], [row]));
        row.Address.ShouldBeNull();
    }

    [Fact]
    public void Unchanged_profile_is_not_counted_as_an_update()
    {
        // Degismeyen alanlar yazilmaz; aksi halde 15 dakikada bir denetim izine gurultu duserdi.
        var person = Person(1);
        var employment = Employ(person, "00001", 10);
        var details = Details(Profile(10, sex: 1, education: "Lisans"));

        PersonnelDetailsSynchronizer.Synchronize(details, Snapshot([person], [employment])).PersonsUpdated.ShouldBe(1);
        PersonnelDetailsSynchronizer.Synchronize(details, Snapshot([person], [employment])).PersonsUpdated.ShouldBe(0);
    }

    [Fact]
    public void Branches_and_departments_are_matched_by_firm_and_number()
    {
        var branch = WithId(Branch.FromLogo(_company.Id, 5, "Eski Ad", "Ankara"), 1);
        var details = new LogoPersonnelDetails(
            [],
            [new LogoUnitRecord(1, 5, "Merkez Laboratuvar", "Ankara"), new LogoUnitRecord(1, 6, "Yeni Sube", null), new LogoUnitRecord(9, 1, "Firmasiz", null)],
            [new LogoUnitRecord(1, 7, "Bilgi Islem", null)],
            []);

        var outcome = PersonnelDetailsSynchronizer.Synchronize(details, Snapshot([], [], branches: [branch]));

        branch.Name.ShouldBe("Merkez Laboratuvar");
        outcome.NewBranches.Select(b => (b.LogoNumber, b.Name)).ShouldBe([((short)6, "Yeni Sube")]);
        outcome.NewDepartments.Single().Name.ShouldBe("Bilgi Islem");
        outcome.UnitsChanged.ShouldBe(3);
    }

    [Fact]
    public void Blood_group_change_is_reported_with_the_plain_code_and_unchanged_value_is_not()
    {
        // SYG-PERS-005, ADR-0020: bu adim duz kodu karsilastirir; sifrelemeyi depo yapar. Adresi
        // olmayan kisiye yalnizca kan grubu icin korumali alan satiri acilir.
        var person = Person(1);
        var employment = Employ(person, "00001", 10);

        var created = PersonnelDetailsSynchronizer.Synchronize(Details(Profile(10) with { BloodGroup = 3 }), Snapshot([person], [employment]));

        var row = created.NewSensitive.ShouldHaveSingleItem();
        created.BloodGroups.ShouldBe([new BloodGroupChange(row, person.PublicId, 3)]);
        created.PersonsUpdated.ShouldBe(1);

        var unchanged = PersonnelDetailsSynchronizer.Synchronize(
            Details(Profile(10) with { BloodGroup = 3 }),
            Snapshot([person], [employment], [row]) with { BloodGroups = new Dictionary<long, short> { [person.Id] = 3 } });
        unchanged.BloodGroups.ShouldBeEmpty();
        unchanged.PersonsUpdated.ShouldBe(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData((short)0)]
    [InlineData((short)9)]
    public void Missing_unspecified_or_invalid_code_clears_the_stored_blood_group(short? logoCode)
    {
        // Ayar kapaliyken (null) mevcut deger temizlenir (SYG-PERS-005); 0 "Belirtilmemis" ve
        // aralik disi kod saklanmaz (KR-129).
        var person = Person(1);
        var employment = Employ(person, "00001", 10);
        var row = PersonSensitive.For(person.Id);

        var outcome = PersonnelDetailsSynchronizer.Synchronize(
            Details(Profile(10) with { BloodGroup = logoCode }),
            Snapshot([person], [employment], [row]) with { BloodGroups = new Dictionary<long, short> { [person.Id] = 6 } });

        outcome.BloodGroups.ShouldBe([new BloodGroupChange(row, person.PublicId, null)]);

        // Hic kan grubu olmayan kiside temizlenecek bir sey yoktur ve satir acilmaz.
        var nothing = PersonnelDetailsSynchronizer.Synchronize(Details(Profile(10) with { BloodGroup = logoCode }), Snapshot([person], [employment]));
        nothing.BloodGroups.ShouldBeEmpty();
        nothing.NewSensitive.ShouldBeEmpty();
    }

    private static LogoProfileRecord Profile(int logoRef, short? sex = 1, string? previousSurname = null, short? marital = 2, string? education = null, string? address = null) =>
        new(logoRef, sex, previousSurname, marital, education, address);

    private static LogoPersonnelDetails Details(params LogoProfileRecord[] profiles) => new(profiles, [], [], []);

    private PersonnelDetailsSnapshot Snapshot(
        IReadOnlyList<Person> persons,
        IReadOnlyList<Employment> employments,
        IReadOnlyList<PersonSensitive>? sensitive = null,
        IReadOnlyList<Branch>? branches = null) =>
        new([_company], persons, employments, sensitive ?? [], branches ?? [], []);

    private static Person Person(int index) =>
        WithId(Dsg.Hrms.Domain.Personnel.Person.Create(NationalId(index), new PersonDetails("Ayse", "Yilmaz", new DateOnly(1990, 1, 1), null, false, null)), index);

    private Employment Employ(Person person, string code, int logoRef, DateOnly? hire = null, DateOnly? terminated = null)
    {
        var employment = Employment.Create(person, _company, code, new EmploymentDetails(hire ?? new DateOnly(2020, 1, 1), terminated, logoRef), Today);
        typeof(Employment).GetProperty(nameof(Employment.PersonId))!.SetValue(employment, person.Id);
        return employment;
    }

    private static T WithId<T>(T entity, long id)
        where T : Entity
    {
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);
        return entity;
    }
}
