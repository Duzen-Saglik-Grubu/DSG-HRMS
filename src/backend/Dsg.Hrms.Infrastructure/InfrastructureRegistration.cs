using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Configuration;
using Dsg.Hrms.Infrastructure.Configuration;
using Dsg.Hrms.Infrastructure.Data;
using Dsg.Hrms.Infrastructure.Data.Interceptors;
using Dsg.Hrms.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dsg.Hrms.Infrastructure;

/// <summary>
/// Altyapi katmaninin bagimlilik kaydi.
/// </summary>
/// <remarks>
/// Api katmani Infrastructure'a YALNIZCA bu nokta uzerinden dokunur (ADR-0002).
/// </remarks>
public static class InfrastructureRegistration
{
    /// <summary>Altyapi servislerini kaydeder.</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddValidatedOptions<DatabaseOptions>(configuration, DatabaseOptions.SectionName);

        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddScoped<AuditFieldsInterceptor>();
        services.AddScoped<AuditTrailInterceptor>();

        AddDatabase(services, configuration, environment);

        return services;
    }

    private static void AddDatabase(
        IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Secenekler burada ERKENDEN okunur ve dogrulanir: baglanti dizesi eksikse
        // uygulama servis saglayici kurulmadan once, anlasilir bir mesajla durur.
        var options = OptionsRegistration.ReadAndValidate<DatabaseOptions>(
            configuration, DatabaseOptions.SectionName);

        OptionsRegistration.EnsureProductionSafety(environment, options.DetailedLoggingEnabled);

        services.AddDbContext<HrmsDbContext>((provider, builder) =>
        {
            builder.UseNpgsql(options.Hrms, npgsql =>
            {
                npgsql.CommandTimeout(options.CommandTimeoutSeconds);
                npgsql.EnableRetryOnFailure(options.RetryCount);
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "public");
            });

            // Tablo, kolon ve kisit adlari snake_case'e donusturulur (ADR-0004 §1).
            // Ad eslemesi elle yazilmaz; C# tarafinda PascalCase kullanilir.
            builder.UseSnakeCaseNamingConvention();

            // SIRA ONEMLIDIR: denetim alanlari once doldurulur ve yumusak silme
            // donusumu once yapilir; denetim izi bu son durumu kaydeder.
            builder.AddInterceptors(
                provider.GetRequiredService<AuditFieldsInterceptor>(),
                provider.GetRequiredService<AuditTrailInterceptor>());

            if (options.DetailedLoggingEnabled)
            {
                // EnsureProductionSafety bu noktaya yalnizca gelistirme ortaminda izin verir.
                builder.EnableDetailedErrors();
                builder.EnableSensitiveDataLogging();
            }
        });
    }
}
