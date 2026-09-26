namespace Dsg.Hrms.Application.Personnel.Sync;

/// <summary>
/// LOGO personel senkronizasyonu ayarlari.
/// </summary>
/// <remarks>
/// Periyot bu sinifta DEGILDIR: parametre deposundan okunur (<c>PRM-ENT-07</c>,
/// SYG-KMLK-075) ve yeniden dagitim gerekmeden degistirilebilir. Yapilandirmadaki
/// <c>PersonnelSync:IntervalMinutes</c> anahtari, veritabaninda deger yoksa kullanilir.
/// </remarks>
public sealed class PersonnelSyncOptions
{
    /// <summary>Yapilandirma bolumunun adi.</summary>
    public const string SectionName = "PersonnelSync";

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
