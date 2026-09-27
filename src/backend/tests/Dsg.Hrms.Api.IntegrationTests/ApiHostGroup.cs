namespace Dsg.Hrms.Api.IntegrationTests;

/// <summary>
/// Uygulamanin tamamini (<c>WebApplicationFactory&lt;Program&gt;</c>) kuran test siniflari.
/// </summary>
/// <remarks>
/// Bu siniflar PARALEL CALISMAZ. Uygulama acilirken Serilog'un genel (statik) onyukleme
/// gunlugunu dondurur; iki ornek ayni anda kurulursa ikincisi "logger is already frozen"
/// hatasiyla acilamaz. Tek koleksiyon, ornekleri sirayla kurar.
/// </remarks>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApiHostGroup
{
    /// <summary>Koleksiyon adi.</summary>
    public const string Name = "ApiHost";
}
