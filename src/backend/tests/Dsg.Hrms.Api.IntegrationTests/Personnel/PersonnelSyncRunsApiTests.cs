using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Domain.Personnel.Sync;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Personnel;

/// <summary>
/// "Senkronizasyon durumu" ekraninin uclari (SYG-PERS-008, 009). Veriler SENTETIKTIR.
/// </summary>
/// <remarks>
/// Bu ortamda LOGO baglantisi tanimli degildir: elle baslatma, calisma surmuyorsa <c>422</c> alir.
/// </remarks>
[Collection(ApiHostGroup.Name)]
public sealed class PersonnelSyncRunsApiTests : IClassFixture<Identity.RegistrationApiFixture>, IAsyncLifetime
{
    private const string SyncRuns = "/api/v1/personnel/sync-runs";
    private const string Password = "Kediler uyur 7";
    private const string UserEmail = "ahmet.yilmaz@duzen.com.tr";

    private readonly Identity.RegistrationApiFixture _fixture;

    public PersonnelSyncRunsApiTests(Identity.RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await _fixture.WithDbAsync(context => context.Database.ExecuteSqlRawAsync("DELETE FROM personnel.sync_warning; DELETE FROM personnel.sync_run;"));
        await CreateAccountAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Viewing_and_starting_need_their_own_permissions()
    {
        // SYG-PERS-008, 009 (SYG-PERS-055): izinsize 403; gorme izni baslatma hakki vermez.
        using var client = _fixture.CreateClient();
        (await SendAsync(client, HttpMethod.Get, SyncRuns, token: null)).Status.ShouldBe(HttpStatusCode.Unauthorized);

        var token = await SignInAsync(client);
        (await SendAsync(client, HttpMethod.Get, SyncRuns, token)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await SendAsync(client, HttpMethod.Post, SyncRuns, token)).Status.ShouldBe(HttpStatusCode.Forbidden);

        await GrantAsync(await CreateRoleAsync(Permissions.SyncView));
        (await SendAsync(client, HttpMethod.Get, SyncRuns, token)).Status.ShouldBe(HttpStatusCode.OK);
        (await SendAsync(client, HttpMethod.Post, SyncRuns, token)).Status.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Runs_are_listed_newest_first_with_their_counts()
    {
        // SYG-PERS-008 (PG-PERS-06): sayfali, en yeni once; sayilar calisma kaydiyla ayni.
        var now = _fixture.Clock.UtcNow;
        await AddRunAsync(now.AddMinutes(-60), SyncTrigger.Scheduled, run => run.Complete(new SyncCounts(600, 2, 3, 40, 4, 50, 1, 0), [], now.AddMinutes(-59)));
        await AddRunAsync(now.AddMinutes(-30), SyncTrigger.Manual, run => run.Fail(SyncFailureReason.SchemaDrift, now.AddMinutes(-30)));
        await AddRunAsync(now.AddMinutes(-15), SyncTrigger.Scheduled, run => run.CompletePartially(new SyncCounts(601, 0, 0, 1, 0, 2, 0, 0), [], SyncFailureReason.SourceUnavailable, now.AddMinutes(-14)));

        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);
        await GrantAsync(Role.HrIdentityOperationsCode);

        var (status, body) = await SendAsync(client, HttpMethod.Get, SyncRuns + "?page=1&pageSize=2", token);

        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("enabled").GetBoolean().ShouldBeFalse();
        body.GetProperty("running").GetBoolean().ShouldBeFalse();
        var runs = body.GetProperty("runs");
        runs.GetProperty("totalCount").GetInt32().ShouldBe(3);
        runs.GetProperty("totalPages").GetInt32().ShouldBe(2);
        var items = runs.GetProperty("items").EnumerateArray().ToList();
        items.Select(i => i.GetProperty("status").GetString()).ShouldBe(["partiallyCompleted", "failed"]);
        items[0].GetProperty("failureReason").GetString().ShouldBe("sourceUnavailable");
        items[1].GetProperty("failureReason").GetString().ShouldBe("schemaDrift");
        items[1].GetProperty("trigger").GetString().ShouldBe("manual");

        var (_, second) = await SendAsync(client, HttpMethod.Get, SyncRuns + "?page=2&pageSize=2", token);
        var oldest = second.GetProperty("runs").GetProperty("items").EnumerateArray().Single();
        oldest.GetProperty("status").GetString().ShouldBe("succeeded");
        oldest.GetProperty("trigger").GetString().ShouldBe("scheduled");
        oldest.GetProperty("finishedAt").GetDateTimeOffset().ShouldBe(now.AddMinutes(-59));
        oldest.GetProperty("recordsRead").GetInt32().ShouldBe(600);
        oldest.GetProperty("personsCreated").GetInt32().ShouldBe(3);
        oldest.GetProperty("personsUpdated").GetInt32().ShouldBe(40);
        oldest.GetProperty("employmentsCreated").GetInt32().ShouldBe(4);
        oldest.GetProperty("employmentsUpdated").GetInt32().ShouldBe(50);
        oldest.GetProperty("employmentsDeactivated").GetInt32().ShouldBe(1);
        oldest.GetProperty("warningCount").GetInt32().ShouldBe(0);

        (await SendAsync(client, HttpMethod.Get, SyncRuns + "?pageSize=101", token)).Status.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Start_is_refused_while_a_run_is_in_progress()
    {
        // SYG-PERS-009: calisma surerken ikinci baslatma reddedilir ve ekranda "calisiyor" gorunur.
        var now = _fixture.Clock.UtcNow;
        await AddRunAsync(now.AddMinutes(-90), SyncTrigger.Scheduled, _ => { });
        await AddRunAsync(now.AddMinutes(-1), SyncTrigger.Manual, _ => { });

        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);
        await GrantAsync(Role.HrIdentityOperationsCode);

        var (_, body) = await SendAsync(client, HttpMethod.Get, SyncRuns, token);
        body.GetProperty("running").GetBoolean().ShouldBeTrue();
        // Daha eski "suruyor" kaydi yarida kalmistir (uygulama calisma sirasinda durdu).
        body.GetProperty("runs").GetProperty("items").EnumerateArray().Select(i => i.GetProperty("status").GetString())
            .ShouldBe(["running", "interrupted"]);

        var (status, problem) = await SendAsync(client, HttpMethod.Post, SyncRuns, token);
        status.ShouldBe(HttpStatusCode.Conflict);
        problem.GetProperty("detail").GetString().ShouldBe("Senkronizasyon şu anda çalışıyor. Bitince yeniden başlatabilirsiniz.");
    }

    [Fact]
    public async Task Start_reaches_the_trigger_when_no_run_is_in_progress()
    {
        // Bu ortamda senkronizasyon tanimli degil: istek yetkiden ve "calisiyor" denetiminden gecer, 422 alir.
        var now = _fixture.Clock.UtcNow;
        await AddRunAsync(now.AddMinutes(-10), SyncTrigger.Scheduled, run => run.Fail(SyncFailureReason.SourceUnavailable, now.AddMinutes(-10)));

        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);
        await GrantAsync(Role.HrIdentityOperationsCode);

        var (status, problem) = await SendAsync(client, HttpMethod.Post, SyncRuns, token);
        status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        problem.GetProperty("detail").GetString().ShouldBe("Personel senkronizasyonu bu ortamda tanımlı değil.");
    }

    private async Task AddRunAsync(DateTimeOffset startedAt, SyncTrigger trigger, Action<PersonnelSyncRun> finish) =>
        await _fixture.WithDbAsync(async context =>
        {
            var run = PersonnelSyncRun.Start(trigger, startedAt);
            finish(run);
            context.Add(run);
            return await context.SaveChangesAsync();
        });

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

    private Task<int> GrantAsync(string roleCode) =>
        _fixture.WithDbAsync(async context =>
        {
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(1));
            var account = await context.Set<UserAccount>().SingleAsync(a => a.PersonId == person.Id);
            var role = await context.Set<Role>().SingleAsync(r => r.Code == roleCode);
            context.Add(UserRole.Assign(account.Id, role.Id));
            return await context.SaveChangesAsync();
        });

    private async Task CreateAccountAsync() =>
        await _fixture.WithServicesAsync(async services =>
        {
            var context = services.GetRequiredService<HrmsDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher>();
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(1));
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

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(HttpClient client, HttpMethod method, string path, string? token)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone());
    }
}
