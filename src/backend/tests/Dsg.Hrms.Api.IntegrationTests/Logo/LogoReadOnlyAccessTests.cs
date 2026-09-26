using System.Globalization;
using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Infrastructure.Logo;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Testcontainers.MsSql;

namespace Dsg.Hrms.Api.IntegrationTests.Logo;

/// <summary>
/// LOGO'ya yazmanin reddedildiginin <b>gercek SQL Server</b> uzerinde kanitlanmasi
/// (SYG-KMLK-002, 003, 012; <c>KR-003</c>, <c>KR-004</c>).
/// </summary>
/// <remarks>
/// <para>
/// Salt-okunur oturum, depodaki <c>docker/logo/salt-okunur-oturum.sql</c> betiginin
/// KENDISI calistirilarak olusturulur. Boylece sinanan sey, uretimde kullanilan
/// betiktir; bir kopyasi degil.
/// </para>
/// <para>
/// SQL Server <b>Express</b> surumuyle calisir: ucretsizdir ve kosulsuz kullanilir
/// (<c>KR-025</c>). Canli LOGO ile ayni ana surum (2019).
/// </para>
/// <para>
/// Bu testler yazma yetkisinin <b>mekanizmasini</b> kanitlar. Canli oturumun
/// yetkilerini ise senkronizasyon her calismadan once, hicbir sey yazmadan denetler
/// (<see cref="LogoPersonnelSource.VerifyReadOnlyAccessAsync"/>).
/// </para>
/// <para>Test verisi SENTETIKTIR (KVKK).</para>
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "CA2100", Justification = "Test SQL'i yalnizca bu siniftaki sabitlerden ve depodaki betik dosyalarindan uretilir; kullanici girdisi yoktur.")]
public sealed class LogoReadOnlyAccessTests : IAsyncLifetime
{
    private const string Database = "BORDRO";
    private const string ReaderLogin = "hrms_logo_reader";
    private const string ReaderPassword = "Okuyucu!Test2026";
    private const string WriterLogin = "hrms_logo_writer_control";
    private const string WriterPassword = "Yazici!Test2026";

    /// <summary>SQL Server "izin reddedildi" hata numarasi.</summary>
    private const int PermissionDenied = 229;

    /// <summary>SQL Server "nesne bulunamadi veya yetkiniz yok" hata numarasi (yetkisiz ALTER).</summary>
    private const int CannotFindObjectOrNoPermission = 1088;

    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2019-latest")
        .WithEnvironment("MSSQL_PID", "Express")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await ExecuteAsync(AdminConnection("master"), $"CREATE DATABASE [{Database}]");
        await ExecuteScriptAsync(AdminConnection(Database), await File.ReadAllTextAsync(TestFile("logo-test-schema.sql")));
        await SeedAsync();

        // URETIMDEKI betik, degiskenleri doldurularak calistirilir.
        var script = (await File.ReadAllTextAsync(TestFile("salt-okunur-oturum.sql")))
            .Replace("$(LogoDatabase)", Database, StringComparison.Ordinal)
            .Replace("$(ReaderLogin)", ReaderLogin, StringComparison.Ordinal)
            .Replace("$(ReaderPassword)", ReaderPassword, StringComparison.Ordinal);
        await ExecuteScriptAsync(AdminConnection("master"), script);

        // Negatif kontrol: DENY'siz, yazma yetkili bir oturum. Yetki denetiminin gercekten
        // yetki tespit edebildigini gostermek icin.
        await ExecuteScriptAsync(AdminConnection(Database), $"""
            CREATE LOGIN [{WriterLogin}] WITH PASSWORD = N'{WriterPassword}', CHECK_POLICY = OFF;
            GO
            CREATE USER [{WriterLogin}] FOR LOGIN [{WriterLogin}];
            GO
            ALTER ROLE [db_datareader] ADD MEMBER [{WriterLogin}];
            ALTER ROLE [db_datawriter] ADD MEMBER [{WriterLogin}];
            GO
            """);
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    // ------------------------------------------------------------------ SYG-KMLK-003

    [Theory]
    [InlineData("INSERT INTO LH_001_PERSON (LREF, CODE, TTFNO, INDATE, FIRMNR) VALUES (999, '99999', '', '2020-01-01', 1)")]
    [InlineData("UPDATE LH_001_PERSON SET NAME = 'Degisti' WHERE LREF = 1")]
    [InlineData("DELETE FROM LH_001_PERSON WHERE LREF = 1")]
    [InlineData("UPDATE LH_001_CONTACT SET EXP1 = 'x@duzen.com.tr' WHERE LREF = 10")]
    [InlineData("INSERT INTO L_CAPIFIRM (NR, NAME) VALUES (99, 'Yeni Firma')")]
    public async Task Read_only_login_cannot_write(string statement)
    {
        // KR-003: LOGO'ya yazma, guncelleme ve silme KESINLIKLE yapilamaz.
        var ex = await Should.ThrowAsync<SqlException>(() => ExecuteAsync(ReaderConnection(), statement));

        ex.Number.ShouldBe(PermissionDenied);
        (await CountAsync("SELECT COUNT(*) FROM LH_001_PERSON")).ShouldBe(3);
        (await ScalarAsync("SELECT NAME FROM LH_001_PERSON WHERE LREF = 1")).ShouldBe("Ahmet");
    }

    [Fact]
    public async Task Read_only_login_cannot_alter_the_schema()
    {
        var ex = await Should.ThrowAsync<SqlException>(() =>
            ExecuteAsync(ReaderConnection(), "ALTER TABLE LH_001_PERSON ADD HRMS_KOLON int NULL"));

        // SQL Server yetkisiz ALTER icin 229 degil 1088 doner ("nesne bulunamadi veya
        // yetkiniz yok"): nesnenin varligini da ele vermemek icin. Asil kanit, kolonun
        // EKLENMEMIS olmasidir.
        ex.Number.ShouldBeOneOf(PermissionDenied, CannotFindObjectOrNoPermission);
        (await CountAsync(
            "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'LH_001_PERSON' AND COLUMN_NAME = 'HRMS_KOLON'"))
            .ShouldBe(0);
    }

    [Fact]
    public async Task Read_only_login_can_read()
    {
        (await CountAsync("SELECT COUNT(*) FROM LH_001_PERSON", ReaderConnection())).ShouldBe(3);
    }

    [Fact]
    public async Task Logo_context_refuses_to_save_even_on_a_writable_connection()
    {
        // Ikinci kilit veritabanindan BAGIMSIZDIR: DENY bir gun kaldirilsa bile baglam
        // yazmaz.
        await using var context = CreateContext(AdminConnection(Database));

        await Should.ThrowAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        Should.Throw<InvalidOperationException>(() => context.SaveChanges());
    }

    // ------------------------------------------------------------------ yetki denetimi

    [Fact]
    public async Task Access_check_confirms_the_read_only_login()
    {
        var result = await WithSourceAsync(ReaderConnection(), source => source.VerifyReadOnlyAccessAsync(CancellationToken.None));

        result.IsReadOnly.ShouldBeTrue();
        result.GrantedWritePermissions.ShouldBeEmpty();
    }

    [Fact]
    public async Task Access_check_detects_a_writable_login()
    {
        // Negatif kontrol: denetim her seye "salt okunur" demiyor, yetkiyi gercekten goruyor.
        var result = await WithSourceAsync(WriterConnection(), source => source.VerifyReadOnlyAccessAsync(CancellationToken.None));

        result.IsReadOnly.ShouldBeFalse();
        result.GrantedWritePermissions.ShouldContain("LH_001_PERSON:INSERT");
        result.GrantedWritePermissions.ShouldContain("LH_001_PERSON:UPDATE");
        result.GrantedWritePermissions.ShouldContain("LH_001_PERSON:DELETE");
    }

    [Fact]
    public async Task Access_check_writes_nothing()
    {
        await WithSourceAsync(WriterConnection(), source => source.VerifyReadOnlyAccessAsync(CancellationToken.None));

        (await CountAsync("SELECT COUNT(*) FROM LH_001_PERSON")).ShouldBe(3);
    }

    // ------------------------------------------------------------------ SYG-KMLK-012

    [Fact]
    public async Task Schema_check_accepts_the_expected_schema()
    {
        var result = await WithSourceAsync(ReaderConnection(), source => source.VerifySchemaAsync(CancellationToken.None));

        result.IsCompatible.ShouldBeTrue(string.Join("; ", result.Problems));
    }

    [Fact]
    public async Task Schema_check_detects_a_changed_type_and_a_missing_column()
    {
        // Sapma ayri bir veritabaninda uretilir; diger testlerin semasi bozulmaz.
        const string driftDb = "BORDRO_SAPMA";
        await ExecuteAsync(AdminConnection("master"), $"CREATE DATABASE [{driftDb}]");
        await ExecuteScriptAsync(AdminConnection(driftDb), await File.ReadAllTextAsync(TestFile("logo-test-schema.sql")));
        await ExecuteAsync(AdminConnection(driftDb), "ALTER TABLE LH_001_PERSON ALTER COLUMN TTFNO nvarchar(21) NULL");
        await ExecuteAsync(AdminConnection(driftDb), "ALTER TABLE LH_001_CONTACT DROP COLUMN TYP");

        var result = await WithSourceAsync(AdminConnection(driftDb), source => source.VerifySchemaAsync(CancellationToken.None));

        result.IsCompatible.ShouldBeFalse();
        result.Problems.ShouldContain("LH_001_PERSON.TTFNO: tip nvarchar, beklenen varchar");
        result.Problems.ShouldContain("LH_001_CONTACT.TYP: bulunamadi");
    }

    // ------------------------------------------------------------------ okuma

    [Fact]
    public async Task Cards_are_read_and_mapped()
    {
        var records = await WithSourceAsync(ReaderConnection(), source => source.GetAllAsync(CancellationToken.None));

        records.Count.ShouldBe(3);

        var first = records.Single(r => r.LogoRef == 1);
        first.RegistryCode.ShouldBe("00001");               // bosluklar atildi
        first.NationalId.ShouldBe(NationalId(1));
        first.FirstName.ShouldBe("Ahmet");
        first.BirthDate.ShouldBe(new DateOnly(1985, 4, 12));
        first.HireDate.ShouldBe(new DateOnly(2020, 1, 1));
        first.TerminationDate.ShouldBeNull();
        first.FirmNumber.ShouldBe((short)1);
        first.FirmName.ShouldBe("Duzen Laboratuvarlar");

        // Kaynak sirasi LREF'tir; ekleme sirasi degil. Senkronizasyon ilkini kullanir.
        first.Emails.ShouldBe(["ilk@duzen.com.tr", "ikinci@duzen.com.tr"]);
        first.MobilePhones.ShouldBe(["0532 123 45 67"]);

        var left = records.Single(r => r.LogoRef == 2);
        left.TerminationDate.ShouldBe(new DateOnly(2024, 6, 30));
        left.NationalId.ShouldBeNull();                     // bos TCKN null olur
        left.Emails.ShouldBeEmpty();                        // tur 1 kayit alinmaz

        records.Single(r => r.LogoRef == 3).FirmName.ShouldBeNull(); // firma listesinde yok
    }

    [Fact]
    public async Task Unreachable_logo_becomes_a_source_unavailable_error()
    {
        var unreachable = new SqlConnectionStringBuilder
        {
            DataSource = "127.0.0.1,1",
            InitialCatalog = Database,
            UserID = ReaderLogin,
            Password = ReaderPassword,
            ConnectTimeout = 2,
            TrustServerCertificate = true,
        }.ConnectionString;

        await Should.ThrowAsync<LogoUnavailableException>(() =>
            WithSourceAsync(unreachable, source => source.VerifyReadOnlyAccessAsync(CancellationToken.None), retry: false));
    }

    // ------------------------------------------------------------------ yardimcilar

    private async Task SeedAsync()
    {
        var sql = string.Create(CultureInfo.InvariantCulture, $"""
            INSERT INTO L_CAPIFIRM (NR, NAME) VALUES (1, 'Duzen Laboratuvarlar'), (2, 'Zeytinim');

            INSERT INTO LH_001_PERSON (LREF, CODE, TTFNO, NAME, SURNAME, BIRTHDATE, INDATE, OUTDATE, FIRMNR) VALUES
                (1, '  00001 ', '{NationalId(1)}', 'Ahmet', 'Yilmaz', '1985-04-12', '2020-01-01', NULL, 1),
                (2, '00002', '', 'Ayse', 'Demir', '1990-02-03', '2019-05-01', '2024-06-30', 2),
                (3, '00003', '{NationalId(3)}', 'Mehmet', 'Kaya', '1988-08-08', '2021-03-01', NULL, 9);

            -- LREF sirasi ekleme sirasindan FARKLI: kaynak sirasi LREF olmali.
            INSERT INTO LH_001_CONTACT (LREF, CARDREF, TYP, EXP1) VALUES
                (20, 1, 6, 'ikinci@duzen.com.tr'),
                (10, 1, 6, 'ilk@duzen.com.tr'),
                (30, 1, 3, '0532 123 45 67'),
                (40, 2, 1, 'sabit hat'),
                (50, 2, 6, '   ');
            """);
        await ExecuteAsync(AdminConnection(Database), sql);
    }

    private string AdminConnection(string database) =>
        new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = database,
            TrustServerCertificate = true,
        }.ConnectionString;

    private string ReaderConnection() => LoginConnection(ReaderLogin, ReaderPassword);

    private string WriterConnection() => LoginConnection(WriterLogin, WriterPassword);

    private string LoginConnection(string login, string password) =>
        new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = Database,
            UserID = login,
            Password = password,
            TrustServerCertificate = true,
        }.ConnectionString;

    private static LogoDbContext CreateContext(string connectionString, bool retry = true) =>
        new(new DbContextOptionsBuilder<LogoDbContext>()
            .UseSqlServer(connectionString, sql =>
            {
                if (retry)
                {
                    sql.EnableRetryOnFailure(3);
                }
            })
            .Options);

    /// <summary>Kaynagi olusturur, islemi calistirir ve baglami kapatir.</summary>
    private static async Task<T> WithSourceAsync<T>(string connectionString, Func<LogoPersonnelSource, Task<T>> action, bool retry = true)
    {
        await using var context = CreateContext(connectionString, retry);
        return await action(new LogoPersonnelSource(context, NullLogger<LogoPersonnelSource>.Instance));
    }

    private static string TestFile(string name) => Path.Combine(AppContext.BaseDirectory, "Logo", name);

    private static async Task ExecuteAsync(string connectionString, string sql)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>"GO" ile ayrilmis toplu isleri sirayla calistirir (sqlcmd gibi).</summary>
    private static async Task ExecuteScriptAsync(string connectionString, string script)
    {
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        var batches = System.Text.RegularExpressions.Regex.Split(script, @"^\s*GO\s*$",
            System.Text.RegularExpressions.RegexOptions.Multiline | System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (var batch in batches.Where(b => !string.IsNullOrWhiteSpace(StripComments(b))))
        {
            await using var command = new SqlCommand(batch, connection);
            await command.ExecuteNonQueryAsync();
        }
    }

    private static string StripComments(string batch) =>
        string.Join('\n', batch.Split('\n').Where(line => !line.TrimStart().StartsWith("--", StringComparison.Ordinal)));

    private async Task<int> CountAsync(string sql, string? connectionString = null) =>
        Convert.ToInt32(await ScalarAsync(sql, connectionString), CultureInfo.InvariantCulture);

    private async Task<object?> ScalarAsync(string sql, string? connectionString = null)
    {
        await using var connection = new SqlConnection(connectionString ?? AdminConnection(Database));
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        return await command.ExecuteScalarAsync();
    }

    private static string NationalId(int index)
    {
        var firstNine = (100000000 + index).ToString(CultureInfo.InvariantCulture);
        var d = firstNine.Select(c => c - '0').ToArray();
        var tenth = ((((d[0] + d[2] + d[4] + d[6] + d[8]) * 7) - (d[1] + d[3] + d[5] + d[7])) % 10 + 10) % 10;
        var eleventh = (d.Sum() + tenth) % 10;
        return $"{firstNine}{tenth}{eleventh}";
    }
}
