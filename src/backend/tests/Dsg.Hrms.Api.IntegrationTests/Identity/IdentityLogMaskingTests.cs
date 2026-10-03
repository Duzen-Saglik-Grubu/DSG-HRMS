using System.Net;
using System.Net.Http.Json;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Identity;

/// <summary>
/// Kimlik akislarinin uygulama gunlugunde TCKN, telefon, e-posta, dogrulama kodu ve parola
/// gorunmez (SYG-KMLK-061, 026; KR-059). Gercek HTTP boru hatti; gunluk, uretimdeki gibi
/// dosyaya yazilir ve dosyanin kendisi okunur. Veriler SENTETIKTIR.
/// </summary>
/// <remarks>
/// Ayni dosyaya diger test siniflari da yazar ve hepsi ayni sentetik kisileri kullanir. Bu
/// bilinclidir: sizinti hangi testte olursa olsun bu test duser.
/// </remarks>
[Collection(ApiHostGroup.Name)]
public sealed class IdentityLogMaskingTests : IClassFixture<RegistrationApiFixture>, IAsyncLifetime
{
    private const string Password = "Kediler uyur 7";
    private const string NewPassword = "Mavi deniz 42 kez";

    private static readonly string[] Emails =
        ["ahmet.yilmaz@duzen.com.tr", "mehmet.kaya@duzen.com.tr", "ortak@duzen.com.tr", "ayrilan@duzen.com.tr", "kisisel@gmail.com"];

    private static readonly string[] Phones = ["5321234567", "5321234568", "5321234569", "5321234570"];

    private readonly RegistrationApiFixture _fixture;

    public IdentityLogMaskingTests(RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Identity_flows_write_no_personal_data_codes_or_passwords_to_the_log()
    {
        var startedAt = DateTime.UtcNow.AddSeconds(-1);
        var codes = new List<string>();
        using var client = _fixture.CreateClient(forwardedFor: "10.1.1.1");

        // Uyelik: e-posta kanaliyla, bir yanlis kod ve hesap olusturma.
        var (_, started) = await client.PostJsonAsync("/api/v1/identity/registrations", new { nationalId = NationalId(1), birthDate = "1985-04-12", email = Emails[0] });
        var id = started.GetProperty("registrationId").GetString();
        await client.PostJsonAsync($"/api/v1/identity/registrations/{id}/code", new { channel = "sms" });
        codes.Add(_fixture.Messages.LastCode());
        await client.PostJsonAsync($"/api/v1/identity/registrations/{id}/code", new { channel = "email" });
        codes.Add(_fixture.Messages.LastCode());
        await client.PostJsonAsync($"/api/v1/identity/registrations/{id}/verification", new { code = "999999" });
        await client.PostJsonAsync($"/api/v1/identity/registrations/{id}/verification", new { code = codes[^1] });
        (await client.PostJsonAsync($"/api/v1/identity/registrations/{id}/account", new { password = Password })).Status.ShouldBe(HttpStatusCode.Created);

        // Eslesmeyen uyelik (yanlis dogum tarihi) ve kurum disi adres.
        await client.PostJsonAsync("/api/v1/identity/registrations", new { nationalId = NationalId(2), birthDate = "1990-01-01", email = Emails[1] });
        await client.PostJsonAsync("/api/v1/identity/registrations", new { nationalId = NationalId(5), birthDate = "1985-04-12", email = Emails[4] });

        // Giris: var olmayan adres, yanlis parola, dogru parola.
        await client.PostAsJsonAsync(new Uri("/api/v1/identity/sessions", UriKind.Relative), new { email = "yok.boyle@duzen.com.tr", password = Password });
        await client.PostAsJsonAsync(new Uri("/api/v1/identity/sessions", UriKind.Relative), new { email = Emails[0], password = "Yanlis parola 1" });
        (await client.PostAsJsonAsync(new Uri("/api/v1/identity/sessions", UriKind.Relative), new { email = Emails[0], password = Password })).StatusCode.ShouldBe(HttpStatusCode.OK);

        // Parola sifirlama.
        var (_, reset) = await client.PostJsonAsync("/api/v1/identity/password-resets", new { nationalId = NationalId(1), birthDate = "1985-04-12", email = Emails[0] });
        var resetId = reset.GetProperty("registrationId").GetString();
        await client.PostJsonAsync($"/api/v1/identity/password-resets/{resetId}/code", new { channel = "sms" });
        codes.Add(_fixture.Messages.LastCode());
        await client.PostJsonAsync($"/api/v1/identity/password-resets/{resetId}/verification", new { code = codes[^1] });
        (await client.PostJsonAsync($"/api/v1/identity/password-resets/{resetId}/password", new { password = NewPassword })).Status.ShouldBe(HttpStatusCode.NoContent);

        var log = await ReadLogSinceAsync(startedAt);

        // Akislar gercekten gunluge yazdi: bos gunlukte "sizinti yok" anlamsiz olurdu.
        log.ShouldContain("Dogrulama kodu uretildi");
        log.ShouldContain("/api/v1/identity/registrations");

        var forbidden = Enumerable.Range(1, 5).Select(NationalId)
            .Concat(Emails)
            .Concat(Phones)
            .Concat(codes.Select(c => $"\"{c}\""))
            .Concat([Password, NewPassword, "Yanlis parola 1", "yok.boyle@duzen.com.tr"]);

        foreach (var value in forbidden)
        {
            log.ShouldNotContain(value, Case.Insensitive, $"Gunlukte acik kisisel veri veya sir: {value}");
        }
    }

    /// <summary>
    /// Test sunucusunun gunluk dosyalarini okur. Dosya yazilirken acik oldugu icin paylasimli
    /// okunur; dosyaya yazim gecikebilecegi icin kisa bir sure beklenir.
    /// </summary>
    private static async Task<string> ReadLogSinceAsync(DateTime startedAt)
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "logs");
        await Task.Delay(500);

        var files = Directory.GetFiles(directory, "test-*.json")
            .Where(f => File.GetLastWriteTimeUtc(f) >= startedAt)
            .ToList();
        files.ShouldNotBeEmpty("Test sunucusu gunluk dosyasina yazmadi.");

        var text = new System.Text.StringBuilder();
        foreach (var file in files)
        {
            await using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            using var reader = new StreamReader(stream);
            text.Append(await reader.ReadToEndAsync());
        }

        return text.ToString();
    }
}
