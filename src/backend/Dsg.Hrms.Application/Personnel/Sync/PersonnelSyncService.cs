using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Settings;
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
    private readonly ISystemParameters _parameters;
    private readonly PersonnelSyncOptions _options;
    private readonly IPhotoThumbnailer _thumbnailer;
    private readonly ILogger<PersonnelSyncService> _logger;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonnelSyncService(
        ILogoPersonnelSource source,
        IPersonnelSyncStore store,
        IDateTimeProvider clock,
        ISystemParameters parameters,
        PersonnelSyncOptions options,
        IPhotoThumbnailer thumbnailer,
        ILogger<PersonnelSyncService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _source = source;
        _store = store;
        _clock = clock;
        _parameters = parameters;
        _options = options;
        _thumbnailer = thumbnailer;
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

            var autoDeactivate = await _parameters
                .GetBooleanAsync(ParameterCatalog.AutoDeactivateOnEmploymentEnd, cancellationToken)
                .ConfigureAwait(false);

            var records = await _source.GetAllAsync(cancellationToken).ConfigureAwait(false);
            var snapshot = await session.LoadAsync(cancellationToken).ConfigureAwait(false);

            var outcome = new PersonnelSynchronizer(_options.ExcludedRegistryCodes)
                .Synchronize(records, snapshot, _clock.Today, autoDeactivate);

            session.Add(outcome.NewCompanies, outcome.NewPersons, outcome.NewEmployments);
            await session.CommitAsync(cancellationToken).ConfigureAwait(false);

            // T1 adimi ilk adim KAYDEDILDIKTEN sonra calisir (SYG-PERS-013, AN-07): burada
            // olusan hata kisi, istihdam ve hesap degisikliklerini geri almaz.
            var (detailsUpdated, assignmentsChanged, detailsFailure) = await RunDetailsStepAsync(session, cancellationToken).ConfigureAwait(false);

            // Fotograflar en son islenir (SYG-PERS-013); hatasi onceki adimlari geri almaz.
            var (photosChanged, photoFailure) = await RunPhotoStepAsync(session, cancellationToken).ConfigureAwait(false);
            var counts = outcome.Counts with
            {
                PersonsUpdated = outcome.Counts.PersonsUpdated + detailsUpdated + photosChanged,
                EmploymentsUpdated = outcome.Counts.EmploymentsUpdated + assignmentsChanged,
            };

            if ((detailsFailure ?? photoFailure) is { } reason)
            {
                run.CompletePartially(counts, outcome.Warnings, reason, _clock.UtcNow);
            }
            else
            {
                run.Complete(counts, outcome.Warnings, _clock.UtcNow);
            }

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

    /// <summary>
    /// T1 adimi: genel bilgiler, adres, sube ve birim tanimlari (SYG-PERS-003). Hata firlatmaz;
    /// basarisizligi nedeniyle doner.
    /// </summary>
    private async Task<(int Updated, int EmploymentsChanged, SyncFailureReason? Failure)> RunDetailsStepAsync(
        IPersonnelSyncSession session,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await session.BeginNextStepAsync(cancellationToken).ConfigureAwait(false))
            {
                LogDetailsStepYielded(_logger);
                return (0, 0, null);
            }

            var details = await _source.GetDetailsAsync(cancellationToken).ConfigureAwait(false);
            var snapshot = await session.LoadDetailsAsync(cancellationToken).ConfigureAwait(false);
            var outcome = PersonnelDetailsSynchronizer.Synchronize(details, snapshot);
            session.AddDetails(outcome);

            // Yeni sube ve birimler kimlik alsin; atamalar onlara baglanir (SYG-PERS-018).
            await session.SaveAsync(cancellationToken).ConfigureAwait(false);
            var withUnits = await session.LoadDetailsAsync(cancellationToken).ConfigureAwait(false);
            var (assignments, employmentsChanged) = PersonnelDetailsSynchronizer.SynchronizeAssignments(details, withUnits, _clock.Today);
            session.AddAssignments(assignments);

            await session.CommitAsync(cancellationToken).ConfigureAwait(false);
            return (outcome.PersonsUpdated, employmentsChanged, null);
        }
        catch (LogoUnavailableException ex)
        {
            LogDetailsStepFailed(_logger, ex);
            return (0, 0, SyncFailureReason.SourceUnavailable);
        }
        catch (SyncPersistenceException ex)
        {
            LogDetailsStepFailed(_logger, ex);
            return (0, 0, SyncFailureReason.PersistenceFailed);
        }
#pragma warning disable CA1031 // Ilk adim kaydedildi; T1 adimindaki hata calismayi "kismen" kapatir.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogDetailsStepFailed(_logger, ex);
            return (0, 0, SyncFailureReason.Unexpected);
        }
    }

    /// <summary>
    /// Fotograf adimi (SYG-PERS-014, ADR-0021 §5): ozetler okunur, yalnizca degisen goruntuler
    /// LOGO'dan alinir, kucultulur ve saklanir. Hata firlatmaz; basarisizligi nedeniyle doner.
    /// </summary>
    /// <returns>Eklenen, degisen veya kaldirilan fotograf sayisi.</returns>
    private async Task<(int Changed, SyncFailureReason? Failure)> RunPhotoStepAsync(
        IPersonnelSyncSession session,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await session.BeginNextStepAsync(cancellationToken).ConfigureAwait(false))
            {
                LogPhotoStepYielded(_logger);
                return (0, null);
            }

            var digests = await _source.GetPhotoDigestsAsync(cancellationToken).ConfigureAwait(false);
            var snapshot = await session.LoadPhotosAsync(cancellationToken).ConfigureAwait(false);
            var plan = PersonnelPhotoSynchronizer.Plan(digests, snapshot, _thumbnailer.Version);
            session.RemovePhotos(plan.ToRemove);

            var changed = plan.ToRemove.Count;
            var undecodable = 0;

            // Ilk aktarimda yaklasik 1.000 goruntu (ort. 280 KB) okunur; bellek ve LOGO sorgusu
            // sinirli kalsin diye parca parca alinir ve her parca hemen veritabanina yazilir.
            foreach (var batch in plan.ToFetch.Keys.Chunk(PhotoBatchSize))
            {
                var photos = await _source.GetPhotosAsync(batch, cancellationToken).ConfigureAwait(false);
                var outcome = PersonnelPhotoSynchronizer.Apply(photos, plan, snapshot, _thumbnailer);
                session.AddPhotos(outcome.Added);
                await session.SaveAsync(cancellationToken).ConfigureAwait(false);

                changed += outcome.Changed;
                undecodable += outcome.Undecodable;
            }

            await session.CommitAsync(cancellationToken).ConfigureAwait(false);

            if (undecodable > 0)
            {
                LogUndecodablePhotos(_logger, undecodable);
            }

            return (changed, null);
        }
        catch (LogoUnavailableException ex)
        {
            LogPhotoStepFailed(_logger, ex);
            return (0, SyncFailureReason.SourceUnavailable);
        }
        catch (SyncPersistenceException ex)
        {
            LogPhotoStepFailed(_logger, ex);
            return (0, SyncFailureReason.PersistenceFailed);
        }
#pragma warning disable CA1031 // Onceki adimlar kaydedildi; fotograf adimindaki hata calismayi "kismen" kapatir.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogPhotoStepFailed(_logger, ex);
            return (0, SyncFailureReason.Unexpected);
        }
    }

    /// <summary>Bir LOGO sorgusunda okunan en fazla goruntu sayisi (yaklasik 14 MB).</summary>
    internal const int PhotoBatchSize = 50;

    [LoggerMessage(EventId = 3110, Level = LogLevel.Error,
        Message = "Senkronizasyonun fotograf adimi basarisiz; onceki adimlar kaydedildi, calisma kismen tamamlandi.")]
    private static partial void LogPhotoStepFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3111, Level = LogLevel.Information,
        Message = "Senkronizasyonun fotograf adimi atlandi: baska bir calisma kilidi aldi ve adimi o yapacak.")]
    private static partial void LogPhotoStepYielded(ILogger logger);

    [LoggerMessage(EventId = 3112, Level = LogLevel.Warning,
        Message = "LOGO'daki {Count} fotograf cozulemedi ve aktarilmadi; sonraki calismada yeniden denenecek.")]
    private static partial void LogUndecodablePhotos(ILogger logger, int count);

    [LoggerMessage(EventId = 3108, Level = LogLevel.Error,
        Message = "Senkronizasyonun T1 adimi basarisiz; kisi ve istihdam degisiklikleri kaydedildi, calisma kismen tamamlandi.")]
    private static partial void LogDetailsStepFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3109, Level = LogLevel.Information,
        Message = "Senkronizasyonun T1 adimi atlandi: baska bir calisma kilidi aldi ve adimi o yapacak.")]
    private static partial void LogDetailsStepYielded(ILogger logger);

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
