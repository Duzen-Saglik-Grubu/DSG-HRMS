using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace Dsg.Hrms.Api.IntegrationTests.Health;

/// <summary>
/// Saglik uc noktalarini <b>gercek PostgreSQL</b> uzerinde dogrular (ADR-0011).
/// </summary>
/// <remarks>
/// Saglik kontrolunun en tehlikeli hâli, yanlis guven vermesidir: veritabani
/// erisilemezken "hazir" diyen bir uc, kapsayici duzenine calismayan bir ornege
/// trafik yonlendirtir. Bu testler tam olarak o durumu olcer.
/// </remarks>
[Collection(ApiHostGroup.Name)]
public sealed class HealthCheckTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("dsg_hrms_health_test")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _factory = CreateFactory(_container.GetConnectionString());
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _container.DisposeAsync();
    }

    private static WebApplicationFactory<Program> CreateFactory(string connectionString)
    {
        var factory = new WebApplicationFactory<Program>();

        // WithWebHostBuilder yeni bir fabrika dondurur ve kaynagini kendisi yonetir;
        // ilk nesne atilmalidir (CA2000).
        using (factory)
        {
            return factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(Environments.Production);
                builder.UseSetting("Database:Hrms", connectionString);

                // Gunluk dosyasi test cikti klasorune yazilir; depo kirletilmez.
                builder.UseSetting("ApplicationLogging:FilePath", "logs/test-.json");
            });
        }
    }

    // ------------------------------------------------------------------
    // Canlilik — bagimlilik yoklamaz
    // ------------------------------------------------------------------

    [Fact]
    public async Task Liveness_reports_healthy_and_checks_no_dependency()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("status").GetString().ShouldBe("Healthy");

        // Canlilik ucu HICBIR kontrol calistirmaz.
        body.GetProperty("checks").GetArrayLength().ShouldBe(0);
    }

    // ------------------------------------------------------------------
    // Senkronizasyon — hazir olmadan AYRI (SYG-KMLK-010, 011)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Sync_endpoint_reports_the_personnel_sync_and_readiness_does_not()
    {
        using var client = _factory.CreateClient();

        var sync = await client.GetFromJsonAsync<JsonElement>(new Uri("/health/sync", UriKind.Relative));
        var ready = await client.GetFromJsonAsync<JsonElement>(new Uri("/health/ready", UriKind.Relative));

        // Testte LOGO tanimli degil: senkronizasyon bilincli olarak devre disi ve saglikli.
        sync.GetProperty("checks").EnumerateArray().Single().GetProperty("name").GetString().ShouldBe("personnel-sync");
        sync.GetProperty("status").GetString().ShouldBe("Healthy");

        // LOGO kesintisi HRMS'in "hazir" durumunu etkilememelidir.
        ready.GetProperty("checks").EnumerateArray()
            .Select(c => c.GetProperty("name").GetString())
            .ShouldNotContain("personnel-sync");
    }

    // ------------------------------------------------------------------
    // Hazir olma — bagimliligi gercekten yoklar
    // ------------------------------------------------------------------

    [Fact]
    public async Task Readiness_reports_the_database_check()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("status").GetString().ShouldBe("Healthy");

        var checks = body.GetProperty("checks").EnumerateArray().ToList();
        checks.Count.ShouldBe(1);
        checks[0].GetProperty("name").GetString().ShouldBe("postgresql");
        checks[0].GetProperty("status").GetString().ShouldBe("Healthy");
        checks[0].GetProperty("durationMs").GetDouble().ShouldBeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Readiness_fails_when_the_database_is_unreachable_but_liveness_still_passes()
    {
        // Bu ayrimin bedeli yuksektir: canlilik ucu veritabanini yoklasaydi,
        // birkac saniyelik bir veritabani kesintisinde kapsayici duzeni uygulamayi
        // YENIDEN BASLATIRDI. Yeniden baslatma veritabanini duzeltmez.
        await using var broken = CreateFactory(
            "Host=127.0.0.1;Port=1;Database=yok;Username=yok;Password=yok;Timeout=2");

        using var client = broken.CreateClient();

        var ready = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));
        ready.StatusCode.ShouldBe(HttpStatusCode.ServiceUnavailable);

        var live = await client.GetAsync(new Uri("/health/live", UriKind.Relative));
        live.StatusCode.ShouldBe(HttpStatusCode.OK, "Bagimlilik cokse de uygulama ayaktadir.");
    }

    // ------------------------------------------------------------------
    // Sizinti — bu uc kimlik dogrulamasi istemez
    // ------------------------------------------------------------------

    [Fact]
    public async Task Health_response_never_leaks_connection_details()
    {
        // Kapsayici duzeni bu ucu kimlik dogrulamadan yoklar; yani yazilan her sey
        // ag icindeki herkese aciktir. Bir baglanti hatasi ise sunucu adini,
        // kullanici adini ve bazen baglanti dizesinin tamamini icerir.
        await using var broken = CreateFactory(
            "Host=127.0.0.1;Port=1;Database=gizli_veritabani;Username=gizli_kullanici;" +
            "Password=CokGizliParola123;Timeout=2");

        using var client = broken.CreateClient();

        var response = await client.GetAsync(new Uri("/health/ready", UriKind.Relative));
        var raw = await response.Content.ReadAsStringAsync();

        raw.ShouldNotContain("CokGizliParola123");
        raw.ShouldNotContain("gizli_kullanici");
        raw.ShouldNotContain("gizli_veritabani");
        raw.ShouldNotContain("127.0.0.1");
        raw.ShouldNotContain("Exception", Case.Insensitive);

        // Yine de tani icin yeterli bilgi vardir: hangi kontrol, hangi durumda.
        raw.ShouldContain("postgresql");
        raw.ShouldContain("Unhealthy");
    }
}
