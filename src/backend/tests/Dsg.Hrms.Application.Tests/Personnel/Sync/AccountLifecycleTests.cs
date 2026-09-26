using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Application.Tests.Settings;
using Dsg.Hrms.Domain.Common;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel.Sync;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using static Dsg.Hrms.Application.Tests.Personnel.Sync.TestData;

namespace Dsg.Hrms.Application.Tests.Personnel.Sync;

/// <summary>
/// Senkronizasyonda hesap yasam dongusu (SYG-KMLK-054, 056; <c>KR-015</c>).
/// </summary>
public sealed class AccountLifecycleTests
{
    private readonly PersonnelSynchronizer _synchronizer = new(["0001000"]);

    /// <summary>
    /// Ilk senkronizasyonu calistirir, kisilere veritabani kimligi verir ve istenen
    /// kisiler icin hesap olusturur: veritabanindan yuklenmis bir anlik goruntuyu taklit eder.
    /// </summary>
    private PersonnelSnapshot Seed(LogoPersonnelRecord[] cards, params string[] accountHolders)
    {
        var outcome = _synchronizer.Synchronize(cards, new PersonnelSnapshot([], [], []), Today);

        long id = 0;
        foreach (var person in outcome.NewPersons)
        {
            typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(person, ++id);
        }

        var accounts = outcome.NewPersons
            .Where(p => accountHolders.Contains(p.NationalId))
            .Select(p => UserAccount.Create(p.Id))
            .ToList();

        return new PersonnelSnapshot([.. outcome.NewCompanies], [.. outcome.NewPersons], [.. outcome.NewEmployments])
        {
            Accounts = accounts,
        };
    }

    private static UserAccount AccountOf(PersonnelSnapshot snapshot, string nationalId) =>
        snapshot.Accounts.Single(a => a.PersonId == snapshot.Persons.Single(p => p.NationalId == nationalId).Id);

    [Fact]
    public void Account_is_deactivated_when_the_only_employment_ends()
    {
        var snapshot = Seed([Card("00001", NationalId(1))], NationalId(1));

        var outcome = _synchronizer.Synchronize(
            [Card("00001", NationalId(1), terminationDate: Today.AddDays(-1))], snapshot, Today);

        AccountOf(snapshot, NationalId(1)).Status.ShouldBe(AccountStatus.Passive);
        AccountOf(snapshot, NationalId(1)).StatusReason.ShouldBe(AccountStatusReason.EmploymentEnded);
        outcome.Counts.AccountsDeactivated.ShouldBe(1);
        outcome.Counts.AccountsReactivated.ShouldBe(0);
    }

    [Fact]
    public void Account_stays_active_on_the_last_working_day()
    {
        // Cikis tarihi son calisma gunudur; o gun hesap acik kalir.
        var snapshot = Seed([Card("00001", NationalId(1))], NationalId(1));

        _synchronizer.Synchronize([Card("00001", NationalId(1), terminationDate: Today)], snapshot, Today);

        AccountOf(snapshot, NationalId(1)).Status.ShouldBe(AccountStatus.Active);
    }

    [Fact]
    public void Account_stays_active_while_another_employment_is_active()
    {
        // AN-09: hesap ancak TUM istihdamlar bittiginde pasiflesir.
        var cards = new[] { Card("00001", NationalId(1), logoRef: 1), Card("00002", NationalId(1), logoRef: 2) };
        var snapshot = Seed(cards, NationalId(1));

        var outcome = _synchronizer.Synchronize(
            [Card("00001", NationalId(1), logoRef: 1, terminationDate: Today.AddDays(-1)), Card("00002", NationalId(1), logoRef: 2)],
            snapshot, Today);

        AccountOf(snapshot, NationalId(1)).Status.ShouldBe(AccountStatus.Active);
        outcome.Counts.AccountsDeactivated.ShouldBe(0);
    }

    [Fact]
    public void Account_is_reactivated_by_a_new_employment_without_creating_another_account()
    {
        // SYG-KMLK-056: ayni kisi yeniden ise girdiginde MEVCUT hesap aktiflesir.
        var snapshot = Seed([Card("00001", NationalId(1), terminationDate: Today.AddDays(-30))], NationalId(1));
        _synchronizer.Synchronize([Card("00001", NationalId(1), terminationDate: Today.AddDays(-30))], snapshot, Today);
        var account = AccountOf(snapshot, NationalId(1));
        account.Status.ShouldBe(AccountStatus.Passive);

        var outcome = _synchronizer.Synchronize(
            [
                Card("00001", NationalId(1), logoRef: 1, terminationDate: Today.AddDays(-30)),
                Card("00009", NationalId(1), logoRef: 9, hireDate: Today),
            ],
            snapshot, Today);

        account.Status.ShouldBe(AccountStatus.Active);
        account.StatusReason.ShouldBe(AccountStatusReason.NewEmployment);
        outcome.Counts.AccountsReactivated.ShouldBe(1);
        outcome.NewEmployments.Single().RegistryCode.ShouldBe("00009");
    }

    [Fact]
    public void Manually_deactivated_account_is_not_reactivated_by_sync()
    {
        var snapshot = Seed([Card("00001", NationalId(1))], NationalId(1));
        var account = AccountOf(snapshot, NationalId(1));
        account.DeactivateManually("Disiplin sureci");

        var outcome = _synchronizer.Synchronize([Card("00001", NationalId(1))], snapshot, Today);

        account.Status.ShouldBe(AccountStatus.Passive);
        account.StatusReason.ShouldBe(AccountStatusReason.Manual);
        outcome.Counts.AccountsReactivated.ShouldBe(0);
    }

    [Fact]
    public void Manually_deactivated_account_of_a_rehired_person_is_reactivated()
    {
        // REQ-KMLK-035: elle pasife alinmis kisi ayrilir ve yeniden ise girerse mevcut
        // hesap aktiflesir. Ayrilma, sayacta "pasiflesme" sayilmaz: hesap zaten pasifti.
        var snapshot = Seed([Card("00001", NationalId(1))], NationalId(1));
        var account = AccountOf(snapshot, NationalId(1));
        account.DeactivateManually("Disiplin sureci");

        var departure = _synchronizer.Synchronize(
            [Card("00001", NationalId(1), terminationDate: Today.AddDays(-1))], snapshot, Today);
        account.StatusReason.ShouldBe(AccountStatusReason.EmploymentEnded);
        departure.Counts.AccountsDeactivated.ShouldBe(0);

        var rehire = _synchronizer.Synchronize(
            [
                Card("00001", NationalId(1), logoRef: 1, terminationDate: Today.AddDays(-1)),
                Card("00007", NationalId(1), logoRef: 7, hireDate: Today),
            ],
            snapshot, Today);

        account.Status.ShouldBe(AccountStatus.Active);
        rehire.Counts.AccountsReactivated.ShouldBe(1);
    }

    [Fact]
    public void Automatic_deactivation_can_be_turned_off()
    {
        // PRM-HSP-02 kapaliysa istihdami biten hesap pasiflesmez.
        var snapshot = Seed([Card("00001", NationalId(1))], NationalId(1));

        var outcome = _synchronizer.Synchronize(
            [Card("00001", NationalId(1), terminationDate: Today.AddDays(-1))], snapshot, Today, autoDeactivateAccounts: false);

        AccountOf(snapshot, NationalId(1)).Status.ShouldBe(AccountStatus.Active);
        outcome.Counts.AccountsDeactivated.ShouldBe(0);
    }

    [Fact]
    public void Repeated_sync_does_not_count_the_same_account_twice()
    {
        var snapshot = Seed([Card("00001", NationalId(1))], NationalId(1));
        var ended = new[] { Card("00001", NationalId(1), terminationDate: Today.AddDays(-1)) };

        _synchronizer.Synchronize(ended, snapshot, Today).Counts.AccountsDeactivated.ShouldBe(1);
        _synchronizer.Synchronize(ended, snapshot, Today).Counts.AccountsDeactivated.ShouldBe(0);
    }

    [Fact]
    public void Employment_missing_from_source_keeps_the_account_as_it_was()
    {
        // Karti LOGO'dan silinen istihdam degistirilmez (ADR-0003); hesap da oldugu
        // gibi kalir. Silinmenin anlamina IK karar verir.
        var snapshot = Seed([Card("00001", NationalId(1)), Card("00002", NationalId(2), logoRef: 2)], NationalId(1));

        var outcome = _synchronizer.Synchronize([Card("00002", NationalId(2), logoRef: 2)], snapshot, Today);

        AccountOf(snapshot, NationalId(1)).Status.ShouldBe(AccountStatus.Active);
        outcome.Counts.AccountsDeactivated.ShouldBe(0);
    }

    [Fact]
    public void Persons_without_accounts_are_unaffected()
    {
        var snapshot = Seed([Card("00001", NationalId(1)), Card("00002", NationalId(2), logoRef: 2)], NationalId(2));

        var outcome = _synchronizer.Synchronize(
            [Card("00001", NationalId(1), terminationDate: Today.AddDays(-1)), Card("00002", NationalId(2), logoRef: 2)],
            snapshot, Today);

        snapshot.Accounts.Single().Status.ShouldBe(AccountStatus.Active);
        outcome.Counts.AccountsDeactivated.ShouldBe(0);
    }

    [Fact]
    public void Account_follows_an_employment_moved_to_another_person()
    {
        // LOGO'da TCKN duzeltildiginde istihdam dogru kisiye tasinir. Eski kisinin baska
        // istihdami yoksa hesabi pasiflesir; aktiflik istihdamin GUNCEL kisisine gore
        // hesaplanir, kayitta kalan eski kimlige gore degil.
        var snapshot = Seed([Card("00001", NationalId(1))], NationalId(1));

        _synchronizer.Synchronize([Card("00001", NationalId(2))], snapshot, Today);

        AccountOf(snapshot, NationalId(1)).Status.ShouldBe(AccountStatus.Passive);
    }

    [Fact]
    public void Account_of_an_unknown_person_is_ignored()
    {
        var snapshot = Seed([Card("00001", NationalId(1))]) with { Accounts = [UserAccount.Create(999)] };

        var outcome = _synchronizer.Synchronize([Card("00001", NationalId(1), terminationDate: Today.AddDays(-1))], snapshot, Today);

        snapshot.Accounts.Single().Status.ShouldBe(AccountStatus.Active);
        outcome.Counts.AccountsDeactivated.ShouldBe(0);
    }

    [Theory]
    [InlineData("true", AccountStatus.Passive)]
    [InlineData("false", AccountStatus.Active)]
    public async Task Service_applies_the_auto_deactivation_parameter(string parameterValue, AccountStatus expected)
    {
        // PRM-HSP-02 parametre deposundan okunur; kod degisikligi olmadan kapatilabilir.
        var snapshot = Seed([Card("00001", NationalId(1))], NationalId(1));

        var session = Substitute.For<IPersonnelSyncSession>();
        session.LoadAsync(Arg.Any<CancellationToken>()).Returns(snapshot);
        var store = Substitute.For<IPersonnelSyncStore>();
        store.TryOpenSessionAsync(Arg.Any<CancellationToken>()).Returns(session);
        var source = Substitute.For<ILogoPersonnelSource>();
        source.VerifyReadOnlyAccessAsync(Arg.Any<CancellationToken>()).Returns(new LogoAccessCheckResult(true, []));
        source.VerifySchemaAsync(Arg.Any<CancellationToken>()).Returns(new LogoSchemaCheckResult(true, []));
        source.GetAllAsync(Arg.Any<CancellationToken>()).Returns([Card("00001", NationalId(1), terminationDate: Today.AddDays(-1))]);
        var clock = Substitute.For<IDateTimeProvider>();
        clock.Today.Returns(Today);
        var parameters = new FakeSystemParameters().With(ParameterCatalog.AutoDeactivateOnEmploymentEnd, parameterValue);

        var result = await new PersonnelSyncService(source, store, clock, parameters, new PersonnelSyncOptions(), NullLogger<PersonnelSyncService>.Instance)
            .RunAsync(SyncTrigger.Scheduled, CancellationToken.None);

        result.Status.ShouldBe(SyncStatus.Succeeded);
        snapshot.Accounts.Single().Status.ShouldBe(expected);
    }
}
