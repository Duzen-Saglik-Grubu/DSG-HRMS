namespace Dsg.Hrms.Application.Personnel.Sync;

/// <summary>
/// Senkronizasyonu elle tetikler (SYG-KMLK-072).
/// </summary>
/// <remarks>
/// Istek calismayi BEKLEMEZ: tam bir calisma bir dakikaya yaklasabilir (SYG-KMLK-078) ve
/// ters vekilin zaman asimina takilirdi. Zamanlayiciya "hemen calis" sinyali verilir;
/// sonuc son calisma bilgisinden okunur. Ayni anda iki calisma olmaz (veritabani kilidi).
/// </remarks>
public interface IPersonnelSyncTrigger
{
    /// <summary>Elle calisma ister.</summary>
    ManualSyncRequest Request();
}

/// <summary>Elle calisma isteginin sonucu.</summary>
public enum ManualSyncRequest
{
    /// <summary>Calisma sira aldi; zamanlayici hemen baslatir.</summary>
    Accepted = 1,

    /// <summary>Zaten sirada bekleyen bir elle calisma var.</summary>
    AlreadyQueued = 2,

    /// <summary>LOGO baglantisi tanimli degil; senkronizasyon bu ortamda devre disi.</summary>
    Disabled = 3,
}
