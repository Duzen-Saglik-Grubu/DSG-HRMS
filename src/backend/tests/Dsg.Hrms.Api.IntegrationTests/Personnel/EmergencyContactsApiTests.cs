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

namespace Dsg.Hrms.Api.IntegrationTests.Personnel;

/// <summary>
/// Acil durum kisileri uctan uca (PG-PERS-44; SYG-PERS-043, 052, 066). Veriler SENTETIKTIR.
/// </summary>
/// <remarks>Kisi 1 oturum acan kullanicidir; acil durum kisisi kisi 3'e girilir.</remarks>
[Collection(ApiHostGroup.Name)]
public sealed class EmergencyContactsApiTests : IClassFixture<Identity.RegistrationApiFixture>, IAsyncLifetime
{
    private const string Password = "Kediler uyur 7";
    private const string UserEmail = "ahmet.yilmaz@duzen.com.tr";
    private const string Phone = "5321112233";

    private readonly Identity.RegistrationApiFixture _fixture;

    public EmergencyContactsApiTests(Identity.RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await _fixture.WithDbAsync(context => context.Database.ExecuteSqlRawAsync("DELETE FROM personnel.emergency_contact"));
        await CreateAccountAsync(1);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Hr_adds_two_contacts_changes_one_deletes_one_and_the_card_shows_the_result()
    {
        // PG-PERS-44 kabul olcutu: birden fazla kisi girilir, degistirilir, silinir.
        var (client, token) = await HrAsync();
        using (client)
        {
            var person = await PersonIdAsync(3);
            var (status, spouse) = await SendAsync(client, HttpMethod.Post, Contacts(person), token, Contact("  Eşi ", "Ayşe", "Deniz", "0532 111 22 33"));
            status.ShouldBe(HttpStatusCode.Created);
            spouse.GetProperty("mobilePhone").GetString().ShouldBe(Phone);
            spouse.GetProperty("relationship").GetString().ShouldBe("Eşi");

            var mother = (await SendAsync(client, HttpMethod.Post, Contacts(person), token, Contact("Annesi", "Fatma", "Deniz", "5334445566"))).Body.GetProperty("id").GetGuid();
            var spouseId = spouse.GetProperty("id").GetGuid();

            var (changed, body) = await SendAsync(client, HttpMethod.Put, $"{Contacts(person)}/{spouseId}", token, Contact("Eşi", "Ayşe", "Deniz", "5339998877"));
            changed.ShouldBe(HttpStatusCode.OK);
            body.GetProperty("mobilePhone").GetString().ShouldBe("5339998877");

            (await SendAsync(client, HttpMethod.Delete, $"{Contacts(person)}/{mother}", token)).Status.ShouldBe(HttpStatusCode.NoContent);
            (await SendAsync(client, HttpMethod.Delete, $"{Contacts(person)}/{mother}", token)).Status.ShouldBe(HttpStatusCode.NotFound);

            var card = (await SendAsync(client, HttpMethod.Get, $"/api/v1/personnel/persons/{person}", token)).Body;
            var contacts = card.GetProperty("sensitive").GetProperty("emergencyContacts");
            contacts.GetArrayLength().ShouldBe(1);
            contacts[0].GetProperty("mobilePhone").GetString().ShouldBe("5339998877");

            // Yumusak silme: kayit veritabaninda kalir.
            (await _fixture.WithDbAsync(context => context.Set<EmergencyContact>().IgnoreQueryFilters().CountAsync(c => c.DeletedAt != null))).ShouldBe(1);
        }
    }

    [Fact]
    public async Task Missing_or_invalid_fields_come_back_under_their_own_names()
    {
        var (client, token) = await HrAsync();
        using (client)
        {
            var (status, problem) = await SendAsync(client, HttpMethod.Post, Contacts(await PersonIdAsync(3)), token, Contact("", "Ayşe", "Deniz", "212 111 22 33"));

            status.ShouldBe(HttpStatusCode.BadRequest);
            var errors = problem.GetProperty("errors");
            errors.TryGetProperty("relationship", out _).ShouldBeTrue();
            errors.GetProperty("mobilePhone")[0].GetString()!.ShouldContain("5 ile başlayan");
            errors.TryGetProperty("firstName", out _).ShouldBeFalse();
        }
    }

    [Fact]
    public async Task Changes_are_written_to_the_audit_trail_with_values_masked()
    {
        // SYG-PERS-066: degisiklik denetim izinde; kisisel veri siniflandirmasina gore maskeli.
        var (client, token) = await HrAsync();
        using (client)
        {
            var person = await PersonIdAsync(3);
            var id = (await SendAsync(client, HttpMethod.Post, Contacts(person), token, Contact("Eşi", "Ayşe", "Deniz", Phone))).Body.GetProperty("id").GetGuid();
            await SendAsync(client, HttpMethod.Put, $"{Contacts(person)}/{id}", token, Contact("Eşi", "Ayşe", "Deniz", "5339998877"));
            await SendAsync(client, HttpMethod.Delete, $"{Contacts(person)}/{id}", token);

            var entries = await _fixture.WithDbAsync(context => context.ChangeLog.Where(e => e.EntityName == nameof(EmergencyContact) && e.EntityId == id).ToListAsync());

            entries.Count.ShouldBe(3);
            entries.ShouldContain(e => e.Operation == AuditOperation.Insert);
            entries.ShouldAllBe(e => !e.Changes.Contains(Phone) && !e.Changes.Contains("5339998877") && !e.Changes.Contains("Ayşe"));
        }
    }

    [Fact]
    public async Task The_person_sees_own_contacts_on_profile_but_cannot_change_them()
    {
        // SYG-PERS-044: kisinin kendisi gorur; PG-PERS-44: degistiremez.
        var self = await AddDirectlyAsync(1);
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var me = (await SendAsync(client, HttpMethod.Get, "/api/v1/personnel/persons/me", token)).Body;
        me.GetProperty("sensitive").GetProperty("emergencyContacts")[0].GetProperty("firstName").GetString().ShouldBe("Ayşe");

        var person = await PersonIdAsync(1);
        (await SendAsync(client, HttpMethod.Put, $"{Contacts(person)}/{self}", token, Contact("Eşi", "Ayşe", "Deniz", Phone))).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await SendAsync(client, HttpMethod.Post, Contacts(person), token, Contact("Eşi", "Ayşe", "Deniz", Phone))).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await SendAsync(client, HttpMethod.Delete, $"{Contacts(person)}/{self}", token)).Status.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Viewer_without_sensitive_permission_gets_no_contacts_in_the_card()
    {
        // Yetki sizintisi testi (SYG-PERS-052): alan yanitta hic yer almaz.
        await AddDirectlyAsync(3);
        await GrantRoleAsync(await CreateRoleAsync(Permissions.PersonView));
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var (status, card) = await SendAsync(client, HttpMethod.Get, $"/api/v1/personnel/persons/{await PersonIdAsync(3)}", token);

        status.ShouldBe(HttpStatusCode.OK);
        card.TryGetProperty("sensitive", out _).ShouldBeFalse();
        card.GetRawText().ShouldNotContain(Phone);
    }

    [Fact]
    public async Task Only_the_update_permission_can_enter_and_scope_still_applies()
    {
        // Izni olmayan 403; izni olsa da kapsam disi kisi 404 alir (SYG-PERS-051).
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);
        var person = await PersonIdAsync(3);
        (await SendAsync(client, HttpMethod.Post, Contacts(person), token, Contact("Eşi", "Ayşe", "Deniz", Phone))).Status.ShouldBe(HttpStatusCode.Forbidden);

        await GrantRoleAsync(await CreateRoleAsync(Permissions.PersonUpdate));
        token = await SignInAsync(client);
        (await SendAsync(client, HttpMethod.Post, Contacts(person), token, Contact("Eşi", "Ayşe", "Deniz", Phone))).Status.ShouldBe(HttpStatusCode.NotFound);
    }

    private static object Contact(string relationship, string firstName, string lastName, string mobilePhone) =>
        new { relationship, firstName, lastName, mobilePhone };

    private static string Contacts(Guid personId) => $"/api/v1/personnel/persons/{personId}/emergency-contacts";

    private Task<Guid> AddDirectlyAsync(int index) =>
        _fixture.WithDbAsync(async context =>
        {
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
            var contact = EmergencyContact.For(person.Id, new EmergencyContactDetails("Eşi", "Ayşe", "Deniz", Phone));
            context.Add(contact);
            await context.SaveChangesAsync();
            return contact.PublicId;
        });

    private async Task<(HttpClient Client, string Token)> HrAsync()
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
