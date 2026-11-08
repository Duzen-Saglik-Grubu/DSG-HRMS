using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Dsg.Hrms.Api.IntegrationTests.Identity;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Dsg.Hrms.Infrastructure.Personnel.Protection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Personnel;

/// <summary>
/// Iki anahtarli sunucu: <c>k1</c> eski, <c>k2</c> guncel (anahtar dondurulmus durum; ADR-0020 §3).
/// </summary>
public sealed class RotatedKeyApiFixture : RegistrationApiFixture
{
    /// <summary>Eski anahtar; test, degerleri bununla sifreleyerek dondurme oncesini kurar.</summary>
    public static readonly string OldKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    /// <summary>Yalnizca eski anahtari bilen sifreleyici: dondurmeden onceki uygulama.</summary>
    public static PersonalDataProtector OldProtector { get; } = new(new PersonalDataProtectionOptions { Keys = new() { ["k1"] = OldKey } });

    /// <inheritdoc />
    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.UseSetting("PersonalDataProtection:Keys:k1", OldKey);
        builder.UseSetting("PersonalDataProtection:Keys:k2", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    }
}

/// <summary>
/// Ozel nitelikli verinin yeniden sifrelenmesi uctan uca (SYG-PERS-062; ADR-0020 §3; KR-138).
/// Veriler SENTETIKTIR.
/// </summary>
/// <remarks>Kisi 2 ilk sistem yoneticisidir (yapilandirma); kisi 1'e testte IK rolu verilir.</remarks>
[Collection(ApiHostGroup.Name)]
public sealed class PersonalDataEncryptionApiTests : IClassFixture<RotatedKeyApiFixture>, IAsyncLifetime
{
    private const string Encryption = "/api/v1/personnel/encryption";
    private const string Password = "Kediler uyur 7";

    private readonly RotatedKeyApiFixture _fixture;

    public PersonalDataEncryptionApiTests(RotatedKeyApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await CreateAccountAsync(1);
        await CreateAccountAsync(2);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Old_key_values_are_re_encrypted_with_the_current_key_without_changing_them()
    {
        var seed = await SeedOldValuesAsync();
        using var client = _fixture.CreateClient();
        var admin = await SignInAsync(client, "mehmet.kaya@duzen.com.tr");

        // Once: iki kan grubu ve bir onerilen kan grubu eski anahtarla; bir deger cozulemez.
        var before = (await SendAsync(client, HttpMethod.Get, Encryption, admin)).Body;
        before.GetProperty("currentKeyId").GetString().ShouldBe("k2");
        Count(before, "k1").ShouldBe(4);
        Count(before, "k2").ShouldBe(0);

        var changesBefore = await _fixture.WithDbAsync(context => context.ChangeLog.CountAsync());
        // Denetim tablolari testler arasinda silinmez (KR-118): yalnizca bu testin olaylari.
        var eventsBefore = await _fixture.WithDbAsync(context => context.SecurityEvents.MaxAsync(e => (long?)e.Id)) ?? 0;
        var (status, result) = await SendAsync(client, HttpMethod.Post, $"{Encryption}/re-encryption", admin);

        status.ShouldBe(HttpStatusCode.OK);
        result.GetProperty("targetKeyId").GetString().ShouldBe("k2");
        result.GetProperty("sourceKeyIds").EnumerateArray().Select(k => k.GetString()).ShouldBe(["k1"]);
        result.GetProperty("reEncrypted").GetInt32().ShouldBe(3);
        result.GetProperty("undecryptable").GetInt32().ShouldBe(1);
        result.GetProperty("remainingOnOldKeys").GetInt32().ShouldBe(1);

        // Degerler ayni; artik guncel anahtarla ve ayni yere bagli (AAD).
        await _fixture.WithServicesAsync(async services =>
        {
            var protector = services.GetRequiredService<PersonalDataProtector>();
            var context = services.GetRequiredService<HrmsDbContext>();
            foreach (var (index, code) in new[] { (3, "3"), (5, "8") })
            {
                var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
                var cipher = await context.Set<PersonSensitive>().Where(s => s.PersonId == person.Id).Select(s => s.BloodGroup!).SingleAsync();
                PersonalDataProtector.KeyIdOf(cipher).ShouldBe("k2");
                protector.TryUnprotect(cipher, ProtectedField.BloodGroup(person.PublicId)).ShouldBe(code);
            }

            var request = await context.Set<CorrectionRequest>().SingleAsync(r => r.PublicId == seed.Request);
            PersonalDataProtector.KeyIdOf(request.ProposedValue!).ShouldBe("k2");
            protector.TryUnprotect(request.ProposedValue!, ProtectedField.CorrectionValue(request.PublicId)).ShouldBe("5");
            return 0;
        });

        // Denetim izine "degisti" yazilmaz; is tek guvenlik olayidir (baslatan, sayi, anahtarlar).
        (await _fixture.WithDbAsync(context => context.ChangeLog.CountAsync())).ShouldBe(changesBefore);
        var events = await _fixture.WithDbAsync(context => context.SecurityEvents
            .Where(e => e.Id > eventsBefore && e.EventType == SecurityEventType.PersonalDataReEncrypted).ToListAsync());
        var entry = events.ShouldHaveSingleItem();
        entry.Detail.ShouldBe("k1>k2 n=3 kalan=1 cozulemeyen=1");
        entry.ActorUserAccountId.ShouldNotBeNull();
        entry.PersonId.ShouldBeNull();

        var after = (await SendAsync(client, HttpMethod.Get, Encryption, admin)).Body;
        Count(after, "k1").ShouldBe(1);
        Count(after, "k2").ShouldBe(3);
    }

    [Fact]
    public async Task Running_again_rewrites_nothing()
    {
        await SeedOldValuesAsync();
        using var client = _fixture.CreateClient();
        var admin = await SignInAsync(client, "mehmet.kaya@duzen.com.tr");

        await SendAsync(client, HttpMethod.Post, $"{Encryption}/re-encryption", admin);
        var ciphers = await CiphersAsync();
        var (_, second) = await SendAsync(client, HttpMethod.Post, $"{Encryption}/re-encryption", admin);

        second.GetProperty("reEncrypted").GetInt32().ShouldBe(0);
        second.GetProperty("sourceKeyIds").GetArrayLength().ShouldBe(0);
        (await CiphersAsync()).ShouldBe(ciphers);
    }

    [Fact]
    public async Task Only_the_system_administrator_may_see_or_run_it()
    {
        // KR-138: izin IK rolunde yoktur; IK'nin diger butun T1 izinleri olsa da 403.
        Permissions.ByRole[Role.HrIdentityOperationsCode].ShouldNotContain(Permissions.EncryptionUpdate);
        Permissions.ByRole[Role.SystemAdministratorCode].ShouldContain(Permissions.EncryptionUpdate);

        await GrantRoleAsync(1, Role.HrIdentityOperationsCode);
        using var client = _fixture.CreateClient();
        var hr = await SignInAsync(client, "ahmet.yilmaz@duzen.com.tr");

        (await SendAsync(client, HttpMethod.Get, Encryption, hr)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await SendAsync(client, HttpMethod.Post, $"{Encryption}/re-encryption", hr)).Status.ShouldBe(HttpStatusCode.Forbidden);
        (await SendAsync(client, HttpMethod.Get, Encryption, null)).Status.ShouldBe(HttpStatusCode.Unauthorized);
        (await _fixture.WithDbAsync(context => context.Set<RoleGrant>()
            .CountAsync(g => g.Permission == Permissions.EncryptionUpdate))).ShouldBe(1);
    }

    private static int Count(JsonElement status, string keyId) =>
        status.GetProperty("keys").EnumerateArray().Where(k => k.GetProperty("keyId").GetString() == keyId)
            .Select(k => k.GetProperty("count").GetInt32()).SingleOrDefault();

    /// <summary>
    /// Dondurmeden onceki durum: kisi 3 ("3") ve kisi 5 ("8") kan grubu ile kisi 1'in onerilen kan
    /// grubu ("5") eski anahtarla; kisi 4'un degeri baska kisinin satirindan kopya (AAD tutmaz).
    /// </summary>
    private Task<Seed> SeedOldValuesAsync() =>
        _fixture.WithDbAsync(async context =>
        {
            await context.Set<CorrectionRequest>().IgnoreQueryFilters().ExecuteDeleteAsync();
            await context.Set<PersonSensitive>().IgnoreQueryFilters().ExecuteDeleteAsync();
            var old = RotatedKeyApiFixture.OldProtector;

            async Task<Person> PersonAsync(int index) => await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));

            var person3 = await PersonAsync(3);
            var person4 = await PersonAsync(4);
            var person5 = await PersonAsync(5);
            foreach (var (person, cipher) in new[]
            {
                (person3, old.Protect("3", ProtectedField.BloodGroup(person3.PublicId))),
                (person5, old.Protect("8", ProtectedField.BloodGroup(person5.PublicId))),
                (person4, old.Protect("1", ProtectedField.BloodGroup(person3.PublicId))),
            })
            {
                var row = PersonSensitive.For(person.Id);
                row.SetProtectedBloodGroup(cipher);
                context.Add(row);
            }

            var person1 = await PersonAsync(1);
            var request = CorrectionRequest.Submit(person1.Id, CorrectionField.BloodGroup, "5", _fixture.Clock.UtcNow);
            request.ProtectValue(code => old.Protect(code, ProtectedField.CorrectionValue(request.PublicId)));
            context.Add(request);
            await context.SaveChangesAsync();
            return new Seed(request.PublicId);
        });

    private Task<List<string>> CiphersAsync() =>
        _fixture.WithDbAsync(async context =>
        {
            var blood = await context.Set<PersonSensitive>().Where(s => s.BloodGroup != null).OrderBy(s => s.Id).Select(s => s.BloodGroup!).ToListAsync();
            var proposed = await context.Set<CorrectionRequest>().Where(r => r.ProposedValue != null).OrderBy(r => r.Id).Select(r => r.ProposedValue!).ToListAsync();
            return blood.Concat(proposed).ToList();
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

    private Task<int> GrantRoleAsync(int index, string roleCode) =>
        _fixture.WithDbAsync(async context =>
        {
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
            var account = await context.Set<UserAccount>().SingleAsync(a => a.PersonId == person.Id);
            var role = await context.Set<Role>().SingleAsync(r => r.Code == roleCode);
            context.Add(UserRole.Assign(account.Id, role.Id));
            return await context.SaveChangesAsync();
        });

    private static async Task<string> SignInAsync(HttpClient client, string email)
    {
        using var response = await client.PostAsJsonAsync(new Uri("/api/v1/identity/sessions", UriKind.Relative), new { email, password = Password });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        return body.GetProperty("session").GetProperty("accessToken").GetString()!;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(HttpClient client, HttpMethod method, string path, string? token)
    {
        using var request = new HttpRequestMessage(method, new Uri(path, UriKind.Relative));
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        var isJson = response.Content.Headers.ContentType?.MediaType?.Contains("json", StringComparison.Ordinal) == true;
        return (response.StatusCode, isJson && text.Length > 0 ? JsonDocument.Parse(text).RootElement.Clone() : default);
    }

    private sealed record Seed(Guid Request);
}
