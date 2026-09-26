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
}

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
