using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Notifications;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Notifications;
using Dsg.Hrms.Domain.Personnel.Sync;
using Microsoft.Extensions.Logging;

namespace Dsg.Hrms.Application.Personnel.Sync;

/// <summary>Basarisiz senkronizasyonu uygulama ici bildirim ve e-postayla haber verir (SYG-PERS-012).</summary>
public interface IPersonnelSyncAlerts
{
    /// <summary>
    /// Calisma basarisiz kapandiysa bildirim yazar ve e-posta gonderir; aksi halde hicbir sey
    /// yapmaz. Hata firlatmaz: uyarinin iletilememesi calismanin sonucunu degistirmez.
    /// </summary>
    Task NotifyAsync(PersonnelSyncRun run, CancellationToken cancellationToken);
}

/// <summary>Uyarinin alicisi.</summary>
/// <param name="UserId">Kullanici hesabinin ic kimligi.</param>
/// <param name="PersonId">Hesabin bagli oldugu kisi.</param>
/// <param name="Email">Kisinin LOGO'daki kurumsal e-posta adresi; yoksa <c>null</c>.</param>
public sealed record SyncAlertRecipient(long UserId, long PersonId, string? Email);

/// <summary>Senkronizasyon uyarilarinin veritabani tarafi.</summary>
public interface IPersonnelSyncAlertStore
{
    /// <summary>
    /// Rollerden birine sahip aktif kullanicilardan, verilen turde okunmamis bildirimi
    /// OLMAYANLAR (SYG-PERS-012: ayni sorun icin acik bildirim varken yenisi yazilmaz).
    /// </summary>
    Task<IReadOnlyList<SyncAlertRecipient>> ListRecipientsAsync(IReadOnlyCollection<string> roleCodes, InboxItemKind kind, CancellationToken cancellationToken);

    /// <summary>Bildirimleri ekler ve kaydeder.</summary>
    Task AddAsync(IReadOnlyCollection<InboxItem> items, CancellationToken cancellationToken);
}

/// <summary>Uyari e-postasinin ayarlari (KR-134).</summary>
/// <param name="SyncPage">
/// Senkronizasyon ekraninin tam adresi (<c>Identity:PublicBaseUrl</c> + <see cref="SyncPagePath"/>);
/// tanimli degilse <c>null</c> ve e-posta baglanti yerine ekranin menudeki yerini yazar.
/// </param>
public sealed record PersonnelSyncAlertOptions(Uri? SyncPage)
{
    /// <summary>Senkronizasyon ekraninin yolu (on yuz <c>routes.syncRuns</c>).</summary>
    public const string SyncPagePath = "/personnel/sync";
}

/// <summary>
/// Sema sapmasi ve basarisiz senkronizasyon uyarisi (SYG-PERS-012; PG-PERS-08, AN-06; ADR-0022 §3;
/// KR-133, KR-134).
/// </summary>
/// <remarks>
/// <para>
/// Alici tek kisi degil roldur (<c>PRM-BLD-01</c>). Her aliciya uygulama ici bildirim yazilir ve
/// kurumsal adresi varsa e-posta gonderilir. Ikisi de kisisel veri tasimaz: bildirim turu ve
/// calismanin kimligini, e-posta nedeni ve senkronizasyon ekranina baglantiyi icerir.
/// </para>
/// <para>
/// "Ayni sorun" bildirimin turudur: sema sapmasi bir tur, diger nedenler baska bir tur. Kullanicinin
/// o turde okunmamis bildirimi varken ne bildirim ne e-posta yazilir; LOGO saatlerce kapaliyken
/// her calismada uyari birikmez. Kullanici bildirimi okuduktan sonraki yeni hata yine bildirilir.
/// </para>
/// <para>
/// Kismen tamamlanan ve uyarili calismalar uyari uretmez: veri guncellenmistir, ayrinti
/// senkronizasyon ekranindadir.
/// </para>
/// </remarks>
public sealed partial class PersonnelSyncAlerts : IPersonnelSyncAlerts
{
    /// <summary>Sema sapmasi e-postasinin sabit konusu.</summary>
    public const string SchemaDriftSubject = "HRMS: LOGO şemasında sapma";

    /// <summary>Basarisiz senkronizasyon e-postasinin sabit konusu.</summary>
    public const string FailedSubject = "HRMS: Senkronizasyon başarısız";

    private readonly IPersonnelSyncAlertStore _store;
    private readonly ISystemParameters _parameters;
    private readonly INotificationDispatch _dispatch;
    private readonly PersonnelSyncAlertOptions _options;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<PersonnelSyncAlerts> _logger;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonnelSyncAlerts(
        IPersonnelSyncAlertStore store,
        ISystemParameters parameters,
        INotificationDispatch dispatch,
        PersonnelSyncAlertOptions options,
        IDateTimeProvider clock,
        ILogger<PersonnelSyncAlerts> logger)
    {
        _store = store;
        _parameters = parameters;
        _dispatch = dispatch;
        _options = options;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>Calismanin bildirim turu; uyari gerekmiyorsa <c>null</c>.</summary>
    public static InboxItemKind? KindOf(PersonnelSyncRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        if (run.Status != SyncStatus.Failed)
        {
            return null;
        }

        return run.FailureReason == SyncFailureReason.SchemaDrift ? InboxItemKind.SyncSchemaDrift : InboxItemKind.SyncFailed;
    }

    /// <summary>
    /// E-posta govdesi: neden ve senkronizasyon ekrani. Kisi adi, sicil veya LOGO verisi YAZILMAZ.
    /// </summary>
    public static string EmailBody(SyncFailureReason? reason, Uri? syncPage)
    {
        var where = syncPage is null
            ? "Ayrıntıyı HRMS'te Personel → Senkronizasyon ekranında görebilirsiniz."
            : $"Ayrıntıyı görmek için:\n{syncPage.AbsoluteUri}";

        return "Merhaba,\n\n" +
               $"LOGO personel senkronizasyonu başarısız oldu: {ReasonText(reason)}.\n" +
               "HRMS son başarılı senkronizasyondaki verilerle çalışmaya devam ediyor.\n\n" +
               $"{where}\n\n" +
               "Sorun sürerse yeni bir e-posta gönderilmez; bildirimi HRMS'te okuduktan sonraki hata yeniden bildirilir.\n\n" +
               "Bu e-posta otomatik gönderilmiştir; yanıtlamayın.";
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
            await _store.AddAsync([.. recipients.Select(r => InboxItem.For(r.UserId, kind, run.PublicId, now))], cancellationToken)
                .ConfigureAwait(false);

            var emailed = SendEmails(run, kind, recipients, now);
            LogNotified(_logger, kind, recipients.Count, emailed);
        }
#pragma warning disable CA1031 // Uyarinin iletilememesi calismanin sonucunu degistirmez; hata gunluge yazilir.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogFailed(_logger, ex, kind);
        }
    }

    private int SendEmails(PersonnelSyncRun run, InboxItemKind kind, IReadOnlyList<SyncAlertRecipient> recipients, DateTimeOffset now)
    {
        var subject = kind == InboxItemKind.SyncSchemaDrift ? SchemaDriftSubject : FailedSubject;
        var body = EmailBody(run.FailureReason, _options.SyncPage);
        var sent = 0;

        // Kuyruk doluysa yalnizca e-posta kaybolur; uygulama ici bildirim kayitlidir.
        foreach (var recipient in recipients.Where(r => !string.IsNullOrWhiteSpace(r.Email)))
        {
            if (_dispatch.TryEnqueue(new OutboundMessage(
                    NotificationChannel.Email, NotificationPurpose.SyncAlert, recipient.PersonId, recipient.Email!, subject, body, now)))
            {
                sent++;
            }
        }

        return sent;
    }

    private static string ReasonText(SyncFailureReason? reason) => reason switch
    {
        SyncFailureReason.SourceUnavailable => "LOGO'ya bağlanılamadı",
        SyncFailureReason.SourceWritable => "LOGO kullanıcısında yazma yetkisi var; güvenlik gereği çalışılmadı",
        SyncFailureReason.SchemaDrift => "LOGO tablolarının yapısı beklenenden farklı",
        SyncFailureReason.PersistenceFailed => "veriler HRMS'e kaydedilemedi",
        _ => "beklenmeyen bir hata oluştu",
    };

    [LoggerMessage(EventId = 3120, Level = LogLevel.Information,
        Message = "Senkronizasyon uyarisi ({Kind}) {Count} kullaniciya bildirildi; {Emailed} e-posta kuyruga alindi.")]
    private static partial void LogNotified(ILogger logger, InboxItemKind kind, int count, int emailed);

    [LoggerMessage(EventId = 3121, Level = LogLevel.Information,
        Message = "Senkronizasyon uyarisi ({Kind}) icin yeni alici yok: rolde aktif kullanici yok veya hepsinde okunmamis bildirim var.")]
    private static partial void LogNoRecipients(ILogger logger, InboxItemKind kind);

    [LoggerMessage(EventId = 3122, Level = LogLevel.Error,
        Message = "Senkronizasyon uyarisi ({Kind}) iletilemedi; calisma kaydi etkilenmedi.")]
    private static partial void LogFailed(ILogger logger, Exception exception, InboxItemKind kind);
}
