using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
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
/// IK davet baglantisi uctan uca: gercek HTTP boru hatti ve gercek PostgreSQL
/// (SYG-KMLK-051…053). Veriler SENTETIKTIR.
/// </summary>
/// <remarks>
/// Kisi 1 IK kullanicisidir. Kisi 2'ye (mehmet.kaya@duzen.com.tr, hesabi yok) davet gonderilir.
/// Kisi 3'un adresi ortak, kisi 4'un istihdami bitmis, kisi 5'in adresi kurum disi.
/// </remarks>
[Collection(ApiHostGroup.Name)]
public sealed partial class InvitationApiTests : IClassFixture<RegistrationApiFixture>, IAsyncLifetime
{
    private const string Accounts = "/api/v1/identity/accounts";
    private const string Invitations = "/api/v1/identity/invitations";
    private const string Password = "Kediler uyur 7";
    private const string NewPassword = "Mavi deniz 42 kez";

    private readonly RegistrationApiFixture _fixture;

    public InvitationApiTests(RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await _fixture.WithDbAsync(context => context.Set<AccountInvitation>().ExecuteDeleteAsync());
        await CreateAccountAsync(1);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Invitation_goes_only_to_the_LOGO_email_and_creates_the_account_once()
    {
        var token = await HrTokenAsync();
        using var client = _fixture.CreateClient();

        var sent = await PostAsync(client, $"{Accounts}/{await PersonIdAsync(2)}/invitations", token, new { reason = "Telefonu yok, SMS alamıyor" });
        sent.Status.ShouldBe(HttpStatusCode.Accepted);

        var message = _fixture.Messages.All.Single();
        message.Recipient.ShouldBe("mehmet.kaya@duzen.com.tr");
        message.Purpose.ShouldBe(NotificationPurpose.Invitation);
        message.MessageBody.ShouldContain("/invite#token=");
        message.MessageBody.ShouldContain("3 saat");
        message.MessageBody.ShouldNotContain("Kaya"); // SYG-KMLK-030: baglanti disinda kisisel veri yok
        var invite = TokenOf(message.MessageBody);

        var (lookupStatus, info) = await PostAsync(client, $"{Invitations}/lookup", null, new { token = invite });
        lookupStatus.ShouldBe(HttpStatusCode.OK);
        info.GetProperty("firstName").GetString().ShouldBe("Ahmet");
        info.GetProperty("accountExists").GetBoolean().ShouldBeFalse();

        (await PostAsync(client, $"{Invitations}/acceptance", null, new { token = invite, password = NewPassword })).Status.ShouldBe(HttpStatusCode.NoContent);
        (await SignInStatusAsync(client, "mehmet.kaya@duzen.com.tr", NewPassword)).ShouldBe(HttpStatusCode.OK);

        // Tek kullanimlik (SYG-KMLK-052).
        (await PostAsync(client, $"{Invitations}/lookup", null, new { token = invite })).Status.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(client, $"{Invitations}/acceptance", null, new { token = invite, password = "Baska bir cumle 5" })).Status.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task A_new_invitation_revokes_the_previous_one_and_links_expire()
    {
        var token = await HrTokenAsync();
        using var client = _fixture.CreateClient();
        var person = await PersonIdAsync(2);

        await PostAsync(client, $"{Accounts}/{person}/invitations", token, new { reason = "Ilk" });
        var first = TokenOf(_fixture.Messages.All[^1].MessageBody);
        await PostAsync(client, $"{Accounts}/{person}/invitations", token, new { reason = "Yeniden" });
        var second = TokenOf(_fixture.Messages.All[^1].MessageBody);

        (await PostAsync(client, $"{Invitations}/lookup", null, new { token = first })).Status.ShouldBe(HttpStatusCode.NotFound);
        (await PostAsync(client, $"{Invitations}/lookup", null, new { token = second })).Status.ShouldBe(HttpStatusCode.OK);

        _fixture.Clock.Advance(TimeSpan.FromHours(3));
        (await PostAsync(client, $"{Invitations}/lookup", null, new { token = second })).Status.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Existing_account_gets_a_new_password_and_its_sessions_end()
    {
        await CreateAccountAsync(2);
        using var target = _fixture.CreateClient();
        var targetToken = await SignInTokenAsync(target, "mehmet.kaya@duzen.com.tr", Password);
        var token = await HrTokenAsync();
        using var client = _fixture.CreateClient();

        await PostAsync(client, $"{Accounts}/{await PersonIdAsync(2)}/invitations", token, new { reason = "Parolasını unuttu" });
        var invite = TokenOf(_fixture.Messages.All[^1].MessageBody);
        (await PostAsync(client, $"{Invitations}/lookup", null, new { token = invite })).Body.GetProperty("accountExists").GetBoolean().ShouldBeTrue();

        (await PostAsync(client, $"{Invitations}/acceptance", null, new { token = invite, password = NewPassword })).Status.ShouldBe(HttpStatusCode.NoContent);

        (await ActivityAsync(target, targetToken)).ShouldBe(HttpStatusCode.Unauthorized);
        (await SignInStatusAsync(client, "mehmet.kaya@duzen.com.tr", Password)).ShouldBe(HttpStatusCode.Unauthorized);
        (await SignInStatusAsync(client, "mehmet.kaya@duzen.com.tr", NewPassword)).ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(3, "yalnızca kendisine ait")] // ortak adres (SYG-KMLK-008)
    [InlineData(5, "kurumsal alan adında")]   // kurum disi adres
    [InlineData(4, "Aktif çalışma kaydı")]    // istihdam bitmis
    public async Task Invitation_is_refused_when_the_person_is_not_eligible(int index, string expected)
    {
        var token = await HrTokenAsync();
        using var client = _fixture.CreateClient();

        var (status, body) = await PostAsync(client, $"{Accounts}/{await PersonIdAsync(index)}/invitations", token, new { reason = "Deneme" });

        status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        body.GetProperty("detail").GetString()!.ShouldContain(expected);
        _fixture.Messages.All.ShouldBeEmpty();
    }

    [Fact]
    public async Task Sending_requires_permission_a_reason_and_the_feature_enabled()
    {
        using var client = _fixture.CreateClient();
        var person = await PersonIdAsync(2);
        var noRole = await SignInTokenAsync(client, "ahmet.yilmaz@duzen.com.tr", Password);
        (await PostAsync(client, $"{Accounts}/{person}/invitations", noRole, new { reason = "Deneme" })).Status.ShouldBe(HttpStatusCode.Forbidden);

        var token = await HrTokenAsync();
        (await PostAsync(client, $"{Accounts}/{person}/invitations", token, new { reason = " " })).Status.ShouldBe(HttpStatusCode.BadRequest);
        (await PostAsync(client, $"{Accounts}/{Guid.NewGuid()}/invitations", token, new { reason = "Deneme" })).Status.ShouldBe(HttpStatusCode.NotFound);

        await SetParameterAsync(ParameterCatalog.HrInviteEnabled, "false");
        (await PostAsync(client, $"{Accounts}/{person}/invitations", token, new { reason = "Deneme" })).Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        _fixture.Messages.All.ShouldBeEmpty();
    }

    [Fact]
    public async Task Sender_recipient_and_reason_are_audited_without_the_token()
    {
        var token = await HrTokenAsync();
        using var client = _fixture.CreateClient();

        await PostAsync(client, $"{Accounts}/{await PersonIdAsync(2)}/invitations", token, new { reason = "Telefonu yok" });
        var invite = TokenOf(_fixture.Messages.All[^1].MessageBody);

        var (entry, senderId, hash) = await _fixture.WithDbAsync(async context =>
        {
            var invitation = await context.Set<AccountInvitation>().SingleAsync();
            var log = await context.ChangeLog.SingleAsync(e => e.EntityName == nameof(AccountInvitation) && e.EntityId == invitation.PublicId && e.Operation == AuditOperation.Insert);
            var sender = await context.Set<UserAccount>().Where(a => a.Id == log.UserAccountId).Select(a => a.PersonId).SingleAsync();
            return (log, sender, invitation.TokenHash);
        });

        entry.Changes.ShouldContain("Telefonu yok");
        entry.Changes.ShouldNotContain(hash);
        entry.Changes.ShouldNotContain(invite);
        senderId.ShouldBe(await _fixture.WithDbAsync(context => context.Set<Person>().Where(p => p.NationalId == NationalId(1)).Select(p => p.Id).SingleAsync()));
    }

    [Fact]
    public async Task Unknown_or_malformed_tokens_are_not_found()
    {
        using var client = _fixture.CreateClient();

        (await PostAsync(client, $"{Invitations}/lookup", null, new { token = "yok-boyle-bir-jeton" })).Status.ShouldBe(HttpStatusCode.NotFound);
        // Bos veya bozuk baglanti: Turkce ve ne yapilacagini soyleyen ileti (SYG-KMLK-064, B-03, B-11).
        foreach (var token in new[] { "", new string('a', 101) })
        {
            var (status, body) = await PostAsync(client, $"{Invitations}/lookup", null, new { token });
            status.ShouldBe(HttpStatusCode.BadRequest);
            body.GetProperty("errors").GetProperty("token")[0].GetString()!.ShouldStartWith("Bu bağlantı geçersiz. Yeni bir bağlantı için");
        }
    }

    // ------------------------------------------------------------------ yardimcilar

    [Fact]
    public async Task Sending_and_accepting_are_recorded_as_identity_events_with_the_actor()
    {
        // SYG-KMLK-060: davet baglantisi olaylari; gonderen IK kullanicisi aktor olarak yazilir.
        var since = _fixture.Clock.UtcNow;
        var token = await HrTokenAsync();
        using var client = _fixture.CreateClient();
        await PostAsync(client, $"{Accounts}/{await PersonIdAsync(2)}/invitations", token, new { reason = "Telefonu yok, SMS alamıyor" });
        var invite = TokenOf(_fixture.Messages.All.Single().MessageBody);
        await PostAsync(client, $"{Invitations}/acceptance", null, new { token = invite, password = NewPassword });

        var (hrAccount, person2) = await _fixture.WithDbAsync(async context =>
        {
            var hrPerson = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(1));
            var hr = await context.Set<UserAccount>().SingleAsync(a => a.PersonId == hrPerson.Id);
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(2));
            return (hr.Id, person.Id);
        });
        var events = await _fixture.WithDbAsync(context => context.Set<SecurityEventEntry>()
            .Where(e => e.OccurredAt >= since && (e.EventType == SecurityEventType.InvitationSent || e.EventType == SecurityEventType.InvitationAccepted))
            .OrderBy(e => e.Id)
            .ToListAsync());

        events.Select(e => (e.EventType, e.PersonId, e.ActorUserAccountId, e.Detail)).ShouldBe(
        [
            (SecurityEventType.InvitationSent, (long?)person2, (long?)hrAccount, (string?)null),
            (SecurityEventType.InvitationAccepted, person2, null, "account-created"),
        ]);
    }

    [GeneratedRegex("#token=([A-Za-z0-9_-]+)")]
    private static partial Regex TokenPattern();

    private static string TokenOf(string body) => TokenPattern().Match(body).Groups[1].Value;

    private async Task<string> HrTokenAsync()
    {
        await _fixture.WithDbAsync(async context =>
        {
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(1));
            var account = await context.Set<UserAccount>().SingleAsync(a => a.PersonId == person.Id);
            var role = await context.Set<Role>().SingleAsync(r => r.Code == Role.HrIdentityOperationsCode);
            if (!await context.Set<UserRole>().AnyAsync(r => r.UserAccountId == account.Id))
            {
                context.Add(UserRole.Assign(account.Id, role.Id));
            }

            return await context.SaveChangesAsync();
        });

        using var client = _fixture.CreateClient();
        return await SignInTokenAsync(client, "ahmet.yilmaz@duzen.com.tr", Password);
    }

    private Task<Guid> PersonIdAsync(int index) =>
        _fixture.WithDbAsync(context => context.Set<Person>().Where(p => p.NationalId == NationalId(index)).Select(p => p.PublicId).SingleAsync());

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

    private static async Task<string> SignInTokenAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync(new Uri("/api/v1/identity/sessions", UriKind.Relative), new { email, password });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        return body.GetProperty("session").GetProperty("accessToken").GetString()!;
    }

    private static async Task<HttpStatusCode> SignInStatusAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsJsonAsync(new Uri("/api/v1/identity/sessions", UriKind.Relative), new { email, password });
        return response.StatusCode;
    }

    private static async Task<HttpStatusCode> ActivityAsync(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/v1/identity/sessions/activity", UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> PostAsync(HttpClient client, string path, string? token, object body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(path, UriKind.Relative)) { Content = JsonContent.Create(body) };
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone());
    }
}
