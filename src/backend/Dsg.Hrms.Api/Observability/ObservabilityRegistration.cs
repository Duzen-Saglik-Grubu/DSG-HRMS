using Dsg.Hrms.Application.Common.Configuration;
using Dsg.Hrms.Infrastructure.Configuration;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Dsg.Hrms.Api.Observability;

/// <summary>
/// Izleme ve olcum altyapisinin kaydi (ADR-0011).
/// </summary>
/// <remarks>
/// Gozlemlenebilirlik olmadan "sistem yavas" sikâyeti olculemez; hangi istegin hangi
/// adimda bekledigi tahmin edilmeye calisilir. Izler, uygulama gunlugundeki
/// <c>CorrelationId</c> ile ayni izleme kimligini tasir; bir kullanicinin bildirdigi
/// numaradan hem gunluk satirlarina hem ize ulasilir.
/// </remarks>
public static class ObservabilityRegistration
{
    /// <summary>
    /// Npgsql'in urettigi izleme kaynagi. Veritabani cagrilari bu ad altinda gorunur.
    /// </summary>
    private const string NpgsqlActivitySource = "Npgsql";

    /// <summary>Izleme ve olcum toplamayi kaydeder.</summary>
    public static IServiceCollection AddHrmsObservability(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddValidatedOptions<ObservabilityOptions>(
            configuration, ObservabilityOptions.SectionName);

        var options = OptionsRegistration.ReadAndValidate<ObservabilityOptions>(
            configuration, ObservabilityOptions.SectionName);

        if (!options.Enabled)
        {
            return services;
        }

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(options.ServiceName))
            .WithTracing(tracing => ConfigureTracing(tracing, options))
            .WithMetrics(metrics => ConfigureMetrics(metrics, options));

        return services;
    }

    private static void ConfigureTracing(TracerProviderBuilder tracing, ObservabilityOptions options)
    {
        tracing
            .SetSampler(new TraceIdRatioBasedSampler(options.SamplingRatio))
            .AddAspNetCoreInstrumentation(instrumentation =>
            {
                // Saglik kontrolleri izlenmez: saniyede bir gelen ve daima ayni olan
                // bu istekler, izleri doldurup gercek trafigi gorunmez kilardi.
                instrumentation.Filter = context =>
                    !context.Request.Path.StartsWithSegments("/health");
            })
            .AddHttpClientInstrumentation()
            .AddSource(NpgsqlActivitySource)

            // KVKK: kisisel veri tasiyabilecek etiketler izden silinir (ADR-0009 §4).
            // Islemci disa aktarimdan ONCE calisir; silinen etiket hicbir hedefe gitmez.
            .AddProcessor<PersonalDataScrubbingProcessor>();

        // Toplayici adresi tanimsizken veriler uretilir ama gonderilmez. Bu, toplayici
        // kurulmadan once de uygulamanin calismasini saglar; toplayici devreye
        // alindiginda tek ayarla akmaya baslar.
        if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
        {
            tracing.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(options.OtlpEndpoint));
        }
    }

    private static void ConfigureMetrics(MeterProviderBuilder metrics, ObservabilityOptions options)
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation();

        if (!string.IsNullOrWhiteSpace(options.OtlpEndpoint))
        {
            metrics.AddOtlpExporter(exporter => exporter.Endpoint = new Uri(options.OtlpEndpoint));
        }
    }
}
