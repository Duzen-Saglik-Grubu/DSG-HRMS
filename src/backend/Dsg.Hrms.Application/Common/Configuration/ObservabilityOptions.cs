using System.ComponentModel.DataAnnotations;

namespace Dsg.Hrms.Application.Common.Configuration;

/// <summary>
/// Izleme ve olcum (OpenTelemetry) ayarlari.
/// </summary>
public sealed class ObservabilityOptions
{
    /// <summary>Yapilandirma bolumunun adi.</summary>
    public const string SectionName = "Observability";

    /// <summary>Izleme ve olcum toplama etkin mi?</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>
    /// Toplayiciya (collector) gonderim adresi. Bos ise <b>disa aktarim yapilmaz</b>.
    /// </summary>
    /// <remarks>
    /// Bilincli olarak zorunlu degildir: toplayici kurulmadan once de uygulama
    /// calisabilmelidir. Adres tanimsizken veriler yalnizca uretilir, gonderilmez;
    /// bir toplayici devreye alindiginda tek ayarla akmaya baslar.
    /// </remarks>
    public string? OtlpEndpoint { get; init; }

    /// <summary>Izlerde gorunecek servis adi.</summary>
    [Required(ErrorMessage = "'Observability:ServiceName' tanimli olmalidir.")]
    public string ServiceName { get; init; } = "dsg-hrms-api";

    /// <summary>
    /// Orneklem orani (0.0 - 1.0). <c>1.0</c> tum istekleri izler.
    /// </summary>
    /// <remarks>
    /// Kurum ici bir sistemde istek hacmi dusuktur; tam orneklem, sorun aninda
    /// "tam o istek orneklenmemis" durumunu ortadan kaldirir. Hacim buyurse oran
    /// yapilandirmayla dusurulebilir.
    /// </remarks>
    [Range(0.0, 1.0, ErrorMessage = "'Observability:SamplingRatio' 0.0 ile 1.0 arasinda olmalidir.")]
    public double SamplingRatio { get; init; } = 1.0;
}
