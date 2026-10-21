using Dsg.Hrms.Api.IntegrationTests.Identity;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Dsg.Hrms.Api.IntegrationTests.Data;

/// <summary>
/// Ad ve soyad Turkce kurala gore siralanir ve aranir (ADR-0004 §10; #244, B-05).
/// </summary>
/// <remarks>
/// LOGO adlari tamamen buyuk harfle yazar (IŞIK, İSMAİL; 21.10.2026 olcumunde 1.373 kisinin
/// tamami). Turkce kural olmadan "ışık" aramasi IŞIK'i bulmuyordu ve Ç/İ/Ö/Ş/Ü Z'den sonra
/// siralaniyordu. Test migration ile kurulan GERCEK kolon kuralini olcer; sorgular hesap
/// listesinin kullandigi ILIKE ve siralamayla aynidir.
/// </remarks>
public sealed class TurkishCollationTests : IAsyncLifetime
{
    private static readonly string[] Surnames = ["ZORLU", "ÇELİK", "ÖZTÜRK", "CAN", "ŞAHİN", "SEZER", "İLHAN", "IŞIK", "ILGAZ", "UĞUR", "ÜNAL"];

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("dsg_hrms_turkish_collation")
        .Build();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        for (var i = 0; i < Surnames.Length; i++)
        {
            context.Add(Person.Create(
                RegistrationApiFixture.NationalId(i + 1),
                new PersonDetails(i == 0 ? "İSMAİL" : "AYŞE", Surnames[i], new DateOnly(1990, 1, 1), null, false, null)));
        }

        await context.SaveChangesAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    [Fact]
    public async Task Name_columns_use_the_Turkish_ICU_collation()
    {
        await using var connection = new NpgsqlConnection(_container.GetConnectionString());
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand(
            "SELECT string_agg(column_name || '=' || collation_name, ',' ORDER BY column_name) FROM information_schema.columns " +
            "WHERE table_schema = 'personnel' AND table_name = 'person' AND column_name IN ('first_name', 'last_name')",
            connection);

        (await command.ExecuteScalarAsync()).ShouldBe($"first_name={DatabaseCollations.Turkish},last_name={DatabaseCollations.Turkish}");
    }

    [Fact]
    public async Task Surnames_are_ordered_by_the_Turkish_alphabet()
    {
        await using var context = CreateContext();

        var ordered = await context.Set<Person>().AsNoTracking()
            .OrderBy(p => p.LastName).Select(p => p.LastName).ToListAsync();

        ordered.ShouldBe(["CAN", "ÇELİK", "ILGAZ", "IŞIK", "İLHAN", "ÖZTÜRK", "SEZER", "ŞAHİN", "UĞUR", "ÜNAL", "ZORLU"]);
    }

    [Theory]
    [InlineData("ışık", "IŞIK")]       // noktasiz ı → I
    [InlineData("işık", null)]         // noktali i, Turkcede I ile eslesmez
    [InlineData("ilhan", "İLHAN")]     // noktali i → İ
    [InlineData("çeli", "ÇELİK")]      // Turkce harfle parca arama
    [InlineData("öztürk", "ÖZTÜRK")]
    public async Task Case_insensitive_search_follows_Turkish_rules(string term, string? expected)
    {
        await using var context = CreateContext();

        var found = await context.Set<Person>().AsNoTracking()
            .Where(p => EF.Functions.ILike(p.LastName, $"%{term}%"))
            .Select(p => p.LastName)
            .ToListAsync();

        if (expected is null)
        {
            found.ShouldBeEmpty();
        }
        else
        {
            found.ShouldBe([expected]);
        }
    }

    [Fact]
    public async Task First_name_with_dotted_capital_i_is_found_by_lowercase_search()
    {
        await using var context = CreateContext();

        (await context.Set<Person>().AsNoTracking().CountAsync(p => EF.Functions.ILike(p.FirstName, "ismail"))).ShouldBe(1);
    }

    private HrmsDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<HrmsDbContext>()
            .UseNpgsql(_container.GetConnectionString(), npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .Options);
}
