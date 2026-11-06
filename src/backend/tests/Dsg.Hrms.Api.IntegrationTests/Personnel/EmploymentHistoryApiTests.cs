using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Domain.Audit;
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
/// Istihdam tarihcesi ve tarihli sorgu uctan uca (PG-PERS-12; SYG-PERS-020; ADR-0021 §2).
/// Veriler SENTETIKTIR.
/// </summary>
/// <remarks>
/// Kisi 1 oturum acan kullanicidir. Kisi 3'un istihdaminda (00003) T1 10.01.2026'da baslar:
/// sube Merkez [10.01, 01.06), Kizilay [01.06, -); gorev Biyolog [10.01, 01.03), Yazilim Uzmani
/// [01.03, 01.05) ve silinmis bir gorev [01.05, -).
/// </remarks>
[Collection(ApiHostGroup.Name)]
public sealed class EmploymentHistoryApiTests : IClassFixture<Identity.RegistrationApiFixture>, IAsyncLifetime
{
    private const string Password = "Kediler uyur 7";
    private const string UserEmail = "ahmet.yilmaz@duzen.com.tr";

    private readonly Identity.RegistrationApiFixture _fixture;

    public EmploymentHistoryApiTests(Identity.RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await _fixture.WithDbAsync(context => context.Database.ExecuteSqlRawAsync("DELETE FROM personnel.employment_duty"));
        await CreateAccountAsync(1);
        await SeedHistoryAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Whole_history_lists_periods_newest_first_without_deleted_duties_and_is_access_logged()
    {
        // SYG-PERS-020: firma, sube, birim, unvan ve gorev tarihcesi baslangic ve bitisiyle.
        var (client, token) = await HrAsync();
        using (client)
        {
            var person = await PersonIdAsync(3);
            var before = await _fixture.WithDbAsync(context => context.AccessLog.MaxAsync(e => (long?)e.Id)) ?? 0;

            var (status, body) = await SendAsync(client, History(person), token);

            status.ShouldBe(HttpStatusCode.OK);
            body.GetProperty("date").ValueKind.ShouldBe(JsonValueKind.Null);
            var assignments = body.GetProperty("assignments").EnumerateArray().ToList();
            assignments.Select(a => a.GetProperty("branchName").GetString()).ShouldBe(["Kızılay", "Merkez"]);
            assignments[0].GetProperty("validTo").ValueKind.ShouldBe(JsonValueKind.Null);
            assignments[1].GetProperty("validFrom").GetString().ShouldBe("2026-01-10");
            assignments[1].GetProperty("validTo").GetString().ShouldBe("2026-06-01");
            assignments[1].GetProperty("companyName").GetString().ShouldBe("Duzen Laboratuvarlar");
            assignments[1].GetProperty("departmentName").GetString().ShouldBe("Biyokimya");

            // Silinmis gorev tarihcede yoktur (SYG-PERS-042).
            var duties = body.GetProperty("duties").EnumerateArray().ToList();
            duties.Select(d => d.GetProperty("name").GetString()).ShouldBe(["Yazılım Uzmanı", "Biyolog"]);
            duties[0].GetProperty("validTo").GetString().ShouldBe("2026-05-01");

            // Tarihce kisisel veridir: kisiye ait View erisim kaydi (AN-23).
            var entries = await _fixture.WithDbAsync(context => context.AccessLog.Where(e => e.Id > before).ToListAsync());
            entries.Select(e => (e.AccessType, e.EntityId)).ShouldBe([(AccessType.View, (Guid?)person)]);
        }
    }

    [Theory]
    [InlineData("2026-02-15", "Merkez", "Biyolog")]
    [InlineData("2026-04-01", "Merkez", "Yazılım Uzmanı")]
    [InlineData("2026-06-01", "Kızılay", null)]
    [InlineData("2026-05-31", "Merkez", null)]
    public async Task Date_query_returns_the_values_valid_on_that_day(string date, string branch, string? duty)
    {
        // PG-PERS-12 kabul olcutu: gecmis bir tarihte gecerli firma, sube, birim, unvan ve gorev.
        // Bitis haric: 01.06'da yeni sube gecerlidir; silinmis gorevin donemi bostur.
        var (client, token) = await HrAsync();
        using (client)
        {
            var (status, body) = await SendAsync(client, $"{History(await PersonIdAsync(3))}?date={date}", token);

            status.ShouldBe(HttpStatusCode.OK);
            body.GetProperty("date").GetString().ShouldBe(date);
            body.GetProperty("assignments").EnumerateArray().Select(a => a.GetProperty("branchName").GetString()).ShouldBe([branch]);
            body.GetProperty("duties").EnumerateArray().Select(d => d.GetProperty("name").GetString())
                .ShouldBe(duty is null ? [] : [duty]);
        }
    }

    [Fact]
    public async Task Date_before_T1_has_no_record_and_an_invalid_date_is_a_400()
    {
        // ADR-0021 §2: T1 oncesi bir tarih icin "kayit yok"; deger tahmin edilmez.
        var (client, token) = await HrAsync();
        using (client)
        {
            var person = await PersonIdAsync(3);
            var (status, body) = await SendAsync(client, $"{History(person)}?date=2026-01-09", token);

            status.ShouldBe(HttpStatusCode.OK);
            body.GetProperty("assignments").GetArrayLength().ShouldBe(0);
            body.GetProperty("duties").GetArrayLength().ShouldBe(0);

            (await SendAsync(client, $"{History(person)}?date=2026-13-01", token)).Status.ShouldBe(HttpStatusCode.BadRequest);
        }
    }

    [Fact]
    public async Task Scope_is_the_card_scope_and_the_person_sees_own_history_without_permission()
    {
        // SYG-PERS-051: kapsam disi kisi ve olmayan sicil 404; SYG-PERS-044: kisi kendi tarihcesini gorur.
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);
        var other = await PersonIdAsync(3);
        var self = await PersonIdAsync(1);

        (await SendAsync(client, History(other), token)).Status.ShouldBe(HttpStatusCode.NotFound);
        var (status, own) = await SendAsync(client, $"/api/v1/personnel/persons/{self}/employments/00001/history", token);
        status.ShouldBe(HttpStatusCode.OK);
        own.GetProperty("assignments").GetArrayLength().ShouldBe(0);

        await GrantRoleAsync(Role.HrIdentityOperationsCode);
        token = await SignInAsync(client);
        (await SendAsync(client, History(other), token)).Status.ShouldBe(HttpStatusCode.OK);
        // Baska kisinin sicili bu kiside yoktur.
        (await SendAsync(client, $"/api/v1/personnel/persons/{other}/employments/00001/history", token)).Status.ShouldBe(HttpStatusCode.NotFound);
        (await SendAsync(client, $"/api/v1/personnel/persons/{Guid.NewGuid()}/employments/00003/history", token)).Status.ShouldBe(HttpStatusCode.NotFound);
    }

    private static string History(Guid personId) => $"/api/v1/personnel/persons/{personId}/employments/00003/history";

    private Task<int> SeedHistoryAsync() =>
        _fixture.WithDbAsync(async context =>
        {
            var employment = await context.Set<Employment>().SingleAsync(e => e.RegistryCode == "00003");
            await context.Database.ExecuteSqlAsync($"DELETE FROM personnel.employment_assignment WHERE employment_id = {employment.Id}");

            var merkez = await BranchAsync(context, employment.CompanyId, 801, "Merkez");
            var kizilay = await BranchAsync(context, employment.CompanyId, 802, "Kızılay");
            var department = await context.Set<Department>().SingleOrDefaultAsync(d => d.CompanyId == employment.CompanyId && d.LogoNumber == 803);
            if (department is null)
            {
                department = Department.FromLogo(employment.CompanyId, 803, "Biyokimya");
                context.Add(department);
            }

            await context.SaveChangesAsync();

            var (first, _) = AssignmentTimeline.Apply(employment, [], new AssignmentValues(employment.CompanyId, merkez.Id, department.Id, "2", "A", null, null), new DateOnly(2026, 1, 10));
            context.Add(first!);
            var (second, _) = AssignmentTimeline.Apply(employment, [first!], new AssignmentValues(employment.CompanyId, kizilay.Id, department.Id, "2", "A", null, null), new DateOnly(2026, 6, 1));
            context.Add(second!);

            var biologist = ReferenceDefinition.Create(ReferenceType.Duty, $"T{Guid.NewGuid():N}"[..10], "Biyolog", 90);
            var engineer = ReferenceDefinition.Create(ReferenceType.Duty, $"T{Guid.NewGuid():N}"[..10], "Yazılım Uzmanı", 91);
            var wrong = ReferenceDefinition.Create(ReferenceType.Duty, $"T{Guid.NewGuid():N}"[..10], "Yanlış Görev", 92);
            context.AddRange(biologist, engineer, wrong);
            await context.SaveChangesAsync();

            var duties = new List<EmploymentDuty>();
            foreach (var (definition, from) in new[] { (biologist, new DateOnly(2026, 1, 10)), (engineer, new DateOnly(2026, 3, 1)), (wrong, new DateOnly(2026, 5, 1)) })
            {
                var (added, _) = DutyTimeline.Assign(employment, duties, definition.Id, from);
                duties.Add(added!);
                context.Add(added!);
            }

            await context.SaveChangesAsync();
            context.Remove(duties[^1]);
            return await context.SaveChangesAsync();
        });

    private static async Task<Branch> BranchAsync(HrmsDbContext context, long companyId, short logoNumber, string name)
    {
        var branch = await context.Set<Branch>().SingleOrDefaultAsync(b => b.CompanyId == companyId && b.LogoNumber == logoNumber);
        if (branch is null)
        {
            branch = Branch.FromLogo(companyId, logoNumber, name, "Ankara");
            context.Add(branch);
        }

        return branch;
    }

    private async Task<(HttpClient Client, string Token)> HrAsync()
    {
        await GrantRoleAsync(Role.HrIdentityOperationsCode);
        var client = _fixture.CreateClient();
        return (client, await SignInAsync(client));
    }

    private Task<Guid> PersonIdAsync(int index) =>
        _fixture.WithDbAsync(context => context.Set<Person>().Where(p => p.NationalId == NationalId(index)).Select(p => p.PublicId).SingleAsync());

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

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(HttpClient client, string path, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone());
    }
}
