using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Dsg.Hrms.Api.IntegrationTests.Data;

/// <summary>
/// Uygulamanin calisma rolu (<c>docker/postgres/app-role.sql</c>; #244, B-01; ADR-0009 §2).
/// </summary>
/// <remarks>
/// <para>
/// Denetim kayitlarinin degistirilemezligi tetikleyiciye dayanir; tetikleyiciyi tablonun
/// sahibi veya super kullanici kapatabilir. Uygulama bu yuzden sahip olmayan bir rolle
/// baglanir. Bu test, dagitimin kullandigi betigin KENDISINI gercek PostgreSQL'de calistirir
/// ve rolun yalnizca izin verilen islemleri yapabildigini olcer.
/// </para>
/// <para>
/// Uygulamanin bu rolle uctan uca calistigi e2e yigininda gosterilir: API orada da ayni
/// rolle baglanir (<c>docker/e2e/up.sh</c>).
/// </para>
/// </remarks>
public sealed class ApplicationRoleTests : IAsyncLifetime
{
    private const string AppRole = "hrms_app";
    private const string AppPassword = "yalnizca-test-parolasi";

    // Sorgular mantiksal degerleri concat_ws ile birlestirir; PostgreSQL onlari t/f olarak yazar.

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("dsg_hrms_app_role")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using (var context = CreateOwnerContext())
        {
            await context.Database.MigrateAsync();
        }

        // Betik iki kez calistirilir: dagitim her surumde bastan calistirir.
        await ApplyRoleScriptAsync();
        await ApplyRoleScriptAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task Role_is_not_a_superuser_and_cannot_create_roles_or_databases()
    {
        var flags = await QueryAsOwnerAsync<string>(
            $"SELECT concat_ws(',', rolsuper, rolcreaterole, rolcreatedb, rolreplication, rolbypassrls) FROM pg_roles WHERE rolname = '{AppRole}'");

        flags.ShouldBe("f,f,f,f,f");
    }

    [Fact]
    public async Task Role_owns_no_table()
    {
        var owned = await QueryAsOwnerAsync<long>(
            $"SELECT count(*) FROM pg_tables WHERE tableowner = '{AppRole}'");

        owned.ShouldBe(0);
    }

    [Fact]
    public async Task Audit_tables_allow_only_select_and_insert()
    {
        foreach (var table in new[] { "audit.change_log", "audit.access_log", "audit.security_event" })
        {
            var privileges = await QueryAsOwnerAsync<string>($"""
                SELECT concat_ws(',',
                  has_table_privilege('{AppRole}', '{table}', 'SELECT'),
                  has_table_privilege('{AppRole}', '{table}', 'INSERT'),
                  has_table_privilege('{AppRole}', '{table}', 'UPDATE'),
                  has_table_privilege('{AppRole}', '{table}', 'DELETE'),
                  has_table_privilege('{AppRole}', '{table}', 'TRUNCATE'))
                """);

            privileges.ShouldBe("t,t,f,f,f", table);
        }
    }

    [Fact]
    public async Task Every_data_table_is_readable_and_writable_by_the_role()
    {
        // Yeni bir tablo yetkisiz kalirsa uygulama ilk sorgusunda "permission denied" alir;
        // bu test onu dagitimdan once yakalar.
        var missing = await QueryAllAsOwnerAsync($"""
            SELECT schemaname || '.' || tablename FROM pg_tables
             WHERE schemaname NOT IN ('pg_catalog', 'information_schema', 'public', 'audit')
               AND NOT (has_table_privilege('{AppRole}', schemaname || '.' || tablename, 'SELECT')
                    AND has_table_privilege('{AppRole}', schemaname || '.' || tablename, 'INSERT')
                    AND has_table_privilege('{AppRole}', schemaname || '.' || tablename, 'UPDATE')
                    AND has_table_privilege('{AppRole}', schemaname || '.' || tablename, 'DELETE'))
            """);

        missing.ShouldBeEmpty();
        (await QueryAsOwnerAsync<long>("SELECT count(*) FROM pg_tables WHERE schemaname = 'personnel'"))
            .ShouldBeGreaterThan(0);
    }

    [Fact]
    public async Task Role_cannot_read_the_migration_history()
    {
        var canRead = await QueryAsOwnerAsync<bool>(
            $"SELECT has_table_privilege('{AppRole}', 'public.__ef_migrations_history', 'SELECT')");

        canRead.ShouldBeFalse();
    }

    [Fact]
    public async Task Overlapping_assignment_ranges_are_rejected_for_the_application_role()
    {
        // SYG-PERS-019 (ADR-0021 §2, B-19): EXCLUDE kisiti calisma roluyle yapilan eklemede de gecerli;
        // rol btree_gist'i kurmaz ama kisitli tabloya yazabilir. Veriler SENTETIKTIR.
        await ExecuteAsOwnerAsync("""
            INSERT INTO organization.company (id, public_id, logo_firm_number, name, created_at) VALUES (901, gen_random_uuid(), 91, 'Test', now());
            INSERT INTO personnel.person (id, public_id, national_id, first_name, last_name, birth_date, is_email_shared, created_at)
                VALUES (901, gen_random_uuid(), '10000000146', 'Test', 'Kisi', '1990-01-01', false, now());
            INSERT INTO personnel.employment (id, public_id, person_id, registry_code, company_id, hire_date, is_active, logo_ref, created_at)
                VALUES (901, gen_random_uuid(), 901, 'T901', 901, '2020-01-01', true, 901, now());
            """);

        const string insert = "INSERT INTO personnel.employment_assignment (public_id, employment_id, company_id, valid_from, valid_to, created_at) VALUES ";
        await ExecuteAsAppAsync(insert + "(gen_random_uuid(), 901, 901, '2026-01-01', '2026-03-01', now())");
        await ExecuteAsAppAsync(insert + "(gen_random_uuid(), 901, 901, '2026-03-01', NULL, now())");

        // Bitisik aralik kabul edilir (ust sinir haric); cakisan aralik ve ikinci acik kayit reddedilir.
        foreach (var range in new[] { "'2026-02-15', '2026-04-01'", "'2026-06-01', NULL" })
        {
            var exception = await Should.ThrowAsync<PostgresException>(() => ExecuteAsAppAsync(insert + $"(gen_random_uuid(), 901, 901, {range}, now())"));
            exception.SqlState.ShouldBe(PostgresErrorCodes.ExclusionViolation, range);
        }

        var empty = await Should.ThrowAsync<PostgresException>(() => ExecuteAsAppAsync(insert + "(gen_random_uuid(), 901, 901, '2025-01-01', '2025-01-01', now())"));
        empty.SqlState.ShouldBe(PostgresErrorCodes.CheckViolation);
    }

    [Fact]
    public async Task Role_can_insert_audit_records_but_cannot_change_them()
    {
        await ExecuteAsAppAsync("""
            INSERT INTO audit.change_log (occurred_at, user_account_id, entity_name, entity_id, operation, changes)
            VALUES (now(), 1, 'Person', gen_random_uuid(), 'Insert', '{}'::jsonb)
            """);

        foreach (var sql in new[]
        {
            "UPDATE audit.change_log SET user_account_id = 999",
            "DELETE FROM audit.change_log",
            "TRUNCATE audit.change_log",
        })
        {
            var exception = await Should.ThrowAsync<PostgresException>(() => ExecuteAsAppAsync(sql));
            exception.SqlState.ShouldBe(PostgresErrorCodes.InsufficientPrivilege, sql);
        }
    }

    [Fact]
    public async Task Role_cannot_disable_the_immutability_trigger_or_change_the_schema()
    {
        foreach (var sql in new[]
        {
            "ALTER TABLE audit.change_log DISABLE TRIGGER ALL",
            "DROP TRIGGER change_log_row_immutable ON audit.change_log",
            "SET session_replication_role = replica",
            "CREATE TABLE personnel.yetkisiz (id int)",
        })
        {
            var exception = await Should.ThrowAsync<PostgresException>(() => ExecuteAsAppAsync(sql));
            exception.SqlState.ShouldBe(PostgresErrorCodes.InsufficientPrivilege, sql);
        }
    }

    [Fact]
    public async Task Tables_added_later_to_an_existing_schema_are_covered_by_default_privileges()
    {
        // Migration'lar sahip rolle uygulanir; yeni tablo betik yeniden calismadan da erisilebilir olmali.
        await ExecuteAsOwnerAsync("CREATE TABLE personnel.later_table (id int); CREATE TABLE audit.later_audit (id int);");

        (await QueryAsOwnerAsync<string>($"""
            SELECT concat_ws(',',
              has_table_privilege('{AppRole}', 'personnel.later_table', 'UPDATE'),
              has_table_privilege('{AppRole}', 'audit.later_audit', 'INSERT'),
              has_table_privilege('{AppRole}', 'audit.later_audit', 'UPDATE'))
            """)).ShouldBe("t,t,f");

        await ExecuteAsOwnerAsync("DROP TABLE personnel.later_table; DROP TABLE audit.later_audit;");
    }

    private async Task ApplyRoleScriptAsync()
    {
        var script = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Data", "app-role.sql"));
        var result = await _container.ExecScriptAsync(
            $"\\set app_role {AppRole}\n\\set app_password '{AppPassword}'\n{script}");

        result.ExitCode.ShouldBe(0, result.Stderr);
    }

    private HrmsDbContext CreateOwnerContext() =>
        new(new DbContextOptionsBuilder<HrmsDbContext>()
            .UseNpgsql(_container.GetConnectionString(), npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .Options);

    private string AppConnectionString() =>
        new NpgsqlConnectionStringBuilder(_container.GetConnectionString())
        {
            Username = AppRole,
            Password = AppPassword,
        }.ConnectionString;

#pragma warning disable CA2100 // Komutlar test sabitleridir; kullanici girdisi yok.
    private async Task ExecuteAsAppAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(AppConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task ExecuteAsOwnerAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<T> QueryAsOwnerAsync<T>(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private async Task<List<string>> QueryAllAsOwnerAsync(string sql)
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync())
        {
            rows.Add(reader.GetString(0));
        }

        return rows;
    }
#pragma warning restore CA2100
}
