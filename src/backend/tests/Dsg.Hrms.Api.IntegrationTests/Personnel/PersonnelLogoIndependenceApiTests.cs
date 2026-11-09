using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dsg.Hrms.Api.IntegrationTests.Identity;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Personnel;

/// <summary>
/// LOGO'ya erisilemeyen sunucu: LOGO kaynagi her cagrida <see cref="LogoUnavailableException"/>
/// firlatir. Kaynak tek ornektir; testler ona yapilan cagrilari sayar.
/// </summary>
public sealed class UnavailableLogoApiFixture : RegistrationApiFixture
{
    /// <summary>Her cagrida "LOGO'ya erisilemedi" hatasi veren kaynak.</summary>
    public ILogoPersonnelSource Source { get; } = CreateUnavailableSource();

    /// <inheritdoc />
    protected override void ConfigureHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<ILogoPersonnelSource>();
            services.AddSingleton(Source);
        });
    }

    private static ILogoPersonnelSource CreateUnavailableSource()
    {
        var source = Substitute.For<ILogoPersonnelSource>();
        source.VerifyReadOnlyAccessAsync(default).ThrowsAsyncForAnyArgs(new LogoUnavailableException());
        source.VerifySchemaAsync(default).ThrowsAsyncForAnyArgs(new LogoUnavailableException());
        source.GetAllAsync(default).ThrowsAsyncForAnyArgs(new LogoUnavailableException());
        source.GetDetailsAsync(default).ThrowsAsyncForAnyArgs(new LogoUnavailableException());
        source.GetPhotoDigestsAsync(default).ThrowsAsyncForAnyArgs(new LogoUnavailableException());
        source.GetPhotosAsync(default!, default).ThrowsAsyncForAnyArgs(new LogoUnavailableException());
        return source;
    }
}

/// <summary>
/// Personel ekranlari LOGO'dan bagimsizdir (SYG-PERS-001; PG-PERS-01). Veriler SENTETIKTIR.
/// </summary>
/// <remarks>
/// Uclar yalnizca HRMS veritabanindan okur; istek sirasinda LOGO'ya baglanilmaz. LOGO erisilemezken
/// liste, kart ve Profilim son basarili senkronizasyonun verisiyle (burada tohum veri) calisir.
/// Kisi 1 oturum acan kullanicidir; testte IK rolu verilir.
/// </remarks>
[Collection(ApiHostGroup.Name)]
public sealed class PersonnelLogoIndependenceApiTests : IClassFixture<UnavailableLogoApiFixture>, IAsyncLifetime
{
    private const string Persons = "/api/v1/personnel/persons";
    private const string Password = "Kediler uyur 7";
    private const string UserEmail = "ahmet.yilmaz@duzen.com.tr";

    private readonly UnavailableLogoApiFixture _fixture;

    public PersonnelLogoIndependenceApiTests(UnavailableLogoApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await _fixture.WithServicesAsync(async services =>
        {
            var context = services.GetRequiredService<HrmsDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher>();
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(1));
            context.Add(UserAccount.Register(person.Id, hasher.Hash(PasswordPolicy.Normalize(Password)), _fixture.Clock.UtcNow));
            await context.SaveChangesAsync();

            var account = await context.Set<UserAccount>().SingleAsync(a => a.PersonId == person.Id);
            var hr = await context.Set<Role>().SingleAsync(r => r.Code == Role.HrIdentityOperationsCode);
            context.Add(UserRole.Assign(account.Id, hr.Id));
            return await context.SaveChangesAsync();
        });
        _fixture.Source.ClearReceivedCalls();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task List_card_and_own_profile_work_while_logo_is_unreachable_and_never_call_logo()
    {
        using var client = _fixture.CreateClient();
        var token = await SignInAsync(client);
        var other = await _fixture.WithDbAsync(context =>
            context.Set<Person>().Where(p => p.NationalId == NationalId(2)).Select(p => p.PublicId).SingleAsync());

        var (listStatus, list) = await GetAsync(client, Persons, token);
        var (cardStatus, card) = await GetAsync(client, $"{Persons}/{other}", token);
        var (ownStatus, own) = await GetAsync(client, $"{Persons}/me", token);
        var (optionsStatus, _) = await GetAsync(client, $"{Persons}/filter-options", token);

        listStatus.ShouldBe(HttpStatusCode.OK);
        list.GetProperty("totalCount").GetInt32().ShouldBeGreaterThan(0);
        cardStatus.ShouldBe(HttpStatusCode.OK);
        card.GetProperty("nationalId").GetString().ShouldBe(NationalId(2));
        ownStatus.ShouldBe(HttpStatusCode.OK);
        own.GetProperty("nationalId").GetString().ShouldBe(NationalId(1));
        optionsStatus.ShouldBe(HttpStatusCode.OK);

        // Hicbir istek LOGO kaynagina gitmedi.
        _fixture.Source.ReceivedCalls().ShouldBeEmpty();
    }

    [Fact]
    public async Task The_host_uses_the_unreachable_logo_source()
    {
        // Pozitif kontrol: sunucu gercekten bu kaynagi kullaniyor ve kaynak hata veriyor. Kaynak
        // sunucuya baglanmasaydi "LOGO'ya cagri yok" iddiasi hicbir seyi denetlemeden gecerdi.
        var resolved = await _fixture.WithServicesAsync(services => Task.FromResult(services.GetRequiredService<ILogoPersonnelSource>()));
        resolved.ShouldBeSameAs(_fixture.Source);
        await Should.ThrowAsync<LogoUnavailableException>(() => resolved.GetAllAsync(CancellationToken.None));
    }

    private static async Task<string> SignInAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(new Uri("/api/v1/identity/sessions", UriKind.Relative), new { email = UserEmail, password = Password });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        return body.GetProperty("session").GetProperty("accessToken").GetString()!;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> GetAsync(HttpClient client, string path, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone());
    }
}
