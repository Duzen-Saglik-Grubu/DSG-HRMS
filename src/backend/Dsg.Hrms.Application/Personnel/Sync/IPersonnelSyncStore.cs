using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Organization;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Domain.Personnel.Sync;

namespace Dsg.Hrms.Application.Personnel.Sync;

/// <summary>
/// Senkronizasyonun HRMS veritabani tarafi.
/// </summary>
/// <remarks>
/// Uygulama katmani veri erisim kutuphanelerine bagimli degildir (ADR-0002); bu arayuz
/// altyapi katmaninda uygulanir.
/// </remarks>
public interface IPersonnelSyncStore
{
    /// <summary>
    /// Senkronizasyon oturumu acar. Baska bir senkronizasyon suruyorsa <c>null</c> doner
    /// (SYG-KMLK-004: ayni anda iki senkronizasyon calisamaz).
    /// </summary>
    /// <remarks>
    /// Oturum tek bir veritabani islemidir (transaction): ya tum degisiklikler
    /// uygulanir ya hicbiri. Yarim kalan bir calisma, kisilerin bir kismini guncel
    /// bir kismini eski birakmaz.
    /// </remarks>
    Task<IPersonnelSyncSession?> TryOpenSessionAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Calisma kaydini yazar veya gunceller.
    /// </summary>
    /// <remarks>
    /// Oturumdan BAGIMSIZ yazilir: senkronizasyon basarisiz olup oturum geri alindiginda
    /// da "calisma basarisiz oldu" kaydi kalmalidir. Aksi halde hata sessiz kalirdi
    /// (SYG-KMLK-010).
    /// </remarks>
    Task SaveRunAsync(PersonnelSyncRun run, CancellationToken cancellationToken);
}

/// <summary>Tek bir senkronizasyonun veritabani oturumu.</summary>
public interface IPersonnelSyncSession : IAsyncDisposable
{
    /// <summary>HRMS'teki mevcut firma, kisi, istihdam ve hesaplari degisiklik izlemeyle yukler.</summary>
    Task<PersonnelSnapshot> LoadAsync(CancellationToken cancellationToken);

    /// <summary>Yeni varliklari oturuma ekler.</summary>
    void Add(IEnumerable<Company> companies, IEnumerable<Person> persons, IEnumerable<Employment> employments);

    /// <summary>Degisiklikleri kaydeder ve islemi onaylar.</summary>
    /// <exception cref="SyncPersistenceException">Kayit basarisiz olursa.</exception>
    Task CommitAsync(CancellationToken cancellationToken);
}

/// <summary>HRMS'teki mevcut durum.</summary>
/// <param name="Companies">Firmalar.</param>
/// <param name="Persons">Kisiler.</param>
/// <param name="Employments">Istihdamlar (kisi ve firma baglantilari yuklu).</param>
public sealed record PersonnelSnapshot(
    IReadOnlyList<Company> Companies,
    IReadOnlyList<Person> Persons,
    IReadOnlyList<Employment> Employments)
{
    /// <summary>Kullanici hesaplari (SYG-KMLK-054, 056).</summary>
    public IReadOnlyList<UserAccount> Accounts { get; init; } = [];
}

/// <summary>Senkronizasyon sonucu HRMS veritabanina yazilamadiginda firlatilir.</summary>
public sealed class SyncPersistenceException : Exception
{
    /// <summary>Yeni ornek olusturur.</summary>
    public SyncPersistenceException()
        : base("Senkronizasyon sonucu kaydedilemedi.")
    {
    }

    /// <summary>Yeni ornek olusturur.</summary>
    public SyncPersistenceException(string message)
        : base(message)
    {
    }

    /// <summary>Yeni ornek olusturur.</summary>
    public SyncPersistenceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
