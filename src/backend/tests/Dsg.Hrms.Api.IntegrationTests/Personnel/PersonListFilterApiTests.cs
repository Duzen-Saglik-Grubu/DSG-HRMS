using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Organization;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Domain.Reference;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Personnel;

/// <summary>
/// Personel listesi suzgecleri uctan uca (SYG-PERS-024, 025, 026, 059). Veriler SENTETIKTIR.
/// </summary>
/// <remarks>
/// Kisi 1 oturum acan kullanicidir. Kisi 2: Merkez (Ankara) / Bilgi Islem, unvan 1, statu A, kadin,
/// evli, lisans, adres "Kızılay Mah.". Kisi 3: Bornova (Izmir) / Laboratuvar, unvan 2, statu B, erkek,
/// bekar, lise, adres "Bornova", gorev Biyolog. Kisi 5: Merkez / Laboratuvar, unvan 3.
/// </remarks>
[Collection(ApiHostGroup.Name)]
public sealed class PersonListFilterApiTests : IClassFixture<Identity.RegistrationApiFixture>, IAsyncLifetime
{
    private const string Persons = "/api/v1/personnel/persons";
    private const string Password = "Kediler uyur 7";
    private const string UserEmail = "ahmet.yilmaz@duzen.com.tr";

    private readonly Identity.RegistrationApiFixture _fixture;
    private Seed _seed = null!;

    public PersonListFilterApiTests(Identity.RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await CreateAccountAsync(1);
        _seed = await _fixture.WithDbAsync(SeedAsync);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Multi_select_matches_any_value_and_different_filters_apply_together()
    {
        // SYG-PERS-025 (PG-PERS-14, 15): secilenlerden herhangi biri; farkli suzgecler VE.
        var (client, jwt) = await HrAsync();
        using (client)
        {
            (await CodesAsync(client, jwt, $"branchId={_seed.Merkez}&branchId={_seed.Bornova}")).ShouldBe(["00002", "00003", "00005"], ignoreOrder: true);
            (await CodesAsync(client, jwt, $"branchId={_seed.Merkez}&departmentId={_seed.Laboratuvar}")).ShouldBe(["00005"]);
        }
    }

    [Fact]
    public async Task Each_card_field_has_a_filter()
    {
        // SYG-PERS-024: kartta gosterilen her alan icin suzgec (bugun var olan alanlar).
        var (client, jwt) = await HrAsync();
        using (client)
        {
            (await CodesAsync(client, jwt, "city=" + Uri.EscapeDataString("İzmir"))).ShouldBe(["00003"]);
            (await CodesAsync(client, jwt, "titleCode=1&titleCode=2")).ShouldBe(["00002", "00003"], ignoreOrder: true);
            (await CodesAsync(client, jwt, "statusCode=B")).ShouldBe(["00003"]);
            (await CodesAsync(client, jwt, "occupationCode=2222.01")).ShouldBe(["00003"]);
            (await CodesAsync(client, jwt, $"dutyId={_seed.Biologist}")).ShouldBe(["00003"]);
            (await CodesAsync(client, jwt, $"companyId={_seed.Company}")).ShouldBe(["00002", "00003", "00005"], ignoreOrder: true);
            (await CodesAsync(client, jwt, "gender=female")).ShouldBe(["00002"]);
            (await CodesAsync(client, jwt, "maritalStatus=notMarried")).ShouldBe(["00003"]);
            (await CodesAsync(client, jwt, "education=Lise")).ShouldBe(["00003"]);
            (await CodesAsync(client, jwt, "lastName=" + Uri.EscapeDataString("çağ"))).ShouldBe(["00003"]);
            (await CodesAsync(client, jwt, "registryCode=0005")).ShouldBe(["00005"]);
            (await CodesAsync(client, jwt, "email=ortak@")).ShouldBe(["00003"]);
            (await CodesAsync(client, jwt, "phone=0532%20123%2045%2068")).ShouldBe(["00003"]);
            (await CodesAsync(client, jwt, "address=kizilay")).ShouldBe(["00002"]);
            (await CodesAsync(client, jwt, $"nationalId={NationalId(5)[..6]}")).ShouldContain("00005");
            (await CodesAsync(client, jwt, "hireDateFrom=2020-01-01&hireDateTo=2020-01-01")).Count.ShouldBe(4);
            (await CodesAsync(client, jwt, "status=terminated&terminationDateFrom=2000-01-01")).ShouldBe(["00004"]);
            (await CodesAsync(client, jwt, "birthDateFrom=1985-04-12&birthDateTo=1985-04-12")).Count.ShouldBe(4);
            (await CodesAsync(client, jwt, "ageMin=41&ageMax=41")).Count.ShouldBe(4);
            (await CodesAsync(client, jwt, "ageMin=42")).ShouldBeEmpty();
            (await CodesAsync(client, jwt, "seniorityMin=6&seniorityMax=6")).Count.ShouldBe(4);
            (await CodesAsync(client, jwt, "seniorityMin=7")).ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task Filters_on_fields_the_user_cannot_see_are_refused_and_not_offered()
    {
        // SYG-PERS-026 (KR-116 madde 3): goremedigi alana gore suzen kullanici gizli degeri cikarirdi.
        await GrantRoleAsync(await CreateRoleAsync(Permissions.PersonView));
        using var client = _fixture.CreateClient();
        var jwt = await SignInAsync(client);

        (await SendAsync(client, $"{Persons}?address=kizilay", jwt)).Status.ShouldBe(HttpStatusCode.BadRequest);
        (await SendAsync(client, $"{Persons}?nationalId=123", jwt)).Status.ShouldBe(HttpStatusCode.BadRequest);

        var options = (await SendAsync(client, $"{Persons}/filter-options", jwt)).Body;
        options.GetProperty("canFilterBySensitive").GetBoolean().ShouldBeFalse();
        options.GetProperty("canFilterByNationalId").GetBoolean().ShouldBeFalse();
        options.GetProperty("branches").EnumerateArray().Select(b => b.GetProperty("label").GetString()).ShouldBe(["Bornova", "Merkez"], ignoreOrder: true);
    }

    [Fact]
    public async Task Hr_is_offered_the_protected_filters()
    {
        var (client, jwt) = await HrAsync();
        using (client)
        {
            var options = (await SendAsync(client, $"{Persons}/filter-options", jwt)).Body;

            options.GetProperty("canFilterBySensitive").GetBoolean().ShouldBeTrue();
            options.GetProperty("defaultPageSize").GetInt32().ShouldBe(24);
            options.GetProperty("maxPageSize").GetInt32().ShouldBe(100);
            options.GetProperty("canFilterByNationalId").GetBoolean().ShouldBeTrue();
            options.GetProperty("cities").EnumerateArray().Select(c => c.GetProperty("value").GetString()).ShouldBe(["Ankara", "İzmir"], ignoreOrder: true);
            options.GetProperty("duties").EnumerateArray().ShouldContain(d => d.GetProperty("label").GetString() == "Biyolog");
        }
    }

    [Fact]
    public async Task Protected_filter_value_is_not_written_to_the_access_log()
    {
        // SYG-PERS-059 (B-01): korumali suzgecin degeri yazilmaz, yalnizca kullanildigi.
        var (client, jwt) = await HrAsync();
        using (client)
        {
            var before = await _fixture.WithDbAsync(context => context.AccessLog.MaxAsync(e => (long?)e.Id)) ?? 0;

            await CodesAsync(client, jwt, "address=kizilay&statusCode=A");

            var entry = await _fixture.WithDbAsync(context => context.AccessLog.Where(e => e.Id > before).SingleAsync());
            entry.Filters.ShouldNotContain("kizilay");
            entry.Filters.ShouldContain("(kullanıldı)");
            entry.Filters.ShouldContain("\"statusCode\": \"A\"");
        }
    }

    private async Task<Seed> SeedAsync(HrmsDbContext context)
    {
        var company = await context.Set<Company>().FirstAsync();
        var merkez = await Unit(context, () => Branch.FromLogo(company.Id, 1, "Merkez", "Ankara"), b => b.LogoNumber == 1 && b.CompanyId == company.Id);
        var bornova = await Unit(context, () => Branch.FromLogo(company.Id, 2, "Bornova", "İzmir"), b => b.LogoNumber == 2 && b.CompanyId == company.Id);
        var bilgiIslem = await Unit(context, () => Department.FromLogo(company.Id, 1, "Bilgi Islem"), d => d.LogoNumber == 1 && d.CompanyId == company.Id);
        var laboratuvar = await Unit(context, () => Department.FromLogo(company.Id, 2, "Laboratuvar"), d => d.LogoNumber == 2 && d.CompanyId == company.Id);
        var biologist = await context.Set<ReferenceDefinition>().SingleOrDefaultAsync(d => d.Type == ReferenceType.Duty && d.Name == "Biyolog")
            ?? ReferenceDefinition.Create(ReferenceType.Duty, "FBIYO", "Biyolog", 50);
        if (biologist.Id == 0)
        {
            context.Add(biologist);
        }

        await context.SaveChangesAsync();

        if (!await context.Set<EmploymentAssignment>().AnyAsync())
        {
            async Task Assign(string code, Branch branch, Department department, string title, string status, string occupation)
            {
                var employment = await context.Set<Employment>().SingleAsync(e => e.RegistryCode == code);
                var (assignment, _) = AssignmentTimeline.Apply(employment, [], new AssignmentValues(company.Id, branch.Id, department.Id, title, status, occupation, "Meslek " + occupation), Today);
                context.Add(assignment!);
            }

            await Assign("00002", merkez, bilgiIslem, "1", "A", "1111.01");
            await Assign("00003", bornova, laboratuvar, "2", "B", "2222.01");
            await Assign("00005", merkez, laboratuvar, "3", "A", "3333.01");

            var third = await context.Set<Employment>().SingleAsync(e => e.RegistryCode == "00003");
            var (duty, _) = DutyTimeline.Assign(third, [], biologist.Id, third.HireDate);
            context.Add(duty!);

            await Profile(context, 2, new PersonProfile(Gender.Female, null, MaritalStatus.Married, "Lisans"), "Kızılay Mah. 1");
            await Profile(context, 3, new PersonProfile(Gender.Male, null, MaritalStatus.NotMarried, "Lise"), "Bornova Cad. 2");
            var person3 = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(3));
            person3.Apply(new PersonDetails("Ahmet", "ÇAĞLAR", person3.BirthDate, person3.Email, person3.IsEmailShared, person3.MobilePhone));
            await context.SaveChangesAsync();
        }

        return new Seed(company.PublicId, merkez.PublicId, bornova.PublicId, laboratuvar.PublicId, biologist.PublicId);
    }

    private static async Task<T> Unit<T>(HrmsDbContext context, Func<T> create, System.Linq.Expressions.Expression<Func<T, bool>> find)
        where T : class
    {
        var existing = await context.Set<T>().SingleOrDefaultAsync(find);
        if (existing is not null)
        {
            return existing;
        }

        var unit = create();
        context.Add(unit);
        return unit;
    }

    private static async Task Profile(HrmsDbContext context, int index, PersonProfile profile, string address)
    {
        var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
        person.ApplyProfile(profile);
        var row = PersonSensitive.For(person.Id);
        row.ApplyAddress(address);
        context.Add(row);
    }

    private static async Task<List<string?>> CodesAsync(HttpClient client, string jwt, string query)
    {
        var (status, body) = await SendAsync(client, $"{Persons}?{query}", jwt);
        status.ShouldBe(HttpStatusCode.OK, query);
        return [.. body.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("registryCode").GetString())];
    }

    private async Task<(HttpClient Client, string Jwt)> HrAsync()
    {
        await GrantRoleAsync(Role.HrIdentityOperationsCode);
        var client = _fixture.CreateClient();
        return (client, await SignInAsync(client));
    }

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

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(HttpClient client, string path, string jwt)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone());
    }

    private sealed record Seed(Guid Company, Guid Merkez, Guid Bornova, Guid Laboratuvar, Guid Biologist);
}
