using Dsg.Hrms.Application.Common.Paging;
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

    /// <summary>
    /// "Senkronizasyon durumu" ekranindaki calismalar (SYG-PERS-008): en yeni once, sayfali.
    /// </summary>
    Task<PagedResult<PersonnelSyncRunDetail>> ListRunsAsync(PageRequest page, CancellationToken cancellationToken);
}

/// <summary>Bir calismanin ekranda gosterilen bilgisi ve sayilari (SYG-PERS-008).</summary>
/// <param name="Id">Calismanin dis kimligi.</param>
/// <param name="Trigger">Zamanlanmis veya elle.</param>
/// <param name="Status">Sonuc.</param>
/// <param name="StartedAt">Baslangic.</param>
/// <param name="FinishedAt">Bitis; suruyorsa veya yarida kaldiysa <c>null</c>.</param>
/// <param name="FailureReason">Basarisizlik nedeni.</param>
/// <param name="RecordsRead">LOGO'dan okunan kart.</param>
/// <param name="PersonsCreated">Eklenen kisi.</param>
/// <param name="PersonsUpdated">Guncellenen kisi.</param>
/// <param name="EmploymentsCreated">Eklenen istihdam (sicil).</param>
/// <param name="EmploymentsUpdated">Guncellenen istihdam.</param>
/// <param name="EmploymentsDeactivated">Pasiflesen istihdam.</param>
/// <param name="WarningCount">Uretilen uyari.</param>
public sealed record PersonnelSyncRunDetail(
    Guid Id,
    SyncTrigger Trigger,
    SyncStatus Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    SyncFailureReason? FailureReason,
    int RecordsRead,
    int PersonsCreated,
    int PersonsUpdated,
    int EmploymentsCreated,
    int EmploymentsUpdated,
    int EmploymentsDeactivated,
    int WarningCount);

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
