using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Dsg.Hrms.Api.IntegrationTests.Data;

/// <summary>
/// UAT dagitiminin kullandigi idempotent sema betigi (runbook §3.3, <c>KR-065</c>).
/// </summary>
/// <remarks>
/// <para>
/// Diger testler semayi <c>Migrate()</c> ile kurar; dagitim ise
/// <c>dotnet ef migrations script --idempotent</c> ciktisini <c>psql</c> ile uygular. Iki yol
/// farklidir: idempotent betik her adimi bir PL/pgSQL blogunun icine sarar. Migration'daki
/// ham SQL <c>Migrate()</c> ile calisip betikte hata verebilir; bu bir kez yasandi (#100:
/// sonucu kullanilmayan <c>SELECT</c>). Bu test hatayi dagitimdan once yakalar.
/// </para>
/// <para>
/// Betik bos veritabanina IKI KEZ uygulanir: ikinci uygulama hicbir sey degistirmeden
/// gecmelidir (dagitim her surumde betigi bastan calistirir).
/// </para>
/// </remarks>
public sealed class IdempotentSchemaScriptTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("dsg_hrms_script_test")
        .Build();

    public Task InitializeAsync() => _container.StartAsync();

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task Idempotent_script_applies_to_an_empty_database_and_can_be_reapplied()
    {
        var script = GenerateScript();

        await ExecuteAsync(script);
        await ExecuteAsync(script);

        await using var context = CreateContext();
        (await context.Database.GetPendingMigrationsAsync()).ShouldBeEmpty();
        (await context.Database.GetAppliedMigrationsAsync()).Count()
            .ShouldBe(context.Database.GetMigrations().Count());
    }

    private string GenerateScript()
    {
        using var context = CreateContext();
        return context.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
    }

    private async Task ExecuteAsync(string script)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
#pragma warning disable CA2100 // Betik kullanici girdisi degil; EF Core migration ciktisidir.
        await using var command = new NpgsqlCommand(script, connection);
#pragma warning restore CA2100
        await command.ExecuteNonQueryAsync();
    }

    private HrmsDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<HrmsDbContext>()
            .UseNpgsql(_container.GetConnectionString(), npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .Options);
}
