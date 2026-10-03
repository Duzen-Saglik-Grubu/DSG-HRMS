using System.Security.Cryptography;
using Dsg.Hrms.Api.IntegrationTests.Settings;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Identity.Verification;
using Dsg.Hrms.Application.Notifications;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Dsg.Hrms.Infrastructure.Data.Interceptors;
using Dsg.Hrms.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace Dsg.Hrms.Api.IntegrationTests.Identity;

/// <summary>
/// Dogrulama kodunun <b>gercek PostgreSQL</b> uzerinde dogrulanmasi (SYG-KMLK-022…027, 059).
/// </summary>
public sealed class VerificationCodeStoreTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("dsg_hrms_verification_test")
        .Build();

    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly HmacVerificationCodeHasher _hasher =
        new(new CodeHashOptions { CodeHashKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) });
    private readonly List<OutboundMessage> _sent = [];
    private readonly INotificationDispatch _dispatch = Substitute.For<INotificationDispatch>();

    private DateTimeOffset _now = new(2026, 9, 27, 9, 0, 0, TimeSpan.Zero);
    private DbContextOptions<HrmsDbContext> _options = null!;
    private long _personId;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _clock.UtcNow.Returns(_ => _now);
        _dispatch.TryEnqueue(Arg.Do<OutboundMessage>(_sent.Add)).Returns(true);

        _options = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseNpgsql(_container.GetConnectionString(), npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                new AuditFieldsInterceptor(_currentUser, _clock),
                new AuditTrailInterceptor(_currentUser, _clock))
            .Options;

        await using var context = new HrmsDbContext(_options);
        await context.Database.MigrateAsync();

        // Sentetik kisi (KVKK): TCKN sagla algoritmasiyla uretildi.
        var person = Person.Create("10000000146", new PersonDetails("Ahmet", "Yilmaz", new DateOnly(1985, 4, 12), "ahmet.yilmaz@duzen.com.tr", false, "5321234567"));
        context.Add(person);
        await context.SaveChangesAsync();
        _personId = person.Id;
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    private async Task<T> WithServiceAsync<T>(Func<VerificationCodeService, Task<T>> action)
    {
        await using var context = new HrmsDbContext(_options);
        var service = new VerificationCodeService(
            new VerificationCodeStore(context), _hasher, _dispatch, new FakeSystemParameters(), _clock,
            Substitute.For<ISecurityEventLog>(), NullLogger<VerificationCodeService>.Instance);
        return await action(service);
    }

    private Task<IssueResult> IssueAsync(VerificationChannel channel = VerificationChannel.Email) =>
        WithServiceAsync(s => s.IssueAsync(
            new VerificationRequest(_personId, VerificationPurpose.Registration, channel,
                channel == VerificationChannel.Email ? "ahmet.yilmaz@duzen.com.tr" : "5321234567"),
            CancellationToken.None));

    private Task<VerificationResult> VerifyAsync(Guid codeId, string code) =>
        WithServiceAsync(s => s.VerifyAsync(codeId, code, CancellationToken.None));

    private string LastCode() => _sent[^1].MessageBody.Split(": ")[1][..6];

    [Fact]
    public async Task Issued_code_verifies_once_and_is_stored_only_as_hash()
    {
        var issued = await IssueAsync();
        var code = LastCode();

        await using (var context = new HrmsDbContext(_options))
        {
            var raw = await context.Database
                .SqlQuery<string>($"SELECT code_hash AS \"Value\" FROM identity.verification_code")
                .SingleAsync();
            raw.ShouldNotContain(code);
            raw.Length.ShouldBe(44);
        }

        (await VerifyAsync(issued.CodeId!.Value, code)).ShouldBe(VerificationResult.Verified);
        (await VerifyAsync(issued.CodeId!.Value, code)).ShouldBe(VerificationResult.NotUsable);
    }

    [Fact]
    public async Task Concurrent_verifications_succeed_only_once()
    {
        // SYG-KMLK-027: iki istek kodu ayni anda okur; yalnizca biri kaydedebilir.
        var issued = await IssueAsync();
        var code = LastCode();

        await using var first = new HrmsDbContext(_options);
        await using var second = new HrmsDbContext(_options);
        var firstService = new VerificationCodeService(new VerificationCodeStore(first), _hasher, _dispatch, new FakeSystemParameters(), _clock, Substitute.For<ISecurityEventLog>(), NullLogger<VerificationCodeService>.Instance);
        var secondService = new VerificationCodeService(new VerificationCodeStore(second), _hasher, _dispatch, new FakeSystemParameters(), _clock, Substitute.For<ISecurityEventLog>(), NullLogger<VerificationCodeService>.Instance);

        // Ikisi de kodu "Issued" olarak yukler.
        (await first.Set<VerificationCode>().SingleAsync()).Status.ShouldBe(VerificationCodeStatus.Issued);
        (await second.Set<VerificationCode>().SingleAsync()).Status.ShouldBe(VerificationCodeStatus.Issued);

        var results = new[]
        {
            await firstService.VerifyAsync(issued.CodeId!.Value, code, CancellationToken.None),
            await secondService.VerifyAsync(issued.CodeId!.Value, code, CancellationToken.None),
        };

        results.Count(r => r == VerificationResult.Verified).ShouldBe(1);
        results.ShouldContain(VerificationResult.NotUsable);
    }

    [Fact]
    public async Task Rate_limit_window_slides_after_fifteen_minutes()
    {
        await IssueAsync();
        await IssueAsync(VerificationChannel.Sms);
        await IssueAsync();
        (await IssueAsync()).IsRateLimited.ShouldBeTrue();

        _now += VerificationCodeService.RateLimitWindow + TimeSpan.FromSeconds(1);

        (await IssueAsync()).IsRateLimited.ShouldBeFalse();
    }

    [Fact]
    public async Task Previous_code_is_invalidated_in_the_database()
    {
        var first = await IssueAsync();
        await IssueAsync(VerificationChannel.Sms);

        await using var context = new HrmsDbContext(_options);
        var codes = await context.Set<VerificationCode>().OrderBy(c => c.Id).ToListAsync();
        codes.Select(c => c.Status).ShouldBe([VerificationCodeStatus.Invalidated, VerificationCodeStatus.Issued]);
        codes[0].PublicId.ShouldBe(first.CodeId!.Value);
    }

    [Fact]
    public async Task Code_and_hash_never_appear_in_the_audit_trail()
    {
        // SYG-KMLK-026.
        var issued = await IssueAsync();
        var code = LastCode();
        await VerifyAsync(issued.CodeId!.Value, "000000");
        await VerifyAsync(issued.CodeId!.Value, code);

        await using var context = new HrmsDbContext(_options);
        var hash = await context.Database.SqlQuery<string>($"SELECT code_hash AS \"Value\" FROM identity.verification_code").SingleAsync();
        var entries = await context.ChangeLog.Where(e => e.EntityName == nameof(VerificationCode)).ToListAsync();

        // Pozitif kontrol: olusturma ve iki guncelleme kayitli; alan adi gorunuyor.
        entries.Count.ShouldBe(3);
        entries.ShouldContain(e => e.Changes.Contains("CodeHash"));

        foreach (var entry in entries)
        {
            entry.Changes.ShouldNotContain(code);
            entry.Changes.ShouldNotContain(hash);
        }
    }

    [Fact]
    public async Task Database_rejects_a_plain_code_in_place_of_the_hash()
    {
        await using var context = new HrmsDbContext(_options);

        var ex = await Should.ThrowAsync<PostgresException>(() => context.Database.ExecuteSqlAsync(
            $"INSERT INTO identity.verification_code (person_id, purpose, channel, code_hash, expires_at, max_failed_attempts, failed_attempts, status, created_at, public_id) VALUES ({_personId}, 1, 1, '482915', now(), 3, 0, 1, now(), gen_random_uuid())"));

        ex.ConstraintName.ShouldBe("ck_verification_code_hash_length");
    }
}
