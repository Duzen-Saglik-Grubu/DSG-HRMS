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
/// Evlilik tarihi ve evlilik yardimi uctan uca (PG-PERS-19; SYG-PERS-036, 052, 054, 066; ADR-0019 §2).
/// Veriler SENTETIKTIR.
/// </summary>
/// <remarks>Kisi 1 oturum acan kullanicidir. Kisi 3 Evli, kisi 2 Bekar.</remarks>
[Collection(ApiHostGroup.Name)]
public sealed class MarriageApiTests : IClassFixture<Identity.RegistrationApiFixture>, IAsyncLifetime
{
    private const string Password = "Kediler uyur 7";
    private const string UserEmail = "ahmet.yilmaz@duzen.com.tr";

    private readonly Identity.RegistrationApiFixture _fixture;

    public MarriageApiTests(Identity.RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await _fixture.WithDbAsync(context => context.Database.ExecuteSqlRawAsync("DELETE FROM personnel.person_marriage"));
        await CreateAccountAsync(1);
        await SetMaritalStatusAsync(3, MaritalStatus.Married);
        await SetMaritalStatusAsync(2, MaritalStatus.NotMarried);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Hr_enters_changes_and_deletes_marriage_and_the_card_shows_the_result()
    {
        // PG-PERS-19 kabul olcutu: Evli personelde IK girer ve degistirir.
        var (client, token) = await HrAsync();
        using (client)
        {
            var person = await PersonIdAsync(3);
            (await MarriageOf(client, person, token)).ShouldBe((null, null, true));

            var (status, body) = await SendAsync(client, HttpMethod.Put, Marriage(person), token, new { marriageDate = "2015-06-20", allowanceReceived = false });
            status.ShouldBe(HttpStatusCode.OK);
            body.GetProperty("marriageDate").GetString().ShouldBe("2015-06-20");

            (await SendAsync(client, HttpMethod.Put, Marriage(person), token, new { marriageDate = "2015-06-21", allowanceReceived = true })).Status.ShouldBe(HttpStatusCode.OK);
            (await MarriageOf(client, person, token)).ShouldBe(("2015-06-21", true, true));

            (await SendAsync(client, HttpMethod.Delete, Marriage(person), token)).Status.ShouldBe(HttpStatusCode.NoContent);
            (await MarriageOf(client, person, token)).ShouldBe((null, null, true));
            (await SendAsync(client, HttpMethod.Delete, Marriage(person), token)).Status.ShouldBe(HttpStatusCode.NotFound);

            // Silinmis kayit yeniden girisi engellemez (kosullu tekil indeks).
            (await SendAsync(client, HttpMethod.Put, Marriage(person), token, new { marriageDate = "2015-06-20", allowanceReceived = false })).Status.ShouldBe(HttpStatusCode.OK);
            (await _fixture.WithDbAsync(context => context.Set<PersonMarriage>().IgnoreQueryFilters().CountAsync())).ShouldBe(2);
        }
    }

    [Fact]
    public async Task Changes_are_written_to_the_audit_trail_with_values_masked()
    {
        // SYG-PERS-066: degisiklik denetim izinde; degerler maskeli (ADR-0021 §6).
        var (client, token) = await HrAsync();
        using (client)
        {
            var person = await PersonIdAsync(3);
            await SendAsync(client, HttpMethod.Put, Marriage(person), token, new { marriageDate = "2015-06-20", allowanceReceived = false });
            var id = await _fixture.WithDbAsync(context => context.Set<PersonMarriage>().Select(m => m.PublicId).SingleAsync());
            await SendAsync(client, HttpMethod.Put, Marriage(person), token, new { marriageDate = "2015-06-21", allowanceReceived = true });
            await SendAsync(client, HttpMethod.Delete, Marriage(person), token);

            var entries = await _fixture.WithDbAsync(context => context.ChangeLog.Where(e => e.EntityName == nameof(PersonMarriage) && e.EntityId == id).ToListAsync());

            entries.Count.ShouldBe(3);
            entries.ShouldContain(e => e.Operation == AuditOperation.Insert);
            entries.ShouldAllBe(e => !e.Changes.Contains("2015-06-20") && !e.Changes.Contains("2015-06-21"));
        }
    }

    [Fact]
    public async Task Marriage_is_entered_only_for_a_married_person_and_kept_read_only_after_status_turns_single()
    {
        // KR-125: Bekar'a donerse bilgi korunur ve gorunur ama duzenlenemez.
        var (client, token) = await HrAsync();
        using (client)
        {
            var single = await PersonIdAsync(2);
            var (status, error) = await SendAsync(client, HttpMethod.Put, Marriage(single), token, new { marriageDate = "2015-06-20", allowanceReceived = false });
            status.ShouldBe(HttpStatusCode.UnprocessableEntity);
            error.GetProperty("detail").GetString().ShouldBe("Evlilik bilgisi yalnızca medeni hâli Evli olan kişide girilir ve değiştirilir.");
            (await MarriageOf(client, single, token)).ShouldBe((null, null, false));

            var married = await PersonIdAsync(3);
            await SendAsync(client, HttpMethod.Put, Marriage(married), token, new { marriageDate = "2015-06-20", allowanceReceived = true });
            await SetMaritalStatusAsync(3, MaritalStatus.NotMarried);

            (await MarriageOf(client, married, token)).ShouldBe(("2015-06-20", true, false));
            (await SendAsync(client, HttpMethod.Put, Marriage(married), token, new { marriageDate = "2015-06-21", allowanceReceived = true })).Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
            (await SendAsync(client, HttpMethod.Delete, Marriage(married), token)).Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        }
    }

    [Fact]
    public async Task Missing_fields_are_400_and_a_future_or_pre_birth_date_is_422()
    {
        var (client, token) = await HrAsync();
        using (client)
        {
            var person = await PersonIdAsync(3);
            var (status, body) = await SendAsync(client, HttpMethod.Put, Marriage(person), token, new { });
            status.ShouldBe(HttpStatusCode.BadRequest);
            body.GetProperty("errors").GetProperty("marriageDate")[0].GetString().ShouldBe("Evlilik tarihini girin.");
            body.GetProperty("errors").GetProperty("allowanceReceived")[0].GetString().ShouldBe("Evlilik yardımı alınıp alınmadığını seçin.");

            var future = Today.AddDays(1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var (late, lateError) = await SendAsync(client, HttpMethod.Put, Marriage(person), token, new { marriageDate = future, allowanceReceived = false });
            late.ShouldBe(HttpStatusCode.UnprocessableEntity);
            lateError.GetProperty("detail").GetString().ShouldBe("Evlilik tarihi bugünden sonra olamaz.");

            var (early, earlyError) = await SendAsync(client, HttpMethod.Put, Marriage(person), token, new { marriageDate = "1985-04-11", allowanceReceived = false });
            early.ShouldBe(HttpStatusCode.UnprocessableEntity);
            earlyError.GetProperty("detail").GetString().ShouldBe("Evlilik tarihi doğum tarihinden önce olamaz.");
        }
    }

    [Fact]
    public async Task Only_view_sensitive_sees_marriage_and_the_person_without_it_does_not_see_own()
    {
        // ADR-0019 §2: yalnizca view_sensitive; kisinin kendisi dahil degil. Goremeyenin yanitinda alan yoktur.
        await _fixture.WithDbAsync(async context =>
        {
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(1));
            person.ApplyProfile(new PersonProfile(null, null, MaritalStatus.Married, null));
            context.Add(PersonMarriage.For(person.Id, new MarriageDetails(new DateOnly(2015, 6, 20), true)));
            return await context.SaveChangesAsync();
        });
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);
        (await SendAsync(client, HttpMethod.Get, "/api/v1/personnel/persons/me", token)).Body.TryGetProperty("marriage", out _).ShouldBeFalse();

        await GrantRoleAsync(await CreateRoleAsync(Permissions.PersonView));
        token = await SignInAsync(client);
        (await SendAsync(client, HttpMethod.Get, $"/api/v1/personnel/persons/{await PersonIdAsync(3)}", token)).Body.TryGetProperty("marriage", out _).ShouldBeFalse();

        // Izin varsa kendi kartinda da gorunur: kural yalnizca izne bakar.
        await GrantRoleAsync(await CreateRoleAsync(Permissions.PersonViewSensitive));
        token = await SignInAsync(client);
        var own = (await SendAsync(client, HttpMethod.Get, "/api/v1/personnel/persons/me", token)).Body.GetProperty("marriage");
        own.GetProperty("marriageDate").GetString().ShouldBe("2015-06-20");
        own.GetProperty("editable").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Only_the_update_permission_can_enter_and_scope_still_applies()
    {
        // Yetkisi olmayan kullanici degistiremez (403). Izni olsa da kapsam disi kisi 404 alir.
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);
        var person = await PersonIdAsync(3);
        (await SendAsync(client, HttpMethod.Put, Marriage(person), token, new { marriageDate = "2015-06-20", allowanceReceived = false })).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await SendAsync(client, HttpMethod.Delete, Marriage(person), token)).Status.ShouldBe(HttpStatusCode.Forbidden);

        await GrantRoleAsync(await CreateRoleAsync(Permissions.PersonUpdate));
        token = await SignInAsync(client);
        (await SendAsync(client, HttpMethod.Put, Marriage(person), token, new { marriageDate = "2015-06-20", allowanceReceived = false })).Status.ShouldBe(HttpStatusCode.NotFound);
    }

    private static string Marriage(Guid personId) => $"/api/v1/personnel/persons/{personId}/marriage";

    private static async Task<(string? Date, bool? Allowance, bool Editable)> MarriageOf(HttpClient client, Guid personId, string token)
    {
        var marriage = (await SendAsync(client, HttpMethod.Get, $"/api/v1/personnel/persons/{personId}", token)).Body.GetProperty("marriage");
        var date = marriage.GetProperty("marriageDate");
        var allowance = marriage.GetProperty("allowanceReceived");
        return (
            date.ValueKind == JsonValueKind.Null ? null : date.GetString(),
            allowance.ValueKind == JsonValueKind.Null ? null : allowance.GetBoolean(),
            marriage.GetProperty("editable").GetBoolean());
    }

    private Task<int> SetMaritalStatusAsync(int index, MaritalStatus status) =>
        _fixture.WithDbAsync(async context =>
        {
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
            person.ApplyProfile(new PersonProfile(null, null, status, null));
            return await context.SaveChangesAsync();
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
