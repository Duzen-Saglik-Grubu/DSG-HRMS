using Dsg.Hrms.Domain.Identity;

namespace Dsg.Hrms.Domain.Tests.Identity;

/// <summary>
/// Uyelik denemesi: eslesmeyen denemenin gercek kodla AYNI kurallari izlemesi (<c>KR-016</c>).
/// </summary>
public sealed class RegistrationAttemptTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 9, 0, 0, TimeSpan.Zero);
    private const string Hash = "aGFzaGhhc2hoYXNoaGFzaGhhc2hoYXNoaGFzaGhhc2g=";

    private static RegistrationAttempt Decoy() =>
        RegistrationAttempt.Start(Hash, personId: null, "10.0.0.1", RegistrationChannels.Email | RegistrationChannels.Sms, Now);

    private static RegistrationAttempt Match() =>
        RegistrationAttempt.Start(Hash, personId: 7, "10.0.0.1", RegistrationChannels.Email, Now);

    [Fact]
    public void Decoy_follows_the_real_attempt_limit()
    {
        // Gercek kodda 3 yanlis deneme hakki vardir, 4. yanlis iptal eder (REQ-KMLK-017).
        var attempt = Decoy();
        attempt.DecoyCodeSent(Now.AddMinutes(5), maxAttempts: 3);

        attempt.VerifyDecoy(Now).ShouldBe(VerificationResult.Mismatch);
        attempt.VerifyDecoy(Now).ShouldBe(VerificationResult.Mismatch);
        attempt.VerifyDecoy(Now).ShouldBe(VerificationResult.Mismatch);
        attempt.VerifyDecoy(Now).ShouldBe(VerificationResult.AttemptsExceeded);
        attempt.VerifyDecoy(Now).ShouldBe(VerificationResult.AttemptsExceeded);
    }

    [Fact]
    public void Decoy_expires_like_a_real_code()
    {
        var attempt = Decoy();
        attempt.DecoyCodeSent(Now.AddMinutes(5), maxAttempts: 3);

        attempt.VerifyDecoy(Now.AddMinutes(5)).ShouldBe(VerificationResult.Expired);
    }

    [Fact]
    public void New_decoy_code_resets_attempts()
    {
        // Gercek akista yeni kod oncekini gecersiz kilar ve deneme hakki yeniden baslar.
        var attempt = Decoy();
        attempt.DecoyCodeSent(Now.AddMinutes(5), 3);
        for (var i = 0; i < 4; i++)
        {
            attempt.VerifyDecoy(Now);
        }

        attempt.DecoyCodeSent(Now.AddMinutes(10), 3);

        attempt.VerifyDecoy(Now).ShouldBe(VerificationResult.Mismatch);
    }

    [Fact]
    public void Decoy_without_a_code_is_not_usable()
    {
        Decoy().VerifyDecoy(Now).ShouldBe(VerificationResult.NotUsable);
    }

    [Fact]
    public void Match_and_decoy_paths_cannot_be_mixed()
    {
        Should.Throw<InvalidOperationException>(() => Match().VerifyDecoy(Now));
        Should.Throw<InvalidOperationException>(() => Decoy().CodeSent(Guid.NewGuid()));
        Should.Throw<InvalidOperationException>(() => Decoy().MarkVerified(Now));
    }

    [Fact]
    public void Verified_attempt_can_be_completed_within_the_window_only()
    {
        var attempt = Match();
        attempt.CodeSent(Guid.NewGuid());
        attempt.CanComplete(Now).ShouldBeFalse();

        attempt.MarkVerified(Now);

        attempt.CanComplete(Now.AddMinutes(14)).ShouldBeTrue();
        attempt.CanComplete(Now + RegistrationAttempt.CompletionWindow).ShouldBeFalse();
        attempt.CanRequestCode(Now).ShouldBeFalse();

        attempt.Complete();
        attempt.Status.ShouldBe(RegistrationStatus.Completed);
        attempt.CanComplete(Now).ShouldBeFalse();
    }

    [Fact]
    public void Unverified_attempt_cannot_be_completed()
    {
        Should.Throw<InvalidOperationException>(() => Match().Complete());
    }

    [Fact]
    public void Attempt_expires_after_its_lifetime()
    {
        var attempt = Match();

        attempt.CanRequestCode(Now + RegistrationAttempt.Lifetime - TimeSpan.FromSeconds(1)).ShouldBeTrue();
        attempt.CanRequestCode(Now + RegistrationAttempt.Lifetime).ShouldBeFalse();
    }

    [Fact]
    public void Attempt_requires_a_hash_and_a_channel()
    {
        Should.Throw<ArgumentException>(() => RegistrationAttempt.Start(" ", null, null, RegistrationChannels.Email, Now));
        Should.Throw<ArgumentException>(() => RegistrationAttempt.Start(Hash, null, null, RegistrationChannels.None, Now));
    }

    [Fact]
    public void Attempt_is_for_registration_by_default_and_can_be_for_password_reset()
    {
        // SYG-KMLK-047: sifirlama ayni akisi kullanir; yalnizca amac farklidir.
        RegistrationAttempt.Start(Hash, 7, null, RegistrationChannels.Email, Now).Purpose.ShouldBe(VerificationPurpose.Registration);
        RegistrationAttempt.Start(Hash, 7, null, RegistrationChannels.Email, Now, VerificationPurpose.PasswordReset)
            .Purpose.ShouldBe(VerificationPurpose.PasswordReset);

        // Iki adimli giris kendi kaydini kullanir; deneme bu amacla acilamaz.
        Should.Throw<ArgumentOutOfRangeException>(() =>
            RegistrationAttempt.Start(Hash, 7, null, RegistrationChannels.Email, Now, VerificationPurpose.TwoFactor));
    }

    [Fact]
    public void Setting_a_password_rotates_the_security_stamp()
    {
        // SYG-KMLK-048: parola degisince diger oturumlar sonlanir.
        var account = UserAccount.Register(7, "ozet-1", Now);
        var stamp = account.SecurityStamp;
        account.PasswordHash.ShouldBe("ozet-1");
        account.PasswordChangedAt.ShouldBe(Now);

        account.SetPassword("ozet-2", Now.AddDays(1));

        account.SecurityStamp.ShouldNotBe(stamp);
        account.PasswordHash.ShouldBe("ozet-2");
        Should.Throw<ArgumentException>(() => account.SetPassword("", Now));
    }

    [Fact]
    public void Code_request_record_requires_a_hash()
    {
        RegistrationCodeRequest.Record(Hash, Now).RequestedAt.ShouldBe(Now);
        Should.Throw<ArgumentException>(() => RegistrationCodeRequest.Record("", Now));
    }
}
