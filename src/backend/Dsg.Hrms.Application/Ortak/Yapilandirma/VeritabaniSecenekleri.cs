using System.ComponentModel.DataAnnotations;

namespace Dsg.Hrms.Application.Ortak.Yapilandirma;

/// <summary>
/// HRMS veritabani baglanti ayarlari.
/// </summary>
/// <remarks>
/// <para>
/// Baglanti dizesi KAYNAK KODA YAZILMAZ (ADR-0008). Gelistirmede .NET User Secrets,
/// UAT ve uretimde ortam degiskeni ile saglanir.
/// </para>
/// <para>
/// Ortam degiskeni karsiligi:
/// <c>Veritabani__Hrms</c>
/// </para>
/// </remarks>
public sealed class VeritabaniSecenekleri
{
    /// <summary>Yapilandirma bolumunun adi.</summary>
    public const string BolumAdi = "Veritabani";

    /// <summary>
    /// HRMS PostgreSQL baglanti dizesi. Zorunludur; eksikse uygulama acilmaz.
    /// </summary>
    [Required(ErrorMessage =
        "HRMS veritabani baglanti dizesi tanimli degil. " +
        "Gelistirmede: dotnet user-secrets set \"Veritabani:Hrms\" \"...\" " +
        "Uretimde: Veritabani__Hrms ortam degiskeni.")]
    public string Hrms { get; init; } = string.Empty;

    /// <summary>
    /// Komut zaman asimi (saniye). Uzun suren sorgularin sistemi kilitlemesini onler.
    /// </summary>
    [Range(5, 300, ErrorMessage = "Komut zaman asimi 5-300 saniye araliginda olmalidir.")]
    public int KomutZamanAsimiSaniye { get; init; } = 30;

    /// <summary>
    /// Gecici hatalarda yeniden deneme sayisi (aci gecis dayanikliligi).
    /// </summary>
    [Range(0, 10, ErrorMessage = "Yeniden deneme sayisi 0-10 araliginda olmalidir.")]
    public int YenidenDenemeSayisi { get; init; } = 3;

    /// <summary>
    /// Ayrintili EF Core gunlugu ve hassas veri kaydi.
    /// </summary>
    /// <remarks>
    /// <b>Yalnizca gelistirme ortaminda</b> acilabilir. Acik oldugunda sorgu
    /// parametreleri gunluge yazilir; bu, kisisel verinin duz metin olarak
    /// diske dusmesi anlamina gelir (ADR-0009 §4). Uretimde acilmasi engellenir.
    /// </remarks>
    public bool AyrintiliGunlukAcik { get; init; }
}
