using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Identity;

/// <summary>
/// IK hesap islemleri uctan uca: gercek HTTP boru hatti ve gercek PostgreSQL
/// (SYG-KMLK-054, 057, 058, 073, 074). Veriler SENTETIKTIR.
/// </summary>
/// <remarks>
/// Kisi 1 IK kullanicisidir (IK Kimlik Islemleri rolu); kisi 2'nin hesabi uzerinde islem yapilir.
/// Kisi 3 uye olmamistir, kisi 4'un istihdami bitmistir.
/// </remarks>
[Collection(ApiHostGroup.Name)]
public sealed class AccountsApiTests : IClassFixture<RegistrationApiFixture>, IAsyncLifetime
{
    private const string Accounts = "/api/v1/identity/accounts";
    private const string Password = "Kediler uyur 7";
    private const string HrEmail = "ahmet.yilmaz@duzen.com.tr";
    private const string TargetEmail = "mehmet.kaya@duzen.com.tr";

    private readonly RegistrationApiFixture _fixture;

    public AccountsApiTests(RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await CreateAccountAsync(1);
        await CreateAccountAsync(2);
        await CreateAccountAsync(4);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task User_without_permission_cannot_list_or_change_accounts()
    {
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client, HrEmail);

        (await GetAsync(client, Accounts, token)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await PostAsync(client, $"{Accounts}/{await PersonIdAsync(2)}/deactivation", token, new { reason = "Deneme" })).Status
            .ShouldBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Result_carries_only_name_registry_code_company_and_account_state()
    {
        // SYG-KMLK-073: TCKN, dogum tarihi, e-posta ve telefon bu ekrana hic gelmez.
        var token = await HrSignInAsync();
        using var client = _fixture.CreateClient();

        var (status, body) = await GetAsync(client, $"{Accounts}?q=00002", token);

        status.ShouldBe(HttpStatusCode.OK);
        var item = body.GetProperty("items").EnumerateArray().Single();
        item.EnumerateObject().Select(p => p.Name).ShouldBe(["personId", "firstName", "lastName", "employments", "state", "statusReason", "isCurrentUser"]);
        item.GetProperty("employments")[0].EnumerateObject().Select(p => p.Name).ShouldBe(["registryCode", "companyName", "isActive"]);
        item.GetProperty("employments")[0].GetProperty("registryCode").GetString().ShouldBe("00002");
        item.GetProperty("state").GetString().ShouldBe("active");
        body.GetRawText().ShouldNotContain(NationalId(2));
        body.GetRawText().ShouldNotContain(TargetEmail);
    }

    [Fact]
    public async Task Search_by_name_or_registry_prefix_is_paged()
    {
        var token = await HrSignInAsync();
        using var client = _fixture.CreateClient();

        var (_, all) = await GetAsync(client, $"{Accounts}?q=yIlMaZ&pageSize=2&page=2", token);
        all.GetProperty("totalCount").GetInt32().ShouldBe(5);
        all.GetProperty("totalPages").GetInt32().ShouldBe(3);
        all.GetProperty("page").GetInt32().ShouldBe(2);
        all.GetProperty("items").GetArrayLength().ShouldBe(2);

        (await GetAsync(client, $"{Accounts}?q=0000", token)).Body.GetProperty("totalCount").GetInt32().ShouldBe(5);
        (await GetAsync(client, $"{Accounts}?q=99999", token)).Body.GetProperty("totalCount").GetInt32().ShouldBe(0);
        (await GetAsync(client, $"{Accounts}?q=%25", token)).Body.GetProperty("totalCount").GetInt32().ShouldBe(0);
        (await GetAsync(client, $"{Accounts}?pageSize=101", token)).Status.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task States_distinguish_no_account_passive_and_locked()
    {
        await _fixture.WithDbAsync(async context =>
        {
            var account = await AccountOfAsync(context, 2);
            account.RecordLockout(_fixture.Clock.UtcNow.AddMinutes(10));
            var ended = await AccountOfAsync(context, 4);
            ended.DeactivateForEmploymentEnd();
            return await context.SaveChangesAsync();
        });
        var token = await HrSignInAsync();
        using var client = _fixture.CreateClient();

        (await StateOfAsync(client, token, "00002")).ShouldBe(("locked", null));
        (await StateOfAsync(client, token, "00003")).ShouldBe(("none", null));
        (await StateOfAsync(client, token, "00004")).ShouldBe(("passive", "employmentEnded"));
    }

    [Fact]
    public async Task Manual_deactivation_ends_open_sessions_and_is_audited_with_the_reason()
    {
        using var target = _fixture.CreateClient();
        var targetSession = await SignInResponseAsync(target, TargetEmail);
        // Kisi 2 ilk sistem yoneticisidir (girisle atanir); son aktif yonetici pasife alinamadigi
        // icin baska bir aktif yonetici de tanimlanir (#138).
        await GrantRoleAsync(4, Role.SystemAdministratorCode);
        var token = await HrSignInAsync();
        using var client = _fixture.CreateClient();
        var personId = await PersonIdAsync(2);

        (await PostAsync(client, $"{Accounts}/{personId}/deactivation", token, new { reason = "  Uzun sureli izin  " })).Status
            .ShouldBe(HttpStatusCode.NoContent);

        (await StateOfAsync(client, token, "00002")).ShouldBe(("passive", "manual"));
        (await SendActivityAsync(target, targetSession)).ShouldBe(HttpStatusCode.Unauthorized);

        var change = await _fixture.WithDbAsync(async context =>
        {
            var account = await AccountOfAsync(context, 2);
            return await context.ChangeLog.Where(e => e.EntityName == nameof(UserAccount) && e.EntityId == account.PublicId && e.Operation == AuditOperation.Update)
                .OrderByDescending(e => e.Id).Select(e => e.Changes).FirstAsync();
        });
        change.ShouldContain("Uzun sureli izin");
        change.ShouldContain("status");
    }

    [Fact]
    public async Task Reactivation_requires_a_reason_and_an_active_employment()
    {
        var token = await HrSignInAsync();
        using var client = _fixture.CreateClient();
        var target = await PersonIdAsync(2);

        (await PostAsync(client, $"{Accounts}/{target}/deactivation", token, new { reason = " " })).Status.ShouldBe(HttpStatusCode.BadRequest);
        (await PostAsync(client, $"{Accounts}/{target}/activation", token, new { reason = "Deneme" })).Body.GetProperty("detail").GetString()
            .ShouldBe("Hesap zaten aktif.");

        (await PostAsync(client, $"{Accounts}/{target}/deactivation", token, new { reason = "Uzun sureli izin" })).Status.ShouldBe(HttpStatusCode.NoContent);
        (await PostAsync(client, $"{Accounts}/{target}/deactivation", token, new { reason = "Tekrar" })).Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await PostAsync(client, $"{Accounts}/{target}/activation", token, new { reason = "Izin bitti" })).Status.ShouldBe(HttpStatusCode.NoContent);
        (await StateOfAsync(client, token, "00002")).ShouldBe(("active", "manual"));

        // Istihdami biten kisi aktiflestirilemez (KR-015); uye olmamis kisinin hesabi yoktur.
        await _fixture.WithDbAsync(async context =>
        {
            (await AccountOfAsync(context, 4)).DeactivateForEmploymentEnd();
            return await context.SaveChangesAsync();
        });
        var ended = await PostAsync(client, $"{Accounts}/{await PersonIdAsync(4)}/activation", token, new { reason = "Deneme" });
        ended.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        ended.Body.GetProperty("detail").GetString().ShouldBe("Aktif çalışma kaydı olmayan kişinin hesabı aktifleştirilemez.");
        (await PostAsync(client, $"{Accounts}/{await PersonIdAsync(3)}/deactivation", token, new { reason = "Deneme" })).Status
            .ShouldBe(HttpStatusCode.UnprocessableEntity);
        (await PostAsync(client, $"{Accounts}/{Guid.NewGuid()}/deactivation", token, new { reason = "Deneme" })).Status
            .ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Own_account_cannot_be_deactivated_and_is_marked_in_the_list()
    {
        // #138: yonetici kendi hesabini pasife alip sistem disinda kalmamali.
        var token = await HrSignInAsync();
        using var client = _fixture.CreateClient();

        (await GetAsync(client, $"{Accounts}?q=00001", token)).Body.GetProperty("items")[0].GetProperty("isCurrentUser").GetBoolean().ShouldBeTrue();
        (await GetAsync(client, $"{Accounts}?q=00002", token)).Body.GetProperty("items")[0].GetProperty("isCurrentUser").GetBoolean().ShouldBeFalse();

        var (status, body) = await PostAsync(client, $"{Accounts}/{await PersonIdAsync(1)}/deactivation", token, new { reason = "Deneme" });
        status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        body.GetProperty("detail").GetString().ShouldBe("Kendi hesabınızı pasife alamazsınız. Gerekirse başka bir yetkili kullanıcıdan isteyin.");
        (await StateOfAsync(client, token, "00001")).ShouldBe(("active", null));
    }

    [Fact]
    public async Task Last_active_system_administrator_cannot_be_deactivated()
    {
        // #138: aktif kalan son sistem yoneticisi pasife alinirsa sistemi yonetecek kimse kalmaz.
        await GrantRoleAsync(2, Role.SystemAdministratorCode);
        var token = await HrSignInAsync();
        using var client = _fixture.CreateClient();
        var administrator = await PersonIdAsync(2);

        var refused = await PostAsync(client, $"{Accounts}/{administrator}/deactivation", token, new { reason = "Deneme" });
        refused.Status.ShouldBe(HttpStatusCode.UnprocessableEntity);
        refused.Body.GetProperty("detail").GetString()!.ShouldStartWith("Bu kişi aktif kalan son sistem yöneticisi");

        // Pasif yonetici sayilmaz: sistemi yonetemez.
        await GrantRoleAsync(4, Role.SystemAdministratorCode);
        await _fixture.WithDbAsync(async context =>
        {
            (await AccountOfAsync(context, 4)).DeactivateForEmploymentEnd();
            return await context.SaveChangesAsync();
        });
        (await PostAsync(client, $"{Accounts}/{administrator}/deactivation", token, new { reason = "Deneme" })).Status
            .ShouldBe(HttpStatusCode.UnprocessableEntity);

        // Baska bir aktif yonetici varken pasife alinabilir.
        await GrantRoleAsync(1, Role.SystemAdministratorCode);
        (await PostAsync(client, $"{Accounts}/{administrator}/deactivation", token, new { reason = "Deneme" })).Status
            .ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Listing_is_written_to_the_access_log()
    {
        var token = await HrSignInAsync();
        using var client = _fixture.CreateClient();
        var before = await _fixture.WithDbAsync(context => context.AccessLog.CountAsync(e => e.EntityName == nameof(Person)));

        await GetAsync(client, $"{Accounts}?q=00001", token);

        var entry = await _fixture.WithDbAsync(context => context.AccessLog.Where(e => e.EntityName == nameof(Person)).OrderByDescending(e => e.Id).FirstAsync());
        (await _fixture.WithDbAsync(context => context.AccessLog.CountAsync(e => e.EntityName == nameof(Person)))).ShouldBe(before + 1);
        entry.AccessType.ShouldBe(AccessType.List);
        entry.RecordCount.ShouldBe(1);
    }

    // ------------------------------------------------------------------ yardimcilar

    private async Task<string> HrSignInAsync()
    {
        await _fixture.WithDbAsync(async context =>
        {
            var account = await AccountOfAsync(context, 1);
            var role = await context.Set<Role>().SingleAsync(r => r.Code == Role.HrIdentityOperationsCode);
            if (!await context.Set<UserRole>().AnyAsync(r => r.UserAccountId == account.Id))
            {
                context.Add(UserRole.Assign(account.Id, role.Id));
            }

            return await context.SaveChangesAsync();
        });

        using var client = _fixture.CreateClient();
        return await SignInAsync(client, HrEmail);
    }

    private Task<int> GrantRoleAsync(int index, string roleCode) =>
        _fixture.WithDbAsync(async context =>
        {
            var account = await AccountOfAsync(context, index);
            var role = await context.Set<Role>().SingleAsync(r => r.Code == roleCode);
            if (!await context.Set<UserRole>().AnyAsync(r => r.UserAccountId == account.Id && r.RoleId == role.Id))
            {
                context.Add(UserRole.Assign(account.Id, role.Id));
            }

            return await context.SaveChangesAsync();
        });

    private static async Task<(string? State, string? Reason)> StateOfAsync(HttpClient client, string token, string registryCode)
    {
        var (_, body) = await GetAsync(client, $"{Accounts}?q={registryCode}", token);
        var item = body.GetProperty("items").EnumerateArray().Single();
        var reason = item.GetProperty("statusReason");
        return (item.GetProperty("state").GetString(), reason.ValueKind == JsonValueKind.Null ? null : reason.GetString());
    }

    private Task<Guid> PersonIdAsync(int index) =>
        _fixture.WithDbAsync(context => context.Set<Person>().Where(p => p.NationalId == NationalId(index)).Select(p => p.PublicId).SingleAsync());

    private static async Task<UserAccount> AccountOfAsync(Infrastructure.Data.HrmsDbContext context, int index)
    {
        var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
        return await context.Set<UserAccount>().SingleAsync(a => a.PersonId == person.Id);
    }

    private async Task CreateAccountAsync(int index) =>
        await _fixture.WithServicesAsync(async services =>
        {
            var context = services.GetRequiredService<Infrastructure.Data.HrmsDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher>();
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
            context.Add(UserAccount.Register(person.Id, hasher.Hash(PasswordPolicy.Normalize(Password)), _fixture.Clock.UtcNow));
            return await context.SaveChangesAsync();
        });

    private static async Task<string> SignInAsync(HttpClient client, string email) => await SignInResponseAsync(client, email);

    private static async Task<string> SignInResponseAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync(new Uri("/api/v1/identity/sessions", UriKind.Relative), new { email, password = Password });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        return body.GetProperty("session").GetProperty("accessToken").GetString()!;
    }

    private static async Task<HttpStatusCode> SendActivityAsync(HttpClient client, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/api/v1/identity/sessions/activity", UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private static Task<(HttpStatusCode Status, JsonElement Body)> GetAsync(HttpClient client, string path, string token) =>
        SendAsync(client, new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative)), token);

    private static Task<(HttpStatusCode Status, JsonElement Body)> PostAsync(HttpClient client, string path, string token, object body) =>
        SendAsync(client, new HttpRequestMessage(HttpMethod.Post, new Uri(path, UriKind.Relative)) { Content = JsonContent.Create(body) }, token);

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(HttpClient client, HttpRequestMessage request, string token)
    {
        using (request)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(request);
            var text = await response.Content.ReadAsStringAsync();
            return (response.StatusCode, string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone());
        }
    }
}
