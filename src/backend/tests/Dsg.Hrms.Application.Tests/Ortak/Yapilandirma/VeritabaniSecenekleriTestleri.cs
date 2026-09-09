using System.ComponentModel.DataAnnotations;
using Dsg.Hrms.Application.Ortak.Yapilandirma;

namespace Dsg.Hrms.Application.Tests.Ortak.Yapilandirma;

/// <summary>
/// Yapilandirma dogrulama kurallarini denetler (ADR-0008 §4).
/// </summary>
/// <remarks>
/// Bu testlerin amaci, eksik veya hatali bir ayarin uygulama ACILIRKEN yakalanmasini
/// garanti etmektir. Yakalanmazsa hata, ayarin ilk kullanildigi anda — belki gunler
/// sonra, bir kullanici isleminin ortasinda — ortaya cikar.
/// </remarks>
public sealed class VeritabaniSecenekleriTestleri
{
    private static List<ValidationResult> Dogrula(VeritabaniSecenekleri secenekler)
    {
        var sonuclar = new List<ValidationResult>();
        Validator.TryValidateObject(
            secenekler,
            new ValidationContext(secenekler),
            sonuclar,
            validateAllProperties: true);

        return sonuclar;
    }

    [Fact]
    public void Baglanti_dizesi_bos_ise_dogrulama_basarisiz_olur()
    {
        var secenekler = new VeritabaniSecenekleri { Hrms = string.Empty };

        var sonuclar = Dogrula(secenekler);

        sonuclar.ShouldNotBeEmpty("Baglanti dizesi zorunludur; eksikse uygulama acilmamalidir.");
        sonuclar.ShouldContain(s => s.MemberNames.Contains(nameof(VeritabaniSecenekleri.Hrms)));
    }

    [Fact]
    public void Hata_mesaji_ayarin_nasil_verilecegini_soyler()
    {
        // Hata mesajinin degeri, sorunu YASAYAN kisiye ne yapacagini soylemesindedir.
        // "Alan zorunludur" demek yeterli degil; nereye yazilacagi da yazmalidir.
        var sonuclar = Dogrula(new VeritabaniSecenekleri { Hrms = string.Empty });

        var mesaj = sonuclar[0].ErrorMessage;

        mesaj.ShouldNotBeNull();
        mesaj.ShouldContain("user-secrets", Case.Insensitive);
        mesaj.ShouldContain("Veritabani__Hrms");
    }

    [Fact]
    public void Gecerli_ayarlarla_dogrulama_basarili_olur()
    {
        var secenekler = new VeritabaniSecenekleri
        {
            Hrms = "Host=localhost;Database=dsg_hrms;Username=test;Password=test",
        };

        Dogrula(secenekler).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(4)]      // alt sinirin altinda
    [InlineData(301)]    // ust sinirin ustunde
    [InlineData(-1)]
    public void Gecersiz_zaman_asimi_reddedilir(int saniye)
    {
        var secenekler = new VeritabaniSecenekleri
        {
            Hrms = "Host=localhost;Database=d;Username=u;Password=p",
            KomutZamanAsimiSaniye = saniye,
        };

        Dogrula(secenekler)
            .ShouldContain(s => s.MemberNames.Contains(nameof(VeritabaniSecenekleri.KomutZamanAsimiSaniye)));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(300)]
    public void Sinir_degerlerindeki_zaman_asimi_kabul_edilir(int saniye)
    {
        var secenekler = new VeritabaniSecenekleri
        {
            Hrms = "Host=localhost;Database=d;Username=u;Password=p",
            KomutZamanAsimiSaniye = saniye,
        };

        Dogrula(secenekler).ShouldBeEmpty();
    }

    [Fact]
    public void Yeniden_deneme_sayisi_sinirlanir()
    {
        var secenekler = new VeritabaniSecenekleri
        {
            Hrms = "Host=localhost;Database=d;Username=u;Password=p",
            YenidenDenemeSayisi = 11,
        };

        Dogrula(secenekler)
            .ShouldContain(s => s.MemberNames.Contains(nameof(VeritabaniSecenekleri.YenidenDenemeSayisi)));
    }

    [Fact]
    public void Ayrintili_gunluk_varsayilan_olarak_kapalidir()
    {
        // Kisisel verinin gunluge dusmesine yol acan bir ayarin varsayilani
        // KAPALI olmalidir; acik olmasi bilincli bir tercih gerektirmelidir.
        new VeritabaniSecenekleri().AyrintiliGunlukAcik.ShouldBeFalse();
    }
}
