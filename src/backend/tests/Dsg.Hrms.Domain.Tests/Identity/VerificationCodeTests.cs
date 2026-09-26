using Dsg.Hrms.Domain.Identity;

namespace Dsg.Hrms.Domain.Tests.Identity;

/// <summary>Dogrulama kodunun durum makinesi (SYG-KMLK §3.2, 023, 024, 027).</summary>
public sealed class VerificationCodeTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 9, 0, 0, TimeSpan.Zero);

    private static VerificationCode NewCode(int maxAttempts = 3) =>
        VerificationCode.Issue(1, VerificationPurpose.Registration, VerificationChannel.Email, Now.AddMinutes(5), maxAttempts);

    [Fact]
    public void Correct_code_is_verified_once()
    {
        // SYG-KMLK-027: tek kullanim.
        var code = NewCode();

        code.Verify(matches: true, Now).ShouldBe(VerificationResult.Verified);
        code.Status.ShouldBe(VerificationCodeStatus.Verified);

        code.Verify(matches: true, Now).ShouldBe(VerificationResult.NotUsable);
    }

    [Fact]
    public void Expired_code_is_rejected_even_if_correct()
    {
        // SYG-KMLK-023: sure dolunca kabul edilmez ve kullaniciya ayrica bildirilir.
        var code = NewCode();

        code.Verify(matches: true, Now.AddMinutes(5)).ShouldBe(VerificationResult.Expired);
        code.Status.ShouldBe(VerificationCodeStatus.Expired);
        code.Verify(matches: true, Now).ShouldBe(VerificationResult.Expired);
    }

    [Fact]
    public void Code_is_valid_until_just_before_expiry()
    {
        NewCode().Verify(matches: true, Now.AddMinutes(5).AddTicks(-1)).ShouldBe(VerificationResult.Verified);
    }

    [Fact]
    public void Fourth_wrong_attempt_cancels_the_code()
    {
        // REQ-KMLK-017 kabul olcutu: "4. yanlis denemede kod iptal oluyor".
        var code = NewCode(maxAttempts: 3);

        code.Verify(false, Now).ShouldBe(VerificationResult.Mismatch);
        code.Verify(false, Now).ShouldBe(VerificationResult.Mismatch);
        code.Verify(false, Now).ShouldBe(VerificationResult.Mismatch);
        code.Verify(false, Now).ShouldBe(VerificationResult.AttemptsExceeded);

        code.Status.ShouldBe(VerificationCodeStatus.AttemptsExceeded);
        code.Verify(true, Now).ShouldBe(VerificationResult.AttemptsExceeded);
    }

    [Fact]
    public void Correct_code_after_three_wrong_attempts_is_accepted()
    {
        var code = NewCode(maxAttempts: 3);
        code.Verify(false, Now);
        code.Verify(false, Now);
        code.Verify(false, Now);

        code.Verify(true, Now).ShouldBe(VerificationResult.Verified);
        code.FailedAttempts.ShouldBe(3);
    }

    [Fact]
    public void Invalidated_code_is_not_usable()
    {
        // SYG-KMLK-019: kanal degisimi veya yeni kod.
        var code = NewCode();

        code.Invalidate().ShouldBeTrue();
        code.Invalidate().ShouldBeFalse();

        code.Verify(true, Now).ShouldBe(VerificationResult.NotUsable);
        code.Status.ShouldBe(VerificationCodeStatus.Invalidated);
    }

    [Fact]
    public void Terminal_states_cannot_be_invalidated()
    {
        var code = NewCode();
        code.Verify(true, Now);

        code.Invalidate().ShouldBeFalse();
        code.Status.ShouldBe(VerificationCodeStatus.Verified);
    }

    [Fact]
    public void Hash_is_written_once()
    {
        var code = NewCode();
        code.SetHash("ozet");

        code.CodeHash.ShouldBe("ozet");
        Should.Throw<InvalidOperationException>(() => code.SetHash("baska"));
        Should.Throw<ArgumentException>(() => NewCode().SetHash(" "));
    }

    [Fact]
    public void Issue_requires_a_person_and_an_attempt_limit()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            VerificationCode.Issue(0, VerificationPurpose.Registration, VerificationChannel.Sms, Now, 3));
        Should.Throw<ArgumentOutOfRangeException>(() =>
            VerificationCode.Issue(1, VerificationPurpose.Registration, VerificationChannel.Sms, Now, 0));
    }
}
