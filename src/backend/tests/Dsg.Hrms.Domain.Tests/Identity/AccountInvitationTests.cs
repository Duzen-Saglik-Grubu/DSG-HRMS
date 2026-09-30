using Dsg.Hrms.Domain.Identity;

namespace Dsg.Hrms.Domain.Tests.Identity;

/// <summary>IK davet baglantisi (SYG-KMLK-052, 053).</summary>
public sealed class AccountInvitationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 30, 9, 0, 0, TimeSpan.Zero);
    private const string Hash = "0123456789012345678901234567890123456789abc=";

    [Fact]
    public void Link_is_usable_until_it_expires()
    {
        var invitation = AccountInvitation.Issue(7, Hash, "  Telefonu yok  ", Now, TimeSpan.FromHours(3));

        invitation.Reason.ShouldBe("Telefonu yok");
        invitation.IsUsable(Now.AddHours(3).AddSeconds(-1)).ShouldBeTrue();
        invitation.IsUsable(Now.AddHours(3)).ShouldBeFalse();
    }

    [Fact]
    public void Link_is_single_use()
    {
        var invitation = AccountInvitation.Issue(7, Hash, "Gerekce", Now, TimeSpan.FromHours(3));

        invitation.Use(Now);

        invitation.IsUsable(Now).ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => invitation.Use(Now));
    }

    [Fact]
    public void Revoked_link_cannot_be_used_and_revoking_a_used_link_changes_nothing()
    {
        var revoked = AccountInvitation.Issue(7, Hash, "Gerekce", Now, TimeSpan.FromHours(3));
        revoked.Revoke(Now);
        revoked.IsUsable(Now).ShouldBeFalse();
        revoked.RevokedAt.ShouldBe(Now);

        var used = AccountInvitation.Issue(7, Hash, "Gerekce", Now, TimeSpan.FromHours(3));
        used.Use(Now);
        used.Revoke(Now.AddMinutes(1));
        used.RevokedAt.ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Invitation_requires_a_reason(string reason)
    {
        Should.Throw<ArgumentException>(() => AccountInvitation.Issue(7, Hash, reason, Now, TimeSpan.FromHours(3)));
    }

    [Fact]
    public void Reason_is_limited_and_person_and_hash_are_required()
    {
        Should.Throw<ArgumentException>(() => AccountInvitation.Issue(7, Hash, new string('a', 501), Now, TimeSpan.FromHours(3)));
        Should.Throw<ArgumentOutOfRangeException>(() => AccountInvitation.Issue(0, Hash, "Gerekce", Now, TimeSpan.FromHours(3)));
        Should.Throw<ArgumentException>(() => AccountInvitation.Issue(7, " ", "Gerekce", Now, TimeSpan.FromHours(3)));
    }
}
