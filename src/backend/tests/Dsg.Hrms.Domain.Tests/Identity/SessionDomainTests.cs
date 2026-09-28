using Dsg.Hrms.Domain.Identity;

namespace Dsg.Hrms.Domain.Tests.Identity;

/// <summary>Oturum, giris sayaci ve bekleyen giris kurallari (SYG-KMLK-033, 037…041).</summary>
public sealed class SessionDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 9, 0, 0, TimeSpan.Zero);
    private static readonly TimeSpan Idle = TimeSpan.FromMinutes(30);
    private static readonly Guid Stamp = Guid.NewGuid();

    private static UserSession NewSession() => UserSession.Start(1, Stamp, "10.0.0.1", Now, TimeSpan.FromHours(8));

    // ------------------------------------------------------------------ oturum

    [Fact]
    public void Open_session_is_valid_until_idle_or_absolute_limit()
    {
        var session = NewSession();

        session.Check(Stamp, accountActive: true, Now.AddMinutes(29), Idle).ShouldBeNull();
        session.Check(Stamp, accountActive: true, Now.AddMinutes(30), Idle).ShouldBe(SessionEndReason.IdleTimeout);
        session.IsOpen.ShouldBeFalse();
    }

    [Fact]
    public void Activity_resets_idle_but_never_extends_the_absolute_limit()
    {
        var session = NewSession();
        for (var minute = 25; minute < 480; minute += 25)
        {
            session.RecordActivity(Now.AddMinutes(minute)).ShouldBeTrue();
            session.Check(Stamp, true, Now.AddMinutes(minute), Idle).ShouldBeNull();
        }

        session.RecordActivity(Now.AddMinutes(479)).ShouldBeTrue();
        session.Check(Stamp, true, Now.AddHours(8), Idle).ShouldBe(SessionEndReason.Expired);
    }

    [Fact]
    public void Activity_is_accepted_twice_a_minute()
    {
        var session = NewSession();

        session.RecordActivity(Now.AddSeconds(10)).ShouldBeTrue();
        session.RecordActivity(Now.AddSeconds(20)).ShouldBeTrue();
        session.RecordActivity(Now.AddSeconds(30)).ShouldBeFalse();
        session.RecordActivity(Now.AddSeconds(71)).ShouldBeTrue();
    }

    [Fact]
    public void Changed_stamp_or_inactive_account_ends_the_session()
    {
        NewSession().Check(Guid.NewGuid(), true, Now, Idle).ShouldBe(SessionEndReason.AccountChanged);
        NewSession().Check(Stamp, false, Now, Idle).ShouldBe(SessionEndReason.AccountChanged);
    }

    [Fact]
    public void Ended_session_keeps_its_first_reason_and_accepts_no_activity()
    {
        var session = NewSession();
        session.End(SessionEndReason.SignedInElsewhere, Now);
        session.End(SessionEndReason.LoggedOut, Now.AddMinutes(1));

        session.EndReason.ShouldBe(SessionEndReason.SignedInElsewhere);
        session.Check(Stamp, true, Now, Idle).ShouldBe(SessionEndReason.SignedInElsewhere);
        session.RecordActivity(Now).ShouldBeFalse();
    }

    [Fact]
    public void Session_requires_an_account()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => UserSession.Start(0, Stamp, null, Now, TimeSpan.FromHours(8)));
    }

    // ------------------------------------------------------------------ sayac (SYG-KMLK-033)

    [Fact]
    public void Fifth_failure_locks_and_expired_lock_resets_the_counter()
    {
        var throttle = LoginThrottle.For("ozet", Now);

        for (var i = 0; i < 4; i++)
        {
            throttle.RegisterFailure(5, TimeSpan.FromMinutes(15), Now).ShouldBeNull();
        }

        throttle.RegisterFailure(5, TimeSpan.FromMinutes(15), Now).ShouldBe(Now.AddMinutes(15));
        throttle.IsLocked(Now.AddMinutes(14)).ShouldBeTrue();
        throttle.IsLocked(Now.AddMinutes(15)).ShouldBeFalse();
        throttle.FailedCount.ShouldBe(0);
    }

    [Fact]
    public void Success_resets_the_counter()
    {
        var throttle = LoginThrottle.For("ozet", Now);
        throttle.RegisterFailure(5, TimeSpan.FromMinutes(15), Now);

        throttle.Reset(Now);

        throttle.FailedCount.ShouldBe(0);
        throttle.IsLocked(Now).ShouldBeFalse();
    }

    [Fact]
    public void Account_lockout_record_can_be_cleared_once()
    {
        var account = UserAccount.Create(1);
        account.ClearLockout().ShouldBeFalse();

        account.RecordLockout(Now.AddMinutes(15));
        account.LockedUntil.ShouldBe(Now.AddMinutes(15));

        account.ClearLockout().ShouldBeTrue();
        account.LockedUntil.ShouldBeNull();
    }

    // ------------------------------------------------------------------ bekleyen giris

    [Fact]
    public void Challenge_is_usable_until_completed_or_expired()
    {
        var challenge = LoginChallenge.Start(1, RegistrationChannels.Email | RegistrationChannels.Sms, null, Now);

        challenge.IsUsable(Now.AddMinutes(9)).ShouldBeTrue();
        challenge.IsUsable(Now + LoginChallenge.Lifetime).ShouldBeFalse();

        challenge.Complete();
        challenge.IsUsable(Now).ShouldBeFalse();
    }

    [Fact]
    public void Challenge_requires_an_account_and_a_channel()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => LoginChallenge.Start(0, RegistrationChannels.Email, null, Now));
        Should.Throw<ArgumentException>(() => LoginChallenge.Start(1, RegistrationChannels.None, null, Now));
    }

    [Fact]
    public void Refresh_token_requires_a_session_and_a_hash()
    {
        RefreshToken.Issue(1, "ozet", Now).UsedAt.ShouldBeNull();
        Should.Throw<ArgumentOutOfRangeException>(() => RefreshToken.Issue(0, "ozet", Now));
        Should.Throw<ArgumentException>(() => RefreshToken.Issue(1, " ", Now));
    }
}
