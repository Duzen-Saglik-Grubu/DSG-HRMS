using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using ClosedXML.Excel;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Application.Personnel.Listing;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Dsg.Hrms.Infrastructure.Personnel.Protection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Personnel;

/// <summary>
/// Personel listesinin disa aktarilmasi (SYG-PERS-032, 033, 052, 053, 059). Veriler SENTETIKTIR.
/// </summary>
/// <remarks>
/// Kisi 1 oturum acan kullanicidir; tohumda bes aktif kisi vardir (00001–00005).
/// </remarks>
[Collection(ApiHostGroup.Name)]
public sealed class PersonExportApiTests : IClassFixture<Identity.RegistrationApiFixture>, IAsyncLifetime
{
    private const string Export = "/api/v1/personnel/persons/export";
    private const string Fields = "/api/v1/personnel/persons/export/fields";
    private const string Password = "Kediler uyur 7";
    private const string UserEmail = "ahmet.yilmaz@duzen.com.tr";
    private const string ExcelType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private readonly Identity.RegistrationApiFixture _fixture;

    public PersonExportApiTests(Identity.RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await CreateAccountAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Without_the_export_permission_both_endpoints_are_forbidden()
    {
        // SYG-PERS-032: goruntuleme izni disa aktarma hakki vermez.
        await GrantRoleAsync(await CreateRoleAsync(Permissions.PersonView));
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        (await SendAsync(client, Fields, token)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await SendAsync(client, Export, token)).Status.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Hr_exports_every_matching_person_to_excel_in_list_order_with_full_national_ids()
    {
        await GrantRoleAsync(Role.HrIdentityOperationsCode);
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var (status, response) = await SendAsync(client, $"{Export}?format=excel&field=registryCode&field=nationalId&field=birthDate&field=age&sort=lastName", token);

        status.ShouldBe(HttpStatusCode.OK);
        response.ContentType.ShouldBe(ExcelType);
        response.FileName.ShouldStartWith("personel-listesi-");
        response.FileName.ShouldEndWith(".xlsx");
        response.NoStore.ShouldBeTrue();

        using var workbook = new XLWorkbook(new MemoryStream(response.Content));
        var data = workbook.Worksheet(2);
        Row(data, 1).ShouldBe(["Ad", "Soyad", "Sicil numarası", "TCKN", "Doğum tarihi", "Yaş"]);

        var rows = Enumerable.Range(2, data.LastRowUsed()!.RowNumber() - 1).Select(r => Row(data, r)).ToList();
        rows.Count.ShouldBe(4); // tohumdaki aktif kisiler; ayrilan (00004) varsayilan suzgecte yok

        // Sayfalama yok; kume ve sira liste ucuyla ayni.
        var (_, list) = await SendAsync(client, "/api/v1/personnel/persons?sort=lastName&pageSize=2", token);
        list.Json.GetProperty("totalCount").GetInt32().ShouldBe(4);
        var (_, second) = await SendAsync(client, "/api/v1/personnel/persons?sort=lastName&pageSize=2&page=2", token);
        rows.Select(r => r[2]).ShouldBe([.. list.Json.GetProperty("items").EnumerateArray().Concat(second.Json.GetProperty("items").EnumerateArray())
            .Select(i => i.GetProperty("registryCode").GetString()!)]);
        rows.Select(r => r[3]).ShouldBe([.. rows.Select(r => NationalId(int.Parse(r[2], CultureInfo.InvariantCulture)))]);
        data.Cell(2, 4).DataType.ShouldBe(XLDataType.Text);
        data.Cell(2, 5).DataType.ShouldBe(XLDataType.DateTime);

        workbook.Worksheet(1).CellsUsed().Select(c => c.GetString()).ShouldContain("4");
    }

    [Fact]
    public async Task Fields_come_from_the_visibility_policy_and_others_are_rejected()
    {
        // SYG-PERS-052 (yetki sizintisi): adres ve kan grubu view_sensitive ister; kume isleminde
        // kisinin kendisi kurali yoktur. Tam TCKN'yi goremeyenin dosyasinda TCKN maskelidir.
        await GrantRoleAsync(await CreateRoleAsync(Permissions.PersonView, Permissions.PersonExport));
        await SetAddressAsync(1, "Gizli Sok. No: 1");
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var (_, fields) = await SendAsync(client, Fields, token);
        var names = fields.Json.EnumerateArray().Select(f => f.GetProperty("field").GetString()).ToList();
        names.ShouldContain("nationalId");
        names.ShouldNotContain("address");
        names.ShouldNotContain("bloodGroup");

        (await SendAsync(client, $"{Export}?field=address", token)).Status.ShouldBe(HttpStatusCode.BadRequest);
        (await SendAsync(client, $"{Export}?field=bloodGroup", token)).Status.ShouldBe(HttpStatusCode.BadRequest);
        (await SendAsync(client, $"{Export}?address=gizli", token)).Status.ShouldBe(HttpStatusCode.BadRequest);
        (await SendAsync(client, $"{Export}?nationalId=1", token)).Status.ShouldBe(HttpStatusCode.BadRequest);

        var (status, response) = await SendAsync(client, $"{Export}?field=nationalId&field=registryCode", token);
        status.ShouldBe(HttpStatusCode.OK);
        using var workbook = new XLWorkbook(new MemoryStream(response.Content));
        var data = workbook.Worksheet(2);
        for (var r = 2; r <= data.LastRowUsed()!.RowNumber(); r++)
        {
            var masked = data.Cell(r, 4).GetString();
            masked.ShouldMatch(@"^\d{3}\*{6}\d{2}$");
        }

        // Dosyanin hicbir hucresinde adres yok.
        workbook.Worksheets.SelectMany(s => s.CellsUsed()).ShouldAllBe(c => !c.GetString().Contains("Gizli Sok.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Export_is_logged_with_fields_count_and_people_without_the_protected_filter_value()
    {
        // SYG-PERS-033, 059.
        await GrantRoleAsync(Role.HrIdentityOperationsCode);
        await SetAddressAsync(2, "Lale Sok. No: 5");
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);
        var before = await _fixture.WithDbAsync(context => context.AccessLog.MaxAsync(e => (long?)e.Id)) ?? 0;

        var (status, response) = await SendAsync(client, $"{Export}?format=excel&field=address&address=lale", token);

        status.ShouldBe(HttpStatusCode.OK);
        var entry = await _fixture.WithDbAsync(context => context.AccessLog.Where(e => e.Id > before).SingleAsync());
        entry.AccessType.ShouldBe(AccessType.Export);
        entry.RecordCount.ShouldBe(1);
        entry.SubjectIds.ShouldBe([await PersonIdAsync(2)]);
        entry.Filters.ShouldContain("Address");
        entry.Filters.ShouldContain("Excel");
        entry.Filters.ShouldContain(PersonListService.ProtectedFilterMarker);
        entry.Filters.ShouldNotContain("lale", Case.Insensitive);

        // Dosyadaki suzgec ozeti de korumali degeri yazmaz.
        using var workbook = new XLWorkbook(new MemoryStream(response.Content));
        workbook.Worksheet(1).CellsUsed().Select(c => c.GetString()).ShouldAllBe(v => !v.Contains("lale", StringComparison.OrdinalIgnoreCase));
        workbook.Worksheet(2).Cell(2, 3).GetString().ShouldBe("Lale Sok. No: 5");
    }

    [Fact]
    public async Task Blood_group_export_writes_a_special_category_record_for_each_person_with_a_value()
    {
        // SYG-PERS-059 (KVKK md. 6): degeri dosyaya giren her kisi icin ayri kayit.
        await GrantRoleAsync(Role.HrIdentityOperationsCode);
        await SetBloodGroupAsync(3, 3);
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);
        var before = await _fixture.WithDbAsync(context => context.AccessLog.MaxAsync(e => (long?)e.Id)) ?? 0;

        var (status, response) = await SendAsync(client, $"{Export}?field=registryCode&field=bloodGroup", token);

        status.ShouldBe(HttpStatusCode.OK);
        using var workbook = new XLWorkbook(new MemoryStream(response.Content));
        var data = workbook.Worksheet(2);
        var values = Enumerable.Range(2, data.LastRowUsed()!.RowNumber() - 1).Select(r => Row(data, r)).ToList();
        values.Single(r => r[2] == "00003")[3].ShouldNotBeNullOrEmpty();
        values.Where(r => r[2] != "00003").ShouldAllBe(r => r[3] == string.Empty);

        var entries = await _fixture.WithDbAsync(context => context.AccessLog.Where(e => e.Id > before).ToListAsync());
        entries.Count(e => e.AccessType == AccessType.Export).ShouldBe(1);
        entries.Where(e => e.AccessType == AccessType.SpecialCategoryView).Select(e => e.EntityId).ShouldBe([await PersonIdAsync(3)]);
    }

    [Fact]
    public async Task Photo_is_pdf_only_and_pdf_export_returns_a_pdf()
    {
        await GrantRoleAsync(Role.HrIdentityOperationsCode);
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        var (excel, problem) = await SendAsync(client, $"{Export}?format=excel&field=photo", token);
        excel.ShouldBe(HttpStatusCode.BadRequest);
        problem.Json.GetProperty("detail").GetString().ShouldBe("Fotoğraf yalnızca PDF'e aktarılabilir.");

        var (status, response) = await SendAsync(client, $"{Export}?format=pdf&field=photo&field=registryCode", token);
        status.ShouldBe(HttpStatusCode.OK);
        response.ContentType.ShouldBe("application/pdf");
        response.FileName.ShouldEndWith(".pdf");
        response.Content[..4].ShouldBe("%PDF"u8.ToArray());
    }

    [Fact]
    public async Task Invalid_format_or_duplicate_fields_are_rejected()
    {
        await GrantRoleAsync(Role.HrIdentityOperationsCode);
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        (await SendAsync(client, $"{Export}?format=csv", token)).Status.ShouldBe(HttpStatusCode.BadRequest);
        (await SendAsync(client, $"{Export}?field=unknown", token)).Status.ShouldBe(HttpStatusCode.BadRequest);
        (await SendAsync(client, $"{Export}?field=age&field=age", token)).Status.ShouldBe(HttpStatusCode.BadRequest);
    }

    private static List<string> Row(IXLWorksheet sheet, int row) =>
        [.. Enumerable.Range(1, sheet.LastColumnUsed()!.ColumnNumber()).Select(c => sheet.Cell(row, c).GetString())];

    private Task<Guid> PersonIdAsync(int index) =>
        _fixture.WithDbAsync(context => context.Set<Person>().Where(p => p.NationalId == NationalId(index)).Select(p => p.PublicId).SingleAsync());

    private Task<int> SetAddressAsync(int index, string address) =>
        _fixture.WithDbAsync(async context =>
        {
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
            var row = await context.Set<PersonSensitive>().SingleOrDefaultAsync(r => r.PersonId == person.Id);
            if (row is null)
            {
                row = PersonSensitive.For(person.Id);
                context.Add(row);
            }

            row.ApplyAddress(address);
            return await context.SaveChangesAsync();
        });

    private Task<int> SetBloodGroupAsync(int index, short code) =>
        _fixture.WithServicesAsync(async services =>
        {
            var protector = services.GetRequiredService<PersonalDataProtector>();
            var context = services.GetRequiredService<HrmsDbContext>();
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
            var row = await context.Set<PersonSensitive>().SingleOrDefaultAsync(r => r.PersonId == person.Id);
            if (row is null)
            {
                row = PersonSensitive.For(person.Id);
                context.Add(row);
            }

            row.SetProtectedBloodGroup(protector.Protect(code.ToString(CultureInfo.InvariantCulture), ProtectedField.BloodGroup(person.PublicId)));
            return await context.SaveChangesAsync();
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
            type,
            response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"'),
            response.Headers.CacheControl?.NoStore == true,
            json));
    }

    private sealed record Download(byte[] Content, string? ContentType, string? FileName, bool NoStore, JsonElement Json);
}
