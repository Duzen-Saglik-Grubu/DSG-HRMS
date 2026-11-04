using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Application.Personnel.Corrections;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Notifications;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Personnel;

/// <summary>
/// "IK'ya bildir" uctan uca (SYG-PERS-045…049): kendi adina talep, IK'ya bildirim ve e-posta,
/// listeleme, kapatma, sifreleme ve yetki. Veriler SENTETIKTIR.
/// </summary>
/// <remarks>
/// Kisi 1 talebi acan, rolsuz kullanicidir. Kisi 2 IK rolundedir.
/// </remarks>
[Collection(ApiHostGroup.Name)]
public sealed class CorrectionsApiTests : IClassFixture<Identity.RegistrationApiFixture>, IAsyncLifetime
{
    private const string Corrections = "/api/v1/personnel/corrections";
    private const string Inbox = "/api/v1/notifications/inbox";
    private const string Password = "Kediler uyur 7";
    private const string RequesterEmail = "ahmet.yilmaz@duzen.com.tr";
    private const string HrEmail = "mehmet.kaya@duzen.com.tr";
    private const string HrMailbox = "ik@duzen.com.tr";

    private readonly Identity.RegistrationApiFixture _fixture;

    public CorrectionsApiTests(Identity.RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await _fixture.WithDbAsync(context => context.Database.ExecuteSqlRawAsync("DELETE FROM personnel.correction_request; DELETE FROM notification.inbox_item;"));
        await CreateAccountAsync(1);
        await CreateAccountAsync(2);
        await AssignRoleAsync(2, Role.HrIdentityOperationsCode);
        _fixture.Messages.Clear();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Person_reports_a_correction_and_every_hr_user_is_notified()
    {
        await SetParameterAsync(ParameterCatalog.CorrectionNoticeEmail, HrMailbox);
        using var client = _fixture.CreateClient();
        var requester = await SignInAsync(client, RequesterEmail);

        var (status, body) = await SendAsync(client, HttpMethod.Post, $"{Corrections}/me", requester, new { field = "address", value = "Yeni Mah. 3. Sok. No: 4" });

        status.ShouldBe(HttpStatusCode.Created);
        var requestId = body.GetProperty("id").GetGuid();

        // Talep ve bildirim ayni islemde: IK rolundeki kullanici okunmamis bildirimi gorur.
        var hr = await SignInAsync(client, HrEmail);
        (await SendAsync(client, HttpMethod.Get, $"{Inbox}/unread-count", hr)).Body.GetProperty("count").GetInt32().ShouldBe(1);
        var item = (await SendAsync(client, HttpMethod.Get, Inbox, hr)).Body.EnumerateArray().Single();
        item.GetProperty("kind").GetString().ShouldBe("correctionRequested");
        item.GetProperty("subjectId").GetGuid().ShouldBe(requestId);

        // Talebi acan kisinin kutusu bostur: bildirim yalnizca IK'ya gider.
        (await SendAsync(client, HttpMethod.Get, $"{Inbox}/unread-count", requester)).Body.GetProperty("count").GetInt32().ShouldBe(0);

        var message = _fixture.Messages.All.ShouldHaveSingleItem();
        message.Recipient.ShouldBe(HrMailbox);
        message.Purpose.ShouldBe(NotificationPurpose.CorrectionRequested);
        message.PersonId.ShouldBeNull();
    }

    [Fact]
    public async Task Notice_email_carries_no_name_field_or_value()
    {
        // SYG-PERS-047: kisisel veri, hele kan grubu, e-postada dolasmaz; yalnizca baglanti.
        await SetParameterAsync(ParameterCatalog.CorrectionNoticeEmail, HrMailbox);
        using var client = _fixture.CreateClient();
        var requester = await SignInAsync(client, RequesterEmail);

        var (_, body) = await SendAsync(client, HttpMethod.Post, $"{Corrections}/me", requester, new { field = "bloodGroup", value = "3" });

        var message = _fixture.Messages.All.ShouldHaveSingleItem();
        message.Subject.ShouldBe("Yeni düzeltme talebi");
        var text = $"{message.Subject}\n{message.MessageBody}";
        foreach (var forbidden in new[] { "Ahmet", "Yilmaz", "Yılmaz", "kan", "Kan", "A Rh", "bloodGroup", "00001" })
        {
            text.ShouldNotContain(forbidden);
        }

        message.MessageBody.ShouldContain($"/personnel/corrections?request={body.GetProperty("id").GetGuid():D}");
    }

    [Fact]
    public async Task No_email_is_sent_when_the_hr_address_is_empty()
    {
        using var client = _fixture.CreateClient();
        var requester = await SignInAsync(client, RequesterEmail);

        (await SendAsync(client, HttpMethod.Post, $"{Corrections}/me", requester, new { field = "education", value = "Lisans" })).Status.ShouldBe(HttpStatusCode.Created);

        _fixture.Messages.All.ShouldBeEmpty();
        (await _fixture.WithDbAsync(context => context.Set<InboxItem>().CountAsync())).ShouldBe(1);
    }

    [Fact]
    public async Task Second_open_request_for_the_same_field_is_rejected()
    {
        // AN-08: ayni alan icin "Yeni" durumda tek talep.
        using var client = _fixture.CreateClient();
        var requester = await SignInAsync(client, RequesterEmail);
        await SendAsync(client, HttpMethod.Post, $"{Corrections}/me", requester, new { field = "mobilePhone", value = "0532 999 88 77" });

        var (status, _) = await SendAsync(client, HttpMethod.Post, $"{Corrections}/me", requester, new { field = "mobilePhone", value = "5329998876" });
        var (_, state) = await SendAsync(client, HttpMethod.Get, $"{Corrections}/me", requester);

        status.ShouldBe(HttpStatusCode.Conflict);
        state.GetProperty("openFields").EnumerateArray().Select(f => f.GetString()).ShouldBe(["mobilePhone"]);
        state.GetProperty("bloodGroups").GetArrayLength().ShouldBe(8);
    }

    [Fact]
    public async Task Invalid_value_is_reported_on_the_value_field()
    {
        using var client = _fixture.CreateClient();
        var requester = await SignInAsync(client, RequesterEmail);

        var (status, body) = await SendAsync(client, HttpMethod.Post, $"{Corrections}/me", requester, new { field = "mobilePhone", value = "0212 123 45 67" });

        status.ShouldBe(HttpStatusCode.BadRequest);
        body.GetProperty("errors").GetProperty("value").GetArrayLength().ShouldBe(1);
        (await _fixture.WithDbAsync(context => context.Set<CorrectionRequest>().CountAsync())).ShouldBe(0);
    }

    [Fact]
    public async Task User_without_permission_cannot_list_or_complete_requests()
    {
        // Yetki sizintisi: rolsuz kullanici yalnizca kendi adina talep acar.
        using var client = _fixture.CreateClient();
        var requester = await SignInAsync(client, RequesterEmail);
        var (_, created) = await SendAsync(client, HttpMethod.Post, $"{Corrections}/me", requester, new { field = "education", value = "Lisans" });
        var id = created.GetProperty("id").GetGuid();

        (await SendAsync(client, HttpMethod.Get, Corrections, requester)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await SendAsync(client, HttpMethod.Get, $"{Corrections}/{id}", requester)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await SendAsync(client, HttpMethod.Post, $"{Corrections}/{id}/complete", requester)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await SendAsync(client, HttpMethod.Get, Corrections, token: null)).Status.ShouldBe(HttpStatusCode.Unauthorized);
        (await SendAsync(client, HttpMethod.Post, $"{Corrections}/me", token: null, new { field = "education", value = "Lisans" })).Status.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Someone_else_cannot_read_the_hr_users_notification()
    {
        using var client = _fixture.CreateClient();
        var requester = await SignInAsync(client, RequesterEmail);
        await SendAsync(client, HttpMethod.Post, $"{Corrections}/me", requester, new { field = "education", value = "Lisans" });
        var hr = await SignInAsync(client, HrEmail);
        var itemId = (await SendAsync(client, HttpMethod.Get, Inbox, hr)).Body.EnumerateArray().Single().GetProperty("id").GetGuid();

        (await SendAsync(client, HttpMethod.Post, $"{Inbox}/{itemId}/read", requester)).Status.ShouldBe(HttpStatusCode.NotFound);

        var (status, read) = await SendAsync(client, HttpMethod.Post, $"{Inbox}/{itemId}/read", hr);
        status.ShouldBe(HttpStatusCode.OK);
        read.GetProperty("readAt").ValueKind.ShouldBe(JsonValueKind.String);
        (await SendAsync(client, HttpMethod.Get, $"{Inbox}/unread-count", hr)).Body.GetProperty("count").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task Blood_group_is_stored_encrypted_and_viewing_it_is_logged()
    {
        // SYG-PERS-049: deger veritabaninda sifreli; IK'nin gormesi ozel nitelikli erisim kaydi uretir.
        using var client = _fixture.CreateClient();
        var requester = await SignInAsync(client, RequesterEmail);
        var (_, created) = await SendAsync(client, HttpMethod.Post, $"{Corrections}/me", requester, new { field = "bloodGroup", value = "3" });
        var id = created.GetProperty("id").GetGuid();

        var stored = await _fixture.WithDbAsync(context => context.Set<CorrectionRequest>().Where(r => r.PublicId == id).Select(r => r.ProposedValue).SingleAsync());
        stored.ShouldNotBeNull();
        stored.ShouldStartWith("k1:");
        stored.ShouldNotContain("A Rh");

        var before = await _fixture.WithDbAsync(context => context.AccessLog.MaxAsync(e => (long?)e.Id)) ?? 0;
        var hr = await SignInAsync(client, HrEmail);
        var (status, list) = await SendAsync(client, HttpMethod.Get, Corrections, hr);

        status.ShouldBe(HttpStatusCode.OK);
        var row = list.GetProperty("items").EnumerateArray().Single();
        row.GetProperty("field").GetString().ShouldBe("bloodGroup");
        row.GetProperty("proposedValue").GetString().ShouldBe("A Rh+");
        row.GetProperty("firstName").GetString().ShouldBe("Ahmet");

        var personId = await PersonIdAsync(1);
        var entries = await _fixture.WithDbAsync(context => context.AccessLog.Where(e => e.Id > before).ToListAsync());
        entries.ShouldContain(e => e.AccessType == AccessType.SpecialCategoryView && e.EntityId == personId);
        entries.ShouldContain(e => e.AccessType == AccessType.List && e.EntityName == CorrectionService.EntityName);
    }

    [Fact]
    public async Task Completing_deletes_the_value_and_is_audited()
    {
        // SYG-PERS-048: kapatinca deger silinir; talepte alan, durum ve tarihler kalir.
        using var client = _fixture.CreateClient();
        var requester = await SignInAsync(client, RequesterEmail);
        var (_, created) = await SendAsync(client, HttpMethod.Post, $"{Corrections}/me", requester, new { field = "address", value = "Yeni Mah. 3. Sok. No: 4" });
        var id = created.GetProperty("id").GetGuid();
        var hr = await SignInAsync(client, HrEmail);

        var (status, completed) = await SendAsync(client, HttpMethod.Post, $"{Corrections}/{id}/complete", hr);

        status.ShouldBe(HttpStatusCode.OK);
        completed.GetProperty("status").GetString().ShouldBe("completed");
        completed.GetProperty("proposedValue").ValueKind.ShouldBe(JsonValueKind.Null);
        completed.GetProperty("field").GetString().ShouldBe("address");

        var row = await _fixture.WithDbAsync(context => context.Set<CorrectionRequest>().AsNoTracking().SingleAsync(r => r.PublicId == id));
        row.ProposedValue.ShouldBeNull();
        row.CompletedAt.ShouldNotBeNull();

        // Denetim izi kapatmayi gosterir; onerilen deger ne olusturmada ne kapatmada yazilir.
        var audit = await _fixture.WithDbAsync(context => context.ChangeLog.Where(e => e.EntityId == id).ToListAsync());
        audit.Select(e => e.Operation).ShouldBe([AuditOperation.Insert, AuditOperation.Update], ignoreOrder: true);
        audit.ShouldAllBe(e => !e.Changes.Contains("Yeni Mah"));

        (await SendAsync(client, HttpMethod.Post, $"{Corrections}/{id}/complete", hr)).Status.ShouldBe(HttpStatusCode.Conflict);

        // Kapanan talep acik sayilmaz: ayni alan icin yeniden bildirilebilir.
        (await SendAsync(client, HttpMethod.Post, $"{Corrections}/me", requester, new { field = "address", value = "Baska Mah. 5" })).Status.ShouldBe(HttpStatusCode.Created);
    }

    [Fact]
    public async Task List_filters_by_status_and_defaults_to_new()
    {
        using var client = _fixture.CreateClient();
        var requester = await SignInAsync(client, RequesterEmail);
        var (_, first) = await SendAsync(client, HttpMethod.Post, $"{Corrections}/me", requester, new { field = "education", value = "Lisans" });
        await SendAsync(client, HttpMethod.Post, $"{Corrections}/me", requester, new { field = "maritalStatus", value = "married" });
        var hr = await SignInAsync(client, HrEmail);
        await SendAsync(client, HttpMethod.Post, $"{Corrections}/{first.GetProperty("id").GetGuid()}/complete", hr);

        (await SendAsync(client, HttpMethod.Get, Corrections, hr)).Body.GetProperty("items").EnumerateArray()
            .Select(r => r.GetProperty("field").GetString()).ShouldBe(["maritalStatus"]);
        (await SendAsync(client, HttpMethod.Get, $"{Corrections}?status=completed", hr)).Body.GetProperty("totalCount").GetInt32().ShouldBe(1);
        (await SendAsync(client, HttpMethod.Get, $"{Corrections}?status=all", hr)).Body.GetProperty("totalCount").GetInt32().ShouldBe(2);
    }

    private async Task CreateAccountAsync(int index) =>
        await _fixture.WithServicesAsync(async services =>
        {
            var context = services.GetRequiredService<HrmsDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher>();
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
            context.Add(UserAccount.Register(person.Id, hasher.Hash(PasswordPolicy.Normalize(Password)), _fixture.Clock.UtcNow));
            return await context.SaveChangesAsync();
        });

    private Task<int> AssignRoleAsync(int index, string roleCode) =>
        _fixture.WithDbAsync(async context =>
        {
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
            var account = await context.Set<UserAccount>().SingleAsync(a => a.PersonId == person.Id);
            var role = await context.Set<Role>().SingleAsync(r => r.Code == roleCode);
            context.Add(UserRole.Assign(account.Id, role.Id));
            return await context.SaveChangesAsync();
        });

    private Task<int> SetParameterAsync(ParameterDefinition parameter, string value) =>
        _fixture.WithServicesAsync(async services =>
        {
            await services.GetRequiredService<ISystemParameterEditor>().UpdateAsync(parameter.Key, value, CancellationToken.None);
            return 0;
        });

    private Task<Guid> PersonIdAsync(int index) =>
        _fixture.WithDbAsync(context => context.Set<Person>().Where(p => p.NationalId == NationalId(index)).Select(p => p.PublicId).SingleAsync());

    private static async Task<string> SignInAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync(new Uri("/api/v1/identity/sessions", UriKind.Relative), new { email, password = Password });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        return body.GetProperty("session").GetProperty("accessToken").GetString()!;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(HttpClient client, HttpMethod method, string path, string? token, object? body = null)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone());
    }
}
