using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Notifications;
using Dsg.Hrms.Domain.Personnel.Sync;
using Microsoft.Extensions.Logging;

namespace Dsg.Hrms.Application.Personnel.Sync;

/// <summary>Basarisiz senkronizasyonu uygulama ici bildirimle haber verir (SYG-PERS-012).</summary>
public interface IPersonnelSyncAlerts
{
    /// <summary>
    /// Calisma basarisiz kapandiysa bildirim yazar; aksi halde hicbir sey yapmaz. Hata firlatmaz:
    /// bildirim yazilamamasi calismanin sonucunu degistirmez.
    /// </summary>
    Task NotifyAsync(PersonnelSyncRun run, CancellationToken cancellationToken);
}

/// <summary>Senkronizasyon uyarilarinin veritabani tarafi.</summary>
public interface IPersonnelSyncAlertStore
{
    /// <summary>
    /// Rollerden birine sahip aktif kullanicilardan, verilen turde okunmamis bildirimi
    /// OLMAYANLAR (SYG-PERS-012: ayni sorun icin acik bildirim varken yenisi yazilmaz).
    /// </summary>
    Task<IReadOnlyList<long>> ListRecipientsAsync(IReadOnlyCollection<string> roleCodes, InboxItemKind kind, CancellationToken cancellationToken);

    /// <summary>Bildirimleri ekler ve kaydeder.</summary>
    Task AddAsync(IReadOnlyCollection<InboxItem> items, CancellationToken cancellationToken);
}

/// <summary>
/// Sema sapmasi ve basarisiz senkronizasyon bildirimi (SYG-PERS-012; PG-PERS-08, AN-06; ADR-0022 §3).
/// </summary>
/// <remarks>
/// <para>
/// Alici tek kisi degil roldur (<c>PRM-BLD-01</c>). Bildirim kisisel veri tasimaz: turu ve
/// calismanin dis kimligini bilir; ayrintiyi senkronizasyon ekrani gosterir.
/// </para>
/// <para>
/// "Ayni sorun" bildirimin turudur: sema sapmasi bir tur, diger nedenler baska bir tur. Kullanicinin
/// o turde okunmamis bildirimi varken yenisi yazilmaz; LOGO saatlerce kapaliyken her calismada
/// bildirim birikmez. Kullanici bildirimi okuduktan sonraki yeni hata yine bildirilir.
/// </para>
/// <para>
/// Kismen tamamlanan ve uyarili calismalar bildirim uretmez: veri guncellenmistir, ayrinti
/// senkronizasyon ekranindadir.
/// </para>
/// </remarks>
public sealed partial class PersonnelSyncAlerts : IPersonnelSyncAlerts
{
    private readonly IPersonnelSyncAlertStore _store;
    private readonly ISystemParameters _parameters;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<PersonnelSyncAlerts> _logger;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonnelSyncAlerts(
        IPersonnelSyncAlertStore store,
        ISystemParameters parameters,
        IDateTimeProvider clock,
        ILogger<PersonnelSyncAlerts> logger)
    {
        _store = store;
        _parameters = parameters;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>Calismanin bildirim turu; bildirim gerekmiyorsa <c>null</c>.</summary>
    public static InboxItemKind? KindOf(PersonnelSyncRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        if (run.Status != SyncStatus.Failed)
        {
            return null;
        }

        return run.FailureReason == SyncFailureReason.SchemaDrift ? InboxItemKind.SyncSchemaDrift : InboxItemKind.SyncFailed;
    }

    /// <inheritdoc />
    public async Task NotifyAsync(PersonnelSyncRun run, CancellationToken cancellationToken)
    {
        if (KindOf(run) is not { } kind)
        {
            return;
        }

        try
        {
            var roles = await _parameters.GetListAsync(ParameterCatalog.AlertRecipientRoles, cancellationToken).ConfigureAwait(false);
            var recipients = await _store.ListRecipientsAsync(roles, kind, cancellationToken).ConfigureAwait(false);

            if (recipients.Count == 0)
            {
                LogNoRecipients(_logger, kind);
                return;
            }

            var now = _clock.UtcNow;
            await _store.AddAsync([.. recipients.Select(userId => InboxItem.For(userId, kind, run.PublicId, now))], cancellationToken)
                .ConfigureAwait(false);

            LogNotified(_logger, kind, recipients.Count);
        }
#pragma warning disable CA1031 // Bildirim yazilamamasi calismanin sonucunu degistirmez; hata gunluge yazilir.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogFailed(_logger, ex, kind);
        }
    }

    [LoggerMessage(EventId = 3120, Level = LogLevel.Information,
        Message = "Senkronizasyon uyarisi ({Kind}) {Count} kullaniciya bildirildi.")]
    private static partial void LogNotified(ILogger logger, InboxItemKind kind, int count);

    [LoggerMessage(EventId = 3121, Level = LogLevel.Information,
        Message = "Senkronizasyon uyarisi ({Kind}) icin yeni alici yok: rolde aktif kullanici yok veya hepsinde okunmamis bildirim var.")]
    private static partial void LogNoRecipients(ILogger logger, InboxItemKind kind);

    [LoggerMessage(EventId = 3122, Level = LogLevel.Error,
        Message = "Senkronizasyon uyarisi ({Kind}) bildirim olarak yazilamadi; calisma kaydi etkilenmedi.")]
    private static partial void LogFailed(ILogger logger, Exception exception, InboxItemKind kind);
}
