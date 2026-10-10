using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dsg.Hrms.Api.IntegrationTests.Identity;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Settings;

/// <summary>
/// Parametre ekrani ve kurumsal logo uctan uca: gercek HTTP boru hatti ve gercek PostgreSQL
/// (SYG-KMLK-069, 075, 076). Veriler SENTETIKTIR.
/// </summary>
/// <remarks>Kisi 2 ilk sistem yoneticisidir (yapilandirma); kisi 1'in rolu yoktur.</remarks>
[Collection(ApiHostGroup.Name)]
public sealed class SystemParametersApiTests : IClassFixture<RegistrationApiFixture>, IAsyncLifetime
{
    private const string Parameters = "/api/v1/system/parameters";
    private const string Logo = "/api/v1/system/logo";
    private const string Password = "Kediler uyur 7";

    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 1, 2, 3, 4];

    private readonly RegistrationApiFixture _fixture;

    public SystemParametersApiTests(RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await CreateAccountAsync(1);
        await CreateAccountAsync(2);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Lists_only_T3_parameters_and_never_returns_secret_values()
    {
        using var client = _fixture.CreateClient();
        var admin = await SignInAsync(client, "mehmet.kaya@duzen.com.tr");

        (await PutJsonAsync(client, $"{Parameters}/PRM-ENT-06", admin, new { value = "Cok.Gizli.Smtp1" })).Status.ShouldBe(HttpStatusCode.NoContent);
        var (status, body) = await GetAsync(client, Parameters, admin);

        status.ShouldBe(HttpStatusCode.OK);
        body.GetRawText().ShouldNotContain("Cok.Gizli.Smtp1");
        var items = body.GetProperty("items").EnumerateArray().ToList();
        body.GetProperty("totalCount").GetInt32().ShouldBe(items.Count);
        items.ShouldAllBe(p => p.GetProperty("key").GetString()!.StartsWith("PRM-", StringComparison.Ordinal));

        var secret = items.Single(p => p.GetProperty("key").GetString() == "PRM-ENT-06");
        secret.GetProperty("type").GetString().ShouldBe("secret");
        secret.GetProperty("value").ValueKind.ShouldBe(JsonValueKind.Null);
        secret.GetProperty("isSet").GetBoolean().ShouldBeTrue();

        var lockout = items.Single(p => p.GetProperty("key").GetString() == "PRM-KML-03");
        lockout.GetProperty("value").GetString().ShouldBe("5");
        lockout.GetProperty("source").GetString().ShouldBe("default");
        lockout.GetProperty("min").GetInt32().ShouldBe(3);
    }

    [Fact]
    public async Task Valid_change_takes_effect_and_is_audited_invalid_change_is_refused()
    {
        using var client = _fixture.CreateClient();
        var admin = await SignInAsync(client, "mehmet.kaya@duzen.com.tr");

        var invalid = await PutJsonAsync(client, $"{Parameters}/PRM-KML-03", admin, new { value = "99" });
        invalid.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        invalid.Body.GetProperty("detail").GetString().ShouldBe("Değer 3–10 aralığında olmalıdır.");

        (await PutJsonAsync(client, $"{Parameters}/PRM-KML-03", admin, new { value = "7" })).Status.ShouldBe(HttpStatusCode.NoContent);

        var lockout = (await GetAsync(client, Parameters, admin)).Body.GetProperty("items").EnumerateArray()
            .Single(p => p.GetProperty("key").GetString() == "PRM-KML-03");
        lockout.GetProperty("value").GetString().ShouldBe("7");
        lockout.GetProperty("source").GetString().ShouldBe("database");

        (await _fixture.WithDbAsync(context => context.ChangeLog.CountAsync(e => e.EntityName == nameof(SystemParameter))))
            .ShouldBeGreaterThan(0);
        (await PutJsonAsync(client, $"{Parameters}/PRM-YOK-01", admin, new { value = "1" })).Status.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Parameters_require_permission()
    {
        using var client = _fixture.CreateClient();
        var user = await SignInAsync(client, "ahmet.yilmaz@duzen.com.tr");

        (await GetAsync(client, Parameters, user)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await PutJsonAsync(client, $"{Parameters}/PRM-KML-03", user, new { value = "7" })).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await GetAsync(client, Parameters, null)).Status.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logo_is_uploaded_served_with_validation_and_removed()
    {
        using var client = _fixture.CreateClient();
        var admin = await SignInAsync(client, "mehmet.kaya@duzen.com.tr");

        // Yuklenmemis logo: istemci varsayilani kullanir. Okuma oturum gerektirmez.
        (await GetAsync(client, Logo, null)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await GetAsync(client, "/api/v1/identity/public-settings", null)).Body.GetProperty("logoVersion").ValueKind.ShouldBe(JsonValueKind.Null);

        (await UploadAsync(client, admin, Png, "logo.png")).ShouldBe(HttpStatusCode.NoContent);
        (await GetAsync(client, "/api/v1/identity/public-settings", null)).Body.GetProperty("logoVersion").GetString()!.Length.ShouldBe(64);

        using var served = await client.GetAsync(new Uri(Logo, UriKind.Relative));
        served.StatusCode.ShouldBe(HttpStatusCode.OK);
        served.Content.Headers.ContentType!.MediaType.ShouldBe("image/png");
        (await served.Content.ReadAsByteArrayAsync()).ShouldBe(Png);
        var etag = served.Headers.ETag!;

        using var conditional = new HttpRequestMessage(HttpMethod.Get, new Uri(Logo, UriKind.Relative));
        conditional.Headers.IfNoneMatch.Add(etag);
        using var notModified = await client.SendAsync(conditional);
        notModified.StatusCode.ShouldBe(HttpStatusCode.NotModified);

        // Icerik ilk baytlara gore denetlenir: uzantisi .png olan metin kabul edilmez; boyut sinirli.
        (await UploadAsync(client, admin, "<html>merhaba</html>"u8.ToArray(), "logo.png")).ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await UploadAsync(client, admin, [.. Png, .. new byte[BrandLogo.MaxSizeBytes]], "buyuk.png")).ShouldBe(HttpStatusCode.UnprocessableEntity);

        using var delete = new HttpRequestMessage(HttpMethod.Delete, new Uri(Logo, UriKind.Relative));
        delete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
        (await client.SendAsync(delete)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await GetAsync(client, Logo, null)).Status.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Logo_change_requires_permission_and_is_audited_without_the_file()
    {
        using var client = _fixture.CreateClient();
        var user = await SignInAsync(client, "ahmet.yilmaz@duzen.com.tr");
        (await UploadAsync(client, user, Png, "logo.png")).ShouldBe(HttpStatusCode.Forbidden);

        var admin = await SignInAsync(client, "mehmet.kaya@duzen.com.tr");
        (await UploadAsync(client, admin, Png, "logo.png")).ShouldBe(HttpStatusCode.NoContent);

        var entry = await _fixture.WithDbAsync(context => context.ChangeLog
            .Where(e => e.EntityName == nameof(BrandLogo) && e.Operation == AuditOperation.Insert)
            .OrderByDescending(e => e.Id)
            .FirstAsync());
        // Izde dosyanin ozeti, boyutu ve turu vardir; icerik maskelenir (ad kurali).
        using var changes = JsonDocument.Parse(entry.Changes);
        changes.RootElement.GetProperty("Sha256").GetProperty("new").GetString().ShouldNotBeNullOrEmpty();
        changes.RootElement.GetProperty("SizeBytes").GetProperty("new").GetInt32().ShouldBe(Png.Length);
        changes.RootElement.GetProperty("Content").GetProperty("new").GetString().ShouldBe("***");
        entry.Changes.ShouldNotContain(Convert.ToBase64String(Png));
    }

    // ------------------------------------------------------------------ 2FA etki uyarisi (SYG-KMLK-035, 080)

    [Fact]
    public async Task Enabling_two_factor_shows_the_impact_and_requires_confirmation()
    {
        using var client = _fixture.CreateClient();
        var admin = await SignInAsync(client, "mehmet.kaya@duzen.com.tr");
        var impact = (await GetAsync(client, $"{Parameters}/two-factor-impact", admin)).Body;
        impact.GetProperty("affectedCount").GetInt32().ShouldBe(0);
        impact.GetProperty("confirmationRequired").GetBoolean().ShouldBeFalse();

        // SYG-KMLK-080: yalnizca kendi tercihi acik AKTIF hesaplar sayilir. Kisi 1 tercihini acti;
        // istihdami biten kisi 4'un ve pasif hesapli kisi 3'un tercihi sayilmaz.
        await CreateAccountAsync(3);
        await CreateAccountAsync(4);
        await EnableTwoFactorPreferenceAsync(1);
        await EnableTwoFactorPreferenceAsync(3);
        await EnableTwoFactorPreferenceAsync(4);
        await _fixture.WithDbAsync(async context =>
        {
            var personId = await PersonIdAsync(context, 3);
            (await context.Set<UserAccount>().SingleAsync(a => a.PersonId == personId)).DeactivateManually("Test");
            return await context.SaveChangesAsync();
        });

        impact = (await GetAsync(client, $"{Parameters}/two-factor-impact", admin)).Body;
        impact.GetProperty("affectedCount").GetInt32().ShouldBe(1);
        impact.GetProperty("confirmationRequired").GetBoolean().ShouldBeTrue();

        var refused = await PutJsonAsync(client, $"{Parameters}/PRM-KML-08", admin, new { value = "true" });
        refused.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        refused.Body.GetProperty("detail").GetString()!.ShouldContain("açmış 1 kişi bundan sonra girişte kod girecek");

        (await PutJsonAsync(client, $"{Parameters}/PRM-KML-08", admin, new { value = "true", confirmed = true })).Status.ShouldBe(HttpStatusCode.NoContent);
        (await PutJsonAsync(client, $"{Parameters}/PRM-KML-08", admin, new { value = "false" })).Status.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Confirmation_is_not_required_when_nobody_has_the_preference_on()
    {
        // SYG-KMLK-080: tercih varsayilan kapali; kanali olmayan kisi tercihi acamaz. Parametre
        // kimsenin girisini engellemez, bu yuzden onay istenmez.
        using var client = _fixture.CreateClient();
        var admin = await SignInAsync(client, "mehmet.kaya@duzen.com.tr");

        (await GetAsync(client, $"{Parameters}/two-factor-impact", admin)).Body.GetProperty("affectedCount").GetInt32().ShouldBe(0);
        (await PutJsonAsync(client, $"{Parameters}/PRM-KML-08", admin, new { value = "true" })).Status.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Confirmation_is_not_required_when_the_warning_is_turned_off()
    {
        using var client = _fixture.CreateClient();
        var admin = await SignInAsync(client, "mehmet.kaya@duzen.com.tr");
        await EnableTwoFactorPreferenceAsync(1);

        (await PutJsonAsync(client, $"{Parameters}/PRM-KML-15", admin, new { value = "false" })).Status.ShouldBe(HttpStatusCode.NoContent);
        (await GetAsync(client, $"{Parameters}/two-factor-impact", admin)).Body.GetProperty("confirmationRequired").GetBoolean().ShouldBeFalse();

        (await PutJsonAsync(client, $"{Parameters}/PRM-KML-08", admin, new { value = "true" })).Status.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Impact_requires_permission()
    {
        using var client = _fixture.CreateClient();
        var user = await SignInAsync(client, "ahmet.yilmaz@duzen.com.tr");

        (await GetAsync(client, $"{Parameters}/two-factor-impact", user)).Status.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ------------------------------------------------------------------ yardimcilar

    private async Task CreateAccountAsync(int index) =>
        await _fixture.WithServicesAsync(async services =>
        {
            var context = services.GetRequiredService<Infrastructure.Data.HrmsDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher>();
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
            context.Add(UserAccount.Register(person.Id, hasher.Hash(PasswordPolicy.Normalize(Password)), _fixture.Clock.UtcNow));
            return await context.SaveChangesAsync();
        });

    private Task<int> EnableTwoFactorPreferenceAsync(int index) =>
        _fixture.WithDbAsync(async context =>
        {
            var personId = await PersonIdAsync(context, index);
            (await context.Set<UserAccount>().SingleAsync(a => a.PersonId == personId)).EnableTwoFactor();
            return await context.SaveChangesAsync();
        });

    private static Task<long> PersonIdAsync(Infrastructure.Data.HrmsDbContext context, int index) =>
        context.Set<Person>().Where(p => p.NationalId == NationalId(index)).Select(p => p.Id).SingleAsync();

    private static async Task<string> SignInAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync(new Uri("/api/v1/identity/sessions", UriKind.Relative), new { email, password = Password });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        return body.GetProperty("session").GetProperty("accessToken").GetString()!;
    }

    private static async Task<HttpStatusCode> UploadAsync(HttpClient client, string token, byte[] content, string fileName)
    {
        using var form = new MultipartFormDataContent();
        using var file = new ByteArrayContent(content);
        file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(file, "file", fileName);

        using var request = new HttpRequestMessage(HttpMethod.Put, new Uri(Logo, UriKind.Relative)) { Content = form };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private static Task<(HttpStatusCode Status, JsonElement Body)> GetAsync(HttpClient client, string path, string? token) =>
        SendAsync(client, new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative)), token);

    private static Task<(HttpStatusCode Status, JsonElement Body)> PutJsonAsync(HttpClient client, string path, string token, object body) =>
        SendAsync(client, new HttpRequestMessage(HttpMethod.Put, new Uri(path, UriKind.Relative)) { Content = JsonContent.Create(body) }, token);

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(HttpClient client, HttpRequestMessage request, string? token)
    {
        using (request)
        {
            if (token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            using var response = await client.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            var isJson = response.Content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.Ordinal) == true;
            return (response.StatusCode, isJson && text.Length > 0 ? JsonDocument.Parse(text).RootElement.Clone() : default);
        }
    }
}
