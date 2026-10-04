using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Notifications;
using Dsg.Hrms.Domain.Personnel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Identity;

/// <summary>
/// Parola sifirlama ve oturum icinde parola degisikligi uctan uca: gercek HTTP boru hatti ve
/// gercek PostgreSQL (SYG-KMLK-047, 048). Veriler SENTETIKTIR.
/// </summary>
[Collection(ApiHostGroup.Name)]
public sealed class PasswordApiTests : IClassFixture<RegistrationApiFixture>, IAsyncLifetime
{
    private const string Resets = "/api/v1/identity/password-resets";
    private const string Sessions = "/api/v1/identity/sessions";
    private const string ChangePassword = "/api/v1/identity/account/password";
    private const string Email = "ahmet.yilmaz@duzen.com.tr";
    private const string OldPassword = "Kediler uyur 7";
    private const string NewPassword = "Mavi deniz 42 kez";

    private readonly RegistrationApiFixture _fixture;

    public PasswordApiTests(RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await CreateAccountAsync(1);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ------------------------------------------------------------------ sifirlama (SYG-KMLK-047)

    [Fact]
    public async Task Reset_uses_the_registration_flow_ends_all_sessions_and_accepts_only_the_new_password()
    {
        using var client = _fixture.CreateClient();
        var session = await SignInAsync(client, OldPassword);
        session.Status.ShouldBe(HttpStatusCode.OK);

        var id = await VerifiedResetAsync(client, 1);
        _fixture.Messages.All[^1].Purpose.ShouldBe(NotificationPurpose.PasswordResetCode);

        (await client.PostJsonAsync($"{Resets}/{id}/password", new { password = NewPassword })).Status.ShouldBe(HttpStatusCode.NoContent);

        (await RefreshAsync(client, session.Cookie)).ShouldEndWith("session-ended/password-changed");
        (await SignInAsync(client, OldPassword)).Status.ShouldBe(HttpStatusCode.Unauthorized);
        (await SignInAsync(client, NewPassword)).Status.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reset_message_names_its_purpose()
    {
        using var client = _fixture.CreateClient();

        await VerifiedResetAsync(client, 1);

        _fixture.Messages.All[^1].MessageBody.ShouldContain("parola sıfırlama");
    }

    [Fact]
    public async Task Reset_responses_do_not_reveal_whether_the_details_match()
    {
        // Eslesme gizliligi sifirlamada da gecerlidir (KR-085): yanlis dogum tarihiyle de ayni
        // yanit doner, kod gonderilmez ve her kod "yanlis" sayilir.
        using var client = _fixture.CreateClient();

        var (status, body) = await client.PostJsonAsync(Resets, new { nationalId = NationalId(1), birthDate = "1990-01-01", email = Email });
        status.ShouldBe(HttpStatusCode.OK);
        body.EnumerateObject().Select(p => p.Name).ShouldBe(["registrationId", "channels", "expiresAt"]);
        var id = body.GetProperty("registrationId").GetString();

        (await client.PostJsonAsync($"{Resets}/{id}/code", new { channel = "email" })).Status.ShouldBe(HttpStatusCode.OK);
        _fixture.Messages.All.ShouldBeEmpty();

        var verification = await client.PostJsonAsync($"{Resets}/{id}/verification", new { code = "123456" });
        verification.Body.GetProperty("result").GetString().ShouldBe("mismatch");
    }

    [Fact]
    public async Task Reset_without_an_account_asks_the_person_to_register()
    {
        using var client = _fixture.CreateClient();
        var id = await VerifiedResetAsync(client, 2, expectAccount: false);

        var (status, body) = await client.PostJsonAsync($"{Resets}/{id}/password", new { password = NewPassword });

        status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        body.GetProperty("detail").GetString()!.ShouldContain("Üye olarak");
    }

    [Fact]
    public async Task Reset_applies_the_password_policy_and_changes_nothing_on_violation()
    {
        using var client = _fixture.CreateClient();
        var id = await VerifiedResetAsync(client, 1);

        var (status, body) = await client.PostJsonAsync($"{Resets}/{id}/password", new { password = "ahmet123" });

        status.ShouldBe(HttpStatusCode.BadRequest);
        body.GetProperty("errors").GetProperty("password").GetArrayLength().ShouldBeGreaterThan(0);
        (await SignInAsync(client, OldPassword)).Status.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reset_lifts_the_sign_in_lock()
    {
        using var client = _fixture.CreateClient();
        for (var i = 0; i < 5; i++)
        {
            await SignInAsync(client, "Yanlis parola 1");
        }

        (await SignInAsync(client, OldPassword)).Status.ShouldBe(HttpStatusCode.TooManyRequests);

        var id = await VerifiedResetAsync(client, 1);
        (await client.PostJsonAsync($"{Resets}/{id}/password", new { password = NewPassword })).Status.ShouldBe(HttpStatusCode.NoContent);

        (await SignInAsync(client, NewPassword)).Status.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reset_is_refused_for_a_manually_deactivated_account()
    {
        using var client = _fixture.CreateClient();
        var id = await VerifiedResetAsync(client, 1);
        await _fixture.WithDbAsync(async context =>
        {
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(1));
            var account = await context.Set<UserAccount>().SingleAsync(a => a.PersonId == person.Id);
            account.DeactivateManually("Test: elle pasife alma");
            return await context.SaveChangesAsync();
        });

        (await client.PostJsonAsync($"{Resets}/{id}/password", new { password = NewPassword })).Status.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task A_verified_registration_that_found_an_account_can_reset_the_password()
    {
        // Uyelikte "hesabiniz zaten var" sonucu: kimlik dogrulandi, bastan baslamaya gerek yok.
        using var client = _fixture.CreateClient();
        var (_, started) = await client.PostJsonAsync("/api/v1/identity/registrations", Start(1));
        var id = started.GetProperty("registrationId").GetString();
        await client.PostJsonAsync($"/api/v1/identity/registrations/{id}/code", new { channel = "email" });
        var verification = await client.PostJsonAsync($"/api/v1/identity/registrations/{id}/verification", new { code = _fixture.Messages.LastCode() });
        verification.Body.GetProperty("accountExists").GetBoolean().ShouldBeTrue();

        (await client.PostJsonAsync($"{Resets}/{id}/password", new { password = NewPassword })).Status.ShouldBe(HttpStatusCode.NoContent);
        (await SignInAsync(client, NewPassword)).Status.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Reset_can_be_completed_only_once()
    {
        using var client = _fixture.CreateClient();
        var id = await VerifiedResetAsync(client, 1);

        (await client.PostJsonAsync($"{Resets}/{id}/password", new { password = NewPassword })).Status.ShouldBe(HttpStatusCode.NoContent);
        (await client.PostJsonAsync($"{Resets}/{id}/password", new { password = "Baska bir cumle 5" })).Status.ShouldBe(HttpStatusCode.NotFound);
    }

    // ------------------------------------------------------------------ oturum icinde degisiklik (SYG-KMLK-048)

    [Fact]
    public async Task Change_keeps_the_current_session_and_ends_the_others()
    {
        await SetParameterAsync(ParameterCatalog.SingleActiveSession, "false");
        using var client = _fixture.CreateClient();
        var current = await SignInAsync(client, OldPassword);
        var other = await SignInAsync(client, OldPassword);

        (await ChangeAsync(client, AccessToken(current), OldPassword, NewPassword)).Status.ShouldBe(HttpStatusCode.NoContent);

        // Bu oturum acik kalir: erisim jetonu da yenileme jetonu da calisir.
        (await ActivityAsync(client, AccessToken(current))).ShouldBe(HttpStatusCode.NoContent);
        (await RefreshAsync(client, current.Cookie)).ShouldBe("ok");

        (await RefreshAsync(client, other.Cookie)).ShouldEndWith("session-ended/password-changed");
        (await SignInAsync(client, NewPassword)).Status.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Change_requires_the_current_password_and_counts_wrong_attempts()
    {
        using var client = _fixture.CreateClient();
        var token = AccessToken(await SignInAsync(client, OldPassword));

        var wrong = await ChangeAsync(client, token, "Yanlis parola 1", NewPassword);
        wrong.Status.ShouldBe(HttpStatusCode.BadRequest);
        wrong.Body.GetProperty("errors").GetProperty("currentPassword")[0].GetString().ShouldBe("Mevcut parolanız hatalı.");

        for (var i = 0; i < 4; i++)
        {
            await ChangeAsync(client, token, "Yanlis parola 1", NewPassword);
        }

        // Acik birakilmis oturumda parola tahmini, girisle ayni kilide takilir (SYG-KMLK-033).
        (await ChangeAsync(client, token, OldPassword, NewPassword)).Status.ShouldBe(HttpStatusCode.TooManyRequests);
        (await SignInAsync(client, OldPassword)).Status.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Change_rejects_the_same_password_and_policy_violations()
    {
        using var client = _fixture.CreateClient();
        var token = AccessToken(await SignInAsync(client, OldPassword));

        var same = await ChangeAsync(client, token, OldPassword, OldPassword);
        same.Status.ShouldBe(HttpStatusCode.BadRequest);
        same.Body.GetProperty("errors").GetProperty("newPassword")[0].GetString().ShouldBe("Yeni parola mevcut parolanızdan farklı olmalıdır.");

        // SYG-KMLK-064, B-02: ileti kac karakter gerektigini soyler (PRM-KML-05 varsayilani 6).
        var tooShort = await ChangeAsync(client, token, OldPassword, "Ab1");
        tooShort.Status.ShouldBe(HttpStatusCode.BadRequest);
        tooShort.Body.GetProperty("errors").GetProperty("newPassword")[0].GetString().ShouldBe("Parola en az 6 karakter olmalıdır.");

        var common = await ChangeAsync(client, token, OldPassword, "123456");
        common.Status.ShouldBe(HttpStatusCode.BadRequest);
        common.Body.GetProperty("errors").TryGetProperty("newPassword", out _).ShouldBeTrue();

        (await SignInAsync(client, OldPassword)).Status.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Change_requires_a_session()
    {
        using var client = _fixture.CreateClient();

        (await ChangeAsync(client, null, OldPassword, NewPassword)).Status.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Change_after_the_session_ended_is_refused()
    {
        using var client = _fixture.CreateClient();
        var session = await SignInAsync(client, OldPassword);
        await SignInAsync(client, OldPassword); // Tek aktif oturum: ilki kapanir.

        (await ChangeAsync(client, AccessToken(session), OldPassword, NewPassword)).Status.ShouldBe(HttpStatusCode.Unauthorized);
        (await SignInAsync(client, OldPassword)).Status.ShouldBe(HttpStatusCode.OK);
    }

    // ------------------------------------------------------------------ yardimcilar

    private sealed record SignInOutcome(HttpStatusCode Status, JsonElement Body, string? Cookie);

    private static object Start(int index) => new { nationalId = NationalId(index), birthDate = "1985-04-12", email = index == 2 ? "mehmet.kaya@duzen.com.tr" : Email };

    private async Task<string> VerifiedResetAsync(HttpClient client, int index, bool expectAccount = true)
    {
        var (status, body) = await client.PostJsonAsync(Resets, Start(index));
        status.ShouldBe(HttpStatusCode.OK);
        var id = body.GetProperty("registrationId").GetString()!;

        (await client.PostJsonAsync($"{Resets}/{id}/code", new { channel = "email" })).Status.ShouldBe(HttpStatusCode.OK);
        var verification = await client.PostJsonAsync($"{Resets}/{id}/verification", new { code = _fixture.Messages.LastCode() });
        verification.Body.GetProperty("result").GetString().ShouldBe("verified");
        verification.Body.GetProperty("accountExists").GetBoolean().ShouldBe(expectAccount);
        return id;
    }

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
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
        var setCookie = response.Headers.TryGetValues("Set-Cookie", out var values) ? values.Single() : null;
        var cookie = setCookie is not null && setCookie.StartsWith("hrms_refresh=", StringComparison.Ordinal)
            ? setCookie["hrms_refresh=".Length..].Split(';')[0]
            : null;
        return new SignInOutcome(response.StatusCode, body, cookie);
    }

    private static string AccessToken(SignInOutcome outcome) =>
        outcome.Body.GetProperty("session").GetProperty("accessToken").GetString()!;

    /// <summary>Yenileme sonucu: basariliysa "ok", degilse hata turu.</summary>
    private static async Task<string> RefreshAsync(HttpClient client, string? cookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Sessions + "/refresh", UriKind.Relative));
        request.Headers.Add("Cookie", $"hrms_refresh={cookie}");
        using var response = await client.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.OK)
        {
            return "ok";
        }

        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        return body.GetProperty("type").GetString()!;
    }

    private static async Task<HttpStatusCode> ActivityAsync(HttpClient client, string accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Sessions + "/activity", UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> ChangeAsync(
        HttpClient client,
        string? accessToken,
        string currentPassword,
        string newPassword)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(ChangePassword, UriKind.Relative))
        {
            Content = JsonContent.Create(new { currentPassword, newPassword }),
        };
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        var body = string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone();
        return (response.StatusCode, body);
    }
}
