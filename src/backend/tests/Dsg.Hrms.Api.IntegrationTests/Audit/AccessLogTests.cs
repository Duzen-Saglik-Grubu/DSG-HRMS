using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Infrastructure.Audit;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace Dsg.Hrms.Api.IntegrationTests.Audit;

/// <summary>
/// Erisim kaydinin <b>gercek PostgreSQL</b> uzerinde dogrulanmasi (ADR-0009 §3).
/// </summary>
/// <remarks>
/// ADR-0009'un kendi ifadesiyle bu, "en sik atlanan ve KVKK denetiminde en sik
/// sorulan kayittir". Bir kullanicinin 400 personelin ozluk dosyasini goruntulemesi
/// hicbir degisiklik uretmez; bu testler o olayin gercekten kaydedildigini olcer.
/// </remarks>
public sealed class AccessLogTests : IAsyncLifetime
{
    private const string NationalIdValue = "12345678901";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("dsg_hrms_access_test")
        .Build();

    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private static readonly DateTimeOffset FixedInstant =
        new(2026, 9, 10, 15, 30, 0, TimeSpan.Zero);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _dateTimeProvider.UtcNow.Returns(FixedInstant);
        _currentUser.UserId.Returns((long?)7);
        _currentUser.TraceId.Returns("ffeeddccbbaa99887766554433221100");
        _currentUser.IpAddress.Returns("192.168.3.42");

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    private HrmsDbContext CreateContext() => new(BuildOptions());

    private DbContextOptions<HrmsDbContext> BuildOptions() =>
        new DbContextOptionsBuilder<HrmsDbContext>()
            .UseNpgsql(_container.GetConnectionString(), npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;

    private AccessLogger CreateLogger() =>
        new(new TestContextFactory(BuildOptions()), _currentUser, _dateTimeProvider);

    // ------------------------------------------------------------------
    // Kayit uretimi
    // ------------------------------------------------------------------

    [Fact]
    public async Task Viewing_a_record_is_logged_with_the_request_context()
    {
        var personId = Guid.CreateVersion7();

        await CreateLogger().LogAsync(AccessRecord.View("Person", personId));

        await using var context = CreateContext();
        var entry = await context.AccessLog.AsNoTracking().SingleAsync();

        entry.AccessType.ShouldBe(AccessType.View);
        entry.EntityName.ShouldBe("Person");
        entry.EntityId.ShouldBe(personId);
        entry.RecordCount.ShouldBe(1);
        entry.UserAccountId.ShouldBe(7);
        entry.OccurredAt.ShouldBe(FixedInstant);
        entry.TraceId.ShouldBe("ffeeddccbbaa99887766554433221100");
        entry.IpAddress.ShouldBe("192.168.3.42");
    }

    [Fact]
    public async Task Export_records_how_many_people_left_the_system()
    {
        // Bir sizinti incelemesinin ilk sorusu budur.
        await CreateLogger().LogAsync(AccessRecord.Export("Person", recordCount: 400));

        await using var context = CreateContext();
        var entry = await context.AccessLog.AsNoTracking().SingleAsync();

        entry.AccessType.ShouldBe(AccessType.Export);
        entry.RecordCount.ShouldBe(400);
    }

    [Fact]
    public async Task Special_category_access_is_recorded_as_its_own_type()
    {
        // KVKK md. 6 kapsamindaki veriler denetimde oncelikle sorulur; sıradan
        // goruntulemeyle ayni kovada durmamalidir.
        await CreateLogger().LogAsync(
            AccessRecord.SpecialCategoryView("HealthReport", Guid.CreateVersion7()));

        await using var context = CreateContext();
        var entry = await context.AccessLog.AsNoTracking().SingleAsync();

        entry.AccessType.ShouldBe(AccessType.SpecialCategoryView);
    }

    // ------------------------------------------------------------------
    // KVKK — kaydin kendisi de sizinti kaynagi olmamalidir
    // ------------------------------------------------------------------

    [Fact]
    public async Task Filter_values_are_masked()
    {
        // Arama kutusuna girilen bir TCKN, maskelenmeseydi erisim kaydina duz
        // metin duserdi (KR-059).
        var filters = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["NationalId"] = NationalIdValue,
            ["Email"] = "ahmet.yilmaz@duzen.com.tr",
            ["DepartmentName"] = "Biyokimya",
        };

        await CreateLogger().LogAsync(AccessRecord.List("Person", 12, filters));

        await using var context = CreateContext();
        var entry = await context.AccessLog.AsNoTracking().SingleAsync();

        entry.Filters.ShouldNotContain(NationalIdValue);
        entry.Filters.ShouldContain("123*****901");
        entry.Filters.ShouldContain("ah***@duzen.com.tr");

        // Hassas olmayan suzgec okunur kalmalidir; aksi hâlde kayit ise yaramaz.
        entry.Filters.ShouldContain("Biyokimya");
    }

    // ------------------------------------------------------------------
    // Butunluk
    // ------------------------------------------------------------------

    [Fact]
    public async Task Access_is_recorded_even_when_the_calling_transaction_is_rolled_back()
    {
        // Cagiran islem geri alinsa bile kullanici veriyi GORMUSTUR. Erisim kaydinin
        // geri alma ile silinmesi izi yaniltici kilardi.
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        await CreateLogger().LogAsync(AccessRecord.View("Person", Guid.CreateVersion7()));

        await transaction.RollbackAsync();

        await using var verification = CreateContext();
        (await verification.AccessLog.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task Access_log_cannot_be_modified_from_the_database()
    {
        await CreateLogger().LogAsync(AccessRecord.View("Person", Guid.CreateVersion7()));

        await using var context = CreateContext();

        var exception = await Should.ThrowAsync<PostgresException>(async () =>
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE audit.access_log SET user_account_id = 999;"));

        exception.MessageText.ShouldContain("degistirilemez");

        await Should.ThrowAsync<PostgresException>(async () =>
            await context.Database.ExecuteSqlRawAsync("DELETE FROM audit.access_log;"));

        await Should.ThrowAsync<PostgresException>(async () =>
            await context.Database.ExecuteSqlRawAsync("TRUNCATE audit.access_log;"));
    }

    [Fact]
    public async Task A_failed_write_is_not_swallowed()
    {
        // Fail-closed: kayit yazilamazsa cagiran veriyi sunmamalidir. Hata yutulsaydi
        // kaydedilmemis bir erisim sessizce gerceklesirdi.
        var brokenOptions = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=yok;Username=yok;Password=yok;Timeout=1")
            .UseSnakeCaseNamingConvention()
            .Options;

        var logger = new AccessLogger(
            new TestContextFactory(brokenOptions), _currentUser, _dateTimeProvider);

        await Should.ThrowAsync<Exception>(async () =>
            await logger.LogAsync(AccessRecord.View("Person", Guid.CreateVersion7())));
    }

    /// <summary>
    /// Uretimde bagimlilik kaydiyla gelen fabrikanin test karsiligi.
    /// </summary>
    private sealed class TestContextFactory(DbContextOptions<HrmsDbContext> options)
        : IDbContextFactory<HrmsDbContext>
    {
        public HrmsDbContext CreateDbContext() => new(options);
    }
}
