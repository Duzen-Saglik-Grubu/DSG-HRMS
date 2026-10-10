using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Application.Identity.Verification;
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
/// Kullanicinin kendi iki adimli dogrulama tercihi uctan uca: gercek HTTP boru hatti ve gercek
/// PostgreSQL (SYG-KMLK-034, 080; REQ-KMLK-053: her durum test edilir). Veriler SENTETIKTIR.
/// </summary>
/// <remarks>Kisi 1'in e-postasi ve cep telefonu, kisi 2'nin yalnizca e-postasi vardir.</remarks>
[Collection(ApiHostGroup.Name)]
public sealed class TwoFactorPreferenceApiTests : IClassFixture<RegistrationApiFixture>, IAsyncLifetime
{
    private const string Sessions = "/api/v1/identity/sessions";
    private const string TwoFactor = "/api/v1/identity/account/two-factor";
    private const string Email = "ahmet.yilmaz@duzen.com.tr";
    private const string SecondEmail = "mehmet.kaya@duzen.com.tr";
    private const string Password = "Kediler uyur 7";
    private const string WrongPassword = "Yanlis parola 1";

    private readonly RegistrationApiFixture _fixture;
    private DateTimeOffset _since;

    public TwoFactorPreferenceApiTests(RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        _since = _fixture.Clock.UtcNow;
        await CreateAccountAsync(1);
        await CreateAccountAsync(2);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    // ------------------------------------------------------------------ durum

    [Fact]
    public async Task Status_shows_availability_preference_and_only_the_channels_of_the_person()
    {
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var off = (await SendAsync(client, HttpMethod.Get, TwoFactor, token)).Body;
        off.GetProperty("available").GetBoolean().ShouldBeFalse();
        off.GetProperty("enabled").GetBoolean().ShouldBeFalse();
        Channels(off).ShouldBe(["email", "sms"]);

        await SetParameterAsync(ParameterCatalog.TwoFactorEnabled, "true");
        (await SendAsync(client, HttpMethod.Get, TwoFactor, token)).Body.GetProperty("available").GetBoolean().ShouldBeTrue();

        // Kisi 2'nin cep telefonu yok; kanal parametresi yalnizca SMS'e izin verirse hic kanali kalmaz.
        var second = await SignInAsync(client, SecondEmail);
        Channels((await SendAsync(client, HttpMethod.Get, TwoFactor, second)).Body).ShouldBe(["email"]);
        await SetParameterAsync(ParameterCatalog.VerificationChannels, "sms");
        Channels((await SendAsync(client, HttpMethod.Get, TwoFactor, second)).Body).ShouldBeEmpty();
    }

    [Fact]
    public async Task Status_requires_a_session()
    {
        using var client = _fixture.CreateClient();

        (await SendAsync(client, HttpMethod.Get, TwoFactor, null)).Status.ShouldBe(HttpStatusCode.Unauthorized);
        (await SendAsync(client, HttpMethod.Post, TwoFactor + "/setup", null, new { currentPassword = Password, channel = "email" }))
            .Status.ShouldBe(HttpStatusCode.Unauthorized);
        (await SendAsync(client, HttpMethod.Post, TwoFactor + "/disable", null, new { currentPassword = Password }))
            .Status.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Public_settings_expose_whether_two_factor_is_available()
    {
        using var client = _fixture.CreateClient();

        (await SendAsync(client, HttpMethod.Get, "/api/v1/identity/public-settings", null)).Body
            .GetProperty("twoFactorAvailable").GetBoolean().ShouldBeFalse();

        await SetParameterAsync(ParameterCatalog.TwoFactorEnabled, "true");

        (await SendAsync(client, HttpMethod.Get, "/api/v1/identity/public-settings", null)).Body
            .GetProperty("twoFactorAvailable").GetBoolean().ShouldBeTrue();
    }

    // ------------------------------------------------------------------ acma

    [Fact]
    public async Task Enabling_with_a_verified_code_turns_the_preference_on_and_next_sign_in_asks_for_a_code()
    {
        await SetParameterAsync(ParameterCatalog.TwoFactorEnabled, "true");
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var setup = await SendAsync(client, HttpMethod.Post, TwoFactor + "/setup", token, new { currentPassword = Password, channel = "sms" });
        setup.Status.ShouldBe(HttpStatusCode.OK);
        setup.Body.GetProperty("codeExpiresAt").GetDateTimeOffset().ShouldBeGreaterThan(_fixture.Clock.UtcNow);
        var codeId = setup.Body.GetProperty("codeId").GetString();
        var message = _fixture.Messages.All.Single();
        message.Purpose.ShouldBe(NotificationPurpose.TwoFactorSetupCode);
        message.Channel.ShouldBe(NotificationChannel.Sms);
        message.Recipient.ShouldBe("5321234567");

        var wrong = await SendAsync(client, HttpMethod.Post, TwoFactor + "/setup/verification", token,
            new { codeId, code = _fixture.Messages.LastCode() == "000000" ? "111111" : "000000" });
        wrong.Body.GetProperty("result").GetString().ShouldBe("mismatch");
        (await PreferenceAsync(1)).ShouldBeFalse();

        var verified = await SendAsync(client, HttpMethod.Post, TwoFactor + "/setup/verification", token,
            new { codeId, code = _fixture.Messages.LastCode() });
        verified.Status.ShouldBe(HttpStatusCode.OK);
        verified.Body.GetProperty("result").GetString().ShouldBe("verified");
        (await PreferenceAsync(1)).ShouldBeTrue();
        (await SendAsync(client, HttpMethod.Get, TwoFactor, token)).Body.GetProperty("enabled").GetBoolean().ShouldBeTrue();

        // Kod tek kullanimliktir.
        (await SendAsync(client, HttpMethod.Post, TwoFactor + "/setup/verification", token, new { codeId, code = _fixture.Messages.LastCode() }))
            .Body.GetProperty("result").GetString().ShouldBe("notUsable");

        // Degisiklik denetim izinde ve kimlik olaylarinda (SYG-KMLK-058, 060).
        var personId = await PersonIdAsync(1);
        var accountId = await _fixture.WithDbAsync(c => c.Set<UserAccount>().Where(a => a.PersonId == personId).Select(a => a.PublicId).SingleAsync());
        var changes = await _fixture.WithDbAsync(c => c.ChangeLog
            .Where(e => e.EntityName == nameof(UserAccount) && e.EntityId == accountId && e.Operation == AuditOperation.Update && e.OccurredAt >= _since)
            .Select(e => e.Changes).ToListAsync());
        changes.ShouldContain(c => c.Contains("TwoFactorEnabled"));
        var events = await EventsAsync();
        events.ShouldContain(e => e.EventType == SecurityEventType.VerificationCodeSent && e.Detail == "two-factor-setup:sms");
        events.Single(e => e.EventType == SecurityEventType.TwoFactorEnabled).PersonId.ShouldBe(personId);

        // Tercih acildi: acik oturum kapanmaz, bir sonraki giris iki adimlidir (SYG-KMLK-034).
        (await SendAsync(client, HttpMethod.Get, TwoFactor, token)).Status.ShouldBe(HttpStatusCode.OK);
        _fixture.Messages.Clear();
        var signIn = await SignInRawAsync(client, Email);
        signIn.GetProperty("status").GetString().ShouldBe("verificationRequired");
        var challengeId = signIn.GetProperty("challenge").GetProperty("challengeId").GetString();
        (await client.PostJsonAsync($"{Sessions}/challenges/{challengeId}/code", new { channel = "email" })).Status.ShouldBe(HttpStatusCode.OK);
        _fixture.Messages.All.Single().Purpose.ShouldBe(NotificationPurpose.TwoFactorCode);
        var completed = await client.PostJsonAsync($"{Sessions}/challenges/{challengeId}/verification", new { code = _fixture.Messages.LastCode() });
        completed.Body.GetProperty("result").GetString().ShouldBe("verified");
        completed.Body.GetProperty("session").GetProperty("accessToken").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Enabling_is_refused_while_two_factor_is_off_in_the_system()
    {
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var refused = await SendAsync(client, HttpMethod.Post, TwoFactor + "/setup", token, new { currentPassword = Password, channel = "email" });

        refused.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        refused.Body.GetProperty("detail").GetString()!.ShouldContain("sistem yöneticisi tarafından kapatıldı");
        _fixture.Messages.All.ShouldBeEmpty();
    }

    [Fact]
    public async Task Enabling_requires_the_current_password_and_counts_wrong_attempts()
    {
        await SetParameterAsync(ParameterCatalog.TwoFactorEnabled, "true");
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var wrong = await SendAsync(client, HttpMethod.Post, TwoFactor + "/setup", token, new { currentPassword = WrongPassword, channel = "email" });

        wrong.Status.ShouldBe(HttpStatusCode.BadRequest);
        wrong.Body.GetProperty("errors").GetProperty("currentPassword")[0].GetString().ShouldBe("Mevcut parolanız hatalı.");
        _fixture.Messages.All.ShouldBeEmpty();
        (await EventsAsync()).ShouldContain(e => e.EventType == SecurityEventType.TwoFactorChangeFailed && e.Detail == "enable:invalid-current-password");

        // Acik birakilmis oturumda parola tahmini, girisle ayni kilide takilir (SYG-KMLK-033).
        for (var i = 0; i < 4; i++)
        {
            await SendAsync(client, HttpMethod.Post, TwoFactor + "/setup", token, new { currentPassword = WrongPassword, channel = "email" });
        }

        (await SendAsync(client, HttpMethod.Post, TwoFactor + "/setup", token, new { currentPassword = Password, channel = "email" }))
            .Status.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Enabling_with_a_channel_the_person_does_not_have_is_refused()
    {
        await SetParameterAsync(ParameterCatalog.TwoFactorEnabled, "true");
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client, SecondEmail);

        // Kisi 2'nin cep telefonu yok.
        var refused = await SendAsync(client, HttpMethod.Post, TwoFactor + "/setup", token, new { currentPassword = Password, channel = "sms" });

        refused.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        refused.Body.GetProperty("detail").GetString().ShouldBe("Seçilen doğrulama yöntemi kullanılamıyor.");
        _fixture.Messages.All.ShouldBeEmpty();

        // Kanal parametresi e-postaya izin vermiyorsa e-posta da kullanilamaz.
        await SetParameterAsync(ParameterCatalog.VerificationChannels, "sms");
        (await SendAsync(client, HttpMethod.Post, TwoFactor + "/setup", token, new { currentPassword = Password, channel = "email" }))
            .Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Enabling_twice_is_refused()
    {
        await SetParameterAsync(ParameterCatalog.TwoFactorEnabled, "true");
        await SetPreferenceAsync(1, true);
        using var client = _fixture.CreateClient();
        var token = await SignInWithTwoFactorAsync(client);

        (await SendAsync(client, HttpMethod.Post, TwoFactor + "/setup", token, new { currentPassword = Password, channel = "email" }))
            .Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Invalid_setup_requests_are_rejected_with_field_errors()
    {
        await SetParameterAsync(ParameterCatalog.TwoFactorEnabled, "true");
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var empty = await SendAsync(client, HttpMethod.Post, TwoFactor + "/setup", token, new { currentPassword = "", channel = "email" });
        empty.Status.ShouldBe(HttpStatusCode.BadRequest);
        empty.Body.GetProperty("errors").GetProperty("currentPassword")[0].GetString().ShouldBe("Mevcut parolanızı girin.");

        var noCode = await SendAsync(client, HttpMethod.Post, TwoFactor + "/setup/verification", token, new { codeId = Guid.NewGuid(), code = "" });
        noCode.Status.ShouldBe(HttpStatusCode.BadRequest);
        noCode.Body.GetProperty("errors").GetProperty("code")[0].GetString().ShouldBe("Doğrulama kodunu girin.");
    }

    [Fact]
    public async Task Code_of_another_person_or_another_flow_cannot_enable_two_factor()
    {
        await SetParameterAsync(ParameterCatalog.TwoFactorEnabled, "true");
        using var first = _fixture.CreateClient();
        using var second = _fixture.CreateClient();
        var firstToken = await SignInAsync(first);
        var secondToken = await SignInAsync(second, SecondEmail);

        // Kisi 2'nin kendi acma kodu kisi 1'in oturumunda kullanilamaz.
        var secondCodeId = (await SendAsync(second, HttpMethod.Post, TwoFactor + "/setup", secondToken, new { currentPassword = Password, channel = "email" }))
            .Body.GetProperty("codeId").GetString();
        var secondCode = _fixture.Messages.LastCode();
        (await SendAsync(first, HttpMethod.Post, TwoFactor + "/setup/verification", firstToken, new { codeId = secondCodeId, code = secondCode }))
            .Body.GetProperty("result").GetString().ShouldBe("notUsable");

        // Kisi 1'e baska bir amacla (parola sifirlama) gonderilmis kod da kullanilamaz.
        var personId = await PersonIdAsync(1);
        var resetCodeId = await _fixture.WithServicesAsync(async services =>
            (await services.GetRequiredService<VerificationCodeService>().IssueAsync(
                new VerificationRequest(personId, VerificationPurpose.PasswordReset, VerificationChannel.Email, Email), CancellationToken.None)).CodeId!.Value);
        (await SendAsync(first, HttpMethod.Post, TwoFactor + "/setup/verification", firstToken, new { codeId = resetCodeId, code = _fixture.Messages.LastCode() }))
            .Body.GetProperty("result").GetString().ShouldBe("notUsable");

        (await PreferenceAsync(1)).ShouldBeFalse();
        (await PreferenceAsync(2)).ShouldBeFalse();

        // Baskasinin denemesi kodu bozmaz; sahibi kullanabilir.
        (await SendAsync(second, HttpMethod.Post, TwoFactor + "/setup/verification", secondToken, new { codeId = secondCodeId, code = secondCode }))
            .Body.GetProperty("result").GetString().ShouldBe("verified");
        (await PreferenceAsync(2)).ShouldBeTrue();
    }

    [Fact]
    public async Task Completing_after_the_system_turned_two_factor_off_is_refused()
    {
        await SetParameterAsync(ParameterCatalog.TwoFactorEnabled, "true");
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);
        var codeId = (await SendAsync(client, HttpMethod.Post, TwoFactor + "/setup", token, new { currentPassword = Password, channel = "email" }))
            .Body.GetProperty("codeId").GetString();

        await SetParameterAsync(ParameterCatalog.TwoFactorEnabled, "false");

        (await SendAsync(client, HttpMethod.Post, TwoFactor + "/setup/verification", token, new { codeId, code = _fixture.Messages.LastCode() }))
            .Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await PreferenceAsync(1)).ShouldBeFalse();
    }

    // ------------------------------------------------------------------ kapatma

    [Fact]
    public async Task Disabling_with_the_current_password_turns_the_preference_off_and_sign_in_is_single_step_again()
    {
        await SetParameterAsync(ParameterCatalog.TwoFactorEnabled, "true");
        await SetPreferenceAsync(1, true);
        using var client = _fixture.CreateClient();
        var token = await SignInWithTwoFactorAsync(client);

        var wrong = await SendAsync(client, HttpMethod.Post, TwoFactor + "/disable", token, new { currentPassword = WrongPassword });
        wrong.Status.ShouldBe(HttpStatusCode.BadRequest);
        wrong.Body.GetProperty("errors").GetProperty("currentPassword")[0].GetString().ShouldBe("Mevcut parolanız hatalı.");
        (await PreferenceAsync(1)).ShouldBeTrue();

        (await SendAsync(client, HttpMethod.Post, TwoFactor + "/disable", token, new { currentPassword = Password })).Status.ShouldBe(HttpStatusCode.NoContent);

        (await PreferenceAsync(1)).ShouldBeFalse();
        var events = await EventsAsync();
        events.ShouldContain(e => e.EventType == SecurityEventType.TwoFactorChangeFailed && e.Detail == "disable:invalid-current-password");
        events.ShouldContain(e => e.EventType == SecurityEventType.TwoFactorDisabled);
        (await SignInRawAsync(client, Email)).GetProperty("status").GetString().ShouldBe("signedIn");
    }

    [Fact]
    public async Task Disabling_is_allowed_while_two_factor_is_off_in_the_system()
    {
        await SetPreferenceAsync(1, true);
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        (await SendAsync(client, HttpMethod.Post, TwoFactor + "/disable", token, new { currentPassword = Password })).Status.ShouldBe(HttpStatusCode.NoContent);

        (await PreferenceAsync(1)).ShouldBeFalse();
    }

    // ------------------------------------------------------------------ yardimcilar

    private static List<string?> Channels(JsonElement status) =>
        [.. status.GetProperty("channels").EnumerateArray().Select(c => c.GetString())];

    private static IQueryable<long> PersonQuery(Infrastructure.Data.HrmsDbContext context, int index) =>
        context.Set<Person>().Where(p => p.NationalId == NationalId(index)).Select(p => p.Id);

    private Task<long> PersonIdAsync(int index) => _fixture.WithDbAsync(c => PersonQuery(c, index).SingleAsync());

    private async Task<bool> PreferenceAsync(int index)
    {
        var personId = await PersonIdAsync(index);
        return await _fixture.WithDbAsync(c => c.Set<UserAccount>().Where(a => a.PersonId == personId).Select(a => a.TwoFactorEnabled).SingleAsync());
    }

    private async Task SetPreferenceAsync(int index, bool enabled)
    {
        var personId = await PersonIdAsync(index);
        await _fixture.WithDbAsync(async c =>
        {
            var account = await c.Set<UserAccount>().SingleAsync(a => a.PersonId == personId);
            if (enabled)
            {
                account.EnableTwoFactor();
            }
            else
            {
                account.DisableTwoFactor();
            }

            return await c.SaveChangesAsync();
        });
    }

    private Task<List<SecurityEventEntry>> EventsAsync() =>
        _fixture.WithDbAsync(context => context.Set<SecurityEventEntry>()
            .Where(e => e.OccurredAt >= _since)
            .OrderBy(e => e.Id)
            .ToListAsync());

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

    private static async Task<JsonElement> SignInRawAsync(HttpClient client, string email)
    {
        var (status, body) = await client.PostJsonAsync(Sessions, new { email, password = Password });
        status.ShouldBe(HttpStatusCode.OK);
        return body;
    }

    /// <summary>Tek adimli giris; erisim jetonunu dondurur.</summary>
    private static async Task<string> SignInAsync(HttpClient client, string email = Email)
    {
        var body = await SignInRawAsync(client, email);
        body.GetProperty("status").GetString().ShouldBe("signedIn");
        return body.GetProperty("session").GetProperty("accessToken").GetString()!;
    }

    /// <summary>Kisi 1 icin iki adimli giris; erisim jetonunu dondurur.</summary>
    private async Task<string> SignInWithTwoFactorAsync(HttpClient client)
    {
        var body = await SignInRawAsync(client, Email);
        body.GetProperty("status").GetString().ShouldBe("verificationRequired");
        var challengeId = body.GetProperty("challenge").GetProperty("challengeId").GetString();
        (await client.PostJsonAsync($"{Sessions}/challenges/{challengeId}/code", new { channel = "email" })).Status.ShouldBe(HttpStatusCode.OK);
        var verified = await client.PostJsonAsync($"{Sessions}/challenges/{challengeId}/verification", new { code = _fixture.Messages.LastCode() });
        _fixture.Messages.Clear();
        return verified.Body.GetProperty("session").GetProperty("accessToken").GetString()!;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(
        HttpClient client,
        HttpMethod method,
        string path,
        string? accessToken,
        object? body = null)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        var json = string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone();
        return (response.StatusCode, json);
    }
}
