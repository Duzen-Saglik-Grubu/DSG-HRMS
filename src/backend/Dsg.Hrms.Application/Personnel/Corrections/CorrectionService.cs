using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Common.Paging;
using Dsg.Hrms.Application.Notifications;
using Dsg.Hrms.Application.Personnel.Access;
using Dsg.Hrms.Application.Personnel.Cards;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Notifications;
using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Application.Personnel.Corrections;

/// <summary>Duzeltme taleplerinin veritabani tarafi.</summary>
public interface ICorrectionStore
{
    /// <summary>Kisinin acik ("Yeni") taleplerinin alanlari.</summary>
    Task<IReadOnlyList<CorrectionField>> ListOpenFieldsAsync(long personId, CancellationToken cancellationToken);

    /// <summary>IK rolundeki aktif hesaplarin ic kimlikleri (SYG-PERS-046).</summary>
    Task<IReadOnlyList<long>> ListHrRecipientsAsync(CancellationToken cancellationToken);

    /// <summary>Kan grubu secenekleri: kod ve ad (referans veri, <c>KR-129</c>).</summary>
    Task<IReadOnlyList<CorrectionOption>> ListBloodGroupsAsync(CancellationToken cancellationToken);

    /// <summary>Talebi ekler; kan grubunun degerini sifreler (ADR-0020 §1, SYG-PERS-049).</summary>
    void Add(CorrectionRequest request);

    /// <summary>Uygulama ici bildirimleri ekler; talep ile ayni islemde kaydedilir.</summary>
    void Add(IEnumerable<InboxItem> items);

    /// <summary>Talep (izlenen); yoksa <c>null</c>.</summary>
    Task<CorrectionRequest?> FindAsync(Guid requestId, CancellationToken cancellationToken);

    /// <summary>
    /// Talepler, en yeni once. Onerilen deger cozulmus ve kan grubunda adiyla doner.
    /// </summary>
    Task<PagedResult<CorrectionRecord>> ListAsync(CorrectionStatus? status, int page, int pageSize, CancellationToken cancellationToken);

    /// <summary>Tek talep; yoksa <c>null</c>.</summary>
    Task<CorrectionRecord?> GetAsync(Guid requestId, CancellationToken cancellationToken);

    /// <summary>
    /// Kaydeder. Ayni alan icin ikinci acik talep (es zamanli iki gonderim) veritabaninin tekillik
    /// kisitina takilir ve <see cref="ConflictException"/> olur (AN-08).
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Secim listesi ogesi.</summary>
/// <param name="Value">Kanonik deger.</param>
/// <param name="Label">Gorunen ad.</param>
public sealed record CorrectionOption(string Value, string Label);

/// <summary>IK'nin gordugu talep.</summary>
/// <param name="Id">Talebin dis kimligi.</param>
/// <param name="PersonId">Kisinin dis kimligi.</param>
/// <param name="FirstName">Ad.</param>
/// <param name="LastName">Soyad.</param>
/// <param name="Field">Alan.</param>
/// <param name="ProposedValue">Onerilen deger (kan grubunda adi); kapali talepte <c>null</c>.</param>
/// <param name="Status">Durum.</param>
/// <param name="SubmittedAt">Gonderim ani.</param>
/// <param name="CompletedAt">Kapatilma ani.</param>
public sealed record CorrectionRecord(
    Guid Id,
    Guid PersonId,
    string FirstName,
    string LastName,
    CorrectionField Field,
    string? ProposedValue,
    CorrectionStatus Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? CompletedAt);

/// <summary>Kisinin "IK'ya bildir" formu icin durumu.</summary>
/// <param name="OpenFields">Acik talebi olan alanlar; bunlar icin yeni talep acilamaz.</param>
/// <param name="BloodGroups">Kan grubu secenekleri.</param>
public sealed record OwnCorrectionState(IReadOnlyList<CorrectionField> OpenFields, IReadOnlyList<CorrectionOption> BloodGroups);

/// <summary>
/// "IK'ya bildir" (PG-PERS-37; ADR-0022 §3; SYG-PERS-045…049).
/// </summary>
/// <remarks>
/// <para>
/// Talebi yalnizca kisinin <b>kendisi</b> acar: kisi oturumdaki kullanicidan gelir, istekte
/// kisi kimligi yoktur. Boylece baskasi adina talep acmanin bir yolu yoktur.
/// </para>
/// <para>
/// Talep ve IK'daki herkese giden uygulama ici bildirim ayni islemde yazilir. E-posta kaydin
/// ardindan bellek ici kuyruga alinir; kuyruk dolu veya kayipsa yalnizca e-posta kaybolur, IK
/// talebi uygulamada yine gorur (ADR-0012 §4 notu).
/// </para>
/// </remarks>
public sealed class CorrectionService
{
    /// <summary>Erisim kaydindaki varlik adi.</summary>
    public const string EntityName = "CorrectionRequest";

    /// <summary>E-postanin sabit konusu (SYG-PERS-047): kisinin adi ve alan yazilmaz.</summary>
    public const string NoticeSubject = "Yeni düzeltme talebi";

    private readonly ICurrentUserScope _scope;
    private readonly ICurrentUser _user;
    private readonly ICorrectionStore _store;
    private readonly ISystemParameters _parameters;
    private readonly INotificationDispatch _dispatch;
    private readonly IAccessLogger _accessLogger;
    private readonly IDateTimeProvider _clock;

    /// <summary>Yeni ornek olusturur.</summary>
    public CorrectionService(
        ICurrentUserScope scope,
        ICurrentUser user,
        ICorrectionStore store,
        ISystemParameters parameters,
        INotificationDispatch dispatch,
        IAccessLogger accessLogger,
        IDateTimeProvider clock)
    {
        _scope = scope;
        _user = user;
        _store = store;
        _parameters = parameters;
        _dispatch = dispatch;
        _accessLogger = accessLogger;
        _clock = clock;
    }

    /// <summary>Kisinin acik talepleri ve formun secenekleri.</summary>
    public async Task<OwnCorrectionState> GetOwnStateAsync(CancellationToken cancellationToken)
    {
        var personId = await OwnPersonIdAsync(cancellationToken).ConfigureAwait(false);

        return new OwnCorrectionState(
            await _store.ListOpenFieldsAsync(personId, cancellationToken).ConfigureAwait(false),
            await _store.ListBloodGroupsAsync(cancellationToken).ConfigureAwait(false));
    }

    /// <summary>
    /// Kisinin kendi adina talep acar (SYG-PERS-045). Deger alana gore onceden dogrulanmis ve
    /// <see cref="CorrectionValue.Normalize"/> ile kanonik bicime getirilmis olmalidir.
    /// </summary>
    /// <param name="field">Alan.</param>
    /// <param name="value">Kanonik deger.</param>
    /// <param name="requestsPage">IK'nin talepler sayfasinin adresi; e-postadaki baglanti bunu kullanir.</param>
    /// <param name="cancellationToken">Iptal.</param>
    public async Task<Guid> SubmitOwnAsync(CorrectionField field, string value, Uri requestsPage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(requestsPage);

        var personId = await OwnPersonIdAsync(cancellationToken).ConfigureAwait(false);
        if ((await _store.ListOpenFieldsAsync(personId, cancellationToken).ConfigureAwait(false)).Contains(field))
        {
            throw new ConflictException("Bu alan için gönderdiğiniz bir talep zaten İK'da bekliyor.");
        }

        var now = _clock.UtcNow;
        var request = CorrectionRequest.Submit(personId, field, value, now);
        _store.Add(request);

        var recipients = await _store.ListHrRecipientsAsync(cancellationToken).ConfigureAwait(false);
        _store.Add(recipients.Select(userId => InboxItem.For(userId, InboxItemKind.CorrectionRequested, request.PublicId, now)));

        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        await NotifyByEmailAsync(request.PublicId, requestsPage, now, cancellationToken).ConfigureAwait(false);
        return request.PublicId;
    }

    /// <summary>
    /// Talepleri listeler (SYG-PERS-048; <c>personnel.correction.view</c>). Onerilen kan grubu
    /// gorunen her talep, ilgili kisi icin ozel nitelikli veri erisim kaydi uretir (SYG-PERS-049).
    /// </summary>
    public async Task<PagedResult<CorrectionRecord>> ListAsync(CorrectionStatus? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (page < 1 || pageSize < 1 || pageSize > PagedResult<CorrectionRecord>.MaxPageSize)
        {
            throw new InvalidRequestException($"Sayfa 1 veya büyük, sayfa boyutu 1–{PagedResult<CorrectionRecord>.MaxPageSize} olmalıdır.");
        }

        var result = await _store.ListAsync(status, page, pageSize, cancellationToken).ConfigureAwait(false);

        await _accessLogger.LogAsync(
            AccessRecord.List(EntityName, result.Items.Count, new Dictionary<string, string?> { ["status"] = status?.ToString() }),
            cancellationToken).ConfigureAwait(false);
        await LogSpecialCategoryAsync(result.Items, cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <summary>Tek talep (bildirimden acilir); yoksa <c>404</c>.</summary>
    public async Task<CorrectionRecord> GetAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var record = await _store.GetAsync(requestId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Düzeltme talebi bulunamadı.");

        await _accessLogger.LogAsync(AccessRecord.View(EntityName, record.Id), cancellationToken).ConfigureAwait(false);
        await LogSpecialCategoryAsync([record], cancellationToken).ConfigureAwait(false);
        return record;
    }

    /// <summary>
    /// Talebi "Tamamlandi" olarak kapatir; onerilen deger silinir, kapatma denetim izine yazilir
    /// (SYG-PERS-048; <c>personnel.correction.update</c>).
    /// </summary>
    public async Task<CorrectionRecord> CompleteAsync(Guid requestId, CancellationToken cancellationToken)
    {
        var request = await _store.FindAsync(requestId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Düzeltme talebi bulunamadı.");

        if (request.Status == CorrectionStatus.Completed)
        {
            throw new ConflictException("Talep zaten tamamlandı.");
        }

        request.Complete(_user.UserId, _clock.UtcNow);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return (await _store.GetAsync(requestId, cancellationToken).ConfigureAwait(false))!;
    }

    private async Task<long> OwnPersonIdAsync(CancellationToken cancellationToken)
    {
        var viewer = await _scope.GetAsync(cancellationToken).ConfigureAwait(false);
        return viewer.PersonId ?? throw new NotFoundException("Hesabınıza bağlı bir personel kaydı bulunamadı.");
    }

    private async Task LogSpecialCategoryAsync(IEnumerable<CorrectionRecord> records, CancellationToken cancellationToken)
    {
        foreach (var personId in records
                     .Where(r => r.Field == CorrectionField.BloodGroup && r.ProposedValue is not null)
                     .Select(r => r.PersonId)
                     .Distinct())
        {
            await _accessLogger.LogAsync(AccessRecord.SpecialCategoryView(PersonCardService.EntityName, personId), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task NotifyByEmailAsync(Guid requestId, Uri requestsPage, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var address = await _parameters.GetAsync(ParameterCatalog.CorrectionNoticeEmail, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(address))
        {
            return;
        }

        // Kuyruk doluysa yalnizca e-posta kaybolur; talep ve uygulama ici bildirim kayitlidir.
        _dispatch.TryEnqueue(new OutboundMessage(
            NotificationChannel.Email,
            NotificationPurpose.CorrectionRequested,
            null,
            address,
            NoticeSubject,
            NoticeBody(requestsPage, requestId),
            now));
    }

    /// <summary>
    /// E-posta govdesi: yalnizca talebe baglanti (SYG-PERS-047). Kisinin adi, alan ve deger
    /// YAZILMAZ; kisisel veri e-postada dolasmaz.
    /// </summary>
    public static string NoticeBody(Uri requestsPage, Guid requestId)
    {
        ArgumentNullException.ThrowIfNull(requestsPage);

        return "Merhaba,\n\n" +
               "İnsan Kaynakları Yönetim Sistemi'nde yeni bir düzeltme talebi var. Talebi görmek için:\n" +
               $"{requestsPage.AbsoluteUri.TrimEnd('/')}?request={requestId:D}\n\n" +
               "Bu e-posta otomatik gönderilmiştir; yanıtlamayın.";
    }
}
