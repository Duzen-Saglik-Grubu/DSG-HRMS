using Dsg.Hrms.Domain.Common;

namespace Dsg.Hrms.Domain.Notifications;

/// <summary>
/// Uygulama ici bildirim: bir kullanicinin bildirim kutusundaki tek kayit (ADR-0022 §3;
/// SYG-PERS-046). ADR-0012 §4'teki bildirim cekirdeginin en kucuk halidir.
/// </summary>
/// <remarks>
/// Kayit kisisel veri tasimaz: yalnizca turu ve ilgili kaydin dis kimligini bilir. Gosterilecek
/// metni ekran turden uretir; boylece bildirim kutusu, ilgili kaydi gorme yetkisi olmayan birine
/// icerik sizdirmaz. Denetim izine girmez (<see cref="NotificationDelivery"/> gibi kendisi bir
/// kayittir).
/// </remarks>
public sealed class InboxItem : Entity
{
    private InboxItem()
    {
    }

    /// <summary>Alici kullanici hesabinin ic kimligi.</summary>
    public long RecipientUserId { get; private set; }

    /// <summary>Tur.</summary>
    public InboxItemKind Kind { get; private set; }

    /// <summary>Ilgili kaydin dis kimligi (ornegin duzeltme talebi).</summary>
    public Guid SubjectId { get; private set; }

    /// <summary>Olusma ani (UTC).</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Okunma ani (UTC); okunmadiysa <c>null</c>.</summary>
    public DateTimeOffset? ReadAt { get; private set; }

    /// <summary>Bildirim olusturur.</summary>
    public static InboxItem For(long recipientUserId, InboxItemKind kind, Guid subjectId, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(recipientUserId);

        return new InboxItem { RecipientUserId = recipientUserId, Kind = kind, SubjectId = subjectId, CreatedAt = now };
    }

    /// <summary>Okundu olarak isaretler; zaten okunduysa ilk okunma ani korunur.</summary>
    public void MarkRead(DateTimeOffset now) => ReadAt ??= now;
}

/// <summary>Uygulama ici bildirimin turu.</summary>
public enum InboxItemKind
{
    /// <summary>Yeni "IK'ya bildir" duzeltme talebi (SYG-PERS-046).</summary>
    CorrectionRequested = 1,

    /// <summary>Senkronizasyon basarisiz oldu (SYG-PERS-012); ilgili kayit calismadir.</summary>
    SyncFailed = 2,

    /// <summary>LOGO semasinda sapma bulundu, senkronizasyon yapilmadi (SYG-PERS-012, PG-PERS-08).</summary>
    SyncSchemaDrift = 3,
}
