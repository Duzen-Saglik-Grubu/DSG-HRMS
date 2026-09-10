using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Dsg.Hrms.Infrastructure.Configuration;

/// <summary>
/// Yapilandirma secenek siniflarinin kaydi ve ACILISTA dogrulanmasi (ADR-0008 §4).
/// </summary>
public static class OptionsRegistration
{
    /// <summary>
    /// Bir secenek sinifini yapilandirmaya baglar ve <b>uygulama acilirken</b> dogrular.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>ValidateOnStart</c> bilincli bir tercihtir: zorunlu bir ayar eksikse uygulama
    /// <b>hic acilmaz</b>. Alternatifi, ayarin ilk kullanildigi anda — belki gunler
    /// sonra, belki bir kullanici isleminin ortasinda — hata vermesidir.
    /// </para>
    /// <para>
    /// Yari calisan bir sistemle uretime cikmak, hic acilmamaktan daha kotudur:
    /// hangi islevin calistigi belirsiz olur.
    /// </para>
    /// </remarks>
    public static IServiceCollection AddValidatedOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class
    {
        services
            .AddOptions<TOptions>()
            .Bind(configuration.GetSection(sectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Kayitli bir secenek nesnesini kurulum sirasinda okur ve dogrular.
    /// </summary>
    /// <remarks>
    /// Bagimlilik kaydi asamasinda (ornegin <c>DbContext</c> kurulurken) secenek
    /// degerine ihtiyac duyulur; o noktada henuz servis saglayici hazir degildir.
    /// Bu yardimci, ayni dogrulamayi erkenden uygular.
    /// </remarks>
    public static TOptions ReadAndValidate<TOptions>(
        IConfiguration configuration,
        string sectionName)
        where TOptions : class, new()
    {
        var options = configuration.GetSection(sectionName).Get<TOptions>() ?? new TOptions();

        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(options, context, results, validateAllProperties: true))
        {
            var errors = string.Join(
                Environment.NewLine,
                results.Select(r => $"  - {r.ErrorMessage}"));

            throw new OptionsValidationException(
                sectionName,
                typeof(TOptions),
                [$"'{sectionName}' yapilandirmasi gecersiz:{Environment.NewLine}{errors}"]);
        }

        return options;
    }

    /// <summary>
    /// Uretim ortaminda acik olmamasi gereken ayarlari denetler.
    /// </summary>
    /// <remarks>
    /// Ayrintili veritabani gunlugu, sorgu parametrelerini — dolayisiyla kisisel
    /// veriyi — gunluge yazar (ADR-0009 §4). Uretimde yanlislikla acik birakilmasi
    /// bir KVKK ihlali uretir; bu nedenle acilis engellenir.
    /// </remarks>
    public static void EnsureProductionSafety(IHostEnvironment environment, bool detailedLoggingEnabled)
    {
        ArgumentNullException.ThrowIfNull(environment);

        if (!environment.IsDevelopment() && detailedLoggingEnabled)
        {
            throw new InvalidOperationException(
                "'Database:DetailedLoggingEnabled' yalnizca gelistirme ortaminda acilabilir. " +
                "Bu ayar sorgu parametrelerini gunluge yazar ve kisisel verinin duz metin " +
                "olarak diske dusmesine yol acar (ADR-0009 §4).");
        }
    }
}
