using Dsg.Hrms.Application.Common.Configuration;
using Dsg.Hrms.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Dsg.Hrms.Infrastructure.Logging;

/// <summary>
/// Uygulama gunlugunun tek yapilandirma noktasi (ADR-0009 §1).
/// </summary>
/// <remarks>
/// <para>
/// Gunluk kayitlari <b>yapisal</b> (JSON) yazilir. Duz metin bir satiri sonradan
/// ayristirmak yerine, alanlar dogrudan sorgulanabilir olur: "hangi kullanici,
/// hangi istekte, hangi hatayi aldi" sorusu tek sorguyla cevaplanir.
/// </para>
/// <para>
/// Maskeleme ilkesi burada, <b>tek yerde</b> baglanir. Yeni bir hedef (sink)
/// eklendiginde maskelemenin ayrica hatirlanmasi gerekmez.
/// </para>
/// </remarks>
public static class SerilogConfiguration
{
    private const int MegabyteInBytes = 1024 * 1024;

    /// <summary>
    /// Gunluk yapilandirmasini uygular.
    /// </summary>
    /// <param name="logger">Yapilandirilacak Serilog yapilandirmasi.</param>
    /// <param name="configuration">Uygulama yapilandirmasi.</param>
    /// <param name="environment">Calisma ortami.</param>
    public static void Configure(
        LoggerConfiguration logger,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var options = OptionsRegistration.ReadAndValidate<LoggingOptions>(
            configuration, LoggingOptions.SectionName);

        logger
            .MinimumLevel.Is(ParseLevel(options.MinimumLevel))

            // Cerceve gurultusu kisilir: ASP.NET Core'un her istek icin urettigi
            // bilgilendirme kayitlari, kendi istek kaydimizin (§2) yaninda tekrar olur.
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)

            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "Dsg.Hrms")
            .Enrich.WithProperty("Environment", environment.EnvironmentName)

            // KVKK denetimi: kisisel veri gunluge maskeli duser (ADR-0009 §4).
            .Destructure.With(new MaskingDestructuringPolicy())

            // Nesne ayristirma sinirlari: dongusel veya cok derin bir nesne
            // gunlugu sisiremesin.
            .Destructure.ToMaximumDepth(6)
            .Destructure.ToMaximumStringLength(2048)
            .Destructure.ToMaximumCollectionCount(64);

        if (options.ConsoleEnabled)
        {
            // Konsol: kapsayici (Docker) gunluklerinin kaynagi.
            logger.WriteTo.Console(new JsonFormatter(renderMessage: true));
        }

        logger.WriteTo.File(
            new JsonFormatter(renderMessage: true),
            options.FilePath,
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: options.RetainedFileCountLimit,
            fileSizeLimitBytes: (long)options.FileSizeLimitMegabytes * MegabyteInBytes,
            rollOnFileSizeLimit: true,
            shared: true);
    }

    /// <summary>
    /// Onyukleme (bootstrap) gunlugu yapilandirmasi.
    /// </summary>
    /// <remarks>
    /// Yapilandirma okunmadan once olusan hatalarin — eksik baglanti dizesi,
    /// bozuk ayar dosyasi — kaybolmamasi icindir. Bu asamada maskeleme ilkesi de
    /// baglidir: acilis hatalari da kisisel veri sizdirmamalidir.
    /// </remarks>
    public static LoggerConfiguration CreateBootstrapConfiguration() =>
        new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .Destructure.With(new MaskingDestructuringPolicy())
            .WriteTo.Console(new JsonFormatter(renderMessage: true));

    private static LogEventLevel ParseLevel(string value) =>
        Enum.Parse<LogEventLevel>(value, ignoreCase: true);
}
