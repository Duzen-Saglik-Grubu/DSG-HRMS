namespace Dsg.Hrms.Domain.Personnel.Sync;

/// <summary>
/// Bir LOGO senkronizasyon calismasinin kalici kaydi (SYG-KMLK-005).
/// </summary>
/// <remarks>
/// <para>
/// Her calisma — basarili da olsa basarisiz da olsa — kayit uretir. "Son senkronizasyon
/// ne zaman, sonucu ne oldu?" sorusu bu tablodan cevaplanir; sistem sagligi ve
/// yonetici bildirimi (SYG-KMLK-010) de buna dayanir.
/// </para>
/// <para>
/// <b>Entity turunden turemez</b> ve denetim izine girmez: kendisi bir kayittir
/// (<c>ChangeLogEntry</c> gibi). Turetilseydi 15 dakikada bir calisan senkronizasyon
/// her calismada denetim izine anlamsiz satirlar eklerdi.
/// </para>
/// <para>
/// Kayit KISISEL VERI TASIMAZ: yalnizca sayilar, zaman ve hata turu. Hata iletisi
/// istisna metninden degil, bilinen bir listeden uretilir.
/// </para>
/// </remarks>
public sealed class PersonnelSyncRun
{
    private readonly List<PersonnelSyncWarning> _warnings = [];

    private PersonnelSyncRun()
    {
    }

    /// <summary>Veritabani ici birincil anahtar.</summary>
    public long Id { get; private set; }

    /// <summary>API'de kullanilan dis kimlik.</summary>
    public Guid PublicId { get; private set; } = Guid.CreateVersion7();

    /// <summary>Calismayi baslatan etken.</summary>
    public SyncTrigger Trigger { get; private set; }

    /// <summary>Baslangic ani (UTC).</summary>
    public DateTimeOffset StartedAt { get; private set; }

    /// <summary>Bitis ani (UTC). Calisma surerken <c>null</c>.</summary>
    public DateTimeOffset? FinishedAt { get; private set; }

    /// <summary>Sonuc.</summary>
    public SyncStatus Status { get; private set; }

    /// <summary>Kaynaktan okunan kart sayisi.</summary>
    public int RecordsRead { get; private set; }

    /// <summary>Kapsam disi birakilan kart sayisi (ornegin <c>KR-034</c>).</summary>
    public int RecordsSkipped { get; private set; }

    /// <summary>Olusturulan kisi sayisi.</summary>
    public int PersonsCreated { get; private set; }

    /// <summary>Guncellenen kisi sayisi.</summary>
    public int PersonsUpdated { get; private set; }

    /// <summary>Olusturulan istihdam sayisi.</summary>
    public int EmploymentsCreated { get; private set; }

    /// <summary>Guncellenen istihdam sayisi.</summary>
    public int EmploymentsUpdated { get; private set; }

    /// <summary>Aktiften pasife gecen istihdam sayisi.</summary>
    public int EmploymentsDeactivated { get; private set; }

    /// <summary>Olusturulan veya guncellenen firma sayisi.</summary>
    public int CompaniesChanged { get; private set; }

    /// <summary>Uretilen veri kalitesi uyarisi sayisi.</summary>
    public int WarningCount { get; private set; }

    /// <summary>Basarisizlik nedeni (kisisel veri icermez). Basariliysa <c>null</c>.</summary>
    public SyncFailureReason? FailureReason { get; private set; }

    /// <summary>Veri kalitesi uyarilari.</summary>
    public IReadOnlyCollection<PersonnelSyncWarning> Warnings => _warnings;

    /// <summary>Yeni bir calisma baslatir.</summary>
    public static PersonnelSyncRun Start(SyncTrigger trigger, DateTimeOffset now) =>
        new() { Trigger = trigger, StartedAt = now, Status = SyncStatus.Running };

    /// <summary>Calismayi basariyla tamamlar.</summary>
    /// <remarks>
    /// Uyari varsa sonuc <see cref="SyncStatus.CompletedWithWarnings"/> olur: veri
    /// aktarildi, ancak IK'nin LOGO'da duzeltmesi gereken kayitlar var.
    /// </remarks>
    public void Complete(SyncCounts counts, IEnumerable<PersonnelSyncWarning> warnings, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(counts);
        ArgumentNullException.ThrowIfNull(warnings);
        EnsureRunning();

        _warnings.AddRange(warnings);

        RecordsRead = counts.RecordsRead;
        RecordsSkipped = counts.RecordsSkipped;
        PersonsCreated = counts.PersonsCreated;
        PersonsUpdated = counts.PersonsUpdated;
        EmploymentsCreated = counts.EmploymentsCreated;
        EmploymentsUpdated = counts.EmploymentsUpdated;
        EmploymentsDeactivated = counts.EmploymentsDeactivated;
        CompaniesChanged = counts.CompaniesChanged;
        WarningCount = _warnings.Count;

        Status = _warnings.Count == 0 ? SyncStatus.Succeeded : SyncStatus.CompletedWithWarnings;
        FinishedAt = now;
    }

    /// <summary>Calismayi basarisiz olarak kapatir. Hicbir veri degisikligi uygulanmamistir.</summary>
    public void Fail(SyncFailureReason reason, DateTimeOffset now)
    {
        EnsureRunning();

        Status = SyncStatus.Failed;
        FailureReason = reason;
        FinishedAt = now;
    }

    private void EnsureRunning()
    {
        if (Status != SyncStatus.Running)
        {
            throw new InvalidOperationException(
                $"Senkronizasyon calismasi zaten kapanmis (durum: {Status}).");
        }
    }
}

/// <summary>Senkronizasyon sayaclari.</summary>
public sealed record SyncCounts(
    int RecordsRead,
    int RecordsSkipped,
    int PersonsCreated,
    int PersonsUpdated,
    int EmploymentsCreated,
    int EmploymentsUpdated,
    int EmploymentsDeactivated,
    int CompaniesChanged);

/// <summary>Senkronizasyonu baslatan etken.</summary>
public enum SyncTrigger
{
    /// <summary>Periyodik calisma.</summary>
    Scheduled = 1,

    /// <summary>Yetkili kullanicinin elle tetiklemesi.</summary>
    Manual = 2,
}

/// <summary>Senkronizasyon sonucu (SYG-KMLK-005: basarili / kismen / basarisiz).</summary>
public enum SyncStatus
{
    /// <summary>Calisma suruyor.</summary>
    Running = 1,

    /// <summary>Tum kartlar uyarisiz aktarildi.</summary>
    Succeeded = 2,

    /// <summary>Aktarim tamamlandi; veri kalitesi uyarilari var ("kismen").</summary>
    CompletedWithWarnings = 3,

    /// <summary>Calisma basarisiz; hicbir degisiklik uygulanmadi.</summary>
    Failed = 4,
}

/// <summary>
/// Basarisizlik nedeni. Istisna metni yerine sabit bir liste kullanilir: istisna
/// metinleri kaynak verinin parcalarini (dolayisiyla kisisel veriyi) tasiyabilir.
/// </summary>
public enum SyncFailureReason
{
    /// <summary>LOGO veritabanina erisilemedi.</summary>
    SourceUnavailable = 1,

    /// <summary>LOGO oturumunda yazma yetkisi tespit edildi; guvenlik geregi calisma reddedildi.</summary>
    SourceWritable = 2,

    /// <summary>LOGO semasi beklenenden farkli (SYG-KMLK-012).</summary>
    SchemaDrift = 3,

    /// <summary>HRMS veritabanina yazilamadi.</summary>
    PersistenceFailed = 4,

    /// <summary>Beklenmeyen hata. Ayrinti uygulama gunlugundedir.</summary>
    Unexpected = 99,
}
