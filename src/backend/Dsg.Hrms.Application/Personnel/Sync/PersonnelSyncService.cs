using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Domain.Personnel.Sync;
using Microsoft.Extensions.Logging;

namespace Dsg.Hrms.Application.Personnel.Sync;

/// <summary>
/// LOGO personel senkronizasyonunu yurutur (ADR-0003 §4, SYG-KMLK-004, 005, 010, 011).
/// </summary>
/// <remarks>
/// <para>Sira:</para>
/// <list type="number">
///   <item>Kilit alinir; baska bir calisma suruyorsa hicbir sey yapilmaz.</item>
///   <item>Calisma kaydi "suruyor" olarak yazilir.</item>
///   <item>LOGO oturumunun salt okunur oldugu ve semanin beklenen yapida oldugu denetlenir.</item>
///   <item>Kartlar okunur, fark hesaplanir ve tek islemde uygulanir.</item>
///   <item>Calisma kaydi sonucla kapatilir.</item>
/// </list>
/// <para>
/// Herhangi bir adimda hata olursa islem geri alinir, calisma "basarisiz" kaydedilir
/// ve sistem son basarili anlik goruntuyle calismaya devam eder (SYG-KMLK-011).
/// </para>
/// </remarks>
public sealed partial class PersonnelSyncService
{
    private readonly ILogoPersonnelSource _source;
    private readonly IPersonnelSyncStore _store;
    private readonly IDateTimeProvider _clock;
    private readonly PersonnelSyncOptions _options;
    private readonly ILogger<PersonnelSyncService> _logger;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonnelSyncService(
        ILogoPersonnelSource source,
        IPersonnelSyncStore store,
        IDateTimeProvider clock,
        PersonnelSyncOptions options,
        ILogger<PersonnelSyncService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _source = source;
        _store = store;
        _clock = clock;
        _options = options;
        _logger = logger;
    }

    /// <summary>Bir senkronizasyon calismasi yurutur.</summary>
    public async Task<PersonnelSyncResult> RunAsync(SyncTrigger trigger, CancellationToken cancellationToken)
    {
        await using var session = await _store.TryOpenSessionAsync(cancellationToken).ConfigureAwait(false);

        if (session is null)
        {
            LogAlreadyRunning(_logger, trigger);
            return PersonnelSyncResult.AlreadyRunning;
        }

        var run = PersonnelSyncRun.Start(trigger, _clock.UtcNow);
        await _store.SaveRunAsync(run, cancellationToken).ConfigureAwait(false);

        var failure = await ExecuteAsync(run, session, cancellationToken).ConfigureAwait(false);

        if (failure is not null)
        {
            run.Fail(failure.Value, _clock.UtcNow);
        }

        // Kayit, iptal istense bile yazilir: yarim kalan bir calismanin "suruyor"
        // olarak asili kalmasi, sagligi yanlis gosterirdi.
        await _store.SaveRunAsync(run, CancellationToken.None).ConfigureAwait(false);

        LogFinished(_logger, run.PublicId, run.Status, run.RecordsRead, run.WarningCount);
        return new PersonnelSyncResult(run.Status, run.PublicId);
    }

    private async Task<SyncFailureReason?> ExecuteAsync(
        PersonnelSyncRun run,
        IPersonnelSyncSession session,
        CancellationToken cancellationToken)
    {
        try
        {
            var access = await _source.VerifyReadOnlyAccessAsync(cancellationToken).ConfigureAwait(false);
            if (!access.IsReadOnly)
            {
                // Bu, bir yapilandirma hatasi degil GUVENLIK olayidir: KR-004 geregi
                // oturumda yazma yetkisi bulunmamalidir.
                LogSourceWritable(_logger, access.GrantedWritePermissions);
                return SyncFailureReason.SourceWritable;
            }

            var schema = await _source.VerifySchemaAsync(cancellationToken).ConfigureAwait(false);
            if (!schema.IsCompatible)
            {
                LogSchemaDrift(_logger, schema.Problems);
                return SyncFailureReason.SchemaDrift;
            }

            var records = await _source.GetAllAsync(cancellationToken).ConfigureAwait(false);
            var snapshot = await session.LoadAsync(cancellationToken).ConfigureAwait(false);

            var outcome = new PersonnelSynchronizer(_options.ExcludedRegistryCodes)
                .Synchronize(records, snapshot, _clock.Today);

            session.Add(outcome.NewCompanies, outcome.NewPersons, outcome.NewEmployments);
            await session.CommitAsync(cancellationToken).ConfigureAwait(false);

            run.Complete(outcome.Counts, outcome.Warnings, _clock.UtcNow);
            return null;
        }
        catch (LogoUnavailableException ex)
        {
            LogSourceUnavailable(_logger, ex);
            return SyncFailureReason.SourceUnavailable;
        }
        catch (SyncPersistenceException ex)
        {
            LogPersistenceFailed(_logger, ex);
            return SyncFailureReason.PersistenceFailed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            LogCancelled(_logger);
            return SyncFailureReason.Unexpected;
        }
#pragma warning disable CA1031 // Calisma kaydi her durumda kapatilmalidir; hata gunluge yazilir.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogUnexpected(_logger, ex);
            return SyncFailureReason.Unexpected;
        }
    }

    [LoggerMessage(EventId = 3100, Level = LogLevel.Information,
        Message = "Personel senkronizasyonu atlandi ({Trigger}): baska bir calisma suruyor.")]
    private static partial void LogAlreadyRunning(ILogger logger, SyncTrigger trigger);

    [LoggerMessage(EventId = 3101, Level = LogLevel.Information,
        Message = "Personel senkronizasyonu bitti {RunId}: {Status}, okunan {RecordsRead}, uyari {WarningCount}.")]
    private static partial void LogFinished(ILogger logger, Guid runId, SyncStatus status, int recordsRead, int warningCount);

    [LoggerMessage(EventId = 3102, Level = LogLevel.Critical,
        Message = "GUVENLIK: LOGO oturumunda yazma yetkisi var ({Permissions}). Senkronizasyon reddedildi (KR-004).")]
    private static partial void LogSourceWritable(ILogger logger, IReadOnlyList<string> permissions);

    [LoggerMessage(EventId = 3103, Level = LogLevel.Error,
        Message = "LOGO semasi beklenenden farkli; senkronizasyon baslatilmadi: {Problems}")]
    private static partial void LogSchemaDrift(ILogger logger, IReadOnlyList<string> problems);

    [LoggerMessage(EventId = 3104, Level = LogLevel.Error,
        Message = "LOGO veritabanina erisilemedi; son anlik goruntuyle devam ediliyor.")]
    private static partial void LogSourceUnavailable(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3105, Level = LogLevel.Error,
        Message = "Senkronizasyon sonucu HRMS veritabanina yazilamadi; degisiklikler geri alindi.")]
    private static partial void LogPersistenceFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3106, Level = LogLevel.Warning,
        Message = "Personel senkronizasyonu iptal edildi; degisiklikler geri alindi.")]
    private static partial void LogCancelled(ILogger logger);

    [LoggerMessage(EventId = 3107, Level = LogLevel.Error,
        Message = "Personel senkronizasyonunda beklenmeyen hata; degisiklikler geri alindi.")]
    private static partial void LogUnexpected(ILogger logger, Exception exception);
}

/// <summary>Senkronizasyon cagrisinin sonucu.</summary>
/// <param name="Status">Sonuc; baska bir calisma suruyorsa <c>null</c>.</param>
/// <param name="RunId">Calisma kaydinin dis kimligi; calisma yapilmadiysa <c>null</c>.</param>
public sealed record PersonnelSyncResult(SyncStatus? Status, Guid? RunId)
{
    /// <summary>Baska bir calisma surdugu icin hicbir sey yapilmadi.</summary>
    public static PersonnelSyncResult AlreadyRunning { get; } = new(null, null);
}
