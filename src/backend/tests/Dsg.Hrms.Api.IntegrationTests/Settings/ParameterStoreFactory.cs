using System.Security.Cryptography;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Infrastructure.Data;
using Dsg.Hrms.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dsg.Hrms.Api.IntegrationTests.Settings;

/// <summary>Gercek PostgreSQL uzerinde calisan parametre deposu kurar.</summary>
public static class ParameterStoreFactory
{
    /// <summary>Her cagrida yeni, rastgele bir test anahtari (Base64).</summary>
    public static string NewKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(SecretProtectionOptions.KeySizeBytes));

    /// <summary>Depoyu ve kapsam saglayicisini olusturur.</summary>
    public static (SystemParameters Parameters, ServiceProvider Provider) Create(
        DbContextOptions<HrmsDbContext> options,
        IDateTimeProvider clock,
        IDictionary<string, string?>? configuration = null,
        string? key = null)
    {
        var services = new ServiceCollection();
        services.AddScoped(_ => new HrmsDbContext(options));
        var provider = services.BuildServiceProvider();

        var parameters = new SystemParameters(
            provider.GetRequiredService<IServiceScopeFactory>(),
            new ConfigurationBuilder().AddInMemoryCollection(configuration ?? new Dictionary<string, string?>()).Build(),
            new AesGcmSecretProtector(new SecretProtectionOptions { Key = key }),
            clock,
            NullLogger<SystemParameters>.Instance);

        return (parameters, provider);
    }
}
