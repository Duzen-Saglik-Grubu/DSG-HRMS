using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Notifications;
using Dsg.Hrms.Domain.Personnel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Identity;

/// <summary>
/// Giris ve oturum uctan uca: gercek HTTP boru hatti ve gercek PostgreSQL
/// (SYG-KMLK-031…043, 055). Veriler SENTETIKTIR.
/// </summary>
[Collection(ApiHostGroup.Name)]
public sealed class SessionApiTests : IClassFixture<RegistrationApiFixture>, IAsyncLifetime
{
    private const string Base = "/api/v1/identity/sessions";
    private const string Email = "ahmet.yilmaz@duzen.com.tr";
    private const string Password = "Kediler uyur 7";

    private readonly RegistrationApiFixture _fixture;

    public SessionApiTests(RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await CreateAccountAsync(1);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ------------------------------------------------------------------ giris (SYG-KMLK-031, 032)

    [Fact]
    public async Task Successful_sign_in_returns_access_token_in_body_and_refresh_token_only_in_a_protected_cookie()
    {
        using var client = _fixture.CreateClient();

        var response = await SignInAsync(client, "  Ahmet.Yilmaz@DUZEN.com.tr ");

        response.Status.ShouldBe(HttpStatusCode.OK);
        response.Body.GetProperty("status").GetString().ShouldBe("signedIn");
        var session = response.Body.GetProperty("session");
        session.GetProperty("accessToken").GetString().ShouldNotBeNullOrWhiteSpace();
        session.GetProperty("idleTimeoutMinutes").GetInt32().ShouldBe(30);
        session.GetProperty("user").GetProperty("firstName").GetString().ShouldBe("Ahmet");
        response.Body.GetRawText().ShouldNotContain(response.Cookie!);

        // SYG-KMLK-037: HttpOnly, Secure, SameSite=Strict; yalnizca oturum uclarina gider.
        var header = response.SetCookie!.ToLowerInvariant();
        header.ShouldContain("httponly");
        header.ShouldContain("secure");
        header.ShouldContain("samesite=strict");
        header.ShouldContain("path=/api/v1/identity/sessions");

        (await ActivityAsync(client, session.GetProperty("accessToken").GetString()!)).ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_get_the_same_response()
    {
        // SYG-KMLK-032: kullanicinin var olup olmadigi yanittan anlasilmaz.
        using var client = _fixture.CreateClient();

        var wrongPassword = await SignInAsync(client, Email, "Yanlis parola 1");
        var unknownEmail = await SignInAsync(client, "yok.boyle@duzen.com.tr", Password);

        wrongPassword.Status.ShouldBe(HttpStatusCode.Unauthorized);
        unknownEmail.Status.ShouldBe(HttpStatusCode.Unauthorized);
        wrongPassword.Body.GetProperty("type").GetString().ShouldBe(unknownEmail.Body.GetProperty("type").GetString());
        wrongPassword.Body.GetProperty("detail").GetString().ShouldBe(unknownEmail.Body.GetProperty("detail").GetString());
        wrongPassword.SetCookie.ShouldBeNull();
    }

    [Fact]
    public async Task National_id_or_registry_code_cannot_be_used_to_sign_in()
    {
        using var client = _fixture.CreateClient();

        (await SignInAsync(client, NationalId(1))).Status.ShouldBe(HttpStatusCode.BadRequest);
        (await SignInAsync(client, "00001")).Status.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ------------------------------------------------------------------ kilit (SYG-KMLK-033, 058)

    [Fact]
    public async Task Fifth_failure_locks_the_email_even_for_the_correct_password_until_lockout_ends()
    {
        var startedAt = _fixture.Clock.UtcNow;
        var accountId = await _fixture.WithDbAsync(c => c.Set<UserAccount>().Select(a => a.PublicId).SingleAsync());
        using var client = _fixture.CreateClient();
        for (var i = 0; i < 5; i++)
        {
            (await SignInAsync(client, Email, "Yanlis parola 1")).Status.ShouldBe(HttpStatusCode.Unauthorized);
        }

        var locked = await SignInAsync(client, Email, Password);
        locked.Status.ShouldBe(HttpStatusCode.TooManyRequests);
        locked.Body.GetProperty("type").GetString()!.ShouldEndWith("sign-in-locked");

        _fixture.Clock.Advance(TimeSpan.FromMinutes(15));
        (await SignInAsync(client, Email, Password)).Status.ShouldBe(HttpStatusCode.OK);

        // Kilitlenme ve kilit kalkmasi denetim izinde (SYG-KMLK-058).
        var changes = await _fixture.WithDbAsync(c => c.ChangeLog
            .Where(e => e.EntityId == accountId && e.OccurredAt >= startedAt && e.Operation == AuditOperation.Update)
            .Select(e => e.Changes).ToListAsync());
        changes.Count(c => c.Contains("LockedUntil")).ShouldBe(2);
    }

    [Fact]
    public async Task Unknown_email_is_locked_the_same_way()
    {
        // AN-05: sayac hesaba degil girilen e-postaya baglidir; "kilitlendi" hesabi ele vermez.
        using var client = _fixture.CreateClient();
        for (var i = 0; i < 5; i++)
        {
            await SignInAsync(client, "yok.boyle@duzen.com.tr", "Yanlis parola 1");
        }

        (await SignInAsync(client, "yok.boyle@duzen.com.tr", "Yanlis parola 1")).Status.ShouldBe(HttpStatusCode.TooManyRequests);
        (await SignInAsync(client, Email, Password)).Status.ShouldBe(HttpStatusCode.OK);
    }

    // ------------------------------------------------------------------ pasif hesap (SYG-KMLK-055)

    [Fact]
    public async Task Passive_account_cannot_sign_in_and_learns_it_only_with_the_correct_password()
    {
        await _fixture.WithDbAsync(async c =>
        {
            var account = await c.Set<UserAccount>().SingleAsync();
            account.DeactivateManually("Test");
            return await c.SaveChangesAsync();
        });
        using var client = _fixture.CreateClient();

        (await SignInAsync(client, Email, "Yanlis parola 1")).Status.ShouldBe(HttpStatusCode.Unauthorized);

        var disabled = await SignInAsync(client, Email, Password);
        disabled.Status.ShouldBe(HttpStatusCode.Forbidden);
        disabled.Body.GetProperty("type").GetString()!.ShouldEndWith("account-disabled");
    }

    [Fact]
    public async Task Deactivation_ends_open_sessions_immediately()
    {
        // SYG-KMLK-054: erisim jetonunun 15 dakikasi beklenmez.
        using var client = _fixture.CreateClient();
        var session = await SignInAsync(client);
        var token = AccessToken(session);

        await _fixture.WithDbAsync(async c =>
        {
            (await c.Set<UserAccount>().SingleAsync()).DeactivateForEmploymentEnd();
            return await c.SaveChangesAsync();
        });

        (await ActivityAsync(client, token)).ShouldBe(HttpStatusCode.Unauthorized);
        (await RefreshAsync(client, session.Cookie)).Type.ShouldEndWith("session-ended/account-changed");
    }

    // ------------------------------------------------------------------ yenileme (SYG-KMLK-040)

    [Fact]
    public async Task Refresh_rotates_the_token_and_reuse_ends_every_session()
    {
        using var client = _fixture.CreateClient();
        var session = await SignInAsync(client);

        var first = await RefreshAsync(client, session.Cookie);
        first.Status.ShouldBe(HttpStatusCode.OK);
        first.Cookie.ShouldNotBe(session.Cookie);

        // Eski jeton tekrar: calinma belirtisi. Yeni jeton dahil TUM oturumlar kapanir.
        var reused = await RefreshAsync(client, session.Cookie);
        reused.Status.ShouldBe(HttpStatusCode.Unauthorized);
        reused.Type.ShouldEndWith("session-ended/token-reuse");

        (await RefreshAsync(client, first.Cookie)).Status.ShouldBe(HttpStatusCode.Unauthorized);
        (await _fixture.WithDbAsync(c => c.Set<UserSession>().CountAsync(s => s.EndedAt == null))).ShouldBe(0);
    }

    [Fact]
    public async Task Concurrent_refreshes_with_the_same_token_succeed_only_once()
    {
        using var client = _fixture.CreateClient();
        var session = await SignInAsync(client);

        var results = await Task.WhenAll(RefreshAsync(client, session.Cookie), RefreshAsync(client, session.Cookie));

        results.Count(r => r.Status == HttpStatusCode.OK).ShouldBeLessThanOrEqualTo(1);
    }

    [Fact]
    public async Task Missing_or_unknown_refresh_token_is_rejected()
    {
        using var client = _fixture.CreateClient();

        (await RefreshAsync(client, null)).Type.ShouldEndWith("session-ended/invalid");
        (await RefreshAsync(client, "uydurma-jeton")).Type.ShouldEndWith("session-ended/invalid");
    }

    // ------------------------------------------------------------------ tek oturum (SYG-KMLK-041, 042)

    [Fact]
    public async Task New_sign_in_ends_the_previous_session_with_a_machine_readable_reason()
    {
        using var first = _fixture.CreateClient();
        using var second = _fixture.CreateClient();
        var old = await SignInAsync(first);

        var current = await SignInAsync(second);

        (await ActivityAsync(first, AccessToken(old))).ShouldBe(HttpStatusCode.Unauthorized);
        var rejected = await RefreshAsync(first, old.Cookie);
        rejected.Type.ShouldEndWith("session-ended/signed-in-elsewhere");
        rejected.Detail.ShouldContain("başka bir cihazdan");

        // Eski sekmenin tekrar denemesi yeni cihazdaki oturumu KAPATMAZ.
        (await RefreshAsync(first, old.Cookie)).Status.ShouldBe(HttpStatusCode.Unauthorized);
        (await RefreshAsync(second, current.Cookie)).Status.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Single_session_rule_can_be_turned_off()
    {
        await SetParameterAsync(ParameterCatalog.SingleActiveSession, "false");
        using var first = _fixture.CreateClient();
        using var second = _fixture.CreateClient();
        var old = await SignInAsync(first);

        await SignInAsync(second);

        (await RefreshAsync(first, old.Cookie)).Status.ShouldBe(HttpStatusCode.OK);
    }

    // ------------------------------------------------------------------ cikis (SYG-KMLK-043)

    [Fact]
    public async Task Sign_out_revokes_the_session_on_the_server()
    {
        using var client = _fixture.CreateClient();
        var session = await SignInAsync(client);

        using var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(Base + "/current", UriKind.Relative));
        request.Headers.Add("Cookie", $"hrms_refresh={session.Cookie}");
        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        response.Headers.GetValues("Set-Cookie").Single().ShouldContain("expires=Thu, 01 Jan 1970");
        (await RefreshAsync(client, session.Cookie)).Type.ShouldEndWith("session-ended/logged-out");
        (await ActivityAsync(client, AccessToken(session))).ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ------------------------------------------------------------------ sureler (SYG-KMLK-037, 038, 039)

    [Fact]
    public async Task Idle_session_ends_after_thirty_minutes_and_refresh_does_not_count_as_activity()
    {
        using var client = _fixture.CreateClient();
        var session = await SignInAsync(client);

        _fixture.Clock.Advance(TimeSpan.FromMinutes(20));
        var refreshed = await RefreshAsync(client, session.Cookie);
        refreshed.Status.ShouldBe(HttpStatusCode.OK);

        _fixture.Clock.Advance(TimeSpan.FromMinutes(11));
        (await RefreshAsync(client, refreshed.Cookie)).Type.ShouldEndWith("session-ended/idle-timeout");
    }

    [Fact]
    public async Task Activity_keeps_the_session_alive_but_not_beyond_eight_hours()
    {
        using var client = _fixture.CreateClient();
        var cookie = (await SignInAsync(client)).Cookie;

        // 19 x 25 dk = 7 sa 55 dk: her adimda etkinlik oldugu icin oturum acik kalir.
        for (var step = 0; step < 19; step++)
        {
            _fixture.Clock.Advance(TimeSpan.FromMinutes(25));
            var refreshed = await RefreshAsync(client, cookie);
            refreshed.Status.ShouldBe(HttpStatusCode.OK);
            cookie = refreshed.Cookie;
            (await ActivityAsync(client, refreshed.AccessToken!)).ShouldBe(HttpStatusCode.NoContent);
        }

        // 8 saat asilinca etkinlik olsa da oturum biter (SYG-KMLK-037).
        _fixture.Clock.Advance(TimeSpan.FromMinutes(25));
        (await RefreshAsync(client, cookie)).Type.ShouldEndWith("session-ended/expired");
    }

    [Fact]
    public async Task Activity_signal_is_accepted_at_most_twice_a_minute()
    {
        using var client = _fixture.CreateClient();
        var token = AccessToken(await SignInAsync(client));

        (await ActivityAsync(client, token)).ShouldBe(HttpStatusCode.NoContent);
        (await ActivityAsync(client, token)).ShouldBe(HttpStatusCode.NoContent);
        (await ActivityAsync(client, token)).ShouldBe(HttpStatusCode.TooManyRequests);

        _fixture.Clock.Advance(TimeSpan.FromMinutes(1));
        (await ActivityAsync(client, token)).ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Access_token_lifetime_follows_the_application_clock()
    {
        // Uygulama saati gercek saatten gunlerce ileride: jetonun omru sistem saatine gore
        // olculseydi gecerli jeton reddedilir, suresi dolmus jeton kabul edilirdi (#97).
        _fixture.Clock.Advance(TimeSpan.FromDays(30));
        using var client = _fixture.CreateClient();
        var token = AccessToken(await SignInAsync(client));

        (await ActivityAsync(client, token)).ShouldBe(HttpStatusCode.NoContent);

        _fixture.Clock.Advance(TimeSpan.FromMinutes(16));
        (await ActivityAsync(client, token)).ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Activity_requires_a_valid_access_token()
    {
        using var client = _fixture.CreateClient();

        (await ActivityAsync(client, null)).ShouldBe(HttpStatusCode.Unauthorized);
        (await ActivityAsync(client, "gecersiz.jeton.degeri")).ShouldBe(HttpStatusCode.Unauthorized);
    }

    // ------------------------------------------------------------------ iki adimli dogrulama (SYG-KMLK-034, 036)

    [Fact]
    public async Task Two_factor_on_requires_a_code_and_uses_the_shared_code_infrastructure()
    {
        // SYG-KMLK-036, 080: 2FA kapaliyken giris tek adimdir (diger testler); sistemde ve
        // kullanicinin kendi tercihinde acikken iki adim.
        await SetParameterAsync(ParameterCatalog.TwoFactorEnabled, "true");
        await EnableTwoFactorPreferenceAsync();
        using var client = _fixture.CreateClient();

        var signIn = await SignInAsync(client);
        signIn.Body.GetProperty("status").GetString().ShouldBe("verificationRequired");
        signIn.SetCookie.ShouldBeNull();
        var challenge = signIn.Body.GetProperty("challenge");
        challenge.GetProperty("channels").EnumerateArray().Select(c => c.GetString()).ShouldBe(["email", "sms"]);
        var id = challenge.GetProperty("challengeId").GetString();

        (await client.PostJsonAsync($"{Base}/challenges/{id}/code", new { channel = "sms" })).Status.ShouldBe(HttpStatusCode.OK);
        var message = _fixture.Messages.All.Single();
        message.Purpose.ShouldBe(NotificationPurpose.TwoFactorCode);
        message.Channel.ShouldBe(NotificationChannel.Sms);

        var (wrongStatus, wrong) = await client.PostJsonAsync($"{Base}/challenges/{id}/verification", new { code = _fixture.Messages.LastCode() == "000000" ? "111111" : "000000" });
        wrongStatus.ShouldBe(HttpStatusCode.OK);
        wrong.GetProperty("result").GetString().ShouldBe("mismatch");

        using var response = await client.PostAsJsonAsync(new Uri($"{Base}/challenges/{id}/verification", UriKind.Relative), new { code = _fixture.Messages.LastCode() });
        var verified = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        verified.GetProperty("result").GetString().ShouldBe("verified");
        verified.GetProperty("session").GetProperty("accessToken").GetString().ShouldNotBeNullOrWhiteSpace();
        response.Headers.GetValues("Set-Cookie").Single().ShouldStartWith("hrms_refresh=");
    }

    [Fact]
    public async Task Two_factor_challenge_expires()
    {
        await SetParameterAsync(ParameterCatalog.TwoFactorEnabled, "true");
        await EnableTwoFactorPreferenceAsync();
        using var client = _fixture.CreateClient();
        var id = (await SignInAsync(client)).Body.GetProperty("challenge").GetProperty("challengeId").GetString();

        _fixture.Clock.Advance(LoginChallenge.Lifetime);

        (await client.PostJsonAsync($"{Base}/challenges/{id}/code", new { channel = "email" })).Status.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Two_factor_wrong_password_is_rejected_before_any_code()
    {
        await SetParameterAsync(ParameterCatalog.TwoFactorEnabled, "true");
        await EnableTwoFactorPreferenceAsync();
        using var client = _fixture.CreateClient();

        (await SignInAsync(client, Email, "Yanlis parola 1")).Status.ShouldBe(HttpStatusCode.Unauthorized);
        _fixture.Messages.All.ShouldBeEmpty();
    }

    [Fact]
    public async Task System_two_factor_on_but_user_preference_off_signs_in_with_a_single_step()
    {
        // SYG-KMLK-080: tercih varsayilan kapali; sistem parametresi tek basina kod istetmez.
        await SetParameterAsync(ParameterCatalog.TwoFactorEnabled, "true");
        using var client = _fixture.CreateClient();

        var signIn = await SignInAsync(client);

        signIn.Body.GetProperty("status").GetString().ShouldBe("signedIn");
        signIn.Cookie.ShouldNotBeNull();
        _fixture.Messages.All.ShouldBeEmpty();
    }

    [Fact]
    public async Task User_preference_on_but_system_two_factor_off_signs_in_with_a_single_step()
    {
        // SYG-KMLK-080: sistemde kapaliyken kimseden kod istenmez.
        await EnableTwoFactorPreferenceAsync();
        using var client = _fixture.CreateClient();

        var signIn = await SignInAsync(client);

        signIn.Body.GetProperty("status").GetString().ShouldBe("signedIn");
        signIn.Cookie.ShouldNotBeNull();
        _fixture.Messages.All.ShouldBeEmpty();
    }

    // ------------------------------------------------------------------ yardimcilar

    private sealed record SignInOutcome(HttpStatusCode Status, JsonElement Body, string? SetCookie, string? Cookie);

    private sealed record RefreshOutcome(HttpStatusCode Status, string Type, string Detail, string? Cookie, string? AccessToken);

    private Task<int> EnableTwoFactorPreferenceAsync() =>
        _fixture.WithDbAsync(async context =>
        {
            (await context.Set<UserAccount>().SingleAsync()).EnableTwoFactor();
            return await context.SaveChangesAsync();
        });

    private async Task CreateAccountAsync(int index) =>
        await _fixture.WithServicesAsync(async services =>
        {
            var context = services.GetRequiredService<Infrastructure.Data.HrmsDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher>();
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
            context.Add(UserAccount.Register(person.Id, hasher.Hash(PasswordPolicy.Normalize(Password)), _fixture.Clock.UtcNow));
            return await context.SaveChangesAsync();
        });

    private Task<int> SetParameterAsync(ParameterDefinition parameter, string value) =>
        _fixture.WithServicesAsync(async services =>
        {
            await services.GetRequiredService<ISystemParameterEditor>().UpdateAsync(parameter.Key, value, CancellationToken.None);
            return 0;
        });

    private static async Task<SignInOutcome> SignInAsync(HttpClient client, string email = Email, string password = Password)
    {
        using var response = await client.PostAsJsonAsync(new Uri(Base, UriKind.Relative), new { email, password });
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
        var setCookie = response.Headers.TryGetValues("Set-Cookie", out var values) ? values.Single() : null;
        return new SignInOutcome(response.StatusCode, body, setCookie, CookieValue(setCookie));
    }

    private static string AccessToken(SignInOutcome outcome) =>
        outcome.Body.GetProperty("session").GetProperty("accessToken").GetString()!;

    private static async Task<RefreshOutcome> RefreshAsync(HttpClient client, string? cookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Base + "/refresh", UriKind.Relative));
        if (cookie is not null)
        {
            request.Headers.Add("Cookie", $"hrms_refresh={cookie}");
        }

        using var response = await client.SendAsync(request);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var setCookie = response.Headers.TryGetValues("Set-Cookie", out var values) ? values.Single() : null;

        return response.StatusCode == HttpStatusCode.OK
            ? new RefreshOutcome(response.StatusCode, string.Empty, string.Empty, CookieValue(setCookie), body.GetProperty("accessToken").GetString())
            : new RefreshOutcome(response.StatusCode, body.GetProperty("type").GetString()!, body.GetProperty("detail").GetString()!, null, null);
    }

    private static async Task<HttpStatusCode> ActivityAsync(HttpClient client, string? accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Base + "/activity", UriKind.Relative));
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private static string? CookieValue(string? setCookie) =>
        setCookie is null || !setCookie.StartsWith("hrms_refresh=", StringComparison.Ordinal)
            ? null
            : setCookie["hrms_refresh=".Length..].Split(';')[0];
}
