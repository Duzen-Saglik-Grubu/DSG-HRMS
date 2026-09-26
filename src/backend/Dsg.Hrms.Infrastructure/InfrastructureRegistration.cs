using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Configuration;
using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Infrastructure.Audit;
using Dsg.Hrms.Infrastructure.Configuration;
using Dsg.Hrms.Infrastructure.Data;
using Dsg.Hrms.Infrastructure.Data.Interceptors;
using Dsg.Hrms.Infrastructure.Logo;
using Dsg.Hrms.Infrastructure.Personnel;
using Dsg.Hrms.Infrastructure.Time;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

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
        services.AddScoped<IAccessLogger, AccessLogger>();

        AddDatabase(services, configuration, environment);
        AddPersonnelSync(services, configuration);

        return services;
    }

    private static void AddPersonnelSync(IServiceCollection services, IConfiguration configuration)
    {
        services.AddValidatedOptions<PersonnelSyncOptions>(configuration, PersonnelSyncOptions.SectionName);

        // Uygulama katmani IOptions paketine bagimli degildir; secenek nesnesi dogrudan verilir.
        services.AddSingleton(provider => provider.GetRequiredService<IOptions<PersonnelSyncOptions>>().Value);

        services.AddScoped<IPersonnelSyncStore, PersonnelSyncStore>();
        services.AddScoped<IPersonnelSyncStatus, PersonnelSyncStatus>();
        services.AddScoped<PersonnelSyncService>();

        AddLogo(services, configuration);

        services.AddHostedService<PersonnelSyncWorker>();
    }

    private static void AddLogo(IServiceCollection services, IConfiguration configuration)
    {
        var logo = OptionsRegistration.ReadAndValidate<LogoOptions>(configuration, LogoOptions.SectionName);
        services.AddSingleton(logo);

        // LOGO erisimi YALNIZCA bu baglam ve LogoPersonnelSource uzerinden yapilir
        // (ADR-0003 §2). Mimari testi, Logo klasoru disinda SQL Server bagimliligi
        // olmadigini denetler.
        services.AddDbContext<LogoDbContext>(builder =>
        {
            void ConfigureSqlServer(Microsoft.EntityFrameworkCore.Infrastructure.SqlServerDbContextOptionsBuilder sql)
            {
                sql.CommandTimeout(logo.CommandTimeoutSeconds);

                // Yalnizca okuma yapildigi icin gecici hatalarda yeniden deneme guvenlidir.
                sql.EnableRetryOnFailure(3);
            }

            // Baglanti tanimli degilse baglam kurulur ama KULLANILMAZ: zamanlayici
            // calismaz. Bos dizeyle kurmak yerine dizesiz kurulur ki yanlislikla
            // kullanilirsa anlasilir bir hata versin.
            if (logo.IsConfigured)
            {
                builder.UseSqlServer(logo.ConnectionString, ConfigureSqlServer);
            }
            else
            {
                builder.UseSqlServer(ConfigureSqlServer);
            }
        });

        services.AddScoped<ILogoPersonnelSource, LogoPersonnelSource>();
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

        void Configure(IServiceProvider provider, DbContextOptionsBuilder builder)
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
        }

        services.AddDbContext<HrmsDbContext>(Configure);

        // Erisim kaydi, cagiranin baglamindan AYRI bir baglamla yazilir (ADR-0009 §3).
        // Kapsam (scoped) omurlu fabrika secilmistir: ara katmanlar kapsam omurlu
        // servislerdir ve tekil (singleton) bir fabrika onlari cozemezdi.
        services.AddDbContextFactory<HrmsDbContext>(Configure, ServiceLifetime.Scoped);
    }
}
