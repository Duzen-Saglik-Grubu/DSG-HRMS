using Dsg.Hrms.Application.Ortak.Soyutlamalar;
using Dsg.Hrms.Application.Ortak.Yapilandirma;
using Dsg.Hrms.Infrastructure.Veri;
using Dsg.Hrms.Infrastructure.Veri.Interceptor;
using Dsg.Hrms.Infrastructure.Yapilandirma;
using Dsg.Hrms.Infrastructure.Zaman;
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
public static class AltyapiKaydi
{
    /// <summary>Altyapi servislerini kaydeder.</summary>
    public static IServiceCollection AltyapiEkle(
        this IServiceCollection servisler,
        IConfiguration yapilandirma,
        IHostEnvironment ortam)
    {
        ArgumentNullException.ThrowIfNull(servisler);
        ArgumentNullException.ThrowIfNull(yapilandirma);
        ArgumentNullException.ThrowIfNull(ortam);

        servisler.SecenekEkle<VeritabaniSecenekleri>(yapilandirma, VeritabaniSecenekleri.BolumAdi);

        servisler.AddSingleton<IZamanSaglayici, SistemZamanSaglayici>();
        servisler.AddScoped<DenetimAlanlariInterceptor>();

        VeritabaniEkle(servisler, yapilandirma, ortam);

        return servisler;
    }

    private static void VeritabaniEkle(
        IServiceCollection servisler,
        IConfiguration yapilandirma,
        IHostEnvironment ortam)
    {
        // Secenekler burada ERKENDEN okunur ve dogrulanir: baglanti dizesi eksikse
        // uygulama servis saglayici kurulmadan once, anlasilir bir mesajla durur.
        var secenekler = SeceneklerKaydi.SecenekOku<VeritabaniSecenekleri>(
            yapilandirma, VeritabaniSecenekleri.BolumAdi);

        SeceneklerKaydi.UretimGuvenligiDenetle(ortam, secenekler.AyrintiliGunlukAcik);

        servisler.AddDbContext<HrmsDbContext>((saglayici, kurucu) =>
        {
            kurucu.UseNpgsql(secenekler.Hrms, npgsql =>
            {
                npgsql.CommandTimeout(secenekler.KomutZamanAsimiSaniye);
                npgsql.EnableRetryOnFailure(secenekler.YenidenDenemeSayisi);
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "public");
            });

            // Tablo, kolon ve kisit adlari snake_case'e donusturulur (ADR-0004 §1).
            // Ad eslemesi elle yazilmaz; C# tarafinda PascalCase kullanilir.
            kurucu.UseSnakeCaseNamingConvention();

            kurucu.AddInterceptors(saglayici.GetRequiredService<DenetimAlanlariInterceptor>());

            if (secenekler.AyrintiliGunlukAcik)
            {
                // UretimGuvenligiDenetle bu noktaya yalnizca gelistirme ortaminda izin verir.
                kurucu.EnableDetailedErrors();
                kurucu.EnableSensitiveDataLogging();
            }
        });
    }
}
