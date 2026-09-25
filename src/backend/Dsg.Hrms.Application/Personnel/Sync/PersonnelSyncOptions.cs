using System.ComponentModel.DataAnnotations;

namespace Dsg.Hrms.Application.Personnel.Sync;

/// <summary>
/// LOGO personel senkronizasyonu ayarlari.
/// </summary>
/// <remarks>
/// Periyot, Y4 Sistem Yonetimi parametre deposu devreye girdiginde <c>PRM-ENT-07</c>
/// parametresinden okunacaktir (SYG-KMLK-075). O zamana kadar yapilandirmadan gelir.
/// </remarks>
public sealed class PersonnelSyncOptions
{
    /// <summary>Yapilandirma bolumunun adi.</summary>
    public const string SectionName = "PersonnelSync";

    /// <summary>Periyot (dakika). Varsayilan 15 (<c>KR-007</c>, <c>PRM-ENT-07</c>).</summary>
    [Range(1, 1440, ErrorMessage = "Senkronizasyon periyodu 1-1440 dakika araliginda olmalidir.")]
    public int IntervalMinutes { get; init; } = 15;

    /// <summary>
    /// Senkronizasyon disi birakilan sicil kodlari.
    /// </summary>
    /// <remarks>
    /// Varsayilan: <c>0001000</c> — personel kaydi olmayan, TCKN'siz sistem karti
    /// (<c>KR-034</c>). Bu kart her calismada "TCKN yok" uyarisi uretir ve gercek
    /// uyarilari gurultu icinde kaybettirirdi.
    /// </remarks>
    public IReadOnlyList<string> ExcludedRegistryCodes { get; init; } = ["0001000"];
}
