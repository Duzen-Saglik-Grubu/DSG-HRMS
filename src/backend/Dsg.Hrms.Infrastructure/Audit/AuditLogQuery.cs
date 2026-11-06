using Dsg.Hrms.Application.Audit;
using Dsg.Hrms.Application.Common.Paging;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Audit;

/// <summary>
/// Denetim kayitlarini okur (SYG-PERS-069). Uc tablo (degisiklik, erisim, guvenlik olayi) ayri ayri
/// zamana gore sirali okunur ve bellekte birlestirilir (AN-25).
/// </summary>
/// <remarks>
/// <para>
/// Her kaynaktan <c>sayfa x sayfa boyutu</c> kadar en yeni satir okunur; birlesik listenin o sayfasi
/// bu satirlarin icindedir. Derinlik <see cref="AuditLogService.MaxDepth"/> ile sinirlidir. Kaynak
/// sayilari ayri sayilir ve toplanir.
/// </para>
/// <para>
/// Islemi yapan ve verisi goruntulenen kisinin adi, sayfadaki satirlar icin ikinci bir sorguyla
/// cozulur. Ayrinti kayitta nasilsa oyle doner; maske acilmaz.
/// </para>
/// </remarks>
public sealed class AuditLogQuery(HrmsDbContext context) : IAuditLogQuery
{
    private const string PersonEntity = "Person";

    /// <inheritdoc />
    public async Task<PagedResult<AuditLogItem>> ListAsync(AuditLogCriteria criteria, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        // Kisi suzgecleri: islemi yapanin hesaplari ve verisi goruntulenen kisinin kimlikleri.
        long[]? actorAccounts = null;
        if (criteria.ActorPersonId is { } actor)
        {
            actorAccounts = await (
                from a in context.Set<UserAccount>().AsNoTracking()
                join p in context.Set<Person>() on a.PersonId equals p.Id
                where p.PublicId == actor
                select a.Id).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        }

        (long Id, Guid PublicId)? subject = null;
        if (criteria.SubjectPersonId is { } subjectId)
        {
            var found = await context.Set<Person>().AsNoTracking()
                .Where(p => p.PublicId == subjectId)
                .Select(p => new { p.Id, p.PublicId })
                .SingleOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);
            subject = found is null ? (-1, subjectId) : (found.Id, found.PublicId);
        }

        var depth = criteria.Page * criteria.PageSize;
        var rows = new List<Row>();
        var total = 0;

        if (criteria.Kind is null or AuditLogKind.Change)
        {
            var changes = context.ChangeLog.AsNoTracking()
                .Where(e => (criteria.FromInstant == null || e.OccurredAt >= criteria.FromInstant) && (criteria.ToInstant == null || e.OccurredAt < criteria.ToInstant));
            if (actorAccounts is not null)
            {
                changes = changes.Where(e => e.UserAccountId != null && actorAccounts.Contains(e.UserAccountId.Value));
            }

            if (subject is { } s)
            {
                changes = changes.Where(e => e.EntityName == PersonEntity && e.EntityId == s.PublicId);
            }

            total += await changes.CountAsync(cancellationToken).ConfigureAwait(false);
            rows.AddRange((await changes
                    .OrderByDescending(e => e.OccurredAt).ThenByDescending(e => e.Id)
                    .Take(depth)
                    .Select(e => new { e.Id, e.OccurredAt, e.UserAccountId, e.EntityName, e.EntityId, e.Operation, e.Changes, e.TraceId, e.IpAddress })
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false))
                .Select(e => new Row(
                    e.Id, $"change-{e.Id}", AuditLogKind.Change, e.OccurredAt, e.Operation.ToString(), e.UserAccountId,
                    e.EntityName, e.EntityId, e.EntityName == PersonEntity ? e.EntityId : null, null, null, 0, e.Changes, e.TraceId, e.IpAddress)));
        }

        if (criteria.Kind is null or AuditLogKind.Access or AuditLogKind.Export)
        {
            var accesses = context.AccessLog.AsNoTracking()
                .Where(e => (criteria.FromInstant == null || e.OccurredAt >= criteria.FromInstant) && (criteria.ToInstant == null || e.OccurredAt < criteria.ToInstant));
            accesses = criteria.Kind switch
            {
                AuditLogKind.Access => accesses.Where(e => e.AccessType != AccessType.Export),
                AuditLogKind.Export => accesses.Where(e => e.AccessType == AccessType.Export),
                _ => accesses,
            };
            if (actorAccounts is not null)
            {
                accesses = accesses.Where(e => e.UserAccountId != null && actorAccounts.Contains(e.UserAccountId.Value));
            }

            if (subject is { } s)
            {
                accesses = accesses.Where(e => (e.EntityName == PersonEntity && e.EntityId == s.PublicId) || e.SubjectIds.Contains(s.PublicId));
            }

            total += await accesses.CountAsync(cancellationToken).ConfigureAwait(false);
            rows.AddRange((await accesses
                    .OrderByDescending(e => e.OccurredAt).ThenByDescending(e => e.Id)
                    .Take(depth)
                    .Select(e => new
                    {
                        e.Id,
                        e.OccurredAt,
                        e.UserAccountId,
                        e.AccessType,
                        e.EntityName,
                        e.EntityId,
                        e.RecordCount,
                        e.Filters,
                        SubjectCount = e.SubjectIds.Count,
                        e.TraceId,
                        e.IpAddress,
                    })
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false))
                .Select(e => new Row(
                    e.Id, $"access-{e.Id}", e.AccessType == AccessType.Export ? AuditLogKind.Export : AuditLogKind.Access, e.OccurredAt,
                    e.AccessType.ToString(), e.UserAccountId, e.EntityName, e.EntityId, e.EntityName == PersonEntity ? e.EntityId : null,
                    null, e.RecordCount, e.SubjectCount, e.Filters, e.TraceId, e.IpAddress)));
        }

        if (criteria.Kind is null or AuditLogKind.Security)
        {
            var events = context.SecurityEvents.AsNoTracking()
                .Where(e => (criteria.FromInstant == null || e.OccurredAt >= criteria.FromInstant) && (criteria.ToInstant == null || e.OccurredAt < criteria.ToInstant));
            if (actorAccounts is not null)
            {
                events = events.Where(e => e.ActorUserAccountId != null && actorAccounts.Contains(e.ActorUserAccountId.Value));
            }

            if (subject is { } s)
            {
                events = events.Where(e => e.PersonId == s.Id);
            }

            total += await events.CountAsync(cancellationToken).ConfigureAwait(false);
            rows.AddRange((await events
                    .OrderByDescending(e => e.OccurredAt).ThenByDescending(e => e.Id)
                    .Take(depth)
                    .Select(e => new { e.Id, e.OccurredAt, e.ActorUserAccountId, e.EventType, e.PersonId, e.Detail, e.TraceId, e.IpAddress })
                    .ToListAsync(cancellationToken)
                    .ConfigureAwait(false))
                .Select(e => new Row(
                    e.Id, $"security-{e.Id}", AuditLogKind.Security, e.OccurredAt, e.EventType.ToString(), e.ActorUserAccountId,
                    "UserAccount", null, null, e.PersonId, null, 0, e.Detail, e.TraceId, e.IpAddress)));
        }

        var page = rows
            .OrderByDescending(r => r.OccurredAt)
            .ThenBy(r => r.Kind)
            .ThenByDescending(r => r.Sequence)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .ToList();

        var names = await NamesAsync(page, cancellationToken).ConfigureAwait(false);
        var items = page.Select(r => new AuditLogItem(
            r.Id,
            r.Kind,
            r.OccurredAt,
            r.Action,
            r.ActorAccountId is { } account && names.ByAccount.TryGetValue(account, out var actorName) ? actorName : null,
            r.EntityName,
            r.EntityId,
            r.SubjectPublicId is { } publicId && names.ByPublicId.TryGetValue(publicId, out var subjectName) ? subjectName
                : r.SubjectPersonId is { } personId && names.ById.TryGetValue(personId, out var eventSubject) ? eventSubject
                : null,
            r.RecordCount,
            r.SubjectCount,
            r.Details,
            r.TraceId,
            r.IpAddress)).ToList();

        return new PagedResult<AuditLogItem>(items, criteria.Page, criteria.PageSize, total);
    }

    /// <summary>Sayfadaki hesap ve kisi kimliklerinin adlari.</summary>
    private async Task<Names> NamesAsync(List<Row> page, CancellationToken cancellationToken)
    {
        long[] accounts = [.. page.Where(r => r.ActorAccountId is not null).Select(r => r.ActorAccountId!.Value).Distinct()];
        Guid[] publicIds = [.. page.Where(r => r.SubjectPublicId is not null).Select(r => r.SubjectPublicId!.Value).Distinct()];
        long[] personIds = [.. page.Where(r => r.SubjectPersonId is not null).Select(r => r.SubjectPersonId!.Value).Distinct()];

        var byAccount = (await (
                from a in context.Set<UserAccount>().AsNoTracking()
                join p in context.Set<Person>() on a.PersonId equals p.Id
                where accounts.Contains(a.Id)
                select new { a.Id, p.PublicId, p.FirstName, p.LastName })
            .ToListAsync(cancellationToken).ConfigureAwait(false))
            .ToDictionary(x => x.Id, x => new AuditPerson(x.PublicId, $"{x.FirstName} {x.LastName}"));

        var persons = await context.Set<Person>().AsNoTracking()
            .Where(p => publicIds.Contains(p.PublicId) || personIds.Contains(p.Id))
            .Select(p => new { p.Id, p.PublicId, p.FirstName, p.LastName })
            .ToListAsync(cancellationToken).ConfigureAwait(false);

        return new Names(
            byAccount,
            persons.ToDictionary(p => p.PublicId, p => new AuditPerson(p.PublicId, $"{p.FirstName} {p.LastName}")),
            persons.ToDictionary(p => p.Id, p => new AuditPerson(p.PublicId, $"{p.FirstName} {p.LastName}")));
    }

    private sealed record Names(
        Dictionary<long, AuditPerson> ByAccount,
        Dictionary<Guid, AuditPerson> ByPublicId,
        Dictionary<long, AuditPerson> ById);

    private sealed record Row(
        long Sequence,
        string Id,
        AuditLogKind Kind,
        DateTimeOffset OccurredAt,
        string Action,
        long? ActorAccountId,
        string EntityName,
        Guid? EntityId,
        Guid? SubjectPublicId,
        long? SubjectPersonId,
        int? RecordCount,
        int SubjectCount,
        string? Details,
        string? TraceId,
        string? IpAddress);
}
