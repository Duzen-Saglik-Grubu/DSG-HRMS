using Dsg.Hrms.Application.Identity.Verification;
using Dsg.Hrms.Domain.Identity;

namespace Dsg.Hrms.Application.Tests.Identity;

/// <summary>Kod iletilerinin metin kurallari (SYG-KMLK-030, <c>analiz/02</c> §5).</summary>
public sealed class VerificationMessagesTests
{
    [Theory]
    [InlineData(VerificationPurpose.Registration)]
    [InlineData(VerificationPurpose.PasswordReset)]
    [InlineData(VerificationPurpose.TwoFactor)]
    public void Sms_fits_one_segment_with_the_longest_parameters(VerificationPurpose purpose)
    {
        // En uzun kod (PRM-KML-09: 8) ve en uzun sure (PRM-KML-10: 30) ile bile tek SMS.
        var sms = VerificationMessages.Sms("12345678", purpose, 30);

        sms.Length.ShouldBeLessThanOrEqualTo(VerificationMessages.SmsLimit);
        sms.ShouldContain("12345678");
        sms.ShouldContain("30 dakika");
        sms.ShouldContain(VerificationMessages.Organization);
        sms.ShouldContain("paylaşmayın");
    }

    [Theory]
    [InlineData(VerificationPurpose.Registration, "üyelik")]
    [InlineData(VerificationPurpose.PasswordReset, "parola sıfırlama")]
    [InlineData(VerificationPurpose.TwoFactor, "giriş")]
    public void Email_names_the_purpose_and_the_organization(VerificationPurpose purpose, string purposeText)
    {
        VerificationMessages.EmailSubject(purpose).ShouldContain(purposeText);

        var body = VerificationMessages.EmailBody("482915", purpose, 5);
        body.ShouldContain("482915");
        body.ShouldContain("5 dakika");
        body.ShouldContain(VerificationMessages.Organization);
        body.ShouldContain("Bilgi İşlem");
    }
}
