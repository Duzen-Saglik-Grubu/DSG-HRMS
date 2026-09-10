using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Infrastructure.Data;
using Dsg.Hrms.Infrastructure.Data.Interceptors;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace Dsg.Hrms.Api.IntegrationTests.Data;

/// <summary>
/// ADR-0004'teki veritabani kurallarini <b>gercek PostgreSQL</b> uzerinde dogrular.
/// </summary>
/// <remarks>
/// EF Core In-Memory saglayicisi kullanilmaz (ADR-0011 §3): kisitlari, kismi
/// dizinleri, <c>xmin</c> eszamanlilik denetimini ve tip donusumlerini uygulamaz.
/// Bu testlerin tamami In-Memory ile "gecerdi" ama uretimde bozuk olabilirdi.
/// </remarks>
public sealed class DatabaseConventionTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("dsg_hrms_test")
        .Build();

    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private static readonly DateTimeOffset FixedInstant =
        new(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _dateTimeProvider.UtcNow.Returns(FixedInstant);
        _currentUser.UserId.Returns((long?)42);

        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    private SampleDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SampleDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new AuditFieldsInterceptor(_currentUser, _dateTimeProvider))
            .Options;

        return new SampleDbContext(options);
    }

    // ------------------------------------------------------------------
    // ADR-0004 §1 — Adlandirma
    // ------------------------------------------------------------------

    [Fact]
    public async Task Column_names_are_generated_in_snake_case()
    {
        await using var context = CreateContext();

        var columns = await context.Database
            .SqlQuery<string>($"""
                SELECT column_name AS "Value"
                FROM information_schema.columns
                WHERE table_name = 'sample_record'
                """)
            .ToListAsync();

        columns.ShouldContain("public_id");
        columns.ShouldContain("created_at");
        columns.ShouldContain("created_by");
        columns.ShouldContain("deleted_at");
        columns.ShouldContain("valid_on");

        // PascalCase kolon uretilmemeli
        columns.ShouldNotContain("PublicId");
    }

    // ------------------------------------------------------------------
    // ADR-0004 §3 — Veri tipleri
    // ------------------------------------------------------------------

    [Fact]
    public async Task Timestamps_use_timestamptz_and_dates_use_date()
    {
        await using var context = CreateContext();

        var columns = await context.Database
            .SqlQuery<ColumnType>($"""
                SELECT column_name AS "Name", data_type AS "Type"
                FROM information_schema.columns
                WHERE table_name = 'sample_record'
                """)
            .ToListAsync();

        static string TypeOf(List<ColumnType> all, string name) =>
            all.Single(c => c.Name == name).Type;

        // Zaman damgasi: saat dilimi BILGISIYLE saklanir. Aksi hâlde UTC/yerel
        // ayrimi kaybolur ve gecmis kayitlar yanlis yorumlanir.
        TypeOf(columns, "created_at").ShouldBe("timestamp with time zone");
        TypeOf(columns, "deleted_at").ShouldBe("timestamp with time zone");

        // Takvim gunu: saat bilgisi TASIMAZ. Izin gunu gibi kavramlar saat
        // diliminden etkilenmemelidir.
        TypeOf(columns, "valid_on").ShouldBe("date");

        // Metin: PostgreSQL'de text tercih edilir.
        TypeOf(columns, "title").ShouldBe("text");

        // Para: kayan noktali tip degil.
        TypeOf(columns, "amount").ShouldBe("numeric");
    }

    // ------------------------------------------------------------------
    // ADR-0004 §4 — Denetim alanlari
    // ------------------------------------------------------------------

    [Fact]
    public async Task Audit_fields_are_filled_automatically_on_insert()
    {
        await using var context = CreateContext();
        var record = new SampleRecord { Title = "Olusturma denemesi" };

        context.SampleRecords.Add(record);
        await context.SaveChangesAsync();

        record.CreatedAt.ShouldBe(FixedInstant);
        record.CreatedBy.ShouldBe(42);
        record.UpdatedAt.ShouldBeNull();
        record.PublicId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Creation_info_cannot_be_changed_on_update()
    {
        await using var context = CreateContext();
        var record = new SampleRecord { Title = "Ilk hâli" };
        context.SampleRecords.Add(record);
        await context.SaveChangesAsync();

        var originalCreatedAt = record.CreatedAt;
        var originalCreatedBy = record.CreatedBy;

        // Baska bir kullanici, baska bir anda gunceller.
        var laterInstant = FixedInstant.AddHours(3);
        _dateTimeProvider.UtcNow.Returns(laterInstant);
        _currentUser.UserId.Returns((long?)99);

        record.Title = "Guncellenmis hâli";
        // Olusturma bilgisini KASITLI olarak bozmaya calisiyoruz.
        record.CreatedBy = 12345;
        await context.SaveChangesAsync();

        // Denetim izinin guvenilirligi, olusturma bilgisinin degismezligine dayanir.
        var stored = await context.SampleRecords
            .AsNoTracking()
            .SingleAsync(r => r.Id == record.Id);

        stored.CreatedAt.ShouldBe(originalCreatedAt);
        stored.CreatedBy.ShouldBe(originalCreatedBy);
        stored.UpdatedAt.ShouldBe(laterInstant);
        stored.UpdatedBy.ShouldBe(99);
    }

    // ------------------------------------------------------------------
    // ADR-0004 §5 — Yumusak silme
    // ------------------------------------------------------------------

    [Fact]
    public async Task Delete_marks_the_row_instead_of_removing_it()
    {
        await using var context = CreateContext();
        var record = new SampleRecord { Title = "Silinecek" };
        context.SampleRecords.Add(record);
        await context.SaveChangesAsync();
        var id = record.Id;

        context.SampleRecords.Remove(record);
        await context.SaveChangesAsync();

        // Satir fiziksel olarak DURUYOR olmali.
        var remainingRowCount = await context.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM sample_record WHERE id = {id}")
            .SingleAsync();

        remainingRowCount.ShouldBe(1, "Is kayitlari fiziksel olarak silinmez, isaretlenir.");

        // Ancak normal sorgulara GIRMEMELI.
        var filtered = await context.SampleRecords.SingleOrDefaultAsync(r => r.Id == id);
        filtered.ShouldBeNull("Silinmis kayit varsayilan sorgulara girmemelidir.");

        // Suzgec kapatildiginda gorunmeli ve silme bilgisi dolu olmali.
        var unfiltered = await context.SampleRecords
            .IgnoreQueryFilters()
            .SingleAsync(r => r.Id == id);

        unfiltered.DeletedAt.ShouldNotBeNull();
        unfiltered.DeletedBy.ShouldBe(42);
    }

    // ------------------------------------------------------------------
    // ADR-0004 §4 / ADR-0010 §7 — Eszamanlilik
    // ------------------------------------------------------------------

    [Fact]
    public async Task Concurrent_update_of_the_same_row_fails_for_the_second_user()
    {
        await using var setup = CreateContext();
        var record = new SampleRecord { Title = "Yaris" };
        setup.SampleRecords.Add(record);
        await setup.SaveChangesAsync();

        // Iki kullanici ayni kaydi ayni anda acar.
        await using var first = CreateContext();
        await using var second = CreateContext();

        var firstCopy = await first.SampleRecords.SingleAsync(r => r.Id == record.Id);
        var secondCopy = await second.SampleRecords.SingleAsync(r => r.Id == record.Id);

        firstCopy.Title = "Birincinin degisikligi";
        await first.SaveChangesAsync();

        secondCopy.Title = "Ikincinin degisikligi";

        // Ikinci kullanicinin degisikligi SESSIZCE uzerine yazmamalidir.
        await Should.ThrowAsync<DbUpdateConcurrencyException>(
            async () => await second.SaveChangesAsync());
    }

    // ------------------------------------------------------------------
    // Migration hatti
    // ------------------------------------------------------------------

    [Fact]
    public async Task Production_migrations_apply_cleanly()
    {
        // Bos bir veritabaninda migration'lar sorunsuz calismalidir.
        // Bu, migration hattinin kendisini dogrular (ADR-0011 §3).
        await using var container = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("migration_check")
            .Build();

        await container.StartAsync();

        try
        {
            var options = new DbContextOptionsBuilder<HrmsDbContext>()
                .UseNpgsql(container.GetConnectionString(), npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "public"))
                .UseSnakeCaseNamingConvention()
                .Options;

            await using var context = new HrmsDbContext(options);

            await Should.NotThrowAsync(async () => await context.Database.MigrateAsync());

            var applied = await context.Database.GetAppliedMigrationsAsync();
            applied.ShouldNotBeEmpty("En az bir migration uygulanmis olmalidir.");
        }
        finally
        {
            await container.DisposeAsync();
        }
    }

    private sealed record ColumnType(string Name, string Type);
}
