using System.Diagnostics;
using System.Net;
using Xunit.Abstractions;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Identity;

/// <summary>
/// KPÖ-KMLK-2: eslesen ve eslesmeyen uyelik isteklerinin medyan yanit suresi farki
/// 1.000 istekte 20 ms'nin altindadir (SYG-KMLK-015).
/// </summary>
/// <remarks>
/// Alt sinir testte 30 ms'ye indirilir (uretimde 1 sn); olculen sey alt sinirin ve
/// isteklerin kendisinin farki gizleyip gizlemedigidir. Istekler sirayla ve donusumlu
/// gonderilir: ayni anda gonderilselerdi birbirlerinin suresini etkilerdi.
/// </remarks>
[Collection(ApiHostGroup.Name)]
public sealed class RegistrationTimingTests : IClassFixture<RegistrationTimingTests.TimingFixture>
{
    private const int RequestsPerKind = 500;
    private const string Base = "/api/v1/identity/registrations";

    private readonly TimingFixture _fixture;
    private readonly ITestOutputHelper _output;

    public RegistrationTimingTests(TimingFixture fixture, ITestOutputHelper output)
    {
        _fixture = fixture;
        _output = output;
    }

    [Fact]
    public async Task Median_response_time_does_not_reveal_a_match()
    {
        using var client = _fixture.CreateClient();
        var match = new List<double>(RequestsPerKind);
        var other = new List<double>(RequestsPerKind);

        // Isinma: ilk istekler JIT ve baglanti havuzu nedeniyle yavastir.
        for (var i = 0; i < 20; i++)
        {
            await MeasureAsync(client, 1);
            await MeasureAsync(client, 99);
        }

        for (var i = 0; i < RequestsPerKind; i++)
        {
            match.Add(await MeasureAsync(client, 1));
            other.Add(await MeasureAsync(client, 99));
        }

        var difference = Math.Abs(Median(match) - Median(other));
        _output.WriteLine($"KPÖ-KMLK-2: eslesen medyan {Median(match):0.00} ms, eslesmeyen medyan {Median(other):0.00} ms, fark {difference:0.00} ms ({RequestsPerKind * 2} istek)");

        match.Min().ShouldBeGreaterThanOrEqualTo(29);
        other.Min().ShouldBeGreaterThanOrEqualTo(29);
        difference.ShouldBeLessThan(20, $"eslesen medyan {Median(match):0.0} ms, eslesmeyen {Median(other):0.0} ms");
    }

    private async Task<double> MeasureAsync(HttpClient client, int index)
    {
        // Saat her istekte ilerletilir: TCKN ve IP basina saatlik sinir olcumu bozmasin.
        _fixture.Clock.Advance(TimeSpan.FromHours(2));

        var body = new
        {
            nationalId = NationalId(index),
            birthDate = "1985-04-12",
            email = index == 1 ? "ahmet.yilmaz@duzen.com.tr" : "yok@duzen.com.tr",
        };

        var stopwatch = Stopwatch.StartNew();
        var (status, _) = await client.PostJsonAsync(Base, body);
        stopwatch.Stop();

        status.ShouldBe(HttpStatusCode.OK);
        return stopwatch.Elapsed.TotalMilliseconds;
    }

    private static double Median(List<double> values)
    {
        var sorted = values.Order().ToList();
        return sorted.Count % 2 == 1 ? sorted[sorted.Count / 2] : (sorted[(sorted.Count / 2) - 1] + sorted[sorted.Count / 2]) / 2;
    }

    /// <summary>Alt siniri 30 ms olan ayri bir uygulama ornegi.</summary>
    public sealed class TimingFixture : RegistrationApiFixture
    {
        public TimingFixture()
        {
            MinimumResponseTime = "00:00:00.030";
        }
    }
}
