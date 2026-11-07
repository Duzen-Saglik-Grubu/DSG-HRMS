using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Audit;

/// <summary>
/// Denetim kayitlari ekrani uctan uca (PG-PERS-28, 30; SYG-PERS-069; ADR-0022 §2). Veriler SENTETIKTIR.
/// </summary>
/// <remarks>
/// Denetim tablolari degistirilemez (KR-060); testler arasinda temizlenmez. Bu yuzden her test kendi
/// kayitlarini Mart 2001'e yazar ve o araligi sorgular. Kisi 1 oturum acan kullanicidir; kisi 2'nin de
/// hesabi vardir. Kayitlar, yeniden eskiye: guvenlik olayi (15.03), disa aktarma (14.03), liste (13.03),
/// kart goruntuleme (12.03), sistemin degisikligi (11.03), kisi 1'in degisikligi (10.03).
/// </remarks>
[Collection(ApiHostGroup.Name)]
public sealed class AuditEntriesApiTests : IClassFixture<Identity.RegistrationApiFixture>, IAsyncLifetime
{
    private const string Password = "Kediler uyur 7";
    private const string UserEmail = "ahmet.yilmaz@duzen.com.tr";
    private const string Entries = "/api/v1/audit/entries";
    private const string March = "from=2001-03-01&to=2001-03-31";

    private readonly Identity.RegistrationApiFixture _fixture;
    private string _marker = string.Empty;

    public AuditEntriesApiTests(Identity.RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await CreateAccountAsync(1);
        await CreateAccountAsync(2);
        _marker = Guid.NewGuid().ToString("N");
        await SeedAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Four_kinds_are_listed_newest_first_with_names_and_masked_details()
    {
        // SYG-PERS-069: dort kayit turu tek, sayfali, salt okunur listede; maskeli degerler maskeli kalir.
        var (client, token) = await AuditorAsync();
        using (client)
        {
            var (status, body) = await SendAsync(client, HttpMethod.Get, $"{Entries}?{March}&pageSize=100", token);

            status.ShouldBe(HttpStatusCode.OK);
            var items = Mine(body);
            items.Select(i => i.GetProperty("type").GetString()).ShouldBe(["security", "export", "access", "access", "change", "change"]);
            items.Select(i => i.GetProperty("action").GetString()).ShouldBe(["SignInSucceeded", "Export", "List", "View", "Insert", "Update"]);

            var security = items[0];
            security.GetProperty("actor").GetProperty("fullName").GetString().ShouldBe("Ahmet Yilmaz");
            security.GetProperty("subject").GetProperty("personId").GetGuid().ShouldBe(await PersonIdAsync(2));
            security.GetProperty("details").GetString().ShouldBe("password");

            var export = items[1];
            export.GetProperty("recordCount").GetInt32().ShouldBe(1);
            export.GetProperty("subjectCount").GetInt32().ShouldBe(1);

            items[2].GetProperty("subjectCount").GetInt32().ShouldBe(2);
            items[3].GetProperty("subject").GetProperty("personId").GetGuid().ShouldBe(await PersonIdAsync(3));

            // Arka plan islemi: islemi yapan yok ("Sistem").
            items[4].GetProperty("actor").ValueKind.ShouldBe(JsonValueKind.Null);
            items[5].GetProperty("details").GetString()!.ShouldContain("***");
            items[5].GetProperty("details").GetString()!.ShouldNotContain("gercek@duzen.com.tr");
        }
    }

    [Fact]
    public async Task Type_actor_subject_and_paging_filters_work()
    {
        var (client, token) = await AuditorAsync();
        using (client)
        {
            (await TypesAsync(client, token, "type=export")).ShouldBe(["export"]);
            (await TypesAsync(client, token, "type=access")).ShouldBe(["access", "access"]);
            (await TypesAsync(client, token, "type=security")).ShouldBe(["security"]);
            (await TypesAsync(client, token, "type=change")).ShouldBe(["change", "change"]);

            // Islemi yapan kisi 2: yalnizca kart goruntuleme. Kisi 1: guvenlik olayi, disa aktarma, liste
            // ve degisiklik; arka plan isleminin degisikligi kimsenin degildir.
            (await TypesAsync(client, token, $"actorId={await PersonIdAsync(2)}")).ShouldBe(["access"]);
            (await TypesAsync(client, token, $"actorId={await PersonIdAsync(1)}")).ShouldBe(["security", "export", "access", "change"]);

            // Verisi goruntulenen kisi 3: kart goruntuleme, listede ve dosyada gorunmesi, kisi kaydi degisikligi.
            (await TypesAsync(client, token, $"subjectId={await PersonIdAsync(3)}")).ShouldBe(["export", "access", "access", "change"]);
            // Kisi 2 guvenlik olayinin konusudur.
            (await TypesAsync(client, token, $"subjectId={await PersonIdAsync(2)}")).ShouldBe(["security"]);

            // Bilinmeyen kisi: kayit yok.
            (await TypesAsync(client, token, $"actorId={Guid.NewGuid()}")).ShouldBeEmpty();

            // Tarih araligi gun olarak dahildir (Europe/Istanbul).
            (await TypesAsync(client, token, "from=2001-03-12&to=2001-03-13")).ShouldBe(["access", "access"]);
        }
    }

    [Fact]
    public async Task Viewing_the_screen_is_access_logged()
    {
        // ADR-0022 §2: ekrana bakmak da erisim kaydina yazilir.
        var (client, token) = await AuditorAsync();
        using (client)
        {
            var before = await _fixture.WithDbAsync(context => context.AccessLog.MaxAsync(e => (long?)e.Id)) ?? 0;

            await SendAsync(client, HttpMethod.Get, $"{Entries}?{March}&type=change", token);

            var entries = await _fixture.WithDbAsync(context => context.AccessLog.Where(e => e.Id > before).ToListAsync());
            entries.Count.ShouldBe(1);
            entries[0].AccessType.ShouldBe(AccessType.List);
            entries[0].EntityName.ShouldBe("AuditLog");
            entries[0].Filters.ShouldContain("type");
        }
    }

    [Fact]
    public async Task Only_audit_log_view_opens_the_list_and_the_endpoint_is_read_only()
    {
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);
        (await SendAsync(client, HttpMethod.Get, $"{Entries}?{March}", token)).Status.ShouldBe(HttpStatusCode.Forbidden);

        await GrantRoleAsync(await CreateRoleAsync(Permissions.AuditLogView));
        token = await SignInAsync(client);
        (await SendAsync(client, HttpMethod.Get, $"{Entries}?{March}", token)).Status.ShouldBe(HttpStatusCode.OK);

        // Kayitlari degistiren bir yol yoktur.
        (await SendAsync(client, HttpMethod.Delete, Entries, token)).Status.ShouldBe(HttpStatusCode.MethodNotAllowed);
        (await SendAsync(client, HttpMethod.Post, Entries, token)).Status.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    [Theory]
    [InlineData("from=2001-03-31&to=2001-03-01")]
    [InlineData("pageSize=101")]
    [InlineData("page=0")]
    [InlineData("page=101&pageSize=100")]
    public async Task Invalid_range_page_or_depth_is_a_400(string query)
    {
        var (client, token) = await AuditorAsync();
        using (client)
        {
            (await SendAsync(client, HttpMethod.Get, $"{Entries}?{query}", token)).Status.ShouldBe(HttpStatusCode.BadRequest);
        }
    }

    /// <summary>Bu testin kayitlari: diger testlerin Mart 2001 kayitlari isaretle ayrilir.</summary>
    private List<JsonElement> Mine(JsonElement body) =>
        [.. body.GetProperty("items").EnumerateArray().Where(i => i.GetProperty("traceId").GetString() == _marker)];

    private async Task<string?[]> TypesAsync(HttpClient client, string token, string filter)
    {
        var query = filter.StartsWith("from=", StringComparison.Ordinal) ? filter : $"{March}&{filter}";
        var (status, body) = await SendAsync(client, HttpMethod.Get, $"{Entries}?{query}&pageSize=100", token);
        status.ShouldBe(HttpStatusCode.OK);
        return [.. Mine(body).Select(i => i.GetProperty("type").GetString())];
    }

    private Task<int> SeedAsync() =>
        _fixture.WithDbAsync(async context =>
        {
            var accounts = await (
                from a in context.Set<UserAccount>()
                join p in context.Set<Person>() on a.PersonId equals p.Id
                select new { a.Id, p.NationalId }).ToListAsync();
            var first = accounts.Single(a => a.NationalId == NationalId(1)).Id;
            var second = accounts.Single(a => a.NationalId == NationalId(2)).Id;
            var persons = await context.Set<Person>().Select(p => new { p.Id, p.PublicId, p.NationalId }).ToListAsync();
            var two = persons.Single(p => p.NationalId == NationalId(2));
            var three = persons.Single(p => p.NationalId == NationalId(3)).PublicId;
            var five = persons.Single(p => p.NationalId == NationalId(5)).PublicId;
            DateTimeOffset At(int day) => new(2001, 3, day, 10, 0, 0, TimeSpan.Zero);

            context.ChangeLog.Add(new ChangeLogEntry
            {
                OccurredAt = At(10),
                UserAccountId = first,
                EntityName = "Person",
                EntityId = three,
                Operation = AuditOperation.Update,
                Changes = """{"Email":{"old":"***","new":"***"}}""",
                TraceId = _marker,
            });
            context.ChangeLog.Add(new ChangeLogEntry
            {
                OccurredAt = At(11),
                UserAccountId = null,
                EntityName = "EmergencyContact",
                EntityId = Guid.NewGuid(),
                Operation = AuditOperation.Insert,
                Changes = "{}",
                TraceId = _marker,
            });
            context.AccessLog.Add(new AccessLogEntry
            {
                OccurredAt = At(12),
                UserAccountId = second,
                AccessType = AccessType.View,
                EntityName = "Person",
                EntityId = three,
                RecordCount = 1,
                Filters = "{}",
                TraceId = _marker,
            });
            context.AccessLog.Add(new AccessLogEntry
            {
                OccurredAt = At(13),
                UserAccountId = first,
                AccessType = AccessType.List,
                EntityName = "Person",
                RecordCount = 2,
                Filters = """{"search":"***"}""",
                SubjectIds = [three, five],
                TraceId = _marker,
            });
            context.AccessLog.Add(new AccessLogEntry
            {
                OccurredAt = At(14),
                UserAccountId = first,
                AccessType = AccessType.Export,
                EntityName = "Person",
                RecordCount = 1,
                Filters = "{}",
                SubjectIds = [three],
                TraceId = _marker,
            });
            context.SecurityEvents.Add(new SecurityEventEntry
            {
                OccurredAt = At(15),
                EventType = SecurityEventType.SignInSucceeded,
                UserAccountId = second,
                PersonId = two.Id,
                ActorUserAccountId = first,
                Detail = "password",
                TraceId = _marker,
            });
            return await context.SaveChangesAsync();
        });

    private async Task<(HttpClient Client, string Token)> AuditorAsync()
    {
        await GrantRoleAsync(Role.HrIdentityOperationsCode);
        var client = _fixture.CreateClient();
        return (client, await SignInAsync(client));
    }

    private Task<Guid> PersonIdAsync(int index) =>
        _fixture.WithDbAsync(context => context.Set<Person>().Where(p => p.NationalId == NationalId(index)).Select(p => p.PublicId).SingleAsync());

    private Task<string> CreateRoleAsync(params string[] permissions) =>
        _fixture.WithDbAsync(async context =>
        {
            var code = $"test-{Guid.NewGuid():N}";
            await context.Database.ExecuteSqlAsync(
                $"INSERT INTO identity.role (public_id, code, name, is_system, created_at) VALUES ({Guid.CreateVersion7()}, {code}, {"Test rolu"}, false, now())");
            foreach (var permission in permissions)
            {
                await context.Database.ExecuteSqlAsync(
                    $"INSERT INTO identity.role_permission (role_id, permission) SELECT id, {permission} FROM identity.role WHERE code = {code}");
            }

            return code;
        });

    private Task<int> GrantRoleAsync(string roleCode) =>
        _fixture.WithDbAsync(async context =>
        {
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(1));
            var account = await context.Set<UserAccount>().SingleAsync(a => a.PersonId == person.Id);
            var role = await context.Set<Role>().SingleAsync(r => r.Code == roleCode);
            if (!await context.Set<UserRole>().AnyAsync(r => r.UserAccountId == account.Id && r.RoleId == role.Id))
            {
                context.Add(UserRole.Assign(account.Id, role.Id));
            }

            return await context.SaveChangesAsync();
        });

    private async Task CreateAccountAsync(int index) =>
        await _fixture.WithServicesAsync(async services =>
        {
            var context = services.GetRequiredService<HrmsDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher>();
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
            context.Add(UserAccount.Register(person.Id, hasher.Hash(PasswordPolicy.Normalize(Password)), _fixture.Clock.UtcNow));
            return await context.SaveChangesAsync();
        });

    private static async Task<string> SignInAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(new Uri("/api/v1/identity/sessions", UriKind.Relative), new { email = UserEmail, password = Password });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        return body.GetProperty("session").GetProperty("accessToken").GetString()!;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(HttpClient client, HttpMethod method, string path, string token)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone());
    }
}
