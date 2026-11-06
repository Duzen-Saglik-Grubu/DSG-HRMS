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
/// Acil durum kisileri uctan uca (PG-PERS-44; SYG-PERS-043, 052, 066). Veriler SENTETIKTIR.
/// </summary>
/// <remarks>Kisi 1 oturum acan kullanicidir; acil durum kisisi kisi 3'e girilir.</remarks>
[Collection(ApiHostGroup.Name)]
public sealed class EmergencyContactsApiTests : IClassFixture<Identity.RegistrationApiFixture>, IAsyncLifetime
{
    private const string Password = "Kediler uyur 7";
    private const string UserEmail = "ahmet.yilmaz@duzen.com.tr";
    private const string Phone = "5321112233";

    // Migration'daki ilk yakinlik tanimlari (KR-135).
    private const long SpouseDefinitionId = 21;
    private static readonly Guid Spouse = new("0192a3b4-1004-7000-8000-000000000001");
    private static readonly Guid Mother = new("0192a3b4-1004-7000-8000-000000000002");

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
            var (status, spouse) = await SendAsync(client, HttpMethod.Post, Contacts(person), token, Contact(Spouse, "Ayşe", "Deniz", "0532 111 22 33"));
            status.ShouldBe(HttpStatusCode.Created);
            spouse.GetProperty("mobilePhone").GetString().ShouldBe(Phone);
            spouse.GetProperty("relationship").GetString().ShouldBe("Eşi");
            spouse.GetProperty("relationshipId").GetGuid().ShouldBe(Spouse);

            var mother = (await SendAsync(client, HttpMethod.Post, Contacts(person), token, Contact(Mother, "Fatma", "Deniz", "5334445566"))).Body.GetProperty("id").GetGuid();
            var spouseId = spouse.GetProperty("id").GetGuid();

            var (changed, body) = await SendAsync(client, HttpMethod.Put, $"{Contacts(person)}/{spouseId}", token, Contact(Spouse, "Ayşe", "Deniz", "5339998877"));
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
    public async Task Relationship_options_are_the_active_definitions_and_need_only_the_update_permission()
    {
        // KR-135: secenekler referans veriden gelir; referans veri izni olmayan rol de formu kullanir.
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);
        (await SendAsync(client, HttpMethod.Get, Relationships, token)).Status.ShouldBe(HttpStatusCode.Forbidden);

        var inactive = await CreateRelationshipAsync("Eski Yakınlık", active: false);
        await GrantRoleAsync(await CreateRoleAsync(Permissions.PersonUpdate));
        token = await SignInAsync(client);
        var (status, options) = await SendAsync(client, HttpMethod.Get, Relationships, token);

        status.ShouldBe(HttpStatusCode.OK);
        var names = options.EnumerateArray().Select(o => o.GetProperty("name").GetString()).ToList();
        names.Take(4).ShouldBe(["Eşi", "Annesi", "Babası", "Kardeşi"]);
        options.EnumerateArray().ShouldNotContain(o => o.GetProperty("id").GetGuid() == inactive);
    }

    [Fact]
    public async Task Inactive_relationship_is_refused_for_new_choice_but_existing_record_keeps_and_shows_it()
    {
        // KR-135: pasif tanim yeni secimde kullanilamaz; eski kayitta adiyla gorunur ve kayit duzenlenebilir.
        var (client, token) = await HrAsync();
        using (client)
        {
            var person = await PersonIdAsync(3);
            var uncle = await CreateRelationshipAsync("Amcası", active: true);
            var id = (await SendAsync(client, HttpMethod.Post, Contacts(person), token, Contact(uncle, "Ali", "Deniz", Phone))).Body.GetProperty("id").GetGuid();
            await DeactivateRelationshipAsync(uncle);

            var (refused, problem) = await SendAsync(client, HttpMethod.Post, Contacts(person), token, Contact(uncle, "Veli", "Deniz", Phone));
            refused.ShouldBe(HttpStatusCode.UnprocessableEntity);
            problem.GetProperty("detail").GetString().ShouldBe("Yakınlık tanımı bulunamadı veya pasif.");

            (await SendAsync(client, HttpMethod.Put, $"{Contacts(person)}/{id}", token, Contact(uncle, "Ali", "Deniz", "5339998877"))).Status.ShouldBe(HttpStatusCode.OK);
            var card = (await SendAsync(client, HttpMethod.Get, $"/api/v1/personnel/persons/{person}", token)).Body;
            card.GetProperty("sensitive").GetProperty("emergencyContacts")[0].GetProperty("relationship").GetString().ShouldBe("Amcası");

            // Baska turdeki tanim (gorev) yakinlik olarak secilemez.
            var duty = await _fixture.WithDbAsync(context => context.Set<ReferenceDefinition>().Where(d => d.Type == ReferenceType.Title).Select(d => d.PublicId).FirstAsync());
            (await SendAsync(client, HttpMethod.Post, Contacts(person), token, Contact(duty, "Veli", "Deniz", Phone))).Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        }
    }

    [Fact]
    public async Task Missing_or_invalid_fields_come_back_under_their_own_names()
    {
        var (client, token) = await HrAsync();
        using (client)
        {
            var (status, problem) = await SendAsync(client, HttpMethod.Post, Contacts(await PersonIdAsync(3)), token, Contact(null, "Ayşe", "Deniz", "212 111 22 33"));

            status.ShouldBe(HttpStatusCode.BadRequest);
            var errors = problem.GetProperty("errors");
            errors.TryGetProperty("relationshipId", out _).ShouldBeTrue();
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
            var id = (await SendAsync(client, HttpMethod.Post, Contacts(person), token, Contact(Spouse, "Ayşe", "Deniz", Phone))).Body.GetProperty("id").GetGuid();
            await SendAsync(client, HttpMethod.Put, $"{Contacts(person)}/{id}", token, Contact(Spouse, "Ayşe", "Deniz", "5339998877"));
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
        // SYG-PERS-044: kisinin kendisi gorur; PG-PERS-44: giris izni olmayan degistiremez (KR-137).
        var self = await AddDirectlyAsync(1);
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var me = (await SendAsync(client, HttpMethod.Get, "/api/v1/personnel/persons/me", token)).Body;
        me.GetProperty("sensitive").GetProperty("emergencyContacts")[0].GetProperty("firstName").GetString().ShouldBe("Ayşe");

        var person = await PersonIdAsync(1);
        (await SendAsync(client, HttpMethod.Put, $"{Contacts(person)}/{self}", token, Contact(Spouse, "Ayşe", "Deniz", Phone))).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await SendAsync(client, HttpMethod.Post, Contacts(person), token, Contact(Spouse, "Ayşe", "Deniz", Phone))).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await SendAsync(client, HttpMethod.Delete, $"{Contacts(person)}/{self}", token)).Status.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task User_with_the_update_permission_enters_own_contacts_and_it_is_audited()
    {
        // KR-137 (#355): giris izni olan kullanici kendi acil durum kisilerini de girer.
        var (client, token) = await HrAsync();
        using (client)
        {
            var self = await PersonIdAsync(1);
            var (status, added) = await SendAsync(client, HttpMethod.Post, Contacts(self), token, Contact(Spouse, "Ayşe", "Deniz", Phone));
            status.ShouldBe(HttpStatusCode.Created);
            var id = added.GetProperty("id").GetGuid();

            (await SendAsync(client, HttpMethod.Put, $"{Contacts(self)}/{id}", token, Contact(Mother, "Fatma", "Deniz", Phone))).Status.ShouldBe(HttpStatusCode.OK);
            var me = (await SendAsync(client, HttpMethod.Get, "/api/v1/personnel/persons/me", token)).Body;
            me.GetProperty("sensitive").GetProperty("emergencyContacts")[0].GetProperty("firstName").GetString().ShouldBe("Fatma");

            (await SendAsync(client, HttpMethod.Delete, $"{Contacts(self)}/{id}", token)).Status.ShouldBe(HttpStatusCode.NoContent);
            (await _fixture.WithDbAsync(context => context.ChangeLog.CountAsync(e => e.EntityName == nameof(EmergencyContact) && e.EntityId == id))).ShouldBe(3);
        }
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
        (await SendAsync(client, HttpMethod.Post, Contacts(person), token, Contact(Spouse, "Ayşe", "Deniz", Phone))).Status.ShouldBe(HttpStatusCode.Forbidden);

        await GrantRoleAsync(await CreateRoleAsync(Permissions.PersonUpdate));
        token = await SignInAsync(client);
        (await SendAsync(client, HttpMethod.Post, Contacts(person), token, Contact(Spouse, "Ayşe", "Deniz", Phone))).Status.ShouldBe(HttpStatusCode.NotFound);
    }

    private static object Contact(Guid? relationshipId, string firstName, string lastName, string mobilePhone) =>
        new { relationshipId, firstName, lastName, mobilePhone };

    private const string Relationships = "/api/v1/personnel/emergency-contact-relationships";

    private Task<Guid> CreateRelationshipAsync(string name, bool active) =>
        _fixture.WithDbAsync(async context =>
        {
            var definition = ReferenceDefinition.Create(ReferenceType.Relationship, $"T{Guid.NewGuid():N}"[..10], name, 90);
            if (!active)
            {
                definition.Deactivate();
            }

            context.Add(definition);
            await context.SaveChangesAsync();
            return definition.PublicId;
        });

    private Task<int> DeactivateRelationshipAsync(Guid id) =>
        _fixture.WithDbAsync(async context =>
        {
            (await context.Set<ReferenceDefinition>().SingleAsync(d => d.PublicId == id)).Deactivate();
            return await context.SaveChangesAsync();
        });

    private static string Contacts(Guid personId) => $"/api/v1/personnel/persons/{personId}/emergency-contacts";

    private Task<Guid> AddDirectlyAsync(int index) =>
        _fixture.WithDbAsync(async context =>
        {
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
            var contact = EmergencyContact.For(person.Id, new EmergencyContactDetails(SpouseDefinitionId, "Ayşe", "Deniz", Phone));
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
