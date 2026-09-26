namespace Dsg.Hrms.Application.Settings;

/// <summary>
/// Parametreleri listeler ve degistirir (SYG-KMLK-075, 076).
/// </summary>
/// <remarks>
/// Parametre ekrani ve uc noktasi bu arayuzu kullanir. Yetki denetimi
/// (<c>system.parameter.view</c>, <c>system.parameter.update</c>) cagiran katmanda yapilir.
/// </remarks>
public interface ISystemParameterEditor
{
    /// <summary>Katalogdaki tum parametreleri gecerli degerleriyle listeler.</summary>
    /// <remarks>Sir parametrelerin degeri HICBIR ZAMAN donmez; yalnizca tanimli olup olmadigi (<c>KR-071</c>).</remarks>
    Task<IReadOnlyList<ParameterView>> ListAsync(CancellationToken cancellationToken);

    /// <summary>Parametrenin degerini degistirir.</summary>
    /// <exception cref="Common.Exceptions.NotFoundException">Parametre katalogda yoksa.</exception>
    /// <exception cref="Common.Exceptions.BusinessRuleException">Deger gecersizse; hicbir sey kaydedilmez.</exception>
    Task UpdateAsync(string key, string value, CancellationToken cancellationToken);
}

/// <summary>Parametrenin ekranda gosterilen hali.</summary>
/// <param name="Key">Katalog kimligi.</param>
/// <param name="Description">Aciklama.</param>
/// <param name="Type">Tur.</param>
/// <param name="Min">Tam sayi icin alt sinir.</param>
/// <param name="Max">Tam sayi icin ust sinir.</param>
/// <param name="Value">Gecerli deger. Sir parametrede DAIMA <c>null</c>.</param>
/// <param name="IsSet">Deger herhangi bir kaynakta tanimli mi.</param>
/// <param name="Source">Degerin geldigi kaynak.</param>
public sealed record ParameterView(
    string Key,
    string Description,
    ParameterType Type,
    int? Min,
    int? Max,
    string? Value,
    bool IsSet,
    ParameterSource Source);

/// <summary>Parametre degerinin kaynagi.</summary>
public enum ParameterSource
{
    /// <summary>Hicbir kaynakta tanimli degil.</summary>
    None = 0,

    /// <summary>Katalog varsayilani.</summary>
    Default = 1,

    /// <summary>Yapilandirma veya ortam degiskeni.</summary>
    Configuration = 2,

    /// <summary>Parametre ekranindan girilen deger.</summary>
    Database = 3,
}
