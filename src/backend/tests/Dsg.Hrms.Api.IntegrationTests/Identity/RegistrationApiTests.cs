using System.Globalization;
using System.Net;
using System.Text.Json;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Identity;

/// <summary>
/// Uyelik akisi uctan uca: gercek HTTP boru hatti ve gercek PostgreSQL
/// (SYG-KMLK-013…021, 044, 045, 059; <c>KR-016</c>, <c>KR-078</c>).
/// </summary>
[Collection(ApiHostGroup.Name)]
public sealed class RegistrationApiTests : IClassFixture<RegistrationApiFixture>, IAsyncLifetime
{
    private const string Base = "/api/v1/identity/registrations";

    private readonly RegistrationApiFixture _fixture;

    public RegistrationApiTests(RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => _fixture.ResetAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    private static object Start(int index, string? email = null, DateOnly? birthDate = null) => new
    {
        nationalId = NationalId(index),
        birthDate = (birthDate ?? BirthDate).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
        email = email ?? index switch
        {
            1 => "ahmet.yilmaz@duzen.com.tr",
            2 => "mehmet.kaya@duzen.com.tr",
            3 => "ortak@duzen.com.tr",
            4 => "ayrilan@duzen.com.tr",
            5 => "kisisel@gmail.com",
            _ => "yok@duzen.com.tr",
        },
    };

    private static async Task<string> StartAsync(HttpClient client, object body)
    {
        var (status, json) = await client.PostJsonAsync(Base, body);
        status.ShouldBe(HttpStatusCode.OK);
        return json.GetProperty("registrationId").GetString()!;
    }

    private static string[] Channels(JsonElement body) =>
        [.. body.GetProperty("channels").EnumerateArray().Select(c => c.GetString()!)];

    // ------------------------------------------------------------------ uctan uca

    [Fact]
    public async Task Matching_person_registers_end_to_end()
    {
        using var client = _fixture.CreateClient();

        var (status, started) = await client.PostJsonAsync(Base, Start(1, email: "Ahmet.Yilmaz@DUZEN.com.tr"));
        status.ShouldBe(HttpStatusCode.OK);
        Channels(started).ShouldBe(["email", "sms"]);
        var id = started.GetProperty("registrationId").GetString();

        var (codeStatus, code) = await client.PostJsonAsync($"{Base}/{id}/code", new { channel = "sms" });
        codeStatus.ShouldBe(HttpStatusCode.OK);
        code.GetProperty("codeExpiresAt").GetDateTimeOffset().ShouldBe(_fixture.Clock.UtcNow.AddMinutes(5));

        var message = _fixture.Messages.All.Single();
        message.Channel.ShouldBe(NotificationChannel.Sms);
        message.Recipient.ShouldBe("5321234567");

        var (verifyStatus, verified) = await client.PostJsonAsync($"{Base}/{id}/verification", new { code = _fixture.Messages.LastCode() });
        verifyStatus.ShouldBe(HttpStatusCode.OK);
        verified.GetProperty("result").GetString().ShouldBe("verified");
        verified.GetProperty("accountExists").GetBoolean().ShouldBeFalse();

        var (accountStatus, _) = await client.PostJsonAsync($"{Base}/{id}/account", new { password = "Kediler uyur 7" });
        accountStatus.ShouldBe(HttpStatusCode.Created);

        var account = await _fixture.WithDbAsync(c => c.Set<UserAccount>().SingleAsync());
        account.Status.ShouldBe(AccountStatus.Active);
        account.PasswordHash.ShouldStartWith("pbkdf2-sha512$");
    }

    [Fact]
    public async Task Registration_responses_never_reveal_the_target_and_audit_has_no_secrets()
    {
        // SYG-KMLK-018: hedef maskeli bile donmez. SYG-KMLK-026/049: kod, parola ve ozet izde yok.
        // Iz yalnizca BU testin urettigi kayitlarla denetlenir: onceki testlerden biriken
        // kayitlarda 6 haneli kodun rakamlari (zaman damgasi, kimlik) rastlantiyla gecebilir.
        var before = await _fixture.WithDbAsync(c => c.ChangeLog.MaxAsync(e => (long?)e.Id)) ?? 0;
        using var client = _fixture.CreateClient(forwardedFor: "10.9.8.7");
        var (_, started) = await client.PostJsonAsync(Base, Start(1));
        var id = started.GetProperty("registrationId").GetString();
        var (_, code) = await client.PostJsonAsync($"{Base}/{id}/code", new { channel = "email" });
        var sent = _fixture.Messages.LastCode();
        await client.PostJsonAsync($"{Base}/{id}/verification", new { code = sent });
        await client.PostJsonAsync($"{Base}/{id}/account", new { password = "Kediler uyur 7" });

        foreach (var body in new[] { started, code })
        {
            var text = body.GetRawText();
            text.ShouldNotContain("ahmet");
            text.ShouldNotContain("5321234567");
            text.ShouldNotContain("567");
        }

        var (entries, hash) = await _fixture.WithDbAsync(async c => (
            await c.ChangeLog.Where(e => e.Id > before).ToListAsync(),
            await c.Set<UserAccount>().Select(a => a.PasswordHash).SingleAsync()));

        entries.ShouldContain(e => e.EntityName == nameof(RegistrationAttempt) && e.Operation == AuditOperation.Insert);
        entries.ShouldContain(e => e.EntityName == nameof(UserAccount) && e.Operation == AuditOperation.Insert);
        entries.ShouldAllBe(e => !e.Changes.Contains(sent) && !e.Changes.Contains("Kediler") && !e.Changes.Contains(hash!) && !e.Changes.Contains(NationalId(1)));

        // SYG-KMLK-059: IP, ters vekilden gelen gercek istemci adresidir; vekilin degil.
        var attempt = await _fixture.WithDbAsync(c => c.Set<RegistrationAttempt>().SingleAsync());
        attempt.IpAddress.ShouldBe("10.9.8.7");
    }

    // ------------------------------------------------------------------ eslesme gizliligi (KR-016)

    [Theory]
    [InlineData(99)] // kisi yok
    [InlineData(3)]  // e-posta ortak (SYG-KMLK-008)
    [InlineData(4)]  // aktif istihdam yok (SYG-KMLK-013)
    [InlineData(5)]  // kurumsal alan adi disi (KR-019)
    public async Task Non_matching_start_looks_exactly_like_a_match(int index)
    {
        using var client = _fixture.CreateClient();

        var (matchStatus, match) = await client.PostJsonAsync(Base, Start(1));
        var (status, body) = await client.PostJsonAsync(Base, Start(index));

        status.ShouldBe(matchStatus);
        Channels(body).ShouldBe(Channels(match));
        body.EnumerateObject().Select(p => p.Name).ShouldBe(match.EnumerateObject().Select(p => p.Name));
        body.GetProperty("expiresAt").GetDateTimeOffset().ShouldBe(match.GetProperty("expiresAt").GetDateTimeOffset());
    }

    [Fact]
    public async Task Wrong_birth_date_or_email_is_a_non_match()
    {
        using var client = _fixture.CreateClient();

        foreach (var body in new[] { Start(1, birthDate: new DateOnly(1985, 4, 13)), Start(1, email: "baska@duzen.com.tr") })
        {
            var id = await StartAsync(client, body);
            (await client.PostJsonAsync($"{Base}/{id}/code", new { channel = "email" })).Status.ShouldBe(HttpStatusCode.OK);
        }

        _fixture.Messages.All.ShouldBeEmpty();
    }

    [Fact]
    public async Task Non_match_sends_no_code_but_behaves_like_a_real_code()
    {
        using var client = _fixture.CreateClient();
        var id = await StartAsync(client, Start(99));

        var (codeStatus, code) = await client.PostJsonAsync($"{Base}/{id}/code", new { channel = "sms" });
        codeStatus.ShouldBe(HttpStatusCode.OK);
        code.GetProperty("codeExpiresAt").GetDateTimeOffset().ShouldBe(_fixture.Clock.UtcNow.AddMinutes(5));
        _fixture.Messages.All.ShouldBeEmpty();

        var results = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            var (status, verified) = await client.PostJsonAsync($"{Base}/{id}/verification", new { code = "123456" });
            status.ShouldBe(HttpStatusCode.OK);
            verified.GetProperty("accountExists").GetBoolean().ShouldBeFalse();
            results.Add(verified.GetProperty("result").GetString()!);
        }

        // Gercek kodla ayni: 3 yanlis hakki, 4. yanlista iptal.
        results.ShouldBe(["mismatch", "mismatch", "mismatch", "attemptsExceeded", "attemptsExceeded"]);
    }

    [Fact]
    public async Task Real_code_gives_the_same_wrong_attempt_sequence()
    {
        using var client = _fixture.CreateClient();
        var id = await StartAsync(client, Start(1));
        await client.PostJsonAsync($"{Base}/{id}/code", new { channel = "email" });
        var wrong = _fixture.Messages.LastCode() == "000000" ? "111111" : "000000";

        var results = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            results.Add((await client.PostJsonAsync($"{Base}/{id}/verification", new { code = wrong })).Body.GetProperty("result").GetString()!);
        }

        results.ShouldBe(["mismatch", "mismatch", "mismatch", "attemptsExceeded", "attemptsExceeded"]);
    }

    [Fact]
    public async Task Expired_codes_look_the_same_for_match_and_non_match()
    {
        using var client = _fixture.CreateClient();
        var match = await StartAsync(client, Start(1));
        var other = await StartAsync(client, Start(99));
        await client.PostJsonAsync($"{Base}/{match}/code", new { channel = "email" });
        await client.PostJsonAsync($"{Base}/{other}/code", new { channel = "email" });
        var code = _fixture.Messages.LastCode();

        _fixture.Clock.Advance(TimeSpan.FromMinutes(6));

        (await client.PostJsonAsync($"{Base}/{match}/verification", new { code })).Body.GetProperty("result").GetString().ShouldBe("expired");
        (await client.PostJsonAsync($"{Base}/{other}/verification", new { code })).Body.GetProperty("result").GetString().ShouldBe("expired");
    }

    [Fact]
    public async Task Person_without_mobile_phone_is_offered_email_only()
    {
        // REQ-KMLK-010 oldugu gibi uygulanir; eslesmeyi dolayli ele vermesi kabul edilen risktir (KR-078).
        using var client = _fixture.CreateClient();

        var (_, body) = await client.PostJsonAsync(Base, Start(2));

        Channels(body).ShouldBe(["email"]);
        (await client.PostJsonAsync($"{Base}/{body.GetProperty("registrationId").GetString()}/code", new { channel = "sms" }))
            .Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
    }

    // ------------------------------------------------------------------ kanal degisimi (SYG-KMLK-019)

    [Fact]
    public async Task Changing_channel_invalidates_the_previous_code()
    {
        using var client = _fixture.CreateClient();
        var id = await StartAsync(client, Start(1));

        await client.PostJsonAsync($"{Base}/{id}/code", new { channel = "sms" });
        var smsCode = _fixture.Messages.LastCode();
        await client.PostJsonAsync($"{Base}/{id}/code", new { channel = "email" });
        var emailCode = _fixture.Messages.LastCode();

        _fixture.Messages.All.Select(m => m.Channel).ShouldBe([NotificationChannel.Sms, NotificationChannel.Email]);
        if (smsCode != emailCode)
        {
            (await client.PostJsonAsync($"{Base}/{id}/verification", new { code = smsCode })).Body.GetProperty("result").GetString().ShouldBe("mismatch");
        }

        (await client.PostJsonAsync($"{Base}/{id}/verification", new { code = emailCode })).Body.GetProperty("result").GetString().ShouldBe("verified");
    }

    // ------------------------------------------------------------------ hiz sinirlari (SYG-KMLK-059)

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public async Task Attempt_over_the_national_id_limit_within_an_hour_is_rejected(int index)
    {
        // PRM-KML-18 varsayilani kadar deneme (#185).
        var limit = int.Parse(ParameterCatalog.RegistrationLimitPerNationalId.DefaultValue!, CultureInfo.InvariantCulture);
        using var client = _fixture.CreateClient();
        for (var i = 0; i < limit; i++)
        {
            await StartAsync(client, Start(index));
        }

        var (status, body) = await client.PostJsonAsync(Base, Start(index));

        status.ShouldBe(HttpStatusCode.TooManyRequests);
        body.GetProperty("type").GetString()!.ShouldEndWith("too-many-requests");

        _fixture.Clock.Advance(TimeSpan.FromHours(1) + TimeSpan.FromSeconds(1));
        (await client.PostJsonAsync(Base, Start(index))).Status.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Attempts_per_ip_are_limited_using_the_forwarded_client_address()
    {
        // IP basina 20 deneme. Adres nginx'in X-Forwarded-For basligindan okunur; farkli
        // istemciler birbirinin sinirini tuketmez.
        using var first = _fixture.CreateClient(forwardedFor: "10.20.30.40");
        using var second = _fixture.CreateClient(forwardedFor: "10.20.30.41");

        for (var i = 0; i < 20; i++)
        {
            (await first.PostJsonAsync(Base, Start(100 + i))).Status.ShouldBe(HttpStatusCode.OK);
        }

        (await first.PostJsonAsync(Base, Start(200))).Status.ShouldBe(HttpStatusCode.TooManyRequests);
        (await second.PostJsonAsync(Base, Start(200))).Status.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(99)]
    public async Task Code_request_over_the_limit_within_fifteen_minutes_is_rejected_for_match_and_non_match(int index)
    {
        // PRM-KML-17 varsayilani kadar kod istegi (#185).
        var limit = int.Parse(ParameterCatalog.CodeSendLimit.DefaultValue!, CultureInfo.InvariantCulture);
        using var client = _fixture.CreateClient();
        var id = await StartAsync(client, Start(index));

        for (var i = 0; i < limit; i++)
        {
            (await client.PostJsonAsync($"{Base}/{id}/code", new { channel = "email" })).Status.ShouldBe(HttpStatusCode.OK);
        }

        (await client.PostJsonAsync($"{Base}/{id}/code", new { channel = "email" })).Status.ShouldBe(HttpStatusCode.TooManyRequests);
    }

    // ------------------------------------------------------------------ mevcut hesap (SYG-KMLK-020, 021)

    [Fact]
    public async Task Existing_account_is_revealed_only_after_verification_and_no_second_account_is_created()
    {
        using var client = _fixture.CreateClient();
        await RegisterAsync(client, 1);
        _fixture.Clock.Advance(TimeSpan.FromHours(2));

        var id = await StartAsync(client, Start(1));
        await client.PostJsonAsync($"{Base}/{id}/code", new { channel = "email" });
        var (_, verified) = await client.PostJsonAsync($"{Base}/{id}/verification", new { code = _fixture.Messages.LastCode() });

        verified.GetProperty("accountExists").GetBoolean().ShouldBeTrue();
        var (status, problem) = await client.PostJsonAsync($"{Base}/{id}/account", new { password = "Baska bir parola 9" });
        status.ShouldBe(HttpStatusCode.Conflict);
        problem.GetProperty("detail").GetString()!.ShouldContain("parola sıfırlama");
        (await _fixture.WithDbAsync(c => c.Set<UserAccount>().CountAsync())).ShouldBe(1);
    }

    [Fact]
    public async Task Account_cannot_be_created_before_verification()
    {
        using var client = _fixture.CreateClient();
        var id = await StartAsync(client, Start(1));
        await client.PostJsonAsync($"{Base}/{id}/code", new { channel = "email" });

        (await client.PostJsonAsync($"{Base}/{id}/account", new { password = "Kediler uyur 7" })).Status.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Completion_window_expires()
    {
        using var client = _fixture.CreateClient();
        var id = await StartAsync(client, Start(1));
        await client.PostJsonAsync($"{Base}/{id}/code", new { channel = "email" });
        await client.PostJsonAsync($"{Base}/{id}/verification", new { code = _fixture.Messages.LastCode() });

        _fixture.Clock.Advance(RegistrationAttempt.CompletionWindow);

        (await client.PostJsonAsync($"{Base}/{id}/account", new { password = "Kediler uyur 7" })).Status.ShouldBe(HttpStatusCode.NotFound);
    }

    // ------------------------------------------------------------------ parola (SYG-KMLK-044, 045)

    [Theory]
    [InlineData("123456789", "yaygın")]
    [InlineData("iloveyou", "yaygın")]
    [InlineData("Yilmaz1985", "adınızdan")]
    [InlineData("ahmet.yilmaz", "adınızdan")]
    [InlineData("duzen2026", "kurum")]
    [InlineData("kisa", "en az 6 karakter")]
    public async Task Weak_passwords_are_rejected_with_a_field_error(string password, string expected)
    {
        using var client = _fixture.CreateClient();
        var id = await VerifiedAsync(client, 1);

        var (status, problem) = await client.PostJsonAsync($"{Base}/{id}/account", new { password });

        status.ShouldBe(HttpStatusCode.BadRequest);
        var messages = problem.GetProperty("errors").GetProperty("password").EnumerateArray().Select(m => m.GetString()!).ToList();
        messages.ShouldContain(m => m.Contains(expected, StringComparison.OrdinalIgnoreCase));
        (await _fixture.WithDbAsync(c => c.Set<UserAccount>().CountAsync())).ShouldBe(0);
    }

    // ------------------------------------------------------------------ bicim (SYG-KMLK-014)

    [Theory]
    [InlineData("12345678901")]
    [InlineData("1234567890")]
    [InlineData("abcdefghijk")]
    public async Task Invalid_national_id_is_a_format_error_and_is_not_matched(string nationalId)
    {
        using var client = _fixture.CreateClient();

        var (status, problem) = await client.PostJsonAsync(Base, new { nationalId, birthDate = "1985-04-12", email = "a@duzen.com.tr" });

        status.ShouldBe(HttpStatusCode.BadRequest);
        problem.GetProperty("errors").GetProperty("nationalId").GetArrayLength().ShouldBe(1);
        (await _fixture.WithDbAsync(c => c.Set<RegistrationAttempt>().CountAsync())).ShouldBe(0);
    }

    [Fact]
    public async Task Unknown_registration_is_not_found()
    {
        using var client = _fixture.CreateClient();

        (await client.PostJsonAsync($"{Base}/{Guid.NewGuid()}/code", new { channel = "email" })).Status.ShouldBe(HttpStatusCode.NotFound);
        (await client.PostJsonAsync($"{Base}/{Guid.NewGuid()}/verification", new { code = "123456" })).Status.ShouldBe(HttpStatusCode.NotFound);
    }

    // ------------------------------------------------------------------ herkese acik ayarlar (SYG-KMLK-070)

    [Fact]
    public async Task Public_settings_expose_only_screen_information()
    {
        using var client = _fixture.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/identity/public-settings", UriKind.Relative));
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("supportContact").GetString().ShouldBe("Bilgi İşlem");
        body.GetProperty("passwordRules").GetProperty("minLength").GetInt32().ShouldBe(6);
        body.GetProperty("passwordRules").GetProperty("requireComplexity").GetBoolean().ShouldBeFalse();
        body.GetProperty("verificationCodeLength").GetInt32().ShouldBe(6);
        body.GetProperty("twoFactorAvailable").GetBoolean().ShouldBeFalse(); // SYG-KMLK-080

        // Yeni bir alan eklenirse bu test bilincli olarak guncellenmelidir: uc kimliksizdir.
        body.EnumerateObject().Select(p => p.Name).ShouldBe(["supportContact", "passwordRules", "verificationCodeLength", "logoVersion", "twoFactorAvailable"]);
    }

    // ------------------------------------------------------------------ yardimcilar

    private async Task<string> VerifiedAsync(HttpClient client, int index)
    {
        var id = await StartAsync(client, Start(index));
        await client.PostJsonAsync($"{Base}/{id}/code", new { channel = "email" });
        (await client.PostJsonAsync($"{Base}/{id}/verification", new { code = _fixture.Messages.LastCode() }))
            .Body.GetProperty("result").GetString().ShouldBe("verified");
        return id;
    }

    private async Task RegisterAsync(HttpClient client, int index)
    {
        var id = await VerifiedAsync(client, index);
        (await client.PostJsonAsync($"{Base}/{id}/account", new { password = "Kediler uyur 7" })).Status.ShouldBe(HttpStatusCode.Created);
    }
}
