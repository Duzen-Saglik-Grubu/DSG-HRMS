using Dsg.Hrms.Domain.Personnel.Sync;

namespace Dsg.Hrms.Application.Personnel.Sync;

/// <summary>
/// Senkronizasyonun durumunu okur (SYG-KMLK-010: sistem sagligi uc noktasi).
/// </summary>
public interface IPersonnelSyncStatus
{
    /// <summary>
    /// LOGO baglantisi tanimli mi. Tanimli degilse senkronizasyon bilincli olarak
    /// devre disidir; bu bir hata degildir.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>En son baslayan calismayi dondurur; hic calisma yoksa <c>null</c>.</summary>
    Task<PersonnelSyncRunSummary?> GetLastRunAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Ekrandaki "Son senkronizasyon" bilgisi (SYG-PERS-010): verinin ne kadar guncel oldugu.
    /// </summary>
    Task<PersonnelSyncFreshness> GetFreshnessAsync(CancellationToken cancellationToken);
}

/// <summary>Personel verisinin guncelligi (SYG-PERS-010).</summary>
/// <param name="LastSucceededAt">
/// Son basarili calismanin bitisi; hic yoksa <c>null</c>. Uyarili tamamlanan calisma basarilidir
/// (veri aktarildi); T1 adimi basarisiz olan ("kismen") calisma basarili sayilmaz.
/// </param>
/// <param name="LastRunFailed">
/// Son BITEN calisma basarisiz veya kismen tamamlandi mi. Suren calisma hesaba katilmaz.
/// </param>
public sealed record PersonnelSyncFreshness(DateTimeOffset? LastSucceededAt, bool LastRunFailed);

/// <summary>Bir calismanin ozeti.</summary>
/// <param name="Status">Sonuc.</param>
/// <param name="StartedAt">Baslangic.</param>
/// <param name="FinishedAt">Bitis; suruyorsa <c>null</c>.</param>
/// <param name="FailureReason">Basarisizlik nedeni.</param>
public sealed record PersonnelSyncRunSummary(
    SyncStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    SyncFailureReason? FailureReason);
