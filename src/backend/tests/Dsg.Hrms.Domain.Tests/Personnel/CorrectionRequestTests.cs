using Dsg.Hrms.Domain.Notifications;
using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Domain.Tests.Personnel;

/// <summary>"IK'ya bildir" talebi ve degerin dogrulanmasi (SYG-PERS-045, 048).</summary>
public sealed class CorrectionRequestTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 9, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("0532 123 45 67", "5321234567")]
    [InlineData("+90 (532) 123-45-67", "5321234567")]
    [InlineData("5321234567", "5321234567")]
    public void Phone_is_normalized_with_the_identity_rule(string input, string expected)
    {
        CorrectionValue.Normalize(CorrectionField.MobilePhone, input).ShouldBe((expected, null));
    }

    [Theory]
    [InlineData("2121234567")]
    [InlineData("532123456")]
    [InlineData("telefon")]
    public void Non_mobile_numbers_are_rejected(string input)
    {
        CorrectionValue.Normalize(CorrectionField.MobilePhone, input).Error.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(" Ayse.Demir@Duzen.com.tr ", "ayse.demir@duzen.com.tr")]
    [InlineData("kisi@ornek.org", "kisi@ornek.org")]
    public void Email_is_normalized(string input, string expected)
    {
        CorrectionValue.Normalize(CorrectionField.Email, input).ShouldBe((expected, null));
    }

    [Theory]
    [InlineData("ayse")]
    [InlineData("ayse@duzen")]
    [InlineData("a@b@duzen.com.tr")]
    [InlineData("ayse@.duzen.com")]
    public void Malformed_email_is_rejected(string input)
    {
        CorrectionValue.Normalize(CorrectionField.Email, input).Error.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(CorrectionField.Address)]
    [InlineData(CorrectionField.Education)]
    public void Text_fields_are_limited_to_500_characters(CorrectionField field)
    {
        CorrectionValue.Normalize(field, new string('a', 500)).Error.ShouldBeNull();
        CorrectionValue.Normalize(field, new string('a', 501)).Error.ShouldNotBeNull();
    }

    [Fact]
    public void Marital_status_accepts_only_the_two_options()
    {
        CorrectionValue.Normalize(CorrectionField.MaritalStatus, "married").ShouldBe(("married", null));
        CorrectionValue.Normalize(CorrectionField.MaritalStatus, "notMarried").ShouldBe(("notMarried", null));
        CorrectionValue.Normalize(CorrectionField.MaritalStatus, "bosanmis").Error.ShouldNotBeNull();
    }

    [Theory]
    [InlineData("1", true)]
    [InlineData("8", true)]
    [InlineData("0", false)]
    [InlineData("9", false)]
    [InlineData("A Rh+", false)]
    [InlineData("+3", false)]
    public void Blood_group_is_one_of_the_eight_codes(string input, bool valid)
    {
        (CorrectionValue.Normalize(CorrectionField.BloodGroup, input).Error is null).ShouldBe(valid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Empty_value_is_rejected(string? input)
    {
        CorrectionValue.Normalize(CorrectionField.Address, input).Error.ShouldNotBeNull();
    }

    [Fact]
    public void Completing_removes_the_proposed_value()
    {
        // KR-126: deger yalnizca IK'nin LOGO'yu duzeltmesi icin tutulur.
        var request = CorrectionRequest.Submit(1, CorrectionField.Address, "Yeni Mah. 3. Sok. No: 4", Now);

        request.Complete(completedBy: 7, Now.AddHours(1));

        request.Status.ShouldBe(CorrectionStatus.Completed);
        request.ProposedValue.ShouldBeNull();
        request.CompletedAt.ShouldBe(Now.AddHours(1));
        request.CompletedBy.ShouldBe(7);
    }

    [Fact]
    public void A_completed_request_cannot_be_completed_again()
    {
        var request = CorrectionRequest.Submit(1, CorrectionField.Education, "Lisans", Now);
        request.Complete(7, Now);

        Should.Throw<InvalidOperationException>(() => request.Complete(8, Now.AddHours(1)));
        request.CompletedBy.ShouldBe(7);
    }

    [Fact]
    public void Protecting_replaces_the_stored_value()
    {
        var request = CorrectionRequest.Submit(1, CorrectionField.BloodGroup, "3", Now);

        request.ProtectValue(code => $"k1:nonce:{code}-sifreli");

        request.ProposedValue.ShouldBe("k1:nonce:3-sifreli");
    }

    [Fact]
    public void Inbox_item_keeps_the_first_read_time()
    {
        var item = InboxItem.For(5, InboxItemKind.CorrectionRequested, Guid.CreateVersion7(), Now);

        item.MarkRead(Now.AddMinutes(1));
        item.MarkRead(Now.AddMinutes(5));

        item.ReadAt.ShouldBe(Now.AddMinutes(1));
    }
}
