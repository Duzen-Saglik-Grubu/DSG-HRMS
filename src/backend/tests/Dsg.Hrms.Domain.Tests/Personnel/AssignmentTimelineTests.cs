using Dsg.Hrms.Domain.Common;
using Dsg.Hrms.Domain.Organization;
using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Domain.Tests.Personnel;

/// <summary>
/// Tarih aralikli atamanin gecis kurallari (ADR-0021 §2; SYG-PERS-018, 021). Veriler SENTETIKTIR.
/// </summary>
public sealed class AssignmentTimelineTests
{
    private static readonly DateOnly Today = new(2026, 10, 24);
    private static readonly AssignmentValues Merkez = new(1, 10, 20, "1", "A", "2511.01", "Programci");
    private static readonly AssignmentValues Sube2 = Merkez with { BranchId = 11 };

    [Fact]
    public void First_sight_of_an_active_employment_opens_a_record_from_today()
    {
        // Gecmis T1'in devreye girdigi gunden baslar; onceki gunler icin kayit yoktur.
        var (added, changed) = AssignmentTimeline.Apply(Active(), [], Merkez, Today);

        changed.ShouldBeTrue();
        added!.ValidFrom.ShouldBe(Today);
        added.ValidTo.ShouldBeNull();
        added.Covers(Today.AddDays(-1)).ShouldBeFalse();
    }

    [Fact]
    public void Unchanged_values_write_nothing()
    {
        var open = Open(Today.AddDays(-30), Merkez);

        AssignmentTimeline.Apply(Active(), [open], Merkez, Today).ShouldBe((null, false));
    }

    [Fact]
    public void A_change_closes_the_open_record_today_and_opens_a_new_one()
    {
        var open = Open(Today.AddDays(-30), Merkez);

        var (added, changed) = AssignmentTimeline.Apply(Active(), [open], Sube2, Today);

        changed.ShouldBeTrue();
        open.ValidTo.ShouldBe(Today);
        added!.ValidFrom.ShouldBe(Today);
        added.BranchId.ShouldBe(11);
        open.Covers(Today.AddDays(-1)).ShouldBeTrue();
        open.Covers(Today).ShouldBeFalse();
    }

    [Fact]
    public void Second_change_on_the_same_day_updates_the_record_opened_today()
    {
        // Sifir uzunlukta aralik olusmaz; gunun son degeri gecerlidir.
        var openedToday = Open(Today, Merkez);

        var (added, changed) = AssignmentTimeline.Apply(Active(), [openedToday], Sube2, Today);

        changed.ShouldBeTrue();
        added.ShouldBeNull();
        openedToday.BranchId.ShouldBe(11);
        openedToday.ValidTo.ShouldBeNull();
    }

    [Fact]
    public void Company_change_is_a_change_too()
    {
        // Degisiklik talebi #284: firma da tarih aralikli tutulur.
        var open = Open(Today.AddDays(-5), Merkez);

        var (added, _) = AssignmentTimeline.Apply(Active(), [open], Merkez with { CompanyId = 2 }, Today);

        open.ValidTo.ShouldBe(Today);
        added!.CompanyId.ShouldBe(2);
    }

    [Fact]
    public void Termination_closes_the_open_record_the_day_after_the_last_working_day()
    {
        // SYG-PERS-021: cikis tarihi son calisma gunudur; ust sinir haric oldugu icin kayit ertesi gun kapanir.
        var open = Open(Today.AddDays(-30), Merkez);
        var termination = Today.AddDays(-3);

        var (added, changed) = AssignmentTimeline.Apply(Terminated(termination), [open], Merkez, Today);

        changed.ShouldBeTrue();
        added.ShouldBeNull();
        open.ValidTo.ShouldBe(termination.AddDays(1));
        open.Covers(termination).ShouldBeTrue();
        open.Covers(termination.AddDays(1)).ShouldBeFalse();
    }

    [Fact]
    public void Backdated_termination_before_the_record_start_never_produces_an_empty_range()
    {
        var open = Open(Today.AddDays(-2), Merkez);

        AssignmentTimeline.Apply(Terminated(Today.AddDays(-10)), [open], Merkez, Today);

        open.ValidTo.ShouldBe(open.ValidFrom.AddDays(1));
    }

    [Fact]
    public void First_sight_of_a_terminated_employment_records_the_values_on_the_last_working_day()
    {
        // LOGO karti cikistan sonra donar: karttaki deger cikis gunundeki deger sayilir.
        var termination = new DateOnly(2024, 6, 30);

        var (added, _) = AssignmentTimeline.Apply(Terminated(termination), [], Merkez, Today);

        added!.ValidFrom.ShouldBe(termination);
        added.ValidTo.ShouldBe(termination.AddDays(1));
    }

    [Fact]
    public void Later_logo_edit_on_a_terminated_employment_updates_the_last_record_in_place()
    {
        var termination = Today.AddDays(-40);
        var closed = Open(Today.AddDays(-100), Merkez);
        AssignmentTimeline.Apply(Terminated(termination), [closed], Merkez, Today);

        var (added, changed) = AssignmentTimeline.Apply(Terminated(termination), [closed], Sube2, Today);

        changed.ShouldBeTrue();
        added.ShouldBeNull();
        closed.BranchId.ShouldBe(11);
        closed.ValidTo.ShouldBe(termination.AddDays(1));
    }

    [Fact]
    public void Rehire_opens_a_new_record_from_today()
    {
        var closed = Open(Today.AddDays(-100), Merkez);
        AssignmentTimeline.Apply(Terminated(Today.AddDays(-40)), [closed], Merkez, Today);

        var (added, _) = AssignmentTimeline.Apply(Active(), [closed], Merkez, Today);

        added!.ValidFrom.ShouldBe(Today);
        added.ValidTo.ShouldBeNull();
    }

    private static EmploymentAssignment Open(DateOnly from, AssignmentValues values)
    {
        var (added, _) = AssignmentTimeline.Apply(Active(), [], values, from);
        return added!;
    }

    private static Employment Active() => Employ(null);

    private static Employment Terminated(DateOnly termination) => Employ(termination);

    private static Employment Employ(DateOnly? termination)
    {
        var company = Company.FromLogo(1, "Duzen Laboratuvarlar");
        var person = Person.Create(TestNationalIds.Sequential(1), new PersonDetails("Ayse", "Yilmaz", new DateOnly(1990, 1, 1), null, false, null));
        var employment = Employment.Create(person, company, "00001", new EmploymentDetails(new DateOnly(2020, 1, 1), termination, 1), Today);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(employment, 7L);
        return employment;
    }
}
