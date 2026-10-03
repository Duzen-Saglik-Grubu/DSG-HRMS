using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Infrastructure.Data;

namespace Dsg.Hrms.Infrastructure.Audit;

/// <summary>
/// Kimlik olaylarini istegin veritabani baglamina ekler (SYG-KMLK-060).
/// </summary>
/// <remarks>
/// Erisim kaydinin (<see cref="AccessLogger"/>) aksine AYRI baglam kullanilmaz: kimlik olayi
/// isin kendisiyle ayni islemde yazilmalidir. Ornegin kilitlenme olayi, kilidin kaydedildigi
/// islemle birlikte yazilir; islem geri alinirsa ikisi de yazilmaz.
/// </remarks>
public sealed class SecurityEventLog(
    HrmsDbContext context,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider) : ISecurityEventLog
{
    /// <inheritdoc />
    public void Record(SecurityEventType type, long? userAccountId, long? personId, string? detail = null) =>
        context.SecurityEvents.Add(Create(type, userAccountId, personId, detail, currentUser, dateTimeProvider.UtcNow));

    /// <summary>Kaydi olusturur; istek baglamindan aktor, IP ve izleme kimligini alir.</summary>
    internal static SecurityEventEntry Create(
        SecurityEventType type,
        long? userAccountId,
        long? personId,
        string? detail,
        ICurrentUser currentUser,
        DateTimeOffset now) => new()
        {
            OccurredAt = now,
            EventType = type,
            UserAccountId = userAccountId,
            PersonId = personId,
            ActorUserAccountId = currentUser.UserId,
            Detail = detail,
            TraceId = currentUser.TraceId,
            IpAddress = currentUser.IpAddress,
        };
}
