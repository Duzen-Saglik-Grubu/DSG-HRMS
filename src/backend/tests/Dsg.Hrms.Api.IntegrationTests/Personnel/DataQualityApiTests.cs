using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Application.Personnel.Quality;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Personnel;

/// <summary>
/// Veri kalitesi raporu (SYG-PERS-016, 070, 071). Veriler SENTETIKTIR.
/// </summary>
/// <remarks>
/// Tohum: 00001 (e-posta ve telefon, hesabi var), 00002 (kurumsal e-posta), 00003 (paylasilan
/// e-posta), 00004 (ayrilmis), 00005 (kisisel alan adli e-posta). Hicbir istihdamin unvani ve
/// gorevi yok. Senkronizasyon calismalari ve kart uyarilari her testte yeniden yazilir.
/// </remarks>
[Collection(ApiHostGroup.Name)]
public sealed class DataQualityApiTests : IClassFixture<Identity.RegistrationApiFixture>, IAsyncLifetime
{
    private const string Report = "/api/v1/personnel/quality";
    private const string Ineligible = "/api/v1/personnel/quality/ineligible";
    private const string Export = "/api/v1/personnel/quality/export";
    private const string Password = "Kediler uyur 7";
    private const string UserEmail = "ahmet.yilmaz@duzen.com.tr";

    private readonly Identity.RegistrationApiFixture _fixture;

    public DataQualityApiTests(Identity.RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await CreateAccountAsync();
        await SeedSyncRunsAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Without_the_view_permission_the_report_is_forbidden_and_view_alone_does_not_allow_export()
    {
        using var client = _fixture.CreateClient();
        await GrantRoleAsync(await CreateRoleAsync(Permissions.PersonView));
        var token = await SignInAsync(client);

        (await SendAsync(client, Report, token)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await SendAsync(client, Ineligible, token)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await SendAsync(client, Export, token)).Status.ShouldBe(HttpStatusCode.Forbidden);

        await GrantRoleAsync(await CreateRoleAsync(Permissions.QualityView));
        token = await SignInAsync(client);
        (await SendAsync(client, Report, token)).Status.ShouldBe(HttpStatusCode.OK);
        (await SendAsync(client, Export, token)).Status.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Report_counts_active_employments_only_and_takes_card_warnings_from_the_last_completed_run()
    {
        await GrantRoleAsync(Role.HrIdentityOperationsCode);
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var (status, body) = await SendAsync(client, Report, token);

        status.ShouldBe(HttpStatusCode.OK);
        var counts = body.Json.GetProperty("counts").EnumerateArray()
            .ToDictionary(c => c.GetProperty("issue").GetString()!, c => c.GetProperty("count").GetInt32());

        // Ayrilmis kartin TCKN uyarisi ve basarisiz calismanin uyarisi sayilmaz.
        counts["missingNationalId"].ShouldBe(1);
        counts["invalidNationalId"].ShouldBe(0);
        counts["invalidMobilePhone"].ShouldBe(1);
        counts["sharedEmail"].ShouldBe(1);
        counts["noContact"].ShouldBe(0);

        // Dort aktif istihdam; ayrilmis 00004 yok.
        counts["missingTitle"].ShouldBe(4);
        counts["missingDuty"].ShouldBe(4);
        body.Json.GetProperty("ineligibleCount").GetInt32().ShouldBe(2);

        body.Json.GetProperty("warningsRun").GetProperty("status").GetString().ShouldBe("completedWithWarnings");
        body.Json.GetProperty("lastRun").GetProperty("status").GetString().ShouldBe("failed");

        var duties = await ItemsAsync(client, token, "missingDuty");
        duties.Select(i => i.GetProperty("registryCode").GetString()).ShouldBe(["00001", "00002", "00003", "00005"]);
    }

    [Fact]
    public async Task Card_without_a_person_shows_only_the_registry_code_and_a_card_with_a_person_shows_the_name()
    {
        await GrantRoleAsync(Role.HrIdentityOperationsCode);
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var missing = (await ItemsAsync(client, token, "missingNationalId")).ShouldHaveSingleItem();
        missing.GetProperty("registryCode").GetString().ShouldBe("99001");
        missing.GetProperty("personId").ValueKind.ShouldBe(JsonValueKind.Null);
        missing.GetProperty("firstName").ValueKind.ShouldBe(JsonValueKind.Null);

        var phone = (await ItemsAsync(client, token, "invalidMobilePhone")).ShouldHaveSingleItem();
        phone.GetProperty("registryCode").GetString().ShouldBe("00002");
        phone.GetProperty("personId").GetGuid().ShouldBe(await PersonIdAsync(2));
        phone.GetProperty("firstName").GetString().ShouldBe("Ahmet");
        phone.GetProperty("company").GetString().ShouldBe("Duzen Laboratuvarlar");
    }

    [Fact]
    public async Task Report_carries_no_national_id_email_or_phone_and_is_logged_with_the_listed_people()
    {
        // Maskeleme testi: raporda iletisim ve kimlik bilgisi yoktur; IK karti sicille bulur.
        await GrantRoleAsync(Role.HrIdentityOperationsCode);
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);
        var before = await _fixture.WithDbAsync(context => context.AccessLog.MaxAsync(e => (long?)e.Id)) ?? 0;

        var (_, body) = await SendAsync(client, $"{Report}?issue=sharedEmail", token);

        var text = body.Json.GetRawText();
        foreach (var forbidden in new[] { NationalId(3), "ortak@duzen.com.tr", "5321234568", "\"nationalId\"", "\"email\"", "\"mobilePhone\"" })
        {
            text.ShouldNotContain(forbidden, Case.Sensitive);
        }

        var entry = await _fixture.WithDbAsync(context => context.AccessLog.Where(e => e.Id > before).SingleAsync());
        entry.AccessType.ShouldBe(AccessType.List);
        entry.EntityName.ShouldBe(DataQualityService.EntityName);
        entry.SubjectIds.ShouldBe([await PersonIdAsync(3)]);
        entry.Filters.ShouldContain("SharedEmail");
    }

    [Fact]
    public async Task Ineligible_lists_unregistered_active_people_who_cannot_sign_up_with_their_reasons()
    {
        // SYG-PERS-071: 00001'in hesabi var; 00002 uye olabilir; 00004 ayrilmis.
        await GrantRoleAsync(Role.HrIdentityOperationsCode);
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var (status, body) = await SendAsync(client, Ineligible, token);

        status.ShouldBe(HttpStatusCode.OK);
        var items = body.Json.GetProperty("items").EnumerateArray().ToList();
        items.Select(i => i.GetProperty("registryCode").GetString()).ShouldBe(["00003", "00005"]);
        items[0].GetProperty("reasons").EnumerateArray().Select(r => r.GetString()).ShouldBe(["sharedEmail"]);
        items[1].GetProperty("reasons").EnumerateArray().Select(r => r.GetString()).ShouldBe(["domainNotAccepted"]);
        body.Json.GetRawText().ShouldNotContain("gmail");
    }

    [Fact]
    public async Task Export_writes_both_sheets_applies_the_issue_filter_and_is_logged()
    {
        await GrantRoleAsync(Role.HrIdentityOperationsCode);
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);
        var before = await _fixture.WithDbAsync(context => context.AccessLog.MaxAsync(e => (long?)e.Id)) ?? 0;

        var (status, body) = await SendAsync(client, $"{Export}?issue=missingTitle", token);

        status.ShouldBe(HttpStatusCode.OK);
        body.FileName.ShouldStartWith("veri-kalitesi-");
        body.FileName.ShouldEndWith(".xlsx");
        body.NoStore.ShouldBeTrue();

        using var workbook = new XLWorkbook(new MemoryStream(body.Content));
        workbook.Worksheets.Select(w => w.Name).ShouldBe(["Rapor bilgisi", "Uyarılar", "Üye olamayacak kişiler"]);

        var issues = workbook.Worksheet("Uyarılar");
        Row(issues, 1).ShouldBe(["Uyarı", "Sicil", "Ad", "Soyad", "Firma", "Şube", "Birim"]);
        Enumerable.Range(2, issues.LastRowUsed()!.RowNumber() - 1).Select(r => issues.Cell(r, 1).GetString()).ShouldAllBe(v => v == "Ünvanı boş");
        issues.LastRowUsed()!.RowNumber().ShouldBe(5);

        var ineligible = workbook.Worksheet("Üye olamayacak kişiler");
        Row(ineligible, 1).ShouldBe(["Sicil", "Ad", "Soyad", "Firma", "Şube", "Birim", "Neden"]);
        ineligible.Cell(3, 7).GetString().ShouldBe("E-postası kabul edilen alan adlarında değil");

        // Maskeleme testi: dosyada da TCKN, e-posta ve telefon yok.
        var cells = workbook.Worksheets.SelectMany(w => w.CellsUsed()).Select(c => c.GetString()).ToList();
        cells.ShouldAllBe(v => !v.Contains('@') && !v.Contains("53212345", StringComparison.Ordinal) && !v.Contains(NationalId(3), StringComparison.Ordinal));

        var entry = await _fixture.WithDbAsync(context => context.AccessLog.Where(e => e.Id > before).SingleAsync());
        entry.AccessType.ShouldBe(AccessType.Export);
        entry.EntityName.ShouldBe(DataQualityService.EntityName);
        entry.RecordCount.ShouldBe(6);
        entry.SubjectIds.Count.ShouldBe(4);
        entry.Filters.ShouldContain("MissingTitle");
    }

    private static async Task<List<JsonElement>> ItemsAsync(HttpClient client, string token, string issue)
    {
        var (_, body) = await SendAsync(client, $"{Report}?issue={issue}&pageSize=100", token);
        return [.. body.Json.GetProperty("items").GetProperty("items").EnumerateArray()];
    }

    /// <summary>
    /// Iki calisma: once uyarili tamamlanan (kart uyarilarinin kaynagi), sonra basarisiz olan.
    /// Basarisiz calismanin "TCKN gecersiz" uyarisi raporda gorunmemelidir.
    /// </summary>
    private Task<int> SeedSyncRunsAsync() =>
        _fixture.WithDbAsync(async context =>
        {
            await context.Database.ExecuteSqlRawAsync("DELETE FROM personnel.sync_warning; DELETE FROM personnel.sync_run;");
            var now = _fixture.Clock.UtcNow;
            var completed = Guid.CreateVersion7();
            var failed = Guid.CreateVersion7();
            await context.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO personnel.sync_run (public_id, trigger, started_at, finished_at, status, records_read, records_skipped, persons_created, persons_updated,
                    employments_created, employments_updated, employments_deactivated, companies_changed, warning_count)
                VALUES ({completed}, 'Scheduled', {now.AddMinutes(-30)}, {now.AddMinutes(-29)}, 'CompletedWithWarnings', 7, 2, 0, 0, 0, 0, 0, 0, 3)
                """);
            await context.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO personnel.sync_run (public_id, trigger, started_at, finished_at, status, records_read, records_skipped, persons_created, persons_updated,
                    employments_created, employments_updated, employments_deactivated, companies_changed, warning_count, failure_reason)
                VALUES ({failed}, 'Scheduled', {now.AddMinutes(-10)}, {now.AddMinutes(-9)}, 'Failed', 0, 0, 0, 0, 0, 0, 0, 0, 0, 'SourceUnavailable')
                """);
            await context.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO personnel.sync_warning (run_id, code, registry_code, detail, is_active_card)
                SELECT id, w.code, w.registry_code, 'test', w.active FROM personnel.sync_run,
                    (VALUES ('MissingNationalId', '99001', true), ('MissingNationalId', '99002', false), ('InvalidMobilePhone', '00002', true)) AS w(code, registry_code, active)
                WHERE public_id = {completed}
                """);
            return await context.Database.ExecuteSqlAsync(
                $"""
                INSERT INTO personnel.sync_warning (run_id, code, registry_code, detail, is_active_card)
                SELECT id, 'InvalidNationalId', '99003', 'test', true FROM personnel.sync_run WHERE public_id = {failed}
                """);
        });

    private static List<string> Row(IXLWorksheet sheet, int row) =>
        [.. Enumerable.Range(1, sheet.LastColumnUsed()!.ColumnNumber()).Select(c => sheet.Cell(row, c).GetString())];

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

    private static async Task<(HttpStatusCode Status, Download Body)> SendAsync(HttpClient client, string path, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        var content = await response.Content.ReadAsByteArrayAsync();
        var type = response.Content.Headers.ContentType?.MediaType;
        var json = type is not null && type.Contains("json", StringComparison.Ordinal) && content.Length > 0
            ? JsonDocument.Parse(content).RootElement.Clone()
            : default;
        return (response.StatusCode, new Download(
            content,
            response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"'),
            response.Headers.CacheControl?.NoStore == true,
            json));
    }

    private sealed record Download(byte[] Content, string? FileName, bool NoStore, JsonElement Json);
}
