using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Domain.Notifications;

namespace Dsg.Hrms.Application.Notifications.Inbox;

/// <summary>Bildirim kutusunun veritabani tarafi.</summary>
public interface IInboxStore
{
    /// <summary>Kullanicinin en yeni bildirimleri.</summary>
    Task<IReadOnlyList<InboxItem>> ListAsync(long userId, int take, CancellationToken cancellationToken);

    /// <summary>Kullanicinin okunmamis bildirim sayisi.</summary>
    Task<int> CountUnreadAsync(long userId, CancellationToken cancellationToken);

    /// <summary>Kullanicinin bildirimi (izlenen); baskasininsa veya yoksa <c>null</c>.</summary>
    Task<InboxItem?> FindAsync(long userId, Guid itemId, CancellationToken cancellationToken);

    /// <summary>Kaydeder.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Uygulama ici bildirim kutusu (ADR-0022 §3; SYG-PERS-046): menude okunmamis sayisi, acilinca
/// okundu ve ilgili kayda gecis.
/// </summary>
/// <remarks>
/// Kullanici yalnizca KENDI bildirimlerini gorur; baskasinin bildirimi "yok" ile ayni <c>404</c>'u
/// alir. Bildirim kisisel veri tasimaz: ilgili kaydi acmak o kaydin kendi yetkisini ister.
/// </remarks>
public sealed class InboxService
{
    /// <summary>Kutuda gosterilen en fazla bildirim.</summary>
    public const int MaxItems = 50;

    private readonly ICurrentUser _user;
    private readonly IInboxStore _store;
    private readonly IDateTimeProvider _clock;

    /// <summary>Yeni ornek olusturur.</summary>
    public InboxService(ICurrentUser user, IInboxStore store, IDateTimeProvider clock)
    {
        _user = user;
        _store = store;
        _clock = clock;
    }

    /// <summary>En yeni bildirimler, en yeni once.</summary>
    public Task<IReadOnlyList<InboxItem>> ListAsync(CancellationToken cancellationToken) =>
        _store.ListAsync(UserId, MaxItems, cancellationToken);

    /// <summary>Okunmamis bildirim sayisi.</summary>
    public Task<int> CountUnreadAsync(CancellationToken cancellationToken) =>
        _store.CountUnreadAsync(UserId, cancellationToken);

    /// <summary>Bildirimi okundu olarak isaretler.</summary>
    public async Task<InboxItem> MarkReadAsync(Guid itemId, CancellationToken cancellationToken)
    {
        var item = await _store.FindAsync(UserId, itemId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Bildirim bulunamadı.");

        item.MarkRead(_clock.UtcNow);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return item;
    }

    private long UserId => _user.UserId ?? throw new NotFoundException("Bildirim bulunamadı.");
}
