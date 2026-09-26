using Dsg.Hrms.Domain.Organization;
using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Domain.Tests.Personnel;

public sealed class PersonAndEmploymentTests
{
    private static readonly DateOnly Today = new(2026, 9, 26);

    private static PersonDetails Details(string lastName = "Yilmaz", string? email = "a.yilmaz@duzen.com.tr") =>
        new("Ahmet", lastName, new DateOnly(1985, 4, 12), email, IsEmailShared: false, MobilePhone: "5321234567");

    // ------------------------------------------------------------------ kisi

    [Fact]
    public void Person_is_created_with_valid_national_id()
    {
        var nationalId = TestNationalIds.From("123456789");

        var person = Person.Create(nationalId, Details());

        person.NationalId.ShouldBe(nationalId);
        person.FirstName.ShouldBe("Ahmet");
        person.Email.ShouldBe("a.yilmaz@duzen.com.tr");
        person.Employments.ShouldBeEmpty();
    }

    [Fact]
    public void Person_cannot_be_created_with_invalid_national_id()
    {
        Should.Throw<ArgumentException>(() => Person.Create("12345678901", Details()));
    }

    [Fact]
    public void Applying_identical_details_reports_no_change()
    {
        // Degismeyen kayit yazilmaz; aksi halde her senkronizasyon denetim izine
        // gurultu eklerdi.
        var person = Person.Create(TestNationalIds.From("123456789"), Details());

        person.Apply(Details()).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Demir", "a.yilmaz@duzen.com.tr")]
    [InlineData("Yilmaz", "yeni@duzen.com.tr")]
    [InlineData("Yilmaz", null)]
    public void Applying_changed_details_updates_the_person(string lastName, string? email)
    {
        var person = Person.Create(TestNationalIds.From("123456789"), Details());

        person.Apply(Details(lastName, email)).ShouldBeTrue();

        person.LastName.ShouldBe(lastName);
        person.Email.ShouldBe(email);
    }

    [Fact]
    public void Shared_email_flag_change_counts_as_a_change()
    {
        var person = Person.Create(TestNationalIds.From("123456789"), Details());

        person.Apply(Details() with { IsEmailShared = true }).ShouldBeTrue();
        person.IsEmailShared.ShouldBeTrue();
    }

    // ------------------------------------------------------------------ istihdam

    [Theory]
    [InlineData(null, true)]
    [InlineData("2026-09-27", true)]   // gelecekte cikis
    [InlineData("2026-09-26", true)]   // bugun son calisma gunu
    [InlineData("2026-09-25", false)]  // dun cikti
    public void Employment_activity_follows_the_termination_date(string? termination, bool expected)
    {
        DateOnly? date = termination is null ? null : DateOnly.Parse(termination, System.Globalization.CultureInfo.InvariantCulture);

        Employment.IsActiveOn(date, Today).ShouldBe(expected);
    }

    [Fact]
    public void Employment_is_created_active_without_termination_date()
    {
        var employment = CreateEmployment(termination: null);

        employment.IsActive.ShouldBeTrue();
        employment.RegistryCode.ShouldBe("00123");
    }

    [Fact]
    public void Terminated_employment_becomes_inactive_on_the_next_day()
    {
        var person = Person.Create(TestNationalIds.From("123456789"), Details());
        var company = Company.FromLogo(1, "Duzen Laboratuvarlar");
        var employment = Employment.Create(person, company, "00123", new(new DateOnly(2020, 1, 1), null, 10), Today);

        var changed = employment.Apply(person, company, new(new DateOnly(2020, 1, 1), Today.AddDays(-1), 10), Today);

        changed.ShouldBeTrue();
        employment.IsActive.ShouldBeFalse();
        employment.TerminationDate.ShouldBe(Today.AddDays(-1));
    }

    [Fact]
    public void Employment_activity_is_recomputed_as_days_pass_without_source_change()
    {
        // Cikis tarihi gelecekte girilmis istihdam, o gun gectiginde kaynak
        // degismese de pasiflesmelidir.
        var person = Person.Create(TestNationalIds.From("123456789"), Details());
        var company = Company.FromLogo(1, "Duzen Laboratuvarlar");
        var details = new EmploymentDetails(new DateOnly(2020, 1, 1), Today, 10);
        var employment = Employment.Create(person, company, "00123", details, Today);

        employment.IsActive.ShouldBeTrue();
        employment.Apply(person, company, details, Today.AddDays(1)).ShouldBeTrue();
        employment.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Applying_identical_employment_reports_no_change()
    {
        var person = Person.Create(TestNationalIds.From("123456789"), Details());
        var company = Company.FromLogo(1, "Duzen Laboratuvarlar");
        var details = new EmploymentDetails(new DateOnly(2020, 1, 1), null, 10);
        var employment = Employment.Create(person, company, "00123", details, Today);

        employment.Apply(person, company, details, Today).ShouldBeFalse();
    }

    [Fact]
    public void Employment_moves_to_another_person_when_the_card_national_id_is_corrected()
    {
        var company = Company.FromLogo(1, "Duzen Laboratuvarlar");
        var wrong = Person.Create(TestNationalIds.From("123456789"), Details());
        var right = Person.Create(TestNationalIds.From("987654321"), Details());
        var details = new EmploymentDetails(new DateOnly(2020, 1, 1), null, 10);
        var employment = Employment.Create(wrong, company, "00123", details, Today);

        employment.Apply(right, company, details, Today).ShouldBeTrue();
        employment.Person.ShouldBeSameAs(right);
    }

    [Fact]
    public void Registry_code_is_required()
    {
        var person = Person.Create(TestNationalIds.From("123456789"), Details());
        var company = Company.FromLogo(1, "Duzen Laboratuvarlar");

        Should.Throw<ArgumentException>(() =>
            Employment.Create(person, company, " ", new(new DateOnly(2020, 1, 1), null, 10), Today));
    }

    // ------------------------------------------------------------------ firma

    [Fact]
    public void Company_rename_reports_change_only_when_the_name_differs()
    {
        var company = Company.FromLogo(3, "Eski Ad");

        company.Rename("Eski Ad").ShouldBeFalse();
        company.Rename("Yeni Ad").ShouldBeTrue();
        company.Name.ShouldBe("Yeni Ad");
        company.LogoFirmNumber.ShouldBe((short)3);
    }

    private static Employment CreateEmployment(DateOnly? termination)
    {
        var person = Person.Create(TestNationalIds.From("123456789"), Details());
        var company = Company.FromLogo(1, "Duzen Laboratuvarlar");
        return Employment.Create(person, company, "00123", new(new DateOnly(2020, 1, 1), termination, 10), Today);
    }
}
