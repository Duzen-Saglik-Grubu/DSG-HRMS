using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Identity;

/// <summary>
/// Kimlik olaylari denetim izine yazilir (SYG-KMLK-060). Gercek HTTP boru hatti ve gercek
/// PostgreSQL. Veriler SENTETIKTIR.
/// </summary>
/// <remarks>
/// Kayitlar degistirilemez (KR-060): testler arasinda silinemez. Her test, fixture saatinin test
/// basindaki anindan sonraki olaylara bakar.
/// </remarks>
[Collection(ApiHostGroup.Name)]
public sealed partial class SecurityEventApiTests : IClassFixture<RegistrationApiFixture>, IAsyncLifetime
{
    private const string Sessions = "/api/v1/identity/sessions";
    private const string Registrations = "/api/v1/identity/registrations";
    private const string Email = "ahmet.yilmaz@duzen.com.tr";
    private const string Password = "Kediler uyur 7";
    private const string NewPassword = "Mavi deniz 42 kez";
    private const string ClientIp = "10.20.30.40";

    private readonly RegistrationApiFixture _fixture;
    private DateTimeOffset _since;

    public SecurityEventApiTests(RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _since = _fixture.Clock.UtcNow;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Registration_records_start_code_verification_and_account_creation()
    {
        using var client = _fixture.CreateClient(forwardedFor: ClientIp);

        var (_, started) = await client.PostJsonAsync(Registrations, new { nationalId = NationalId(1), birthDate = "1985-04-12", email = Email });
        var id = started.GetProperty("registrationId").GetString();
        await client.PostJsonAsync($"{Registrations}/{id}/code", new { channel = "email" });
        await client.PostJsonAsync($"{Registrations}/{id}/verification", new { code = "000000" == _fixture.Messages.LastCode() ? "111111" : "000000" });
        await client.PostJsonAsync($"{Registrations}/{id}/verification", new { code = _fixture.Messages.LastCode() });
        (await client.PostJsonAsync($"{Registrations}/{id}/account", new { password = Password })).Status.ShouldBe(HttpStatusCode.Created);

        var personId = await PersonIdAsync(1);
        var events = await EventsAsync();
        events.Select(e => (e.EventType, e.Detail)).ShouldBe(
        [
            (SecurityEventType.RegistrationStarted, "registration:matched"),
            (SecurityEventType.VerificationCodeSent, "registration:email"),
            (SecurityEventType.VerificationFailed, "registration:mismatch"),
            (SecurityEventType.VerificationSucceeded, "registration:verified"),
            (SecurityEventType.AccountCreated, "registration"),
        ]);
        events.ShouldAllBe(e => e.PersonId == personId && e.IpAddress == ClientIp && e.TraceId != null);
    }

    [Fact]
    public async Task Unmatched_registration_is_recorded_without_a_person()
    {
        using var client = _fixture.CreateClient();

        var (_, started) = await client.PostJsonAsync(Registrations, new { nationalId = NationalId(1), birthDate = "1990-01-01", email = Email });
        var id = started.GetProperty("registrationId").GetString();
        await client.PostJsonAsync($"{Registrations}/{id}/code", new { channel = "email" });
        await client.PostJsonAsync($"{Registrations}/{id}/verification", new { code = "123456" });

        var events = await EventsAsync();
        events.Select(e => (e.EventType, e.Detail)).ShouldBe(
        [
            (SecurityEventType.RegistrationStarted, "registration:unmatched"),
            (SecurityEventType.VerificationFailed, "registration:unmatched"),
        ]);
        events.ShouldAllBe(e => e.PersonId == null && e.UserAccountId == null);
    }

    [Fact]
    public async Task Sign_in_failure_lockout_success_and_sign_out_are_recorded()
    {
        var accountId = await CreateAccountAsync(1);
        using var client = _fixture.CreateClient(forwardedFor: ClientIp);

        await SignInAsync(client, "yok.boyle@duzen.com.tr", Password);
        for (var i = 0; i < 5; i++)
        {
            await SignInAsync(client, Email, "Yanlis parola 1");
        }

        await SignInAsync(client, Email, Password); // kilitli

        var events = await EventsAsync();
        events[0].ShouldSatisfyAllConditions(
            e => e.EventType.ShouldBe(SecurityEventType.SignInFailed),
            e => e.UserAccountId.ShouldBeNull(),
            e => e.Detail.ShouldBe("invalid-credentials"));
        events.Count(e => e.EventType == SecurityEventType.SignInFailed && e.UserAccountId == accountId && e.Detail == "invalid-credentials").ShouldBe(5);
        events.Single(e => e.EventType == SecurityEventType.AccountLocked).ShouldSatisfyAllConditions(
            e => e.UserAccountId.ShouldBe(accountId),
            e => e.Detail.ShouldBe("sign-in"));
        events[^1].ShouldSatisfyAllConditions(
            e => e.EventType.ShouldBe(SecurityEventType.SignInFailed),
            e => e.Detail.ShouldBe("locked"));
        events.ShouldAllBe(e => e.IpAddress == ClientIp);
    }

    [Fact]
    public async Task Session_open_and_close_are_recorded_with_the_reason()
    {
        var accountId = await CreateAccountAsync(1);
        using var client = _fixture.CreateClient();

        var (cookie, _) = await SignInAsync(client, Email, Password);
        using (var request = new HttpRequestMessage(HttpMethod.Delete, new Uri(Sessions + "/current", UriKind.Relative)))
        {
            request.Headers.Add("Cookie", $"hrms_refresh={cookie}");
            (await client.SendAsync(request)).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        }

        var events = await EventsAsync();
        events.Select(e => (e.EventType, e.Detail)).ShouldBe(
        [
            (SecurityEventType.SignInSucceeded, "password"),
            (SecurityEventType.SessionEnded, "logged-out"),
        ]);
        events.ShouldAllBe(e => e.UserAccountId == accountId);
    }

    [Fact]
    public async Task Token_reuse_ends_all_sessions_and_is_recorded()
    {
        var accountId = await CreateAccountAsync(1);
        using var client = _fixture.CreateClient();
        var (cookie, _) = await SignInAsync(client, Email, Password);

        (await RefreshAsync(client, cookie)).ShouldBe(HttpStatusCode.OK);
        (await RefreshAsync(client, cookie)).ShouldBe(HttpStatusCode.Unauthorized); // eski jeton tekrar

        var events = await EventsAsync();
        events.ShouldContain(e => e.EventType == SecurityEventType.TokenReuseDetected && e.UserAccountId == accountId);
        events.ShouldContain(e => e.EventType == SecurityEventType.SessionEnded && e.Detail == "token-reuse");
    }

    [Fact]
    public async Task Password_change_and_its_failure_are_recorded()
    {
        var accountId = await CreateAccountAsync(1);
        using var client = _fixture.CreateClient();
        var (_, token) = await SignInAsync(client, Email, Password);

        (await ChangeAsync(client, token, "Yanlis parola 1", NewPassword)).ShouldBe(HttpStatusCode.BadRequest);
        (await ChangeAsync(client, token, Password, NewPassword)).ShouldBe(HttpStatusCode.NoContent);

        var events = await EventsAsync();
        events.Where(e => e.EventType is SecurityEventType.PasswordChangeFailed or SecurityEventType.PasswordChanged)
            .Select(e => (e.EventType, e.Detail, e.UserAccountId, e.ActorUserAccountId))
            .ShouldBe(
            [
                (SecurityEventType.PasswordChangeFailed, "invalid-current-password", accountId, accountId),
                (SecurityEventType.PasswordChanged, null, accountId, accountId),
            ]);
    }

    [Fact]
    public async Task Password_reset_is_recorded_and_ends_the_sessions()
    {
        var accountId = await CreateAccountAsync(1);
        using var client = _fixture.CreateClient();
        await SignInAsync(client, Email, Password);

        var (_, started) = await client.PostJsonAsync("/api/v1/identity/password-resets", new { nationalId = NationalId(1), birthDate = "1985-04-12", email = Email });
        var id = started.GetProperty("registrationId").GetString();
        await client.PostJsonAsync($"/api/v1/identity/password-resets/{id}/code", new { channel = "email" });
        await client.PostJsonAsync($"/api/v1/identity/password-resets/{id}/verification", new { code = _fixture.Messages.LastCode() });
        (await client.PostJsonAsync($"/api/v1/identity/password-resets/{id}/password", new { password = NewPassword })).Status.ShouldBe(HttpStatusCode.NoContent);

        var events = await EventsAsync();
        events.ShouldContain(e => e.EventType == SecurityEventType.RegistrationStarted && e.Detail == "password-reset:matched");
        events.ShouldContain(e => e.EventType == SecurityEventType.VerificationSucceeded && e.Detail == "password-reset:verified");
        events.ShouldContain(e => e.EventType == SecurityEventType.PasswordReset && e.UserAccountId == accountId);
        events.ShouldContain(e => e.EventType == SecurityEventType.SessionEnded && e.Detail == "password-changed" && e.UserAccountId == accountId);
    }

    [Fact]
    public async Task Events_carry_no_personal_data()
    {
        await CreateAccountAsync(1);
        using var client = _fixture.CreateClient();
        await SignInAsync(client, Email, "Yanlis parola 1");
        await SignInAsync(client, Email, Password);

        var events = await EventsAsync();
        events.ShouldNotBeEmpty();
        events.ShouldAllBe(e => e.Detail == null || DetailPattern().IsMatch(e.Detail));
    }

    [Fact]
    public async Task Events_cannot_be_changed_or_deleted()
    {
        await CreateAccountAsync(1);
        using var client = _fixture.CreateClient();
        await SignInAsync(client, Email, "Yanlis parola 1");

        var id = (await EventsAsync()).Single().Id;

        // Veritabani tetikleyicisi son savunma hattidir (KR-060).
        await Should.ThrowAsync<PostgresException>(() => _fixture.WithDbAsync(context =>
            context.Database.ExecuteSqlAsync($"UPDATE audit.security_event SET detail = 'x' WHERE id = {id}")));
        await Should.ThrowAsync<PostgresException>(() => _fixture.WithDbAsync(context =>
            context.Database.ExecuteSqlAsync($"DELETE FROM audit.security_event WHERE id = {id}")));
    }

    // ------------------------------------------------------------------ yardimcilar

    [GeneratedRegex("^[a-z]+(-[a-z]+)*(:[a-z]+(-[a-z]+)*)?$")]
    private static partial Regex DetailPattern();

    private Task<List<SecurityEventEntry>> EventsAsync() =>
        _fixture.WithDbAsync(context => context.Set<SecurityEventEntry>()
            .Where(e => e.OccurredAt >= _since)
            .OrderBy(e => e.Id)
            .ToListAsync());

    private Task<long> PersonIdAsync(int index) =>
        _fixture.WithDbAsync(context => context.Set<Person>().Where(p => p.NationalId == NationalId(index)).Select(p => p.Id).SingleAsync());

    private Task<long> CreateAccountAsync(int index) =>
        _fixture.WithServicesAsync(async services =>
        {
            var context = services.GetRequiredService<Infrastructure.Data.HrmsDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher>();
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
            var account = UserAccount.Register(person.Id, hasher.Hash(PasswordPolicy.Normalize(Password)), _fixture.Clock.UtcNow);
            context.Add(account);
            await context.SaveChangesAsync();
            return account.Id;
        });

    private static async Task<(string? Cookie, string? AccessToken)> SignInAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync(new Uri(Sessions, UriKind.Relative), new { email, password });
        if (response.StatusCode != HttpStatusCode.OK)
        {
            return (null, null);
        }

        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        var cookie = response.Headers.GetValues("Set-Cookie").Single()["hrms_refresh=".Length..].Split(';')[0];
        return (cookie, body.GetProperty("session").GetProperty("accessToken").GetString());
    }

    private static async Task<HttpStatusCode> RefreshAsync(HttpClient client, string? cookie)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(Sessions + "/refresh", UriKind.Relative));
        request.Headers.Add("Cookie", $"hrms_refresh={cookie}");
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private static async Task<HttpStatusCode> ChangeAsync(HttpClient client, string? accessToken, string currentPassword, string newPassword)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/v1/identity/account/password", UriKind.Relative))
        {
            Content = JsonContent.Create(new { currentPassword, newPassword }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }
}
