using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Identity;

/// <summary>
/// Zorunlu parola degisimi uctan uca: ilk giris (SYG-KMLK-050) ve periyodik degisim
/// (SYG-KMLK-046). Gercek HTTP boru hatti ve gercek PostgreSQL. Veriler SENTETIKTIR.
/// </summary>
/// <remarks>
/// Kisi 2 ilk sistem yoneticisidir (fixture): kisitin "yetki yok" ile karismamasi icin korunan
/// uc olarak parametre listesi kullanilir.
/// </remarks>
[Collection(ApiHostGroup.Name)]
public sealed class PasswordChangeRequirementApiTests : IClassFixture<RegistrationApiFixture>, IAsyncLifetime
{
    private const string Sessions = "/api/v1/identity/sessions";
    private const string ChangePassword = "/api/v1/identity/account/password";
    private const string Parameters = "/api/v1/system/parameters";
    private const string Email = "mehmet.kaya@duzen.com.tr";
    private const string OldPassword = "Kediler uyur 7";
    private const string NewPassword = "Mavi deniz 42 kez";

    private readonly RegistrationApiFixture _fixture;

    public PasswordChangeRequirementApiTests(RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await CreateAccountAsync(2);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ------------------------------------------------------------------ ilk giris (SYG-KMLK-050)

    [Fact]
    public async Task By_default_no_change_is_required()
    {
        using var client = _fixture.CreateClient();
        var session = await SignInAsync(client, OldPassword);

        Reason(session).ShouldBeNull();
        (await GetAsync(client, session.AccessToken, Parameters)).Status.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task First_sign_in_requires_a_change_and_only_the_allowed_endpoints_work_until_then()
    {
        await SetParameterAsync(ParameterCatalog.RequirePasswordChangeOnFirstLogin, "true");
        using var client = _fixture.CreateClient();
        var session = await SignInAsync(client, OldPassword);

        Reason(session).ShouldBe("firstSignIn");

        var blocked = await GetAsync(client, session.AccessToken, Parameters);
        blocked.Status.ShouldBe(HttpStatusCode.Forbidden);
        blocked.Body.GetProperty("type").GetString()!.ShouldEndWith("password-change-required");

        // Oturum uclari calisir; yenileme nedeni yeniden bildirir.
        (await ActivityAsync(client, session.AccessToken)).ShouldBe(HttpStatusCode.NoContent);
        var refreshed = await RefreshAsync(client, session.Cookie);
        Reason(refreshed.Body).ShouldBe("firstSignIn");

        (await ChangeAsync(client, session.AccessToken, OldPassword, NewPassword)).ShouldBe(HttpStatusCode.NoContent);

        // Kisit, yeni erisim jetonu beklenmeden kalkar.
        (await GetAsync(client, session.AccessToken, Parameters)).Status.ShouldBe(HttpStatusCode.OK);
        Reason((await RefreshAsync(client, refreshed.Cookie)).Body).ShouldBeNull();
    }

    [Fact]
    public async Task Signing_out_without_changing_keeps_the_requirement()
    {
        await SetParameterAsync(ParameterCatalog.RequirePasswordChangeOnFirstLogin, "true");
        using var client = _fixture.CreateClient();
        await SignInAsync(client, OldPassword);

        // Ikinci giris "ilk giris" degildir ama degisim hala yapilmadi.
        Reason(await SignInAsync(client, OldPassword)).ShouldBe("firstSignIn");
    }

    [Fact]
    public async Task An_account_that_signed_in_before_the_rule_was_enabled_is_not_affected()
    {
        using var client = _fixture.CreateClient();
        await SignInAsync(client, OldPassword);
        await SetParameterAsync(ParameterCatalog.RequirePasswordChangeOnFirstLogin, "true");

        Reason(await SignInAsync(client, OldPassword)).ShouldBeNull();
    }

    [Fact]
    public async Task Disabling_the_rule_lifts_a_pending_first_sign_in_change()
    {
        await SetParameterAsync(ParameterCatalog.RequirePasswordChangeOnFirstLogin, "true");
        using var client = _fixture.CreateClient();
        await SignInAsync(client, OldPassword);
        await SetParameterAsync(ParameterCatalog.RequirePasswordChangeOnFirstLogin, "false");

        Reason(await SignInAsync(client, OldPassword)).ShouldBeNull();
    }

    [Fact]
    public async Task The_new_password_must_differ_from_the_current_one()
    {
        await SetParameterAsync(ParameterCatalog.RequirePasswordChangeOnFirstLogin, "true");
        using var client = _fixture.CreateClient();
        var session = await SignInAsync(client, OldPassword);

        (await ChangeAsync(client, session.AccessToken, OldPassword, OldPassword)).ShouldBe(HttpStatusCode.BadRequest);
        (await GetAsync(client, session.AccessToken, Parameters)).Status.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ------------------------------------------------------------------ periyodik (SYG-KMLK-046)

    [Fact]
    public async Task Periodic_change_is_required_once_the_password_is_older_than_the_period()
    {
        await SetParameterAsync(ParameterCatalog.RequirePeriodicPasswordChange, "true");
        using var client = _fixture.CreateClient();

        _fixture.Clock.Advance(TimeSpan.FromDays(89));
        Reason(await SignInAsync(client, OldPassword)).ShouldBeNull();

        _fixture.Clock.Advance(TimeSpan.FromDays(1));
        var session = await SignInAsync(client, OldPassword);
        Reason(session).ShouldBe("expired");
        (await GetAsync(client, session.AccessToken, Parameters)).Status.ShouldBe(HttpStatusCode.Forbidden);

        (await ChangeAsync(client, session.AccessToken, OldPassword, NewPassword)).ShouldBe(HttpStatusCode.NoContent);
        Reason(await SignInAsync(client, NewPassword)).ShouldBeNull();
    }

    [Fact]
    public async Task The_period_comes_from_its_parameter()
    {
        await SetParameterAsync(ParameterCatalog.RequirePeriodicPasswordChange, "true");
        await SetParameterAsync(ParameterCatalog.PasswordMaxAgeDays, "30");
        using var client = _fixture.CreateClient();

        _fixture.Clock.Advance(TimeSpan.FromDays(30));

        Reason(await SignInAsync(client, OldPassword)).ShouldBe("expired");
    }

    [Fact]
    public async Task The_period_alone_does_nothing_while_periodic_change_is_off()
    {
        using var client = _fixture.CreateClient();

        _fixture.Clock.Advance(TimeSpan.FromDays(400));

        Reason(await SignInAsync(client, OldPassword)).ShouldBeNull();
    }

    // ------------------------------------------------------------------ yardimcilar

    private sealed record SignInOutcome(JsonElement Body, string? Cookie)
    {
        public string AccessToken => Body.GetProperty("session").GetProperty("accessToken").GetString()!;
    }

    private static string? Reason(SignInOutcome outcome) => Reason(outcome.Body.GetProperty("session"));

    private static string? Reason(JsonElement session) =>
        session.GetProperty("passwordChangeRequired") is { ValueKind: JsonValueKind.String } reason ? reason.GetString() : null;

    private async Task CreateAccountAsync(int index) =>
        await _fixture.WithServicesAsync(async services =>
        {
            var context = services.GetRequiredService<Infrastructure.Data.HrmsDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher>();
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
            context.Add(UserAccount.Register(person.Id, hasher.Hash(PasswordPolicy.Normalize(OldPassword)), _fixture.Clock.UtcNow));
            return await context.SaveChangesAsync();
        });

    private Task<int> SetParameterAsync(ParameterDefinition parameter, string value) =>
        _fixture.WithServicesAsync(async services =>
        {
            await services.GetRequiredService<ISystemParameterEditor>().UpdateAsync(parameter.Key, value, CancellationToken.None);
            return 0;
        });

    private static async Task<SignInOutcome> SignInAsync(HttpClient client, string password)
    {
        using var response = await client.PostAsJsonAsync(new Uri(Sessions, UriKind.Relative), new { email = Email, password });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
        var setCookie = response.Headers.GetValues("Set-Cookie").Single();
        return new SignInOutcome(body, setCookie["hrms_refresh=".Length..].Split(';')[0]);
    }

    /// <summary>Yenileme: jeton her kullanimda degisir, sonraki yenileme donen cerezle yapilir.</summary>
    private static async Task<(JsonElement Body, string Cookie)> RefreshAsync(HttpClient client, string? cookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Sessions + "/refresh", UriKind.Relative));
        request.Headers.Add("Cookie", $"hrms_refresh={cookie}");
        using var response = await client.SendAsync(request);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var setCookie = response.Headers.GetValues("Set-Cookie").Single();
        return (JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone(), setCookie["hrms_refresh=".Length..].Split(';')[0]);
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> GetAsync(HttpClient client, string accessToken, string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone());
    }

    private static async Task<HttpStatusCode> ActivityAsync(HttpClient client, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Sessions + "/activity", UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private static async Task<HttpStatusCode> ChangeAsync(HttpClient client, string accessToken, string currentPassword, string newPassword)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(ChangePassword, UriKind.Relative))
        {
            Content = JsonContent.Create(new { currentPassword, newPassword }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }
}
