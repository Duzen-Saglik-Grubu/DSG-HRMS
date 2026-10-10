using Dsg.Hrms.Domain.Identity;

namespace Dsg.Hrms.Domain.Tests.Identity;

/// <summary>
/// Hesap yasam dongusu ve zorunlu parola degisimi kurallari (SYG-KMLK-046, 050, 054, 056, 057).
/// </summary>
public sealed class UserAccountTests
{
    [Fact]
    public void New_account_is_active()
    {
        var account = UserAccount.Create(personId: 7);

        account.PersonId.ShouldBe(7);
        account.Status.ShouldBe(AccountStatus.Active);
        account.StatusReason.ShouldBeNull();
        account.SecurityStamp.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Account_requires_a_person()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => UserAccount.Create(0));
    }

    [Fact]
    public void Employment_end_deactivates_and_rotates_the_security_stamp()
    {
        // SYG-KMLK-054: acik oturumlar ve yenileme jetonlari damgaya bagli oldugu icin
        // damga degisince hepsi gecersizlesir.
        var account = UserAccount.Create(1);
        var stamp = account.SecurityStamp;

        account.DeactivateForEmploymentEnd().ShouldBeTrue();

        account.Status.ShouldBe(AccountStatus.Passive);
        account.StatusReason.ShouldBe(AccountStatusReason.EmploymentEnded);
        account.SecurityStamp.ShouldNotBe(stamp);
    }

    [Fact]
    public void Deactivating_a_passive_account_changes_nothing()
    {
        // Her senkronizasyon (15 dk) ayni hesabi yeniden pasiflestirip denetim izine
        // satir eklememelidir.
        var account = UserAccount.Create(1);
        account.DeactivateForEmploymentEnd();
        var stamp = account.SecurityStamp;

        account.DeactivateForEmploymentEnd().ShouldBeFalse();
        account.SecurityStamp.ShouldBe(stamp);
    }

    [Fact]
    public void New_employment_reactivates_an_account_deactivated_by_employment_end()
    {
        var account = UserAccount.Create(1);
        account.DeactivateForEmploymentEnd();

        account.ReactivateForNewEmployment().ShouldBeTrue();

        account.Status.ShouldBe(AccountStatus.Active);
        account.StatusReason.ShouldBe(AccountStatusReason.NewEmployment);
    }

    [Fact]
    public void New_employment_does_not_reactivate_a_manually_deactivated_account()
    {
        // IK'nin gerekceli karari periyodik bir surecle sessizce geri alinmamalidir (#83).
        var account = UserAccount.Create(1);
        account.DeactivateManually("Disiplin sureci");

        account.ReactivateForNewEmployment().ShouldBeFalse();

        account.Status.ShouldBe(AccountStatus.Passive);
        account.StatusReason.ShouldBe(AccountStatusReason.Manual);
    }

    [Fact]
    public void Employment_end_supersedes_a_manual_deactivation()
    {
        // Kisi ayrildiginda elle verilen karar yerini ayriliga birakir; yeniden ise
        // girdiginde hesap REQ-KMLK-035 geregi aktiflesir.
        var account = UserAccount.Create(1);
        account.DeactivateManually("Disiplin sureci");
        var stamp = account.SecurityStamp;

        account.DeactivateForEmploymentEnd().ShouldBeTrue();

        account.Status.ShouldBe(AccountStatus.Passive);
        account.StatusReason.ShouldBe(AccountStatusReason.EmploymentEnded);
        account.StatusNote.ShouldBeNull();
        account.SecurityStamp.ShouldBe(stamp); // zaten pasifti; oturumlar pasiflesirken kapandi

        account.ReactivateForNewEmployment().ShouldBeTrue();
        account.Status.ShouldBe(AccountStatus.Active);
    }

    [Fact]
    public void Reactivating_an_active_account_changes_nothing()
    {
        UserAccount.Create(1).ReactivateForNewEmployment().ShouldBeFalse();
    }

    [Fact]
    public void Manual_deactivation_records_the_reason()
    {
        var account = UserAccount.Create(1);
        var stamp = account.SecurityStamp;

        account.DeactivateManually("  Uzun sureli izin  ");

        account.Status.ShouldBe(AccountStatus.Passive);
        account.StatusReason.ShouldBe(AccountStatusReason.Manual);
        account.StatusNote.ShouldBe("Uzun sureli izin");
        account.SecurityStamp.ShouldNotBe(stamp);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Manual_changes_require_a_reason(string note)
    {
        // SYG-KMLK-057: gerekce girilmeden islem tamamlanmaz.
        var active = UserAccount.Create(1);
        Should.Throw<ArgumentException>(() => active.DeactivateManually(note));
        active.Status.ShouldBe(AccountStatus.Active);

        var passive = UserAccount.Create(2);
        passive.DeactivateForEmploymentEnd();
        Should.Throw<ArgumentException>(() => passive.ActivateManually(note, hasActiveEmployment: true));
        passive.Status.ShouldBe(AccountStatus.Passive);
    }

    [Fact]
    public void Manual_deactivation_of_a_passive_account_is_rejected()
    {
        var account = UserAccount.Create(1);
        account.DeactivateForEmploymentEnd();

        Should.Throw<InvalidOperationException>(() => account.DeactivateManually("Gerekce"));
    }

    [Fact]
    public void Manual_activation_records_the_reason()
    {
        var account = UserAccount.Create(1);
        account.DeactivateManually("Disiplin sureci");

        account.ActivateManually("Surec tamamlandi", hasActiveEmployment: true);

        account.Status.ShouldBe(AccountStatus.Active);
        account.StatusReason.ShouldBe(AccountStatusReason.Manual);
        account.StatusNote.ShouldBe("Surec tamamlandi");
    }

    [Fact]
    public void Manual_activation_without_active_employment_is_rejected()
    {
        // KR-015: ayrilmis kisinin hesabi elle de acilamaz.
        var account = UserAccount.Create(1);
        account.DeactivateForEmploymentEnd();

        Should.Throw<InvalidOperationException>(() => account.ActivateManually("Gerekce", hasActiveEmployment: false));
        account.Status.ShouldBe(AccountStatus.Passive);
    }

    [Fact]
    public void Manual_activation_of_an_active_account_is_rejected()
    {
        Should.Throw<InvalidOperationException>(() => UserAccount.Create(1).ActivateManually("Gerekce", hasActiveEmployment: true));
    }

    [Fact]
    public void Automatic_reactivation_clears_the_manual_note()
    {
        // Elle aktiflestirilip sonra istihdami biten ve yeniden ise giren kisinin
        // hesabinda eski gerekce kalmamalidir.
        var account = UserAccount.Create(1);
        account.DeactivateManually("Disiplin sureci");
        account.ActivateManually("Surec tamamlandi", hasActiveEmployment: true);
        account.DeactivateForEmploymentEnd();
        account.StatusNote.ShouldBeNull();

        account.ReactivateForNewEmployment().ShouldBeTrue();
        account.StatusNote.ShouldBeNull();
    }

    // ------------------------------------------------------------------ zorunlu parola degisimi (SYG-KMLK-046, 050)

    private static readonly DateTimeOffset Registered = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void First_sign_in_with_the_rule_requires_a_change_until_the_password_changes()
    {
        var account = UserAccount.Register(1, "hash", Registered);

        account.RecordSignIn(Registered.AddMinutes(5), requirePasswordChange: true);
        account.FirstSignedInAt.ShouldBe(Registered.AddMinutes(5));
        account.RequiredPasswordChange(true, null, Registered.AddMinutes(5)).ShouldBe(PasswordChangeReason.FirstSignIn);

        // Sonraki giris ilk giris degildir; degistirilmeyen parola zorunlulugu surdurur.
        account.RecordSignIn(Registered.AddDays(1), requirePasswordChange: true);
        account.FirstSignedInAt.ShouldBe(Registered.AddMinutes(5));
        account.RequiredPasswordChange(true, null, Registered.AddDays(1)).ShouldBe(PasswordChangeReason.FirstSignIn);

        account.SetPassword("new-hash", Registered.AddDays(1));
        account.FirstPasswordChangePending.ShouldBeFalse();
        account.RequiredPasswordChange(true, null, Registered.AddDays(1)).ShouldBeNull();
    }

    [Fact]
    public void First_sign_in_without_the_rule_is_recorded_and_never_requires_a_change_later()
    {
        var account = UserAccount.Register(1, "hash", Registered);

        account.RecordSignIn(Registered, requirePasswordChange: false);
        account.RecordSignIn(Registered.AddDays(1), requirePasswordChange: true);

        account.RequiredPasswordChange(true, null, Registered.AddDays(1)).ShouldBeNull();
    }

    [Fact]
    public void Disabled_rule_ignores_a_pending_first_sign_in_change()
    {
        var account = UserAccount.Register(1, "hash", Registered);
        account.RecordSignIn(Registered, requirePasswordChange: true);

        account.RequiredPasswordChange(false, null, Registered).ShouldBeNull();
    }

    [Fact]
    public void Password_expires_when_it_reaches_the_maximum_age()
    {
        var account = UserAccount.Register(1, "hash", Registered);
        var maxAge = TimeSpan.FromDays(90);

        account.RequiredPasswordChange(false, maxAge, Registered.AddDays(90).AddTicks(-1)).ShouldBeNull();
        account.RequiredPasswordChange(false, maxAge, Registered.AddDays(90)).ShouldBe(PasswordChangeReason.Expired);
        account.RequiredPasswordChange(false, null, Registered.AddDays(400)).ShouldBeNull();
    }

    [Fact]
    public void Password_with_an_unknown_age_counts_as_expired()
    {
        UserAccount.Create(1).RequiredPasswordChange(false, TimeSpan.FromDays(90), Registered).ShouldBe(PasswordChangeReason.Expired);
    }

    [Fact]
    public void First_sign_in_reason_takes_precedence_over_expiry()
    {
        var account = UserAccount.Register(1, "hash", Registered);
        account.RecordSignIn(Registered.AddDays(100), requirePasswordChange: true);

        account.RequiredPasswordChange(true, TimeSpan.FromDays(90), Registered.AddDays(100)).ShouldBe(PasswordChangeReason.FirstSignIn);
    }

    [Fact]
    public void Two_factor_preference_is_off_by_default_and_toggles_without_rotating_the_stamp()
    {
        // SYG-KMLK-080: tercih varsayilan kapali; degisince acik oturumlar kapanmaz.
        var account = UserAccount.Register(1, "hash", Registered);
        var stamp = account.SecurityStamp;
        account.TwoFactorEnabled.ShouldBeFalse();

        account.EnableTwoFactor().ShouldBeTrue();
        account.TwoFactorEnabled.ShouldBeTrue();
        account.EnableTwoFactor().ShouldBeFalse();

        account.DisableTwoFactor().ShouldBeTrue();
        account.TwoFactorEnabled.ShouldBeFalse();
        account.DisableTwoFactor().ShouldBeFalse();

        account.SecurityStamp.ShouldBe(stamp);
    }

    [Fact]
    public void Hr_reset_turns_two_factor_off_with_the_reason_and_re_enabling_clears_it()
    {
        // SYG-KMLK-081: kodu alamayan kisi icin IK gerekceyle kapatir; kisi yeniden acarsa
        // eski gerekce gecerli durumu anlatmaz.
        var account = UserAccount.Register(1, "hash", Registered);
        account.EnableTwoFactor();
        var stamp = account.SecurityStamp;

        account.ResetTwoFactor("  Telefonunu kaybetti  ");

        account.TwoFactorEnabled.ShouldBeFalse();
        account.TwoFactorResetNote.ShouldBe("Telefonunu kaybetti");
        account.SecurityStamp.ShouldBe(stamp);

        account.EnableTwoFactor().ShouldBeTrue();
        account.TwoFactorResetNote.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Hr_reset_requires_a_reason(string reason)
    {
        var account = UserAccount.Register(1, "hash", Registered);
        account.EnableTwoFactor();

        Should.Throw<ArgumentException>(() => account.ResetTwoFactor(reason));

        account.TwoFactorEnabled.ShouldBeTrue();
        account.TwoFactorResetNote.ShouldBeNull();
    }

    [Fact]
    public void Hr_reset_of_a_disabled_preference_is_rejected()
    {
        var account = UserAccount.Register(1, "hash", Registered);

        Should.Throw<InvalidOperationException>(() => account.ResetTwoFactor("Gerekce"));
        account.TwoFactorResetNote.ShouldBeNull();
    }
}
