using Dsg.Hrms.Domain.Notifications;

namespace Dsg.Hrms.Domain.Tests.Notifications;

/// <summary>Ileti amaclarinin siniflandirilmasi (SYG-KMLK-062, KR-056).</summary>
public sealed class NotificationPurposeRulesTests
{
    [Fact]
    public void Every_purpose_is_classified()
    {
        // Yeni bir amac eklenip siniflandirilmazsa bu test duser.
        foreach (var purpose in Enum.GetValues<NotificationPurpose>())
        {
            Should.NotThrow(() => purpose.IsTransactional());
        }
    }

    [Theory]
    [InlineData(NotificationPurpose.RegistrationCode)]
    [InlineData(NotificationPurpose.PasswordResetCode)]
    [InlineData(NotificationPurpose.TwoFactorCode)]
    [InlineData(NotificationPurpose.Invitation)]
    public void Identity_messages_are_transactional(NotificationPurpose purpose)
    {
        purpose.IsTransactional().ShouldBeTrue();
    }
}
