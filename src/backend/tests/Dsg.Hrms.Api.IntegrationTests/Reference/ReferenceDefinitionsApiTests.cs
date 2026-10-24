using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Domain.Reference;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Reference;

/// <summary>
/// Referans veri cekirdegi uctan uca (SYG-PERS-072; ADR-0022 §1, KR-129). Veriler SENTETIKTIR.
/// </summary>
[Collection(ApiHostGroup.Name)]
public sealed class ReferenceDefinitionsApiTests : IClassFixture<Identity.RegistrationApiFixture>, IAsyncLifetime
{
    private const string Definitions = "/api/v1/reference/definitions";
    private const string Password = "Kediler uyur 7";
    private const string UserEmail = "ahmet.yilmaz@duzen.com.tr";

    private readonly Identity.RegistrationApiFixture _fixture;

    public ReferenceDefinitionsApiTests(Identity.RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await CreateAccountAsync(1);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Seeded_titles_and_blood_groups_are_listed_in_order()
    {
        // PG-PERS-21: unvan 1–4; KR-129: kan grubu 1–8, sistem tanimi.
        var token = await HrTokenAsync();
        using var client = _fixture.CreateClient();

        var titles = (await SendAsync(client, HttpMethod.Get, $"{Definitions}?type=title", token)).Body;
        var bloodGroups = (await SendAsync(client, HttpMethod.Get, $"{Definitions}?type=bloodGroup", token)).Body;

        titles.EnumerateArray().Select(t => (t.GetProperty("code").GetString(), t.GetProperty("name").GetString())).ShouldBe([
            ("1", "Birim Operatörü"), ("2", "Birim Teknik Sorumlusu"), ("3", "Birim Sorumlusu"), ("4", "Yardımcı Hizmetler"),
        ]);
        bloodGroups.EnumerateArray().Select(b => b.GetProperty("name").GetString()).ShouldBe(
            ["0 Rh+", "0 Rh-", "A Rh+", "A Rh-", "B Rh+", "B Rh-", "AB Rh+", "AB Rh-"]);
        bloodGroups.EnumerateArray().ShouldAllBe(b => b.GetProperty("isSystem").GetBoolean());
    }

    [Fact]
    public async Task Hr_adds_a_duty_with_a_generated_code_and_it_is_audited()
    {
        var token = await HrTokenAsync();
        using var client = _fixture.CreateClient();

        var (status, created) = await SendAsync(client, HttpMethod.Post, Definitions, token, new { type = "duty", name = $"Yazılım Uzmanı {Guid.NewGuid():N}" });

        status.ShouldBe(HttpStatusCode.Created);
        created.GetProperty("code").GetString().ShouldStartWith("G");
        var id = created.GetProperty("id").GetGuid();
        (await _fixture.WithDbAsync(context => context.ChangeLog.CountAsync(e => e.EntityName == nameof(ReferenceDefinition) && e.EntityId == id && e.Operation == AuditOperation.Insert)))
            .ShouldBe(1);
    }

    [Fact]
    public async Task Inactive_definition_is_hidden_from_new_selection_but_kept_in_the_full_list()
    {
        var token = await HrTokenAsync();
        using var client = _fixture.CreateClient();
        var id = (await SendAsync(client, HttpMethod.Post, Definitions, token, new { type = "duty", name = "Eski Görev" })).Body.GetProperty("id").GetGuid();

        (await SendAsync(client, HttpMethod.Post, $"{Definitions}/{id}/deactivation", token)).Status.ShouldBe(HttpStatusCode.NoContent);

        (await SendAsync(client, HttpMethod.Get, $"{Definitions}?type=duty", token)).Body.EnumerateArray()
            .ShouldNotContain(d => d.GetProperty("id").GetGuid() == id);
        (await SendAsync(client, HttpMethod.Get, $"{Definitions}?type=duty&includeInactive=true", token)).Body.EnumerateArray()
            .Single(d => d.GetProperty("id").GetGuid() == id).GetProperty("isActive").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Blood_group_definitions_cannot_be_renamed_deactivated_or_added()
    {
        // KR-129: bir adin yanlis duzenlenmesi herkesin saglik verisini yanlis gosterirdi.
        var token = await HrTokenAsync();
        using var client = _fixture.CreateClient();
        var aPositive = (await SendAsync(client, HttpMethod.Get, $"{Definitions}?type=bloodGroup", token)).Body.EnumerateArray()
            .Single(b => b.GetProperty("code").GetString() == "3").GetProperty("id").GetGuid();

        (await SendAsync(client, HttpMethod.Put, $"{Definitions}/{aPositive}/name", token, new { name = "B Rh+" })).Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await SendAsync(client, HttpMethod.Post, $"{Definitions}/{aPositive}/deactivation", token)).Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await SendAsync(client, HttpMethod.Post, Definitions, token, new { type = "bloodGroup", code = "9", name = "Yeni" })).Status.ShouldBe(HttpStatusCode.UnprocessableEntity);

        (await _fixture.WithDbAsync(context => context.Set<ReferenceDefinition>().SingleAsync(d => d.PublicId == aPositive))).Name.ShouldBe("A Rh+");
    }

    [Fact]
    public async Task Renamed_title_appears_on_the_card_without_a_code_change()
    {
        // SYG-PERS-038, 072 (PG-PERS-21): unvan karsiligi referans veriden; karsiligi olmayan kod bos.
        var token = await HrTokenAsync();
        using var client = _fixture.CreateClient();
        await SetTitleCodeAsync(4, "2");
        var title2 = (await SendAsync(client, HttpMethod.Get, $"{Definitions}?type=title", token)).Body.EnumerateArray()
            .Single(t => t.GetProperty("code").GetString() == "2").GetProperty("id").GetGuid();
        var person4 = await _fixture.WithDbAsync(context => context.Set<Person>().Where(p => p.NationalId == NationalId(4)).Select(p => p.PublicId).SingleAsync());

        (await SendAsync(client, HttpMethod.Put, $"{Definitions}/{title2}/name", token, new { name = "Teknik Sorumlu" })).Status.ShouldBe(HttpStatusCode.NoContent);
        var renamed = (await SendAsync(client, HttpMethod.Get, $"/api/v1/personnel/persons/{person4}", token)).Body
            .GetProperty("employments")[0].GetProperty("workLife");
        await SetTitleCodeAsync(4, "9");
        var unknown = (await SendAsync(client, HttpMethod.Get, $"/api/v1/personnel/persons/{person4}", token)).Body
            .GetProperty("employments")[0].GetProperty("workLife");

        renamed.GetProperty("titleName").GetString().ShouldBe("Teknik Sorumlu");
        unknown.GetProperty("titleCode").GetString().ShouldBe("9");
        unknown.GetProperty("titleName").ValueKind.ShouldBe(JsonValueKind.Null);

        // Diger testler seed adlarina bakar; ad geri alinir.
        await SendAsync(client, HttpMethod.Put, $"{Definitions}/{title2}/name", token, new { name = "Birim Teknik Sorumlusu" });
    }

    [Fact]
    public async Task User_without_permission_cannot_list_or_change_definitions()
    {
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);

        (await SendAsync(client, HttpMethod.Get, $"{Definitions}?type=title", token)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await SendAsync(client, HttpMethod.Post, Definitions, token, new { type = "duty", name = "X" })).Status.ShouldBe(HttpStatusCode.Forbidden);
    }

    /// <summary>Kisi 4'un istihdamina tek bir kapali donem yazar; unvan kodu verilir.</summary>
    private Task<int> SetTitleCodeAsync(int index, string titleCode) =>
        _fixture.WithDbAsync(async context =>
        {
            var employment = await context.Set<Employment>().SingleAsync(e => e.Person.NationalId == NationalId(index));
            var existing = await context.Set<EmploymentAssignment>().Where(a => a.EmploymentId == employment.Id).ToListAsync();
            context.RemoveRange(existing);
            await context.SaveChangesAsync();
            var (assignment, _) = AssignmentTimeline.Apply(
                employment, [], new AssignmentValues(employment.CompanyId, null, null, titleCode, null, null, null), Today);
            context.Add(assignment!);
            return await context.SaveChangesAsync();
        });

    private async Task<string> HrTokenAsync()
    {
        await _fixture.WithDbAsync(async context =>
        {
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(1));
            var account = await context.Set<UserAccount>().SingleAsync(a => a.PersonId == person.Id);
            var role = await context.Set<Role>().SingleAsync(r => r.Code == Role.HrIdentityOperationsCode);
            if (!await context.Set<UserRole>().AnyAsync(r => r.UserAccountId == account.Id && r.RoleId == role.Id))
            {
                context.Add(UserRole.Assign(account.Id, role.Id));
            }

            return await context.SaveChangesAsync();
        });
        using var client = _fixture.CreateClient();
        return await SignInAsync(client);
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
