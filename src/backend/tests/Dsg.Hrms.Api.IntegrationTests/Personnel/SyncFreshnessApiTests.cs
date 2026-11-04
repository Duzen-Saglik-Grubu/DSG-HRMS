using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Domain.Personnel.Sync;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Personnel;

/// <summary>
/// "Son senkronizasyon" ucu (SYG-PERS-010): izinsiz kullaniciya da acik, yalnizca guncellik
/// bilgisi doner. Veriler SENTETIKTIR.
/// </summary>
[Collection(ApiHostGroup.Name)]
public sealed class SyncFreshnessApiTests : IClassFixture<Identity.RegistrationApiFixture>, IAsyncLifetime
{
    private const string Freshness = "/api/v1/personnel/sync-freshness";
    private const string Password = "Kediler uyur 7";
    private const string UserEmail = "ahmet.yilmaz@duzen.com.tr";

    private static readonly SyncCounts Counts = new(1, 0, 0, 0, 0, 0, 0, 0);

    private readonly Identity.RegistrationApiFixture _fixture;

    public SyncFreshnessApiTests(Identity.RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await _fixture.WithDbAsync(context => context.Database.ExecuteSqlRawAsync("DELETE FROM personnel.sync_warning; DELETE FROM personnel.sync_run;"));
        await CreateAccountAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Requires_sign_in()
    {
        using var client = _fixture.CreateClient();

        (await GetAsync(client, token: null)).Status.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task User_without_a_role_sees_freshness_only()
    {
        // Kendi kartini goren izinsiz kullanici da verinin ne kadar guncel oldugunu bilmelidir.
        var succeededAt = _fixture.Clock.UtcNow.AddMinutes(-20);
        await AddRunAsync(succeededAt.AddMinutes(-1), run => run.Complete(Counts, [], succeededAt));

        using var client = _fixture.CreateClient();
        var (status, body) = await GetAsync(client, await SignInAsync(client));

        status.ShouldBe(HttpStatusCode.OK);
        body.EnumerateObject().Select(p => p.Name).ShouldBe(["enabled", "lastSucceededAt", "lastRunFailed"], ignoreOrder: true);
        body.GetProperty("lastSucceededAt").GetDateTimeOffset().ShouldBe(succeededAt);
        body.GetProperty("lastRunFailed").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Running_run_is_ignored_and_partial_run_raises_the_warning()
    {
        var now = _fixture.Clock.UtcNow;
        var succeededAt = now.AddMinutes(-40);
        await AddRunAsync(succeededAt.AddMinutes(-1), run => run.Complete(Counts, [], succeededAt));
        await AddRunAsync(now.AddMinutes(-25), run => run.CompletePartially(Counts, [], SyncFailureReason.SourceUnavailable, now.AddMinutes(-24)));
        await AddRunAsync(now.AddMinutes(-1), _ => { });

        using var client = _fixture.CreateClient();
        var (_, body) = await GetAsync(client, await SignInAsync(client));

        // Kismen tamamlanan calisma basarili sayilmaz; suren calisma uyariyi silmez.
        body.GetProperty("lastSucceededAt").GetDateTimeOffset().ShouldBe(succeededAt);
        body.GetProperty("lastRunFailed").GetBoolean().ShouldBeTrue();
    }

    private async Task AddRunAsync(DateTimeOffset startedAt, Action<PersonnelSyncRun> finish) =>
        await _fixture.WithDbAsync(async context =>
        {
            var run = PersonnelSyncRun.Start(SyncTrigger.Scheduled, startedAt);
            finish(run);
            context.Add(run);
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

    private static async Task<(HttpStatusCode Status, JsonElement Body)> GetAsync(HttpClient client, string? token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(Freshness, UriKind.Relative));
        if (token is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone());
    }
}
