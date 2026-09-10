using System.ComponentModel.DataAnnotations;

namespace Dsg.Hrms.Application.Common.Configuration;

/// <summary>
/// Uygulama gunlugu ayarlari (ADR-0009 §1).
/// </summary>
/// <remarks>
/// Bolum adi olarak <c>ApplicationLogging</c> kullanilir; .NET'in kendi
/// <c>Logging</c> bolumuyle karismamasi icin bilincli olarak ayrilmistir.
/// </remarks>
public sealed class LoggingOptions
{
    /// <summary>Yapilandirma bolumunun adi.</summary>
    public const string SectionName = "ApplicationLogging";

    /// <summary>
    /// Yazilacak en dusuk kayit seviyesi.
    /// </summary>
    /// <remarks>
    /// Gecerli olmayan bir deger sessizce yok sayilmaz: uygulama acilista durur.
    /// Yanlis yazilmis bir seviye yuzunden hata kayitlarinin hic yazilmadigini
    /// aylar sonra fark etmek, acilmayan bir uygulamadan daha maliyetlidir.
    /// </remarks>
    [AllowedValues("Verbose", "Debug", "Information", "Warning", "Error", "Fatal",
        ErrorMessage = "'ApplicationLogging:MinimumLevel' gecersiz. Gecerli degerler: " +
            "Verbose, Debug, Information, Warning, Error, Fatal.")]
    public string MinimumLevel { get; init; } = "Information";

    /// <summary>Konsola yazilsin mi? Kapsayici gunluklerinin kaynagi budur.</summary>
    public bool ConsoleEnabled { get; init; } = true;

    /// <summary>
    /// Gunluk dosyasi yolu. Tarih, dosya adinin sonuna eklenir.
    /// </summary>
    [Required(ErrorMessage = "'ApplicationLogging:FilePath' tanimli olmalidir.")]
    public string FilePath { get; init; } = "logs/dsg-hrms-.json";

    /// <summary>
    /// Saklanacak gunluk dosyasi sayisi (gunluk dosyalari icin ~gun sayisi).
    /// </summary>
    /// <remarks>
    /// Sinirsiz saklama iki yonlu risktir: disk dolar ve KVKK'nin sakla-sil
    /// yukumlulugu ihlal edilir (KR-023).
    /// </remarks>
    [Range(1, 3650, ErrorMessage = "'ApplicationLogging:RetainedFileCountLimit' 1-3650 arasinda olmalidir.")]
    public int RetainedFileCountLimit { get; init; } = 90;

    /// <summary>Tek bir gunluk dosyasinin ust siniri (MB).</summary>
    [Range(1, 1024, ErrorMessage = "'ApplicationLogging:FileSizeLimitMegabytes' 1-1024 arasinda olmalidir.")]
    public int FileSizeLimitMegabytes { get; init; } = 64;
}
