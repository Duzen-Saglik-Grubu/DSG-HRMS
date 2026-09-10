using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Dsg.Hrms.Api.IntegrationTests.Audit;

/// <summary>
/// Denetim izinin <b>veritabani duzeyinde</b> degistirilemez oldugunu dogrular
/// (ADR-0009 §2).
/// </summary>
/// <remarks>
/// <para>
/// Uygulama katmanindaki denetim yalnizca uygulama uzerinden gelen islemleri kapsar.
/// Bir yonetim araciyla veritabanina dogrudan baglanan kisi, uygulamayi tamamen
/// atlayarak denetim izini degistirebilirdi. KVKK denetiminde sorulan soru "kaydi
/// tutuyor musunuz" degil, "kaydin degistirilmedigini nasil biliyorsunuz" sorusudur.
/// </para>
/// <para>
/// Bu testler <b>migration ile</b> kurulan veritabanina karsi calisir; boylece
/// uretimde gercekten uygulanacak tetikleyici olculur.
/// </para>
/// </remarks>
public sealed class AuditImmutabilityTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("dsg_hrms_audit_immutability")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        // Uygulamayi atlayarak dogrudan bir denetim kaydi eklenir; ekleme SERBESTTIR.
        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO audit.change_log
                (occurred_at, user_account_id, entity_name, entity_id, operation, changes)
            VALUES
                (now(), 1, 'Person', gen_random_uuid(), 'Insert', '{{}}'::jsonb);
            """);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    private HrmsDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseNpgsql(_container.GetConnectionString(), npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;

        return new HrmsDbContext(options);
    }

    [Fact]
    public async Task Direct_update_is_rejected_by_the_database()
    {
        await using var context = CreateContext();

        var exception = await Should.ThrowAsync<PostgresException>(async () =>
            await context.Database.ExecuteSqlRawAsync(
                "UPDATE audit.change_log SET user_account_id = 999;"));

        exception.MessageText.ShouldContain("degistirilemez");
    }

    [Fact]
    public async Task Direct_delete_is_rejected_by_the_database()
    {
        await using var context = CreateContext();

        await Should.ThrowAsync<PostgresException>(async () =>
            await context.Database.ExecuteSqlRawAsync("DELETE FROM audit.change_log;"));
    }

    [Fact]
    public async Task Truncate_is_rejected_by_the_database()
    {
        // TRUNCATE satir bazli tetikleyiciyi calistirmaz; ayri bir ifade bazli
        // tetikleyici olmasaydi tek komutla tum denetim izi silinebilirdi.
        await using var context = CreateContext();

        await Should.ThrowAsync<PostgresException>(async () =>
            await context.Database.ExecuteSqlRawAsync("TRUNCATE audit.change_log;"));
    }

    [Fact]
    public async Task Insert_is_still_allowed()
    {
        // Koruma "hicbir sey yazilamaz" anlamina gelmemelidir; aksi hâlde denetim
        // izinin kendisi calismazdi.
        await using var context = CreateContext();

        await Should.NotThrowAsync(async () =>
            await context.Database.ExecuteSqlRawAsync("""
                INSERT INTO audit.change_log
                    (occurred_at, entity_name, entity_id, operation, changes)
                VALUES
                    (now(), 'Person', gen_random_uuid(), 'Update', '{{}}'::jsonb);
                """));

        (await context.ChangeLog.CountAsync()).ShouldBe(2);
    }
}
