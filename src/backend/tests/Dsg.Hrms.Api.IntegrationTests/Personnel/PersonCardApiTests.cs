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
/// Kisi karti ve "Profilim" uctan uca: kapsam, alan gorunurlugu, 404 esitligi ve erisim
/// kaydi (SYG-PERS-034, 037, 044, 050, 051, 052, 053, 057). Veriler SENTETIKTIR.
/// </summary>
/// <remarks>
/// Kisi 1 oturum acan kullanicidir; rolu teste gore verilir. Kisi 3 ve 4 baskalaridir; kisi
/// 4'un istihdami fikstur gununden bir gun once bitmistir.
/// </remarks>
[Collection(ApiHostGroup.Name)]
public sealed class PersonCardApiTests : IClassFixture<Identity.RegistrationApiFixture>, IAsyncLifetime
{
    private const string Persons = "/api/v1/personnel/persons";
    private const string Password = "Kediler uyur 7";
    private const string UserEmail = "ahmet.yilmaz@duzen.com.tr";

    private readonly Identity.RegistrationApiFixture _fixture;

    public PersonCardApiTests(Identity.RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await CreateAccountAsync(1);
        await SetDetailsAsync(1, new PersonProfile(Gender.Female, "Demir", MaritalStatus.Married, "Lisans"), "Ornek Mah. 1. Sok. No: 2");
        await SetDetailsAsync(4, new PersonProfile(Gender.Male, null, MaritalStatus.NotMarried, "Lise"), null);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task User_without_a_role_sees_only_their_own_card()
    {
        // SYG-PERS-050 (PG-PERS-25, 35): izin olmadan kapsam Self; kendi karti izin gerektirmez.
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var (ownStatus, own) = await GetAsync(client, $"{Persons}/me", token);
        ownStatus.ShouldBe(HttpStatusCode.OK);
        own.GetProperty("isSelf").GetBoolean().ShouldBeTrue();
        own.GetProperty("personId").GetGuid().ShouldBe(await PersonIdAsync(1));

        (await GetAsync(client, $"{Persons}/{await PersonIdAsync(1)}", token)).Status.ShouldBe(HttpStatusCode.OK);
        (await GetAsync(client, $"{Persons}/{await PersonIdAsync(3)}", token)).Status.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Out_of_scope_person_and_missing_person_get_the_same_404()
    {
        // SYG-PERS-051 (PG-PERS-26, 27): 403 "kayit var ama goremezsin" bilgisini sizdirirdi.
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var (outOfScopeStatus, outOfScope) = await GetAsync(client, $"{Persons}/{await PersonIdAsync(3)}", token);
        var (missingStatus, missing) = await GetAsync(client, $"{Persons}/{Guid.CreateVersion7()}", token);

        outOfScopeStatus.ShouldBe(HttpStatusCode.NotFound);
        missingStatus.ShouldBe(HttpStatusCode.NotFound);
        Comparable(outOfScope).ShouldBe(Comparable(missing));
    }

    [Fact]
    public async Task National_id_is_masked_for_the_person_themselves_without_the_permission()
    {
        // SYG-PERS-053 (S-01): tam TCKN'yi goremeyen kullanici, kisinin kendisi dahil, maskeli gorur;
        // tam deger yanitta hic yoktur.
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var (_, card) = await GetAsync(client, $"{Persons}/me", token);

        var nationalId = NationalId(1);
        card.GetProperty("nationalId").GetString().ShouldBe($"{nationalId[..3]}******{nationalId[^2..]}");
        card.GetProperty("isNationalIdMasked").GetBoolean().ShouldBeTrue();
        card.GetRawText().ShouldNotContain(nationalId);
    }

    [Fact]
    public async Task Hr_sees_any_person_with_the_full_national_id_and_seniority()
    {
        // SYG-PERS-050, 052: IK (personnel.person.view, view_national_id) tum personeli ve tam TCKN'yi gorur.
        // SYG-PERS-039: ayrilan istihdamda cikis gunu sayilir (giris 01.01.2020, cikis 26.09.2026).
        await GrantRoleAsync(1, Role.HrIdentityOperationsCode);
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var (status, card) = await GetAsync(client, $"{Persons}/{await PersonIdAsync(4)}", token);

        status.ShouldBe(HttpStatusCode.OK);
        card.GetProperty("nationalId").GetString().ShouldBe(NationalId(4));
        card.GetProperty("isNationalIdMasked").GetBoolean().ShouldBeFalse();
        card.GetProperty("isSelf").GetBoolean().ShouldBeFalse();
        var employment = card.GetProperty("employments").EnumerateArray().Single();
        employment.GetProperty("registryCode").GetString().ShouldBe("00004");
        employment.GetProperty("isActive").GetBoolean().ShouldBeFalse();
        employment.GetProperty("seniority").GetProperty("text").GetString().ShouldBe("6 Yıl 8 Ay 26 Gün");
    }

    [Fact]
    public async Task Card_carries_no_field_outside_the_reviewed_list()
    {
        // SYG-PERS-052: yanita eklenen her alan bilincli bir karardir; korumali alanlar
        // (adres, kan grubu, acil durum, evlilik) ayri tablolarla ve gorunurluk testleriyle gelir.
        await GrantRoleAsync(1, Role.HrIdentityOperationsCode);
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var (_, card) = await GetAsync(client, $"{Persons}/{await PersonIdAsync(4)}", token);

        card.EnumerateObject().Select(p => p.Name).ShouldBe(
            ["personId", "firstName", "lastName", "nationalId", "isNationalIdMasked", "birthDate", "age", "email", "mobilePhone", "isSelf", "employments",
             "gender", "previousSurname", "maritalStatus", "educationName", "sensitive"]);
        card.GetProperty("sensitive").EnumerateObject().Select(p => p.Name).ShouldBe(["address"]);
    }

    [Fact]
    public async Task General_information_is_shown_on_the_card()
    {
        // SYG-PERS-034: cinsiyet, onceki soyadi (kadin ve doluysa), medeni hal, egitim.
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var (_, card) = await GetAsync(client, $"{Persons}/me", token);

        card.GetProperty("gender").GetString().ShouldBe("female");
        card.GetProperty("previousSurname").GetString().ShouldBe("Demir");
        card.GetProperty("maritalStatus").GetString().ShouldBe("married");
        card.GetProperty("educationName").GetString().ShouldBe("Lisans");
    }

    [Fact]
    public async Task Person_sees_their_own_address_and_hr_sees_everyones()
    {
        // SYG-PERS-037, 052 (PG-PERS-20): adresi IK, Sistem Yoneticisi ve kisinin kendisi gorur.
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);
        (await GetAsync(client, $"{Persons}/me", token)).Body.GetProperty("sensitive").GetProperty("address").GetString()
            .ShouldBe("Ornek Mah. 1. Sok. No: 2");

        await GrantRoleAsync(1, Role.HrIdentityOperationsCode);
        token = await SignInAsync(client);
        var other = (await GetAsync(client, $"{Persons}/{await PersonIdAsync(4)}", token)).Body;

        // Adresi kayitli olmayan kiside bolum vardir, deger bostur: ekranda "Kayitli degil".
        other.GetProperty("sensitive").GetProperty("address").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Employment_tab_shows_the_latest_work_life_values()
    {
        // SYG-PERS-038: sehir, firma, sube, birim, unvan, statu, SGK meslek. Bitmis istihdamda son
        // bilinen degerler (cikis gunundeki) gorunur; hic kaydi olmayan istihdamda bolum bostur.
        await GrantRoleAsync(1, Role.HrIdentityOperationsCode);
        await _fixture.WithDbAsync(async context =>
        {
            var employment = await context.Set<Employment>().SingleAsync(e => e.RegistryCode == "00004");
            if (await context.Set<EmploymentAssignment>().AnyAsync(a => a.EmploymentId == employment.Id))
            {
                return 0;
            }

            var branch = Domain.Organization.Branch.FromLogo(employment.CompanyId, 5, "Merkez Laboratuvar", "Ankara");
            var department = Domain.Organization.Department.FromLogo(employment.CompanyId, 7, "Bilgi Islem");
            context.AddRange(branch, department);
            await context.SaveChangesAsync();
            var (assignment, _) = AssignmentTimeline.Apply(
                employment, [], new AssignmentValues(employment.CompanyId, branch.Id, department.Id, "2", "A", "2511.01", "Programci"), Today);
            context.Add(assignment!);
            return await context.SaveChangesAsync();
        });
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var workLife = (await GetAsync(client, $"{Persons}/{await PersonIdAsync(4)}", token)).Body
            .GetProperty("employments")[0].GetProperty("workLife");
        var own = (await GetAsync(client, $"{Persons}/me", token)).Body.GetProperty("employments")[0];

        workLife.GetProperty("city").GetString().ShouldBe("Ankara");
        workLife.GetProperty("branchName").GetString().ShouldBe("Merkez Laboratuvar");
        workLife.GetProperty("departmentName").GetString().ShouldBe("Bilgi Islem");
        workLife.GetProperty("titleCode").GetString().ShouldBe("2");
        workLife.GetProperty("occupationName").GetString().ShouldBe("Programci");
        workLife.GetProperty("since").GetString().ShouldBe(Today.AddDays(-1).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));
        own.GetProperty("workLife").ValueKind.ShouldBe(JsonValueKind.Null);
    }

    [Fact]
    public async Task Viewer_without_the_sensitive_permission_gets_no_sensitive_section_at_all()
    {
        // SYG-PERS-052 (yetki sizintisi testi): tum personeli gorebilen ama view_sensitive izni
        // olmayan kullanicinin yanitinda korumali alan bolumu HIC YOKTUR; ekranda gizlemek yetmez.
        await GrantRoleAsync(1, await CreateRoleAsync(Permissions.PersonView));
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);
        await SetDetailsAsync(3, new PersonProfile(Gender.Female, null, null, null), "Gizli Mah. 9");

        var (status, card) = await GetAsync(client, $"{Persons}/{await PersonIdAsync(3)}", token);

        status.ShouldBe(HttpStatusCode.OK);
        card.TryGetProperty("sensitive", out _).ShouldBeFalse();
        card.GetRawText().ShouldNotContain("Gizli Mah.");

        // Kendi karti tek kayittir: kisinin kendisi kurali gecerlidir.
        (await GetAsync(client, $"{Persons}/me", token)).Body.TryGetProperty("sensitive", out _).ShouldBeTrue();
    }

    [Fact]
    public async Task Every_card_view_is_written_to_the_access_log_including_the_own_card()
    {
        // SYG-PERS-057 (PG-PERS-28, AN-10): kim, kimin kartini, ne zaman.
        await GrantRoleAsync(1, Role.HrIdentityOperationsCode);
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);
        var other = await PersonIdAsync(4);
        // Erisim kaydi degistirilemez ve silinemez (KR-118); test kendinden sonraki kayitlara bakar.
        var before = await _fixture.WithDbAsync(context => context.AccessLog.MaxAsync(e => (long?)e.Id)) ?? 0;

        await GetAsync(client, $"{Persons}/me", token);
        await GetAsync(client, $"{Persons}/{other}", token);
        await GetAsync(client, $"{Persons}/{Guid.CreateVersion7()}", token);

        var entries = await _fixture.WithDbAsync(context => context.AccessLog.Where(e => e.Id > before).OrderBy(e => e.Id).ToListAsync());
        var account = await _fixture.WithDbAsync(context => AccountIdAsync(context, 1));
        entries.Select(e => (e.AccessType, e.EntityName, e.EntityId, e.UserAccountId))
            .ShouldBe([
                (AccessType.View, "Person", (Guid?)await PersonIdAsync(1), (long?)account),
                (AccessType.View, "Person", (Guid?)other, (long?)account),
            ]);
    }

    [Fact]
    public async Task Card_requires_a_session()
    {
        using var client = _fixture.CreateClient();

        (await GetAsync(client, $"{Persons}/me", null)).Status.ShouldBe(HttpStatusCode.Unauthorized);
        (await GetAsync(client, $"{Persons}/{await PersonIdAsync(1)}", null)).Status.ShouldBe(HttpStatusCode.Unauthorized);
    }

    /// <summary>Problem Details govdesinin istege ozgu olmayan kismi.</summary>
    private static string Comparable(JsonElement body) =>
        string.Join('|', body.EnumerateObject()
            .Where(p => p.Name is not ("traceId" or "instance" or "correlationId"))
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .Select(p => $"{p.Name}={p.Value.GetRawText()}"));

    private Task<Guid> PersonIdAsync(int index) =>
        _fixture.WithDbAsync(context => context.Set<Person>().Where(p => p.NationalId == NationalId(index)).Select(p => p.PublicId).SingleAsync());

    private static async Task<long> AccountIdAsync(HrmsDbContext context, int index)
    {
        var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
        return await context.Set<UserAccount>().Where(a => a.PersonId == person.Id).Select(a => a.Id).SingleAsync();
    }

    private Task<int> SetDetailsAsync(int index, PersonProfile profile, string? address) =>
        _fixture.WithDbAsync(async context =>
        {
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
            person.ApplyProfile(profile);
            var row = await context.Set<PersonSensitive>().SingleOrDefaultAsync(r => r.PersonId == person.Id);
            if (row is null)
            {
                row = PersonSensitive.For(person.Id);
                context.Add(row);
            }

            row.ApplyAddress(address);
            return await context.SaveChangesAsync();
        });

    /// <summary>Yalnizca verilen izinleri tasiyan bir test rolu olusturur; kodunu doner.</summary>
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

    private Task<int> GrantRoleAsync(int index, string roleCode) =>
        _fixture.WithDbAsync(async context =>
        {
            var accountId = await AccountIdAsync(context, index);
            var role = await context.Set<Role>().SingleAsync(r => r.Code == roleCode);
            context.Add(UserRole.Assign(accountId, role.Id));
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

    private static async Task<(HttpStatusCode Status, JsonElement Body)> GetAsync(HttpClient client, string path, string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone());
    }
}
