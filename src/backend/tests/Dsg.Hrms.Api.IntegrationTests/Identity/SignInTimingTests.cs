using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Identity;

/// <summary>
/// Uyelikteki en kisa yanit suresi (SYG-KMLK-015) giris ucuna uygulanmaz (SYG-KMLK-077, #120).
/// </summary>
/// <remarks>
/// Alt sinir bu ornekte 2 saniyedir. Uyelik onu bekler, giris beklemez. Giris yine de parola
/// ozetini her durumda hesaplar (SYG-KMLK-032); var olan ve olmayan kullanici ayni isi yapar.
/// </remarks>
[Collection(ApiHostGroup.Name)]
public sealed class SignInTimingTests : IClassFixture<SignInTimingTests.SlowFloorFixture>
{
    private static readonly TimeSpan Floor = TimeSpan.FromSeconds(2);

    private readonly SlowFloorFixture _fixture;

    public SignInTimingTests(SlowFloorFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Sign_in_is_not_padded_to_the_registration_floor()
    {
        using var client = _fixture.CreateClient();
        await TimeAsync(() => SignInAsync(client)); // isinma

        var signIn = await TimeAsync(() => SignInAsync(client));
        var registration = await TimeAsync(() => client.PostAsJsonAsync(
            new Uri("/api/v1/identity/registrations", UriKind.Relative),
            new { nationalId = NationalId(1), birthDate = "1990-01-01", email = "ahmet.yilmaz@duzen.com.tr" }));

        registration.ShouldBeGreaterThanOrEqualTo(Floor);
        signIn.ShouldBeLessThan(Floor);
    }

    private static Task<HttpResponseMessage> SignInAsync(HttpClient client) =>
        client.PostAsJsonAsync(new Uri("/api/v1/identity/sessions", UriKind.Relative), new { email = "yok.boyle@duzen.com.tr", password = "Yanlis parola 1" });

    private static async Task<TimeSpan> TimeAsync(Func<Task<HttpResponseMessage>> request)
    {
        var watch = Stopwatch.StartNew();
        using var response = await request();
        watch.Stop();
        response.StatusCode.ShouldNotBe(HttpStatusCode.InternalServerError);
        return watch.Elapsed;
    }

    /// <summary>Alt siniri 2 saniye olan ayri bir uygulama ornegi.</summary>
    public sealed class SlowFloorFixture : RegistrationApiFixture
    {
        public SlowFloorFixture()
        {
            MinimumResponseTime = "00:00:02";
        }
    }
}
