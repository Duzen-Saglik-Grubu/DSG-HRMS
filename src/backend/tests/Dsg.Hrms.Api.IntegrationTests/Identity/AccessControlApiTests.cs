using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Identity;

/// <summary>
/// Eylem yetkisi uctan uca: gercek HTTP boru hatti ve gercek PostgreSQL (ADR-0007 §1, §3;
/// SYG-KMLK-072, 074). Veriler SENTETIKTIR.
/// </summary>
[Collection(ApiHostGroup.Name)]
public sealed class AccessControlApiTests : IClassFixture<RegistrationApiFixture>, IAsyncLifetime
{
    private const string Sessions = "/api/v1/identity/sessions";
    private const string SyncRuns = "/api/v1/identity/sync-runs";
    private const string UserEmail = "ahmet.yilmaz@duzen.com.tr";
    private const string Password = "Kediler uyur 7";

    private readonly RegistrationApiFixture _fixture;

    public AccessControlApiTests(RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await CreateAccountAsync(1);
        await CreateAccountAsync(2);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Seeded_roles_carry_the_fixed_T3_permissions()
    {
        var grants = await _fixture.WithDbAsync(async context =>
            await (from grant in context.Set<RoleGrant>()
                   join role in context.Set<Role>() on grant.RoleId equals role.Id
                   select new { role.Code, grant.Permission }).ToListAsync());

        grants.Where(g => g.Code == Role.SystemAdministratorCode).Select(g => g.Permission)
            .ShouldBe(Permissions.All.Keys, ignoreOrder: true);
        grants.Where(g => g.Code == Role.HrIdentityOperationsCode).Select(g => g.Permission)
            .ShouldBe([Permissions.AccountView, Permissions.AccountUpdate, Permissions.InviteCreate, Permissions.SyncView], ignoreOrder: true);
    }

    [Fact]
    public async Task User_without_a_role_gets_403_with_a_Turkish_problem_details_body()
    {
        using var client = _fixture.CreateClient();
        var (token, permissions) = await SignInAsync(client, UserEmail);
        permissions.ShouldBeEmpty();

        var (status, body) = await SendAsync(client, HttpMethod.Get, SyncRuns + "/latest", token);

        status.ShouldBe(HttpStatusCode.Forbidden);
        body.GetProperty("type").GetString().ShouldEndWith("/forbidden");
        body.GetProperty("detail").GetString().ShouldBe("Bu işlem için yetkiniz bulunmuyor.");
        (await SendAsync(client, HttpMethod.Post, SyncRuns, token)).Status.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Protected_endpoints_require_a_session()
    {
        using var client = _fixture.CreateClient();

        (await SendAsync(client, HttpMethod.Get, SyncRuns + "/latest", null)).Status.ShouldBe(HttpStatusCode.Unauthorized);
        (await SendAsync(client, HttpMethod.Post, SyncRuns, null)).Status.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Bootstrap_administrator_gets_the_role_once_and_the_assignment_is_audited()
    {
        using var client = _fixture.CreateClient();

        await SignInAsync(client, "mehmet.kaya@duzen.com.tr");

        // Ikinci giris rolu yeniden atamaz (tek aktif oturum ilkini kapatir; bu jeton kullanilir).
        var (token, permissions) = await SignInAsync(client, "mehmet.kaya@duzen.com.tr");

        permissions.ShouldBe([.. Permissions.All.Keys.Order(StringComparer.Ordinal)]);
        var assignments = await _fixture.WithDbAsync(context => context.Set<UserRole>().ToListAsync());
        assignments.Count.ShouldBe(1);
        (await _fixture.WithDbAsync(context => context.ChangeLog.CountAsync(e => e.EntityName == nameof(UserRole) && e.EntityId == assignments[0].PublicId)))
            .ShouldBe(1);

        var (status, body) = await SendAsync(client, HttpMethod.Get, SyncRuns + "/latest", token);
        status.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("enabled").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Hr_role_can_view_the_sync_status_but_cannot_start_a_run()
    {
        await AssignRoleAsync(1, Role.HrIdentityOperationsCode);
        using var client = _fixture.CreateClient();
        var (token, permissions) = await SignInAsync(client, UserEmail);

        permissions.ShouldContain(Permissions.SyncView);
        permissions.ShouldNotContain(Permissions.SyncCreate);
        (await SendAsync(client, HttpMethod.Get, SyncRuns + "/latest", token)).Status.ShouldBe(HttpStatusCode.OK);
        (await SendAsync(client, HttpMethod.Post, SyncRuns, token)).Status.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Manual_run_is_refused_when_synchronisation_is_not_configured()
    {
        using var client = _fixture.CreateClient();
        var (token, _) = await SignInAsync(client, "mehmet.kaya@duzen.com.tr");

        var (status, body) = await SendAsync(client, HttpMethod.Post, SyncRuns, token);

        status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        body.GetProperty("detail").GetString().ShouldBe("Personel senkronizasyonu bu ortamda tanımlı değil.");
    }

    [Fact]
    public async Task Removing_a_role_takes_effect_on_the_next_request()
    {
        // Izinler erisim jetonunda tasinmaz: rol kaldirilinca ayni jetonla yetki kalmaz.
        await AssignRoleAsync(1, Role.HrIdentityOperationsCode);
        using var client = _fixture.CreateClient();
        var (token, _) = await SignInAsync(client, UserEmail);
        (await SendAsync(client, HttpMethod.Get, SyncRuns + "/latest", token)).Status.ShouldBe(HttpStatusCode.OK);

        await _fixture.WithDbAsync(context => context.Set<UserRole>().ExecuteDeleteAsync());

        (await SendAsync(client, HttpMethod.Get, SyncRuns + "/latest", token)).Status.ShouldBe(HttpStatusCode.Forbidden);
    }

    // ------------------------------------------------------------------ yardimcilar

    private async Task CreateAccountAsync(int index) =>
        await _fixture.WithServicesAsync(async services =>
        {
            var context = services.GetRequiredService<Infrastructure.Data.HrmsDbContext>();
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

    private static async Task<(string Token, string[] Permissions)> SignInAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync(new Uri(Sessions, UriKind.Relative), new { email, password = Password });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var session = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.GetProperty("session");
        var permissions = session.GetProperty("user").GetProperty("permissions").EnumerateArray().Select(p => p.GetString()!).ToArray();
        return (session.GetProperty("accessToken").GetString()!, permissions);
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
