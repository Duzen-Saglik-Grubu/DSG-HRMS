using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Dsg.Hrms.Infrastructure.Yapilandirma;

/// <summary>
/// Yapilandirma secenek siniflarinin kaydi ve ACILISTA dogrulanmasi (ADR-0008 §4).
/// </summary>
public static class SeceneklerKaydi
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
    public static IServiceCollection SecenekEkle<TSecenek>(
        this IServiceCollection servisler,
        IConfiguration yapilandirma,
        string bolumAdi)
        where TSecenek : class
    {
        servisler
            .AddOptions<TSecenek>()
            .Bind(yapilandirma.GetSection(bolumAdi))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return servisler;
    }

    /// <summary>
    /// Kayitli bir secenek nesnesini kurulum sirasinda okur ve dogrular.
    /// </summary>
    /// <remarks>
    /// Bagimlilik kaydi asamasinda (ornegin <c>DbContext</c> kurulurken) secenek
    /// degerine ihtiyac duyulur; o noktada henuz servis saglayici hazir degildir.
    /// Bu yardimci, ayni dogrulamayi erkenden uygular.
    /// </remarks>
    public static TSecenek SecenekOku<TSecenek>(
        IConfiguration yapilandirma,
        string bolumAdi)
        where TSecenek : class, new()
    {
        var secenek = yapilandirma.GetSection(bolumAdi).Get<TSecenek>() ?? new TSecenek();

        var baglam = new System.ComponentModel.DataAnnotations.ValidationContext(secenek);
        var sonuclar = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        if (!System.ComponentModel.DataAnnotations.Validator.TryValidateObject(
                secenek, baglam, sonuclar, validateAllProperties: true))
        {
            var hatalar = string.Join(
                Environment.NewLine,
                sonuclar.Select(s => $"  - {s.ErrorMessage}"));

            throw new OptionsValidationException(
                bolumAdi,
                typeof(TSecenek),
                [$"'{bolumAdi}' yapilandirmasi gecersiz:{Environment.NewLine}{hatalar}"]);
        }

        return secenek;
    }

    /// <summary>
    /// Uretim ortaminda acik olmamasi gereken ayarlari denetler.
    /// </summary>
    /// <remarks>
    /// Ayrintili veritabani gunlugu, sorgu parametrelerini — dolayisiyla kisisel
    /// veriyi — gunluge yazar (ADR-0009 §4). Uretimde yanlislikla acik birakilmasi
    /// bir KVKK ihlali uretir; bu nedenle acilis engellenir.
    /// </remarks>
    public static void UretimGuvenligiDenetle(IHostEnvironment ortam, bool ayrintiliGunlukAcik)
    {
        ArgumentNullException.ThrowIfNull(ortam);

        if (!ortam.IsDevelopment() && ayrintiliGunlukAcik)
        {
            throw new InvalidOperationException(
                "'Veritabani:AyrintiliGunlukAcik' yalnizca gelistirme ortaminda acilabilir. " +
                "Bu ayar sorgu parametrelerini gunluge yazar ve kisisel verinin duz metin " +
                "olarak diske dusmesine yol acar (ADR-0009 §4).");
        }
    }
}
