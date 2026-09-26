using System.Text.Json;
using Dsg.Hrms.Application.Common.Configuration;
using Dsg.Hrms.Application.Notifications;
using Dsg.Hrms.Domain.Notifications;
using Dsg.Hrms.Infrastructure.Configuration;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Dsg.Hrms.Api.Health;

/// <summary>
/// Saglik kontrollerinin kaydi ve uc noktalari (ADR-0011).
/// </summary>
/// <remarks>
/// <para>
/// <b>Iki ucun ayrimi kritiktir.</b> <c>/health/live</c> yalnizca uygulamanin ayakta
/// olup olmadigini soyler ve <b>hicbir bagimliligi yoklamaz</b>; <c>/health/ready</c>
/// ise trafik almaya hazir olup olmadigini soyler ve bagimliliklari yoklar.
/// </para>
/// <para>
/// Canlilik ucunun veritabanini yoklamasi yaygin bir hatadir: veritabani birkac
/// saniye erisilemez oldugunda kapsayici duzeni uygulamayi <b>yeniden baslatir</b>.
/// Yeniden baslatma veritabanini duzeltmez; yalnizca uygulamayi da kaybettirir ve
/// sorunu buyutur.
/// </para>
/// </remarks>
public static class HealthCheckRegistration
{
    /// <summary>Bagimlilik yoklayan kontrollerin etiketi.</summary>
    public const string ReadyTag = "ready";

    /// <summary>LOGO senkronizasyonu sagligi; hazir olma denetiminden AYRIDIR (SYG-KMLK-011).</summary>
    public const string SyncTag = "sync";

    /// <summary>E-posta ve SMS kanallarinin sagligi; hazir olma denetiminden AYRIDIR (#85).</summary>
    public const string NotificationsTag = "notifications";

    /// <summary>Saglik kontrollerini kaydeder.</summary>
    public static IServiceCollection AddHrmsHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var database = OptionsRegistration.ReadAndValidate<DatabaseOptions>(
            configuration, DatabaseOptions.SectionName);

        services.AddHealthChecks()
            .AddNpgSql(
                connectionString: database.Hrms,
                name: "postgresql",
                failureStatus: HealthStatus.Unhealthy,
                tags: [ReadyTag],
                timeout: TimeSpan.FromSeconds(5))
            .AddCheck<PersonnelSyncHealthCheck>("personnel-sync", tags: [SyncTag])
            .Add(ChannelCheck("email", NotificationChannel.Email))
            .Add(ChannelCheck("sms", NotificationChannel.Sms));

        return services;
    }

    /// <summary>Saglik uc noktalarini yayimlar.</summary>
    public static WebApplication MapHrmsHealthChecks(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // Canlilik: uygulama ayakta mi? Bagimlilik YOKLANMAZ.
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = WriteResponseAsync,
        });

        // Hazir olma: bagimliliklar erisilebilir mi?
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadyTag),
            ResponseWriter = WriteResponseAsync,
        });

        // Senkronizasyon: LOGO senkronizasyonunun son durumu (SYG-KMLK-010).
        app.MapHealthChecks("/health/sync", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(SyncTag),
            ResponseWriter = WriteResponseAsync,
        });

        // Bildirim kanallari: SMTP kimlik dogrulamasi ve NetGSM bakiye/baslik sorgusu.
        // Hicbir ileti gonderilmez.
        app.MapHealthChecks("/health/notifications", new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(NotificationsTag),
            ResponseWriter = WriteResponseAsync,
        });

        return app;
    }

    private static Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckRegistration ChannelCheck(string name, NotificationChannel channel) =>
        new(
            name,
            provider => new NotificationChannelHealthCheck(provider.GetRequiredService<INotificationChannelProbe>(), channel),
            failureStatus: null,
            tags: [NotificationsTag],
            timeout: TimeSpan.FromSeconds(30));

    /// <summary>
    /// Saglik yanitini JSON olarak yazar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Yanit yalnizca <b>kontrol adi, durumu ve suresi</b> tasir. Istisna mesaji ve
    /// kontrol aciklamasi <b>bilerek yazilmaz</b>: bir veritabani baglanti hatasi,
    /// sunucu adini, kullanici adini ve bazen baglanti dizesinin tamamini icerir.
    /// Bu uc kimlik dogrulamasi istemez (kapsayici duzeni onu boyle yoklar), yani
    /// yazilan her sey ag icindeki herkese aciktir (KR-062 ile ayni ilke).
    /// </para>
    /// <para>
    /// Ayrinti kaybolmaz: hata, uygulama gunlugune yazilir ve oradan okunur.
    /// </para>
    /// </remarks>
    private static async Task WriteResponseAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 1),
            }),
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}
