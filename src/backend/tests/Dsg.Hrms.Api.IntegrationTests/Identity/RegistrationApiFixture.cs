using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Notifications;
using Dsg.Hrms.Domain.Organization;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace Dsg.Hrms.Api.IntegrationTests.Identity;

/// <summary>
/// Uyelik API testleri icin gercek HTTP boru hatti ve gercek PostgreSQL.
/// </summary>
/// <remarks>
/// Gonderim kuyrugu yakalayici bir sahteyle degistirilir: kodlar test icinde okunur,
/// hicbir ileti gonderilmez. Saat test tarafindan ilerletilir. Veriler SENTETIKTIR (KVKK).
/// </remarks>
public class RegistrationApiFixture : IAsyncLifetime
{
    /// <summary>Test istemcisinin ters vekil arkasindaki adresi (Docker koprusu).</summary>
    public static readonly IPAddress ProxyAddress = IPAddress.Parse("172.18.0.5");

    public static readonly DateOnly Today = new(2026, 9, 27);

    /// <summary>Ilk sistem yoneticisi olarak yapilandirilan kisinin e-postasi (sentetik kisi 2).</summary>
    public const string BootstrapAdministratorEmail = "Mehmet.Kaya@duzen.com.tr";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("dsg_hrms_registration_test")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;

    public CapturingDispatch Messages { get; } = new();

    public TestClock Clock { get; } = new();

    /// <summary>Test basina ayarlanan en kisa yanit suresi.</summary>
    public string MinimumResponseTime { get; set; } = "00:00:00";

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _factory = CreateFactory();

        await using var scope = _factory.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<HrmsDbContext>();
        await context.Database.MigrateAsync();
        await SeedAsync(context);
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _container.DisposeAsync();
    }

    public HttpClient CreateClient(string? forwardedFor = null)
    {
        // Cerezler test icinde elle yonetilir: yenileme jetonu cerezi "Secure" isaretlidir ve
        // eski bir cerezi bilerek tekrar gondermek gerekir (SYG-KMLK-040).
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = false,
            BaseAddress = new Uri("https://localhost"),
        });
        if (forwardedFor is not null)
        {
            client.DefaultRequestHeaders.Add("X-Forwarded-For", forwardedFor);
        }

        return client;
    }

    /// <summary>Uygulamanin bir kapsamda servisini kullanir.</summary>
    public async Task<T> WithServicesAsync<T>(Func<IServiceProvider, Task<T>> action)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider);
    }

    public async Task<T> WithDbAsync<T>(Func<HrmsDbContext, Task<T>> action)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<HrmsDbContext>());
    }

    /// <summary>Denemeler, kod istekleri ve hesaplar silinir; kisiler kalir. Saat ilerletilir.</summary>
    public async Task ResetAsync()
    {
        Messages.Clear();
        Clock.Advance(TimeSpan.FromHours(3));
        await WithDbAsync(async context =>
        {
            await context.Database.ExecuteSqlRawAsync(
                "DELETE FROM identity.refresh_token; DELETE FROM identity.user_session; DELETE FROM identity.login_challenge; DELETE FROM identity.login_throttle; " +
                "DELETE FROM identity.registration_code_request; DELETE FROM identity.registration_attempt; DELETE FROM identity.verification_code; DELETE FROM identity.user_role; DELETE FROM identity.user_account; DELETE FROM identity.account_invitation; " +
                "DELETE FROM settings.system_parameter; DELETE FROM settings.brand_logo;");
            return 0;
        });

        // Parametre onbellegi temizlenir: onceki testin ayarladigi deger tasinmasin.
        _factory.Services.GetRequiredService<Dsg.Hrms.Infrastructure.Settings.SystemParameters>().Invalidate();
    }

    private WebApplicationFactory<Program> CreateFactory()
    {
        var factory = new WebApplicationFactory<Program>();
        using (factory)
        {
            return factory.WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(Environments.Production);
                builder.UseSetting("Database:Hrms", _container.GetConnectionString());
                builder.UseSetting("ApplicationLogging:FilePath", "logs/test-.json");
                builder.UseSetting("Identity:CodeHashKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
                builder.UseSetting("Identity:JwtSigningKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
                builder.UseSetting("ReverseProxy:TrustedNetworks", "172.16.0.0/12");
                builder.UseSetting("AccessControl:BootstrapAdministrators", BootstrapAdministratorEmail);
                builder.UseSetting("ParameterProtection:Key", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));

                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<INotificationDispatch>();
                    services.AddSingleton<INotificationDispatch>(Messages);
                    services.RemoveAll<IDateTimeProvider>();
                    services.AddSingleton<IDateTimeProvider>(Clock);
                    services.AddSingleton<IStartupFilter>(new ProxyAddressStartupFilter());
                    services.PostConfigure<Api.Identity.RegistrationTimingOptions>(options =>
                        options.MinimumResponseTime = TimeSpan.Parse(MinimumResponseTime, CultureInfo.InvariantCulture));
                });
            });
        }
    }

    private static async Task SeedAsync(HrmsDbContext context)
    {
        var company = Company.FromLogo(1, "Duzen Laboratuvarlar");
        context.Add(company);

        void Add(int index, string registryCode, string? email, string? phone, bool shared = false, DateOnly? terminated = null)
        {
            var person = Person.Create(NationalId(index), new PersonDetails("Ahmet", "Yilmaz", BirthDate, email, shared, phone));
            context.Add(person);
            context.Add(Employment.Create(person, company, registryCode, new EmploymentDetails(new DateOnly(2020, 1, 1), terminated, index), Today));
        }

        Add(1, "00001", "ahmet.yilmaz@duzen.com.tr", "5321234567");
        Add(2, "00002", "mehmet.kaya@duzen.com.tr", phone: null);
        Add(3, "00003", "ortak@duzen.com.tr", "5321234568", shared: true);
        Add(4, "00004", "ayrilan@duzen.com.tr", "5321234569", terminated: Today.AddDays(-1));
        Add(5, "00005", "kisisel@gmail.com", "5321234570");

        await context.SaveChangesAsync();
    }

    public static readonly DateOnly BirthDate = new(1985, 4, 12);

    /// <summary>Sirali numarayla gecerli bir TCKN uretir.</summary>
    public static string NationalId(int index)
    {
        var firstNine = (100000000 + index).ToString(CultureInfo.InvariantCulture);
        var d = firstNine.Select(c => c - '0').ToArray();
        var tenth = ((((d[0] + d[2] + d[4] + d[6] + d[8]) * 7) - (d[1] + d[3] + d[5] + d[7])) % 10 + 10) % 10;
        var eleventh = (d.Sum() + tenth) % 10;
        return $"{firstNine}{tenth}{eleventh}";
    }

    /// <summary>Yakalayici gonderim kuyrugu.</summary>
    public sealed class CapturingDispatch : INotificationDispatch
    {
        private readonly List<OutboundMessage> _messages = [];

        public IReadOnlyList<OutboundMessage> All
        {
            get
            {
                lock (_messages)
                {
                    return [.. _messages];
                }
            }
        }

        public bool TryEnqueue(OutboundMessage message)
        {
            lock (_messages)
            {
                _messages.Add(message);
            }

            return true;
        }

        public void Clear()
        {
            lock (_messages)
            {
                _messages.Clear();
            }
        }

        /// <summary>Son iletideki kodu okur.</summary>
        public string LastCode() => All[^1].MessageBody.Split(": ")[1][..6];
    }

    /// <summary>Test tarafindan ilerletilen saat.</summary>
    public sealed class TestClock : IDateTimeProvider
    {
        private DateTimeOffset _now = new(2026, 9, 27, 9, 0, 0, TimeSpan.Zero);

        public DateTimeOffset UtcNow => _now;

        public DateOnly Today => DateOnly.FromDateTime(_now.UtcDateTime);

        public void Advance(TimeSpan by) => _now += by;
    }

    /// <summary>Istegin ters vekilden (nginx) geliyormus gibi gorunmesini saglar.</summary>
    private sealed class ProxyAddressStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = ProxyAddress;
                return nextMiddleware();
            });
            next(app);
        };
    }
}

/// <summary>Yanit yardimcilari.</summary>
public static class HttpTestExtensions
{
    public static async Task<(HttpStatusCode Status, JsonElement Body)> PostJsonAsync(this HttpClient client, string path, object body)
    {
        using var response = await client.PostAsJsonAsync(new Uri(path, UriKind.Relative), body);
        var text = await response.Content.ReadAsStringAsync();
        var json = text.Length == 0 ? default : JsonDocument.Parse(text).RootElement.Clone();
        return (response.StatusCode, json);
    }
}
