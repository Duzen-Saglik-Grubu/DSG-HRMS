using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Domain.Reference;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Personnel;

/// <summary>
/// Kisiye gorev girisi uctan uca (SYG-PERS-019, 042, 066; ADR-0021 §2). Veriler SENTETIKTIR.
/// </summary>
/// <remarks>Kisi 1 IK kullanicisidir; gorev kisi 3'un istihdamina (00003, ise giris 01.01.2020) girilir.</remarks>
[Collection(ApiHostGroup.Name)]
public sealed class EmploymentDutiesApiTests : IClassFixture<Identity.RegistrationApiFixture>, IAsyncLifetime
{
    private const string Password = "Kediler uyur 7";
    private const string UserEmail = "ahmet.yilmaz@duzen.com.tr";

    private readonly Identity.RegistrationApiFixture _fixture;
    private Guid _biologist;
    private Guid _engineer;

    public EmploymentDutiesApiTests(Identity.RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await _fixture.WithDbAsync(context => context.Database.ExecuteSqlRawAsync("DELETE FROM personnel.employment_duty"));
        await CreateAccountAsync(1);
        (_biologist, _engineer) = await _fixture.WithDbAsync(async context =>
        {
            var biologist = ReferenceDefinition.Create(ReferenceType.Duty, $"T{Guid.NewGuid():N}"[..10], "Biyolog", 90);
            var engineer = ReferenceDefinition.Create(ReferenceType.Duty, $"T{Guid.NewGuid():N}"[..10], "Yazılım Uzmanı", 91);
            context.AddRange(biologist, engineer);
            await context.SaveChangesAsync();
            return (biologist.PublicId, engineer.PublicId);
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task New_duty_keeps_the_previous_one_as_history_and_shows_on_the_card()
    {
        // SYG-PERS-042 (PG-PERS-43): gorev degistirildiginde onceki gorev tarih araligiyla korunur.
        var (client, token) = await HrAsync();
        using (client)
        {
            (await AssignAsync(client, token, _biologist, "2021-01-01")).Status.ShouldBe(HttpStatusCode.Created);
            var (status, second) = await AssignAsync(client, token, _engineer, "2023-06-01");

            status.ShouldBe(HttpStatusCode.Created);
            second.GetProperty("name").GetString().ShouldBe("Yazılım Uzmanı");
            var history = await _fixture.WithDbAsync(context => context.Set<EmploymentDuty>().OrderBy(d => d.ValidFrom).Select(d => new { d.ValidFrom, d.ValidTo }).ToListAsync());
            history.Select(h => (h.ValidFrom, h.ValidTo)).ShouldBe([
                (new DateOnly(2021, 1, 1), (DateOnly?)new DateOnly(2023, 6, 1)),
                (new DateOnly(2023, 6, 1), (DateOnly?)null),
            ]);

            var card = (await SendAsync(client, HttpMethod.Get, $"/api/v1/personnel/persons/{await PersonIdAsync(3)}", token)).Body;
            card.GetProperty("employments")[0].GetProperty("duty").GetProperty("name").GetString().ShouldBe("Yazılım Uzmanı");
        }
    }

    [Fact]
    public async Task Entry_before_the_open_duty_or_outside_the_employment_is_refused()
    {
        var (client, token) = await HrAsync();
        using (client)
        {
            await AssignAsync(client, token, _biologist, "2022-01-01");

            (await AssignAsync(client, token, _engineer, "2021-06-01")).Status.ShouldBe(HttpStatusCode.Conflict);
            (await AssignAsync(client, token, _engineer, "2019-12-31")).Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        }
    }

    [Fact]
    public async Task Overlap_with_a_closed_period_is_a_409_from_the_database_but_a_deleted_duty_does_not_count()
    {
        // SYG-PERS-019: EXCLUDE ... WHERE deleted_at IS NULL; 23P01 500 degil 409 doner.
        var (client, token) = await HrAsync();
        using (client)
        {
            await AssignAsync(client, token, _biologist, "2021-01-01");
            var open = (await AssignAsync(client, token, _engineer, "2023-01-01")).Body.GetProperty("id").GetGuid();
            (await SendAsync(client, HttpMethod.Delete, $"{Duties(await PersonIdAsync(3))}/{open}", token)).Status.ShouldBe(HttpStatusCode.NoContent);

            var (status, problem) = await AssignAsync(client, token, _engineer, "2022-06-01");
            status.ShouldBe(HttpStatusCode.Conflict);
            problem.GetProperty("detail").GetString()!.ShouldContain("çakışıyor");

            // Silinen kaydin donemi bostur; ayni baslangicla yeniden girilebilir.
            (await AssignAsync(client, token, _engineer, "2023-01-01")).Status.ShouldBe(HttpStatusCode.Created);
            (await _fixture.WithDbAsync(context => context.Set<EmploymentDuty>().IgnoreQueryFilters().CountAsync(d => d.DeletedAt != null))).ShouldBe(1);
        }
    }

    [Fact]
    public async Task Duty_change_is_written_to_the_audit_trail()
    {
        // SYG-PERS-066.
        var (client, token) = await HrAsync();
        using (client)
        {
            var id = (await AssignAsync(client, token, _biologist, "2021-01-01")).Body.GetProperty("id").GetGuid();

            (await _fixture.WithDbAsync(context => context.ChangeLog.CountAsync(e => e.EntityName == nameof(EmploymentDuty) && e.EntityId == id && e.Operation == AuditOperation.Insert)))
                .ShouldBe(1);
        }
    }

    [Fact]
    public async Task Only_the_update_permission_can_assign_and_scope_still_applies()
    {
        // PG-PERS-43: yetkisi olmayan kullanici gorevi degistiremez. Izni olsa da kapsam disi kisi 404 alir.
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);
        (await AssignAsync(client, token, _biologist, "2021-01-01")).Status.ShouldBe(HttpStatusCode.Forbidden);

        await GrantRoleAsync(await CreateRoleAsync(Permissions.PersonUpdate));
        token = await SignInAsync(client);
        (await AssignAsync(client, token, _biologist, "2021-01-01")).Status.ShouldBe(HttpStatusCode.NotFound);
    }

    private async Task<(HttpClient Client, string Token)> HrAsync()
    {
        await GrantRoleAsync(Role.HrIdentityOperationsCode);
        var client = _fixture.CreateClient();
        return (client, await SignInAsync(client));
    }

    private static string Duties(Guid personId) => $"/api/v1/personnel/persons/{personId}/employments/00003/duties";

    private async Task<(HttpStatusCode Status, JsonElement Body)> AssignAsync(HttpClient client, string token, Guid definitionId, string validFrom) =>
        await SendAsync(client, HttpMethod.Post, Duties(await PersonIdAsync(3)), token, new { definitionId, validFrom });

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

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(HttpClient client, HttpMethod method, string path, string token, object? body = null)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone());
    }
}
