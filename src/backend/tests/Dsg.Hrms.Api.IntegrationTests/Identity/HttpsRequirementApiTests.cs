using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Identity;

/// <summary>
/// API yalnizca HTTPS ile gelen istege hizmet verir (SYG-KMLK-063). Gercek HTTP boru hatti;
/// uygulama uretim ortamindaki gibi calisir. Veriler SENTETIKTIR.
/// </summary>
[Collection(ApiHostGroup.Name)]
public sealed class HttpsRequirementApiTests : IClassFixture<RegistrationApiFixture>, IAsyncLifetime
{
    private const string Sessions = "/api/v1/identity/sessions";

    private readonly RegistrationApiFixture _fixture;

    public HttpsRequirementApiTests(RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Plain_http_is_refused_without_a_redirect()
    {
        using var client = _fixture.CreateClient(https: false);

        var (status, body, location) = await SignInAsync(client);

        // Yonlendirme yok: govde zaten sifresiz gonderildi, tarayici POST'u GET'e cevirirdi.
        status.ShouldBe(HttpStatusCode.Forbidden);
        location.ShouldBeNull();
        body.GetProperty("type").GetString()!.ShouldEndWith("https-required");
        body.GetProperty("detail").GetString()!.ShouldContain("güvenli bağlantı");
        body.GetProperty("detail").GetString()!.ShouldNotContain("HTTPS"); // teknik terim yok (SYG-KMLK-064, B-06)
    }

    [Fact]
    public async Task Https_reported_by_the_trusted_proxy_is_accepted()
    {
        // TLS nginx'te sonlanir; API'ye istek duz HTTP ile ve X-Forwarded-Proto ile gelir.
        using var client = _fixture.CreateClient(https: false);
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");

        (await SignInAsync(client)).Status.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Http_reported_by_the_trusted_proxy_is_refused()
    {
        // Sertifika eksik kaldiginda nginx duz HTTP kipine duser ve "http" bildirir.
        using var client = _fixture.CreateClient(https: false);
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "http");

        (await SignInAsync(client)).Status.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Https_claimed_by_an_untrusted_client_is_ignored()
    {
        // Guvenilen vekil aglarinin disindan gelen baslik dikkate alinmaz.
        using var client = _fixture.CreateClient(https: false);
        client.DefaultRequestHeaders.Add("X-Forwarded-Proto", "https");
        client.DefaultRequestHeaders.Add(RemoteAddressHeader, "10.1.2.3");

        (await SignInAsync(client)).Status.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Health_endpoints_stay_available_over_plain_http()
    {
        // Konteyner saglik kontrolu konteyner icinden duz HTTP ile cagirir.
        using var client = _fixture.CreateClient(https: false);

        using var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body, Uri? Location)> SignInAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            new Uri(Sessions, UriKind.Relative),
            new { email = "ahmet.yilmaz@duzen.com.tr", password = "Yanlis parola 1" });
        var text = await response.Content.ReadAsStringAsync();
        var body = string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone();
        return (response.StatusCode, body, response.Headers.Location);
    }
}
