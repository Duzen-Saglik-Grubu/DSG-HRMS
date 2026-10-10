using System.Globalization;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Identity.Verification;
using Dsg.Hrms.Application.Notifications;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Application.Tests.Settings;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Notifications;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Dsg.Hrms.Application.Tests.Identity;

/// <summary>Kod uretim ve dogrulama akisi (SYG-KMLK-019, 022…027, 059).</summary>
public sealed class VerificationCodeServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 27, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryStore _store = new();
    private readonly FakeHasher _hasher = new();
    private readonly List<OutboundMessage> _sent = [];
    private readonly INotificationDispatch _dispatch = Substitute.For<INotificationDispatch>();
    private readonly FakeSystemParameters _parameters = new();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ISecurityEventLog _events = Substitute.For<ISecurityEventLog>();

    public VerificationCodeServiceTests()
    {
        _clock.UtcNow.Returns(_ => Now);
        _dispatch.TryEnqueue(Arg.Do<OutboundMessage>(_sent.Add)).Returns(true);
    }

    private VerificationCodeService CreateService() =>
        new(_store, _hasher, _dispatch, _parameters, _clock, _events, NullLogger<VerificationCodeService>.Instance);

    private static VerificationRequest Email(long personId = 1, VerificationPurpose purpose = VerificationPurpose.Registration) =>
        new(personId, purpose, VerificationChannel.Email, "ahmet.yilmaz@duzen.com.tr");

    private static VerificationRequest Sms(long personId = 1) =>
        new(personId, VerificationPurpose.Registration, VerificationChannel.Sms, "5321234567");

    private string LastCode() => _hasher.LastCode!;

    // ------------------------------------------------------------------ kimlik olaylari (SYG-KMLK-060)

    [Fact]
    public async Task Issuing_records_a_code_sent_event_without_the_code_or_recipient()
    {
        await CreateService().IssueAsync(Sms(personId: 7), CancellationToken.None);

        _events.Received(1).Record(SecurityEventType.VerificationCodeSent, null, 7, "registration:sms");
    }

    [Fact]
    public async Task Verification_records_success_and_failure_with_the_result()
    {
        var service = CreateService();
        var issued = await service.IssueAsync(Email(personId: 7, purpose: VerificationPurpose.PasswordReset), CancellationToken.None);

        await service.VerifyAsync(issued.CodeId!.Value, "000000" == LastCode() ? "111111" : "000000", CancellationToken.None);
        await service.VerifyAsync(issued.CodeId!.Value, LastCode(), CancellationToken.None);

        _events.Received(1).Record(SecurityEventType.VerificationFailed, null, 7, "password-reset:mismatch");
        _events.Received(1).Record(SecurityEventType.VerificationSucceeded, null, 7, "password-reset:verified");
    }

    // ------------------------------------------------------------------ uretim

    [Fact]
    public async Task Issued_code_is_stored_only_as_hash_and_sent_to_the_chosen_channel()
    {
        var result = await CreateService().IssueAsync(Email(), CancellationToken.None);

        result.IsRateLimited.ShouldBeFalse();
        result.ExpiresAt.ShouldBe(Now.AddMinutes(5)); // PRM-KML-10 varsayilani

        var stored = _store.Codes.Single();
        stored.PublicId.ShouldBe(result.CodeId!.Value);
        stored.CodeHash.ShouldNotContain(LastCode());
        stored.MaxFailedAttempts.ShouldBe(3); // PRM-KML-16

        var message = _sent.Single();
        message.Channel.ShouldBe(NotificationChannel.Email);
        message.Purpose.ShouldBe(NotificationPurpose.RegistrationCode);
        message.Recipient.ShouldBe("ahmet.yilmaz@duzen.com.tr");
        message.MessageBody.ShouldContain(LastCode());
        message.Subject.ShouldNotBeNull();
    }

    [Fact]
    public async Task Sms_channel_produces_a_single_sms_message()
    {
        await CreateService().IssueAsync(Sms(), CancellationToken.None);

        var message = _sent.Single();
        message.Channel.ShouldBe(NotificationChannel.Sms);
        message.Subject.ShouldBeNull();
        message.MessageBody.Length.ShouldBeLessThanOrEqualTo(VerificationMessages.SmsLimit);
        message.MessageBody.ShouldContain(LastCode());
    }

    [Fact]
    public async Task Code_length_follows_the_parameter()
    {
        _parameters.With(ParameterCatalog.VerificationCodeLength, "8");

        await CreateService().IssueAsync(Email(), CancellationToken.None);

        LastCode().Length.ShouldBe(8);
        LastCode().ShouldAllBe(c => char.IsAsciiDigit(c));
    }

    [Fact]
    public async Task New_code_invalidates_the_previous_one()
    {
        // SYG-KMLK-019: kanal degisimi ve tekrar gonderimde onceki kod gecersizlesir.
        var service = CreateService();
        var first = await service.IssueAsync(Email(), CancellationToken.None);
        var firstCode = LastCode();

        await service.IssueAsync(Sms(), CancellationToken.None);

        (await service.VerifyAsync(first.CodeId!.Value, firstCode, CancellationToken.None)).ShouldBe(VerificationResult.NotUsable);
        _store.Codes.Count(c => c.Status == VerificationCodeStatus.Issued).ShouldBe(1);
    }

    [Fact]
    public async Task Codes_for_another_purpose_stay_valid()
    {
        var service = CreateService();
        var registration = await service.IssueAsync(Email(), CancellationToken.None);
        var registrationCode = LastCode();

        await service.IssueAsync(Email(purpose: VerificationPurpose.PasswordReset), CancellationToken.None);

        (await service.VerifyAsync(registration.CodeId!.Value, registrationCode, CancellationToken.None)).ShouldBe(VerificationResult.Verified);
    }

    [Fact]
    public async Task Code_over_the_limit_within_fifteen_minutes_is_rate_limited()
    {
        // SYG-KMLK-059, PRM-KML-17: kisi basina 15 dakikada parametredeki sayi kadar kod
        // (varsayilan 10, #185); kanal degisimi de sayilir.
        var limit = int.Parse(ParameterCatalog.CodeSendLimit.DefaultValue!, CultureInfo.InvariantCulture);
        var service = CreateService();
        for (var i = 0; i < limit; i++)
        {
            (await service.IssueAsync(i % 2 == 0 ? Email() : Sms(), CancellationToken.None)).IsRateLimited.ShouldBeFalse();
        }

        var overLimit = await service.IssueAsync(Sms(), CancellationToken.None);

        overLimit.ShouldBe(IssueResult.RateLimited);
        _sent.Count.ShouldBe(limit);
        _store.Codes.Count.ShouldBe(limit);
    }

    [Fact]
    public async Task Rate_limit_is_per_person()
    {
        var limit = int.Parse(ParameterCatalog.CodeSendLimit.DefaultValue!, CultureInfo.InvariantCulture);
        var service = CreateService();
        for (var i = 0; i < limit; i++)
        {
            await service.IssueAsync(Email(personId: 1), CancellationToken.None);
        }

        (await service.IssueAsync(Email(personId: 2), CancellationToken.None)).IsRateLimited.ShouldBeFalse();
    }

    [Fact]
    public async Task Full_queue_still_records_the_code()
    {
        _dispatch.TryEnqueue(Arg.Any<OutboundMessage>()).Returns(false);

        var result = await CreateService().IssueAsync(Email(), CancellationToken.None);

        result.CodeId.ShouldNotBeNull();
        _store.Codes.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Issue_without_hash_key_fails_loudly()
    {
        _hasher.IsConfigured = false;

        await Should.ThrowAsync<InvalidOperationException>(() => CreateService().IssueAsync(Email(), CancellationToken.None));
        _sent.ShouldBeEmpty();
    }

    // ------------------------------------------------------------------ dogrulama

    [Fact]
    public async Task Correct_code_verifies()
    {
        var service = CreateService();
        var issued = await service.IssueAsync(Email(), CancellationToken.None);

        (await service.VerifyAsync(issued.CodeId!.Value, LastCode(), CancellationToken.None)).ShouldBe(VerificationResult.Verified);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12ab56")]
    [InlineData("000000000000")]
    public async Task Malformed_input_counts_as_a_wrong_attempt(string? input)
    {
        var service = CreateService();
        var issued = await service.IssueAsync(Email(), CancellationToken.None);

        (await service.VerifyAsync(issued.CodeId!.Value, input, CancellationToken.None)).ShouldBe(VerificationResult.Mismatch);
        _store.Codes.Single().FailedAttempts.ShouldBe(1);
    }

    [Fact]
    public async Task Surrounding_spaces_are_ignored()
    {
        var service = CreateService();
        var issued = await service.IssueAsync(Email(), CancellationToken.None);

        (await service.VerifyAsync(issued.CodeId!.Value, $" {LastCode()} ", CancellationToken.None)).ShouldBe(VerificationResult.Verified);
    }

    [Fact]
    public async Task Unknown_code_id_is_not_usable()
    {
        (await CreateService().VerifyAsync(Guid.NewGuid(), "123456", CancellationToken.None)).ShouldBe(VerificationResult.NotUsable);
    }

    [Fact]
    public async Task Concurrent_save_conflict_rejects_the_attempt()
    {
        // SYG-KMLK-027: eszamanli iki dogrulamadan yalnizca biri basarili olabilir.
        var service = CreateService();
        var issued = await service.IssueAsync(Email(), CancellationToken.None);
        _store.ConflictOnNextSave = true;

        (await service.VerifyAsync(issued.CodeId!.Value, LastCode(), CancellationToken.None)).ShouldBe(VerificationResult.NotUsable);
    }

    [Fact]
    public async Task Expired_code_is_reported_as_expired()
    {
        var service = CreateService();
        var issued = await service.IssueAsync(Email(), CancellationToken.None);
        _clock.UtcNow.Returns(Now.AddMinutes(6));

        (await service.VerifyAsync(issued.CodeId!.Value, LastCode(), CancellationToken.None)).ShouldBe(VerificationResult.Expired);
    }

    [Fact]
    public async Task Code_of_another_person_or_purpose_is_not_usable_and_stays_intact()
    {
        // SYG-KMLK-080: iki adimli dogrulamayi acma kodu yalnizca kendi kisisi ve amaciyla kullanilir.
        var service = CreateService();
        var issued = await service.IssueAsync(Email(personId: 7, purpose: VerificationPurpose.PasswordReset), CancellationToken.None);
        var code = LastCode();

        (await service.VerifyAsync(issued.CodeId!.Value, code, 7, VerificationPurpose.TwoFactorSetup, CancellationToken.None))
            .ShouldBe(VerificationResult.NotUsable);
        (await service.VerifyAsync(issued.CodeId!.Value, code, 8, VerificationPurpose.PasswordReset, CancellationToken.None))
            .ShouldBe(VerificationResult.NotUsable);

        var stored = _store.Codes.Single();
        stored.Status.ShouldBe(VerificationCodeStatus.Issued);
        stored.FailedAttempts.ShouldBe(0);

        (await service.VerifyAsync(issued.CodeId!.Value, code, 7, VerificationPurpose.PasswordReset, CancellationToken.None))
            .ShouldBe(VerificationResult.Verified);
    }

    [Fact]
    public async Task Two_factor_setup_code_has_its_own_purpose_and_message()
    {
        await CreateService().IssueAsync(Email(personId: 7, purpose: VerificationPurpose.TwoFactorSetup), CancellationToken.None);

        _events.Received(1).Record(SecurityEventType.VerificationCodeSent, null, 7, "two-factor-setup:email");
        var message = _sent.Single();
        message.Purpose.ShouldBe(NotificationPurpose.TwoFactorSetupCode);
        message.Subject.ShouldNotBeNull().ShouldContain("iki adımlı doğrulamayı açma");
    }

    // ------------------------------------------------------------------ uretec

    [Fact]
    public void Generated_codes_keep_leading_zeros_and_vary()
    {
        // SYG-KMLK-022: guvenli uretec, tum araligi kapsar.
        var codes = Enumerable.Range(0, 2000).Select(_ => VerificationCodeService.Generate(6)).ToList();

        codes.ShouldAllBe(c => c.Length == 6 && c.All(char.IsAsciiDigit));
        codes.Distinct().Count().ShouldBeGreaterThan(1900);
        codes.ShouldContain(c => c[0] == '0'); // 2000 kodda basi sifir olan olmama olasiligi ~1e-92
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void Generator_rejects_unsupported_lengths(int length)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => VerificationCodeService.Generate(length));
    }

    [Fact]
    public void Request_and_message_never_print_the_code_or_recipient()
    {
        // SYG-KMLK-026: yanlislikla gunluge veya hata iletisine yazilsalar bile.
        var message = new OutboundMessage(NotificationChannel.Sms, NotificationPurpose.RegistrationCode, 1, "5321234567", null, "kodunuz: 482915", Now);

        message.ToString().ShouldNotContain("482915");
        message.ToString().ShouldNotContain("5321234567");
        Email().ToString().ShouldNotContain("ahmet.yilmaz");
    }

    // ------------------------------------------------------------------ sahteler

    private sealed class FakeHasher : IVerificationCodeHasher
    {
        public bool IsConfigured { get; set; } = true;

        public string? LastCode { get; private set; }

        public string Hash(Guid codeId, string code)
        {
            LastCode = code;
            return $"h:{codeId:N}:{new string(code.Reverse().ToArray())}";
        }

        public bool Matches(Guid codeId, string code, string storedHash) =>
            storedHash == $"h:{codeId:N}:{new string(code.Reverse().ToArray())}";
    }

    private sealed class InMemoryStore : IVerificationCodeStore
    {
        public List<VerificationCode> Codes { get; } = [];

        public bool ConflictOnNextSave { get; set; }

        public Task<int> CountIssuedSinceAsync(long personId, DateTimeOffset since, CancellationToken cancellationToken) =>
            Task.FromResult(Codes.Count(c => c.PersonId == personId));

        public Task<IReadOnlyList<VerificationCode>> GetOpenAsync(long personId, VerificationPurpose purpose, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<VerificationCode>>(
                Codes.Where(c => c.PersonId == personId && c.Purpose == purpose && c.Status == VerificationCodeStatus.Issued).ToList());

        public Task<VerificationCode?> FindAsync(Guid publicId, CancellationToken cancellationToken) =>
            Task.FromResult(Codes.SingleOrDefault(c => c.PublicId == publicId));

        public void Add(VerificationCode code) => Codes.Add(code);

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            if (ConflictOnNextSave)
            {
                ConflictOnNextSave = false;
                throw new VerificationConflictException();
            }

            return Task.CompletedTask;
        }
    }
}
