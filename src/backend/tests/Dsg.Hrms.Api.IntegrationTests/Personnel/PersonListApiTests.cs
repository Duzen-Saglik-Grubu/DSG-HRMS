using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Organization;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Personnel;

/// <summary>
/// Personel listesi uctan uca (SYG-PERS-022, 027, 028, 029, 030, 031, 058). Veriler SENTETIKTIR.
/// </summary>
/// <remarks>
/// Kisi 1 oturum acan kullanicidir. Kisi 3'un adi IŞIK ÇAĞLAR'dir; kisi 4 ayrilmistir; kisi 2'nin
/// iki aktif sicili vardir (00002 ve sonradan giren 00006).
/// </remarks>
[Collection(ApiHostGroup.Name)]
public sealed class PersonListApiTests : IClassFixture<Identity.RegistrationApiFixture>, IAsyncLifetime
{
    private const string Persons = "/api/v1/personnel/persons";
    private const string Password = "Kediler uyur 7";
    private const string UserEmail = "ahmet.yilmaz@duzen.com.tr";

    private readonly Identity.RegistrationApiFixture _fixture;

    public PersonListApiTests(Identity.RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await CreateAccountAsync(1);
        await _fixture.WithDbAsync(async context =>
        {
            var third = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(3));
            third.Apply(new PersonDetails("IŞIK", "ÇAĞLAR", third.BirthDate, third.Email, third.IsEmailShared, third.MobilePhone));

            await SetTerminationAsync(context, "00004", Today.AddDays(-1));

            var second = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(2));
            if (!await context.Set<Employment>().AnyAsync(e => e.RegistryCode == "00006"))
            {
                var company = await context.Set<Company>().FirstAsync();
                context.Add(Employment.Create(second, company, "00006", new EmploymentDetails(new DateOnly(2024, 5, 1), null, 6), Today));
            }

            return await context.SaveChangesAsync();
        });
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Default_list_shows_only_active_people_and_status_filter_reaches_the_others()
    {
        // SYG-PERS-027 (PG-PERS-16).
        var (client, token) = await HrAsync();
        using (client)
        {
            (await ListAsync(client, token, "")).GetProperty("totalCount").GetInt32().ShouldBe(4);
            Codes(await ListAsync(client, token, "?status=terminated")).ShouldBe(["00004"]);
            (await ListAsync(client, token, "?status=all")).GetProperty("totalCount").GetInt32().ShouldBe(5);
        }
    }

    [Fact]
    public async Task Quick_search_ignores_turkish_letters_and_covers_registry_codes()
    {
        // SYG-PERS-030 (ADR-0021 §7): "isik" IŞIK'i, "ışık" da ayni kisiyi bulur.
        var (client, token) = await HrAsync();
        using (client)
        {
            Codes(await ListAsync(client, token, "?q=isik")).ShouldBe(["00003"]);
            Codes(await ListAsync(client, token, "?q=%C4%B1%C5%9F%C4%B1k")).ShouldBe(["00003"]);
            Codes(await ListAsync(client, token, "?q=cagl")).ShouldBe(["00003"]);
            Codes(await ListAsync(client, token, "?q=00003")).ShouldBe(["00003"]);
        }
    }

    [Fact]
    public async Task National_id_search_needs_the_national_id_permission()
    {
        // SYG-PERS-030 (AN-02, B-17): tam TCKN'yi goremeyen, TCKN ile kisinin varligini dogrulayamaz.
        var (client, token) = await HrAsync();
        using (client)
        {
            Codes(await ListAsync(client, token, $"?q={NationalId(3)}")).ShouldBe(["00003"]);
        }

        await GrantRoleAsync(await CreateRoleAsync(Permissions.PersonView));
        await RevokeRoleAsync(Role.HrIdentityOperationsCode);
        using var viewer = _fixture.CreateClient();
        var viewerToken = await SignInAsync(viewer);
        (await ListAsync(viewer, viewerToken, $"?q={NationalId(3)}")).GetProperty("totalCount").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task Page_size_defaults_to_the_parameter_and_cannot_exceed_its_maximum()
    {
        // SYG-PERS-029 (PG-PERS-17): varsayilan 24; en buyuk deger parametredir, kod tavani 100.
        var (client, token) = await HrAsync();
        using (client)
        {
            (await ListAsync(client, token, "")).GetProperty("pageSize").GetInt32().ShouldBe(24);

            var paged = await ListAsync(client, token, "?pageSize=3&page=2");
            paged.GetProperty("items").GetArrayLength().ShouldBe(1);
            paged.GetProperty("totalPages").GetInt32().ShouldBe(2);

            await SetParameterAsync(ParameterCatalog.MaxPageSize, "30");
            (await SendAsync(client, $"{Persons}?pageSize=50", token)).Status.ShouldBe(HttpStatusCode.BadRequest);
            (await SendAsync(client, $"{Persons}?pageSize=101", token)).Status.ShouldBe(HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task Terminated_person_disappears_from_list_and_card_after_the_visibility_period()
    {
        // SYG-PERS-028 (PG-PERS-38, KR-114, KR-126): bos parametre suresizdir; sure son cikistan baslar.
        await _fixture.WithDbAsync(async context =>
        {
            await SetTerminationAsync(context, "00004", Today.AddYears(-3));
            return await context.SaveChangesAsync();
        });
        var (client, token) = await HrAsync();
        using (client)
        {
            var person4 = await PersonIdAsync(4);
            Codes(await ListAsync(client, token, "?status=terminated")).ShouldBe(["00004"]);

            await SetParameterAsync(ParameterCatalog.TerminatedVisibilityYears, "2");

            (await ListAsync(client, token, "?status=all")).GetProperty("totalCount").GetInt32().ShouldBe(4);
            (await SendAsync(client, $"{Persons}/{person4}", token)).Status.ShouldBe(HttpStatusCode.NotFound);

            await SetParameterAsync(ParameterCatalog.TerminatedVisibilityYears, "5");
            (await SendAsync(client, $"{Persons}/{person4}", token)).Status.ShouldBe(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Person_with_two_active_registrations_appears_once_with_the_latest_hire()
    {
        // SYG-PERS-022 (KR-126): kart esas istihdami gosterir ve "X sicil" sayisini tasir.
        var (client, token) = await HrAsync();
        using (client)
        {
            var item = (await ListAsync(client, token, "?q=00006")).GetProperty("items").EnumerateArray().Single();

            item.GetProperty("registryCode").GetString().ShouldBe("00006");
            item.GetProperty("employmentCount").GetInt32().ShouldBe(2);
            (await ListAsync(client, token, "?q=00002")).GetProperty("totalCount").GetInt32().ShouldBe(1);
        }
    }

    [Fact]
    public async Task List_is_ordered_by_turkish_alphabet()
    {
        // SYG-PERS-031: ad, sonra soyad; Turkce alfabe (I, Ç'den sonra gelir).
        var (client, token) = await HrAsync();
        using (client)
        {
            var names = (await ListAsync(client, token, "?status=all&sort=firstName")).GetProperty("items").EnumerateArray()
                .Select(i => i.GetProperty("firstName").GetString()).Distinct().ToList();
            names.ShouldBe(["Ahmet", "IŞIK"]);
            var descending = (await ListAsync(client, token, "?status=all&sort=firstName&order=desc")).GetProperty("items")[0];
            descending.GetProperty("firstName").GetString().ShouldBe("IŞIK");
        }
    }

    [Fact]
    public async Task User_without_a_role_lists_only_themselves_and_the_list_is_logged_with_the_people_shown()
    {
        // SYG-PERS-050 (Self kapsami) ve SYG-PERS-058: liste kaydinda ekranda gosterilen kisilerin kimlikleri.
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);
        var before = await _fixture.WithDbAsync(context => context.AccessLog.MaxAsync(e => (long?)e.Id)) ?? 0;

        var list = await ListAsync(client, token, "?q=ahmet");

        list.GetProperty("totalCount").GetInt32().ShouldBe(1);
        var own = await PersonIdAsync(1);
        list.GetProperty("items")[0].GetProperty("personId").GetGuid().ShouldBe(own);
        var entry = await _fixture.WithDbAsync(context => context.AccessLog.Where(e => e.Id > before).SingleAsync());
        entry.AccessType.ShouldBe(AccessType.List);
        entry.RecordCount.ShouldBe(1);
        entry.SubjectIds.ShouldBe([own]);
        entry.Filters.ShouldContain("ahmet");
    }

    private static List<string?> Codes(JsonElement page) =>
        [.. page.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("registryCode").GetString())];

    private static async Task SetTerminationAsync(HrmsDbContext context, string registryCode, DateOnly termination)
    {
        var employment = await context.Set<Employment>().Include(e => e.Person).Include(e => e.Company).SingleAsync(e => e.RegistryCode == registryCode);
        employment.Apply(employment.Person, employment.Company, new EmploymentDetails(employment.HireDate, termination, employment.LogoRef), Today);
    }

    private static async Task<JsonElement> ListAsync(HttpClient client, string token, string query)
    {
        var (status, body) = await SendAsync(client, Persons + query, token);
        status.ShouldBe(HttpStatusCode.OK);
        return body;
    }

    private async Task<(HttpClient Client, string Token)> HrAsync()
    {
        await GrantRoleAsync(Role.HrIdentityOperationsCode);
        var client = _fixture.CreateClient();
        return (client, await SignInAsync(client));
    }

    private Task<Guid> PersonIdAsync(int index) =>
        _fixture.WithDbAsync(context => context.Set<Person>().Where(p => p.NationalId == NationalId(index)).Select(p => p.PublicId).SingleAsync());

    private Task<int> SetParameterAsync(ParameterDefinition parameter, string value) =>
        _fixture.WithServicesAsync(async services =>
        {
            await services.GetRequiredService<ISystemParameterEditor>().UpdateAsync(parameter.Key, value, CancellationToken.None);
            return 0;
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

    private Task<int> GrantRoleAsync(string roleCode) =>
        _fixture.WithDbAsync(async context =>
        {
            var account = await AccountAsync(context);
            var role = await context.Set<Role>().SingleAsync(r => r.Code == roleCode);
            if (!await context.Set<UserRole>().AnyAsync(r => r.UserAccountId == account.Id && r.RoleId == role.Id))
            {
                context.Add(UserRole.Assign(account.Id, role.Id));
            }

            return await context.SaveChangesAsync();
        });

    private Task<int> RevokeRoleAsync(string roleCode) =>
        _fixture.WithDbAsync(async context =>
        {
            var account = await AccountAsync(context);
            var role = await context.Set<Role>().SingleAsync(r => r.Code == roleCode);
            context.RemoveRange(context.Set<UserRole>().Where(r => r.UserAccountId == account.Id && r.RoleId == role.Id));
            return await context.SaveChangesAsync();
        });

    private static async Task<UserAccount> AccountAsync(HrmsDbContext context)
    {
        var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(1));
        return await context.Set<UserAccount>().SingleAsync(a => a.PersonId == person.Id);
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

    private static async Task<string> SignInAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(new Uri("/api/v1/identity/sessions", UriKind.Relative), new { email = UserEmail, password = Password });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        return body.GetProperty("session").GetProperty("accessToken").GetString()!;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(HttpClient client, string path, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone());
    }
}
