using Dsg.Hrms.Application.Ortak.Soyutlamalar;
using Dsg.Hrms.Infrastructure.Veri;
using Dsg.Hrms.Infrastructure.Veri.Interceptor;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace Dsg.Hrms.Api.IntegrationTests.Veri;

/// <summary>
/// ADR-0004'teki veritabani kurallarini <b>gercek PostgreSQL</b> uzerinde dogrular.
/// </summary>
/// <remarks>
/// EF Core In-Memory saglayicisi kullanilmaz (ADR-0011 §3): kisitlari, kismi
/// dizinleri, <c>xmin</c> eszamanlilik denetimini ve tip donusumlerini uygulamaz.
/// Bu testlerin tamami In-Memory ile "gecerdi" ama uretimde bozuk olabilirdi.
/// </remarks>
public sealed class VeritabaniKurallariTestleri : IAsyncLifetime
{
    private readonly PostgreSqlContainer _konteyner = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("dsg_hrms_test")
        .Build();

    private readonly IMevcutKullanici _mevcutKullanici = Substitute.For<IMevcutKullanici>();
    private readonly IZamanSaglayici _zamanSaglayici = Substitute.For<IZamanSaglayici>();

    private static readonly DateTimeOffset SabitAn =
        new(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);

    public async Task InitializeAsync()
    {
        await _konteyner.StartAsync();

        _zamanSaglayici.SuAn.Returns(SabitAn);
        _mevcutKullanici.KullaniciId.Returns((long?)42);

        await using var baglam = BaglamOlustur();
        await baglam.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _konteyner.DisposeAsync();

    private DenemeDbContext BaglamOlustur()
    {
        var secenekler = new DbContextOptionsBuilder<DenemeDbContext>()
            .UseNpgsql(_konteyner.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(new DenetimAlanlariInterceptor(_mevcutKullanici, _zamanSaglayici))
            .Options;

        return new DenemeDbContext(secenekler);
    }

    // ------------------------------------------------------------------
    // ADR-0004 §1 — Adlandirma
    // ------------------------------------------------------------------

    [Fact]
    public async Task Kolon_adlari_snake_case_uretilir()
    {
        await using var baglam = BaglamOlustur();

        var kolonlar = await baglam.Database
            .SqlQuery<string>($"""
                SELECT column_name AS "Value"
                FROM information_schema.columns
                WHERE table_name = 'deneme_kaydi'
                """)
            .ToListAsync();

        kolonlar.ShouldContain("genel_id");
        kolonlar.ShouldContain("olusturma_ani");
        kolonlar.ShouldContain("olusturan_kullanici_id");
        kolonlar.ShouldContain("silinme_ani");
        kolonlar.ShouldContain("gecerlilik_tarihi");

        // PascalCase kolon uretilmemeli
        kolonlar.ShouldNotContain("GenelId");
    }

    // ------------------------------------------------------------------
    // ADR-0004 §3 — Veri tipleri
    // ------------------------------------------------------------------

    [Fact]
    public async Task Zaman_damgalari_timestamptz_tarihler_date_olur()
    {
        await using var baglam = BaglamOlustur();

        var tipler = await baglam.Database
            .SqlQuery<KolonTipi>($"""
                SELECT column_name AS "Ad", data_type AS "Tip"
                FROM information_schema.columns
                WHERE table_name = 'deneme_kaydi'
                """)
            .ToListAsync();

        static string TipiniBul(List<KolonTipi> t, string ad) =>
            t.Single(x => x.Ad == ad).Tip;

        // Zaman damgasi: saat dilimi BILGISIYLE saklanir. Aksi hâlde UTC/yerel
        // ayrimi kaybolur ve gecmis kayitlar yanlis yorumlanir.
        TipiniBul(tipler, "olusturma_ani").ShouldBe("timestamp with time zone");
        TipiniBul(tipler, "silinme_ani").ShouldBe("timestamp with time zone");

        // Takvim gunu: saat bilgisi TASIMAZ. Izin gunu gibi kavramlar saat
        // diliminden etkilenmemelidir.
        TipiniBul(tipler, "gecerlilik_tarihi").ShouldBe("date");

        // Metin: PostgreSQL'de text tercih edilir.
        TipiniBul(tipler, "baslik").ShouldBe("text");

        // Para: kayan noktali tip degil.
        TipiniBul(tipler, "tutar").ShouldBe("numeric");
    }

    // ------------------------------------------------------------------
    // ADR-0004 §4 — Denetim alanlari
    // ------------------------------------------------------------------

    [Fact]
    public async Task Olusturmada_denetim_alanlari_otomatik_dolar()
    {
        await using var baglam = BaglamOlustur();
        var kayit = new DenemeKaydi { Baslik = "Olusturma denemesi" };

        baglam.DenemeKayitlari.Add(kayit);
        await baglam.SaveChangesAsync();

        kayit.OlusturmaAni.ShouldBe(SabitAn);
        kayit.OlusturanKullaniciId.ShouldBe(42);
        kayit.GuncellemeAni.ShouldBeNull();
        kayit.GenelId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task Guncellemede_olusturma_bilgisi_degismez()
    {
        await using var baglam = BaglamOlustur();
        var kayit = new DenemeKaydi { Baslik = "Ilk hâli" };
        baglam.DenemeKayitlari.Add(kayit);
        await baglam.SaveChangesAsync();

        var ilkOlusturmaAni = kayit.OlusturmaAni;
        var ilkOlusturan = kayit.OlusturanKullaniciId;

        // Baska bir kullanici, baska bir anda gunceller.
        var sonrakiAn = SabitAn.AddHours(3);
        _zamanSaglayici.SuAn.Returns(sonrakiAn);
        _mevcutKullanici.KullaniciId.Returns((long?)99);

        kayit.Baslik = "Guncellenmis hâli";
        // Olusturma bilgisini KASITLI olarak bozmaya calisiyoruz.
        kayit.OlusturanKullaniciId = 12345;
        await baglam.SaveChangesAsync();

        // Denetim izinin guvenilirligi, olusturma bilgisinin degismezligine dayanir.
        var veritabanindaki = await baglam.DenemeKayitlari
            .AsNoTracking()
            .SingleAsync(k => k.Id == kayit.Id);

        veritabanindaki.OlusturmaAni.ShouldBe(ilkOlusturmaAni);
        veritabanindaki.OlusturanKullaniciId.ShouldBe(ilkOlusturan);
        veritabanindaki.GuncellemeAni.ShouldBe(sonrakiAn);
        veritabanindaki.GuncelleyenKullaniciId.ShouldBe(99);
    }

    // ------------------------------------------------------------------
    // ADR-0004 §5 — Yumusak silme
    // ------------------------------------------------------------------

    [Fact]
    public async Task Silme_istegi_fiziksel_silme_yapmaz_isaretler()
    {
        await using var baglam = BaglamOlustur();
        var kayit = new DenemeKaydi { Baslik = "Silinecek" };
        baglam.DenemeKayitlari.Add(kayit);
        await baglam.SaveChangesAsync();
        var id = kayit.Id;

        baglam.DenemeKayitlari.Remove(kayit);
        await baglam.SaveChangesAsync();

        // Satir fiziksel olarak DURUYOR olmali.
        var kalanSatirSayisi = await baglam.Database
            .SqlQuery<int>($"SELECT COUNT(*)::int AS \"Value\" FROM deneme_kaydi WHERE id = {id}")
            .SingleAsync();

        kalanSatirSayisi.ShouldBe(1, "Is kayitlari fiziksel olarak silinmez, isaretlenir.");

        // Ancak normal sorgulara GIRMEMELI.
        var suzgecliSonuc = await baglam.DenemeKayitlari.SingleOrDefaultAsync(k => k.Id == id);
        suzgecliSonuc.ShouldBeNull("Silinmis kayit varsayilan sorgulara girmemelidir.");

        // Suzgec kapatildiginda gorunmeli ve silme bilgisi dolu olmali.
        var suzgecsizSonuc = await baglam.DenemeKayitlari
            .IgnoreQueryFilters()
            .SingleAsync(k => k.Id == id);

        suzgecsizSonuc.SilinmeAni.ShouldNotBeNull();
        suzgecsizSonuc.SilenKullaniciId.ShouldBe(42);
    }

    // ------------------------------------------------------------------
    // ADR-0004 §4 / ADR-0010 §7 — Eszamanlilik
    // ------------------------------------------------------------------

    [Fact]
    public async Task Ayni_kaydi_iki_kullanici_duzenlerse_ikincisi_hata_alir()
    {
        await using var kurulum = BaglamOlustur();
        var kayit = new DenemeKaydi { Baslik = "Yaris" };
        kurulum.DenemeKayitlari.Add(kayit);
        await kurulum.SaveChangesAsync();

        // Iki kullanici ayni kaydi ayni anda acar.
        await using var birinci = BaglamOlustur();
        await using var ikinci = BaglamOlustur();

        var birinciKopya = await birinci.DenemeKayitlari.SingleAsync(k => k.Id == kayit.Id);
        var ikinciKopya = await ikinci.DenemeKayitlari.SingleAsync(k => k.Id == kayit.Id);

        birinciKopya.Baslik = "Birincinin degisikligi";
        await birinci.SaveChangesAsync();

        ikinciKopya.Baslik = "Ikincinin degisikligi";

        // Ikinci kullanicinin degisikligi SESSIZCE uzerine yazmamalidir.
        await Should.ThrowAsync<DbUpdateConcurrencyException>(
            async () => await ikinci.SaveChangesAsync());
    }

    // ------------------------------------------------------------------
    // Migration hatti
    // ------------------------------------------------------------------

    [Fact]
    public async Task Uretim_migrationlari_temiz_uygulanir()
    {
        // Bos bir veritabaninda migration'lar sorunsuz calismalidir.
        // Bu, migration hattinin kendisini dogrular (ADR-0011 §3).
        await using var konteyner = new PostgreSqlBuilder("postgres:17-alpine")
            .WithDatabase("migration_denemesi")
            .Build();

        await konteyner.StartAsync();

        try
        {
            var secenekler = new DbContextOptionsBuilder<HrmsDbContext>()
                .UseNpgsql(konteyner.GetConnectionString(), npgsql =>
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "public"))
                .UseSnakeCaseNamingConvention()
                .Options;

            await using var baglam = new HrmsDbContext(secenekler);

            await Should.NotThrowAsync(async () => await baglam.Database.MigrateAsync());

            var uygulananlar = await baglam.Database.GetAppliedMigrationsAsync();
            uygulananlar.ShouldNotBeEmpty("En az bir migration uygulanmis olmalidir.");
        }
        finally
        {
            await konteyner.DisposeAsync();
        }
    }

    private sealed record KolonTipi(string Ad, string Tip);
}
