using Dsg.Hrms.Domain.Common;
using Dsg.Hrms.Domain.Organization;
using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Domain.Tests.Personnel;

/// <summary>
/// Gorev zaman cizelgesi kurallari (ADR-0021 §2; SYG-PERS-021, 042). Veriler SENTETIKTIR.
/// </summary>
public sealed class DutyTimelineTests
{
    private static readonly DateOnly Today = new(2026, 10, 24);
    private static readonly DateOnly Hire = new(2020, 1, 1);

    [Fact]
    public void New_duty_closes_the_open_one_with_its_start_date()
    {
        var employment = Employ(null);
        var (first, _) = DutyTimeline.Assign(employment, [], 10, Hire);

        var (second, change) = DutyTimeline.Assign(employment, [first!], 20, Today);

        change.ShouldBe(DutyChange.Opened);
        first!.ValidTo.ShouldBe(Today);
        second!.ValidFrom.ShouldBe(Today);
        second.ValidTo.ShouldBeNull();
        second.DutyDefinitionId.ShouldBe(20);
    }

    [Fact]
    public void Same_day_entry_replaces_the_duty_instead_of_an_empty_range()
    {
        var employment = Employ(null);
        var (open, _) = DutyTimeline.Assign(employment, [], 10, Today);

        DutyTimeline.Assign(employment, [open!], 20, Today).ShouldBe((null, DutyChange.Replaced));
        open!.DutyDefinitionId.ShouldBe(20);
        DutyTimeline.Assign(employment, [open], 20, Today).ShouldBe((null, DutyChange.Unchanged));
    }

    [Fact]
    public void Entry_before_the_open_duty_is_a_conflict()
    {
        // ADR-0021 §2: mevcut kaydi kisaltmak icin once o kayit duzeltilir.
        var employment = Employ(null);
        var (open, _) = DutyTimeline.Assign(employment, [], 10, Today);

        Should.Throw<DutyRuleException>(() => DutyTimeline.Assign(employment, [open!], 20, Today.AddDays(-1))).IsConflict.ShouldBeTrue();
    }

    [Fact]
    public void Start_must_be_within_the_employment()
    {
        // AN-16: gorev istihdamin disina tasamaz.
        Should.Throw<DutyRuleException>(() => DutyTimeline.Assign(Employ(null), [], 10, Hire.AddDays(-1))).IsConflict.ShouldBeFalse();
        Should.Throw<DutyRuleException>(() => DutyTimeline.Assign(Employ(Today.AddDays(-10)), [], 10, Today.AddDays(-9))).IsConflict.ShouldBeFalse();
    }

    [Fact]
    public void Backdated_duty_on_a_terminated_employment_ends_the_day_after_termination()
    {
        var termination = Today.AddDays(-10);

        var (added, _) = DutyTimeline.Assign(Employ(termination), [], 10, Hire);

        added!.ValidTo.ShouldBe(termination.AddDays(1));
    }

    [Fact]
    public void Termination_closes_the_open_duty_the_day_after_the_last_working_day()
    {
        // SYG-PERS-021.
        var (open, _) = DutyTimeline.Assign(Employ(null), [], 10, Hire);
        var termination = Today.AddDays(-3);

        DutyTimeline.CloseOnTermination(Employ(termination), [open!]).ShouldBeTrue();
        open!.ValidTo.ShouldBe(termination.AddDays(1));
        DutyTimeline.CloseOnTermination(Employ(termination), [open]).ShouldBeFalse();
        DutyTimeline.CloseOnTermination(Employ(null), [open]).ShouldBeFalse();
    }

    private static Employment Employ(DateOnly? termination)
    {
        var company = Company.FromLogo(1, "Duzen Laboratuvarlar");
        var person = Person.Create(TestNationalIds.Sequential(1), new PersonDetails("Ayse", "Yilmaz", new DateOnly(1990, 1, 1), null, false, null));
        var employment = Employment.Create(person, company, "00001", new EmploymentDetails(Hire, termination, 1), Today);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(employment, 7L);
        return employment;
    }
}
