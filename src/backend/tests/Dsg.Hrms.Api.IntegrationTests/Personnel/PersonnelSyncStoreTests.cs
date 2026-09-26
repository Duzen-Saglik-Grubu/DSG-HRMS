using System.Globalization;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Domain.Personnel.Sync;
using Dsg.Hrms.Infrastructure.Data;
using Dsg.Hrms.Infrastructure.Data.Interceptors;
using Dsg.Hrms.Infrastructure.Logo;
using Dsg.Hrms.Infrastructure.Personnel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Testcontainers.PostgreSql;

namespace Dsg.Hrms.Api.IntegrationTests.Personnel;

/// <summary>
/// Personel senkronizasyonunun <b>gercek PostgreSQL</b> uzerinde dogrulanmasi.
/// </summary>
/// <remarks>
/// <para>
/// Sema, uretimdeki gibi <b>migration'larla</b> kurulur (<c>EnsureCreated</c> degil);
/// boylece migration'in kendisi de sinanir.
/// </para>
/// <para>
/// HRMS baglami uretimdeki gibi <b>yeniden deneme stratejisiyle</b> yapilandirilir.
/// Senkronizasyon oturumu bu stratejiyi kapatir; kapatmasaydi kullanici islemi
/// acilamazdi ve testler bunu yakalardi.
/// </para>
/// <para>
/// Test verisi SENTETIKTIR (KVKK).
/// </para>
/// </remarks>
public sealed class PersonnelSyncStoreTests : IAsyncLifetime
{
    private static readonly DateOnly Today = new(2026, 9, 26);

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("dsg_hrms_personnel_test")
        .Build();

    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly ILogoPersonnelSource _source = Substitute.For<ILogoPersonnelSource>();

    private DbContextOptions<HrmsDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _clock.UtcNow.Returns(new DateTimeOffset(2026, 9, 26, 9, 0, 0, TimeSpan.Zero));
        _clock.Today.Returns(Today);
        _currentUser.UserId.Returns((long?)null); // sistem islemi

        _options = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseNpgsql(_container.GetConnectionString(), npgsql =>
            {
                npgsql.EnableRetryOnFailure(3);
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "public");
            })
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                new AuditFieldsInterceptor(_currentUser, _clock),
                new AuditTrailInterceptor(_currentUser, _clock))
            .Options;

        await using var context = new HrmsDbContext(_options);
        await context.Database.MigrateAsync();

        _source.VerifyReadOnlyAccessAsync(Arg.Any<CancellationToken>()).Returns(new LogoAccessCheckResult(true, []));
        _source.VerifySchemaAsync(Arg.Any<CancellationToken>()).Returns(new LogoSchemaCheckResult(true, []));
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    private PersonnelSyncStore CreateStore() => new(_options, new Factory(_options));

    private async Task<PersonnelSyncResult> SyncAsync(params LogoPersonnelRecord[] cards)
    {
        _source.GetAllAsync(Arg.Any<CancellationToken>()).Returns(cards);

        await using var store = CreateStore();
        var service = new PersonnelSyncService(
            _source, store, _clock, new PersonnelSyncOptions(), NullLogger<PersonnelSyncService>.Instance);

        return await service.RunAsync(SyncTrigger.Scheduled, CancellationToken.None);
    }

    private HrmsDbContext Read() => new(_options);

    // ------------------------------------------------------------------ yazma

    [Fact]
    public async Task Sync_persists_persons_employments_companies_and_the_run()
    {
        var result = await SyncAsync(
            // Ayni kisinin iki karti ayni e-postayi tasir; farkli olsaydi motor dogru
            // olarak "kartlar arasinda farkli e-posta" uyarisi uretirdi.
            Card("00001", NationalId(1), logoRef: 1, emails: ["ortak.kisi@duzen.com.tr"]),
            Card("00002", NationalId(1), logoRef: 2, firm: 2, firmName: "Zeytinim", emails: ["ortak.kisi@duzen.com.tr"]),
            Card("00003", NationalId(2), logoRef: 3, terminationDate: Today.AddDays(-10)));

        result.Status.ShouldBe(SyncStatus.Succeeded);

        await using var context = Read();
        var persons = await context.Set<Person>().Include(p => p.Employments).ToListAsync();
        persons.Count.ShouldBe(2);
        persons.Single(p => p.NationalId == NationalId(1)).Employments.Count.ShouldBe(2);
        (await context.Set<Employment>().CountAsync(e => e.IsActive)).ShouldBe(2);
        (await context.Set<Domain.Organization.Company>().CountAsync()).ShouldBe(2);

        var run = await context.Set<PersonnelSyncRun>().SingleAsync();
        run.Status.ShouldBe(SyncStatus.Succeeded);
        run.PersonsCreated.ShouldBe(2);
        run.EmploymentsCreated.ShouldBe(3);
        run.FinishedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task Second_identical_sync_adds_nothing_to_the_audit_trail()
    {
        // 15 dakikada bir calisan senkronizasyon, degisiklik yoksa denetim izine
        // TEK SATIR eklememelidir.
        var cards = new[] { Card("00001", NationalId(1)), Card("00002", NationalId(2)) };
        await SyncAsync(cards);

        long auditRowsAfterFirst;
        await using (var context = Read())
        {
            auditRowsAfterFirst = await context.ChangeLog.LongCountAsync();
        }

        var second = await SyncAsync(cards);

        second.Status.ShouldBe(SyncStatus.Succeeded);
        await using (var context = Read())
        {
            (await context.ChangeLog.LongCountAsync()).ShouldBe(auditRowsAfterFirst);
            var lastRun = await context.Set<PersonnelSyncRun>().OrderByDescending(r => r.Id).FirstAsync();
            lastRun.PersonsUpdated.ShouldBe(0);
            lastRun.EmploymentsUpdated.ShouldBe(0);
        }
    }

    [Fact]
    public async Task Warnings_are_persisted_with_the_run()
    {
        await SyncAsync(Card("00001", nationalId: null), Card("00002", NationalId(2)));

        await using var context = Read();
        var run = await context.Set<PersonnelSyncRun>().Include(r => r.Warnings).SingleAsync();
        run.Status.ShouldBe(SyncStatus.CompletedWithWarnings);
        run.Warnings.Single().Code.ShouldBe(SyncWarningCode.MissingNationalId);
        run.Warnings.Single().RegistryCode.ShouldBe("00001");
    }

    [Fact]
    public async Task Personal_data_is_masked_in_the_audit_trail()
    {
        var nationalId = NationalId(1);
        await SyncAsync(Card("00001", nationalId, emails: ["ahmet.yilmaz@duzen.com.tr"], phones: ["05321234567"],
            birthDate: new DateOnly(1985, 4, 12)));

        await using var context = Read();
        var entry = await context.ChangeLog.SingleAsync(e => e.EntityName == nameof(Person));
        var changes = entry.Changes.ToString();

        // Pozitif kontrol: alanlar kayitta VAR ve maskelenmeyen ad acik gorunuyor.
        // Bu olmadan asagidaki "icermiyor" kontrolleri, bos bir kayitta da gecerdi.
        changes.ShouldContain("NationalId");
        changes.ShouldContain("BirthDate");
        changes.ShouldContain("Ahmet");

        changes.ShouldNotContain(nationalId);
        changes.ShouldNotContain("ahmet.yilmaz");
        changes.ShouldNotContain("5321234567");
        changes.ShouldNotContain("1985-04-12");
        entry.Operation.ShouldBe(AuditOperation.Insert);
    }

    // ------------------------------------------------------------------ durum (SYG-KMLK-010)

    [Fact]
    public async Task Status_reports_nothing_before_the_first_run()
    {
        await using var context = Read();
        var status = new PersonnelSyncStatus(context, new LogoOptions { ConnectionString = "Server=logo" });

        status.IsEnabled.ShouldBeTrue();
        (await status.GetLastRunAsync(CancellationToken.None)).ShouldBeNull();
    }

    [Fact]
    public async Task Status_reports_the_latest_run()
    {
        await SyncAsync(Card("00001", NationalId(1)));
        _source.GetAllAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new LogoUnavailableException());
        await using (var store = CreateStore())
        {
            await new PersonnelSyncService(_source, store, _clock, new PersonnelSyncOptions(), NullLogger<PersonnelSyncService>.Instance)
                .RunAsync(SyncTrigger.Scheduled, CancellationToken.None);
        }

        await using var context = Read();
        var last = await new PersonnelSyncStatus(context, new LogoOptions()).GetLastRunAsync(CancellationToken.None);

        last.ShouldNotBeNull();
        last.Status.ShouldBe(SyncStatus.Failed);
        last.FailureReason.ShouldBe(SyncFailureReason.SourceUnavailable);
        new PersonnelSyncStatus(context, new LogoOptions()).IsEnabled.ShouldBeFalse();
    }

    // ------------------------------------------------------------------ kilit

    [Fact]
    public async Task Only_one_session_can_be_open_at_a_time()
    {
        // SYG-KMLK-004: ayni anda iki senkronizasyon calisamaz.
        await using var firstStore = CreateStore();
        await using var secondStore = CreateStore();

        var first = await firstStore.TryOpenSessionAsync(CancellationToken.None);
        first.ShouldNotBeNull();

        (await secondStore.TryOpenSessionAsync(CancellationToken.None)).ShouldBeNull();

        // Kilit islemle birlikte birakilir.
        await first.DisposeAsync();
        await using var third = await secondStore.TryOpenSessionAsync(CancellationToken.None);
        third.ShouldNotBeNull();
    }

    [Fact]
    public async Task Concurrent_sync_is_skipped_while_another_holds_the_lock()
    {
        await using var holder = CreateStore();
        await using var session = await holder.TryOpenSessionAsync(CancellationToken.None);
        session.ShouldNotBeNull();

        var result = await SyncAsync(Card("00001", NationalId(1)));

        result.ShouldBe(PersonnelSyncResult.AlreadyRunning);
        await using var context = Read();
        (await context.Set<PersonnelSyncRun>().CountAsync()).ShouldBe(0);
    }

    // ------------------------------------------------------------------ hata davranisi

    [Fact]
    public async Task Uncommitted_session_leaves_no_data()
    {
        await using (var store = CreateStore())
        {
            var session = await store.TryOpenSessionAsync(CancellationToken.None);
            session.ShouldNotBeNull();
            await session.LoadAsync(CancellationToken.None);
            session.Add([], [Person.Create(NationalId(1), new PersonDetails("A", "B", new DateOnly(1990, 1, 1), null, false, null))], []);
            await session.DisposeAsync(); // onaylanmadan kapatildi
        }

        await using var context = Read();
        (await context.Set<Person>().CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Failed_run_is_recorded_even_though_its_changes_are_rolled_back()
    {
        _source.GetAllAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new LogoUnavailableException());

        await using (var store = CreateStore())
        {
            var service = new PersonnelSyncService(
                _source, store, _clock, new PersonnelSyncOptions(), NullLogger<PersonnelSyncService>.Instance);
            (await service.RunAsync(SyncTrigger.Scheduled, CancellationToken.None)).Status.ShouldBe(SyncStatus.Failed);
        }

        await using var context = Read();
        var run = await context.Set<PersonnelSyncRun>().SingleAsync();
        run.Status.ShouldBe(SyncStatus.Failed);
        run.FailureReason.ShouldBe(SyncFailureReason.SourceUnavailable);
        (await context.Set<Person>().CountAsync()).ShouldBe(0);
    }

    // ------------------------------------------------------------------ veritabani kisitlari

    [Theory]
    [InlineData("0123456789a")]
    [InlineData("1234567890")]
    public async Task Database_rejects_malformed_national_id(string nationalId)
    {
        // Son savunma hatti: uygulama katmani atlatilsa bile bicimi gecersiz TCKN giremez.
        await using var context = Read();

        var ex = await Should.ThrowAsync<PostgresException>(() => context.Database.ExecuteSqlAsync(
            $"INSERT INTO personnel.person (national_id, first_name, last_name, birth_date, is_email_shared, created_at, public_id) VALUES ({nationalId}, 'A', 'B', DATE '1990-01-01', false, now(), gen_random_uuid())"));

        ex.ConstraintName.ShouldBe("ck_person_national_id_format");
    }

    [Fact]
    public async Task Database_rejects_uppercase_email()
    {
        await using var context = Read();

        var ex = await Should.ThrowAsync<PostgresException>(() => context.Database.ExecuteSqlAsync(
            $"INSERT INTO personnel.person (national_id, first_name, last_name, birth_date, email, is_email_shared, created_at, public_id) VALUES ({NationalId(1)}, 'A', 'B', DATE '1990-01-01', 'A@DUZEN.COM.TR', false, now(), gen_random_uuid())"));

        ex.ConstraintName.ShouldBe("ck_person_email_lowercase");
    }

    // ------------------------------------------------------------------ yardimcilar

    private static string NationalId(int index)
    {
        var firstNine = (100000000 + index).ToString(CultureInfo.InvariantCulture);
        var d = firstNine.Select(c => c - '0').ToArray();
        var tenth = ((((d[0] + d[2] + d[4] + d[6] + d[8]) * 7) - (d[1] + d[3] + d[5] + d[7])) % 10 + 10) % 10;
        var eleventh = (d.Sum() + tenth) % 10;
        return $"{firstNine}{tenth}{eleventh}";
    }

    private static LogoPersonnelRecord Card(
        string registryCode,
        string? nationalId,
        int logoRef = 1,
        DateOnly? birthDate = null,
        DateOnly? terminationDate = null,
        short firm = 1,
        string firmName = "Duzen Laboratuvarlar",
        string[]? emails = null,
        string[]? phones = null) =>
        new(logoRef, registryCode, nationalId, "Ahmet", "Yilmaz", birthDate ?? new DateOnly(1985, 4, 12),
            new DateOnly(2020, 1, 1), terminationDate, firm, firmName,
            emails ?? [$"p{registryCode}@duzen.com.tr"], phones ?? ["05321234567"]);

    private sealed class Factory(DbContextOptions<HrmsDbContext> options) : IDbContextFactory<HrmsDbContext>
    {
        public HrmsDbContext CreateDbContext() => new(options);
    }
}
