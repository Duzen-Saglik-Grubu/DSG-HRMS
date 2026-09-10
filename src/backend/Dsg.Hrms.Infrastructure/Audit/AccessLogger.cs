using System.Text.Json.Nodes;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Security;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Audit;

/// <summary>
/// Erisim kaydini veritabanina yazar (ADR-0009 §3).
/// </summary>
/// <remarks>
/// <para>
/// Kayit, cagiran kodun veritabani baglamindan <b>ayri</b> bir baglamla yazilir.
/// Bunun iki nedeni vardir:
/// </para>
/// <list type="number">
/// <item>
/// Goruntuleme islemi cogunlukla salt okunurdur ve acik bir islem (transaction)
/// icinde calismaz; erisim kaydini cagiranin baglamina eklemek, o baglamda bekleyen
/// baska degisiklikleri de istemeden kaydederdi.
/// </item>
/// <item>
/// Cagiran islem geri alinsa bile <b>erisim gerceklesmistir</b>: kullanici veriyi
/// gormustur. Erisim kaydinin o geri alma ile silinmesi izi yaniltici kilardi.
/// </item>
/// </list>
/// </remarks>
public sealed class AccessLogger(
    IDbContextFactory<HrmsDbContext> contextFactory,
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider) : IAccessLogger
{
    /// <inheritdoc />
    public async Task LogAsync(AccessRecord record, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(record);

        await using var context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        context.AccessLog.Add(new AccessLogEntry
        {
            OccurredAt = dateTimeProvider.UtcNow,
            UserAccountId = currentUser.UserId,
            AccessType = record.AccessType,
            EntityName = record.EntityName,
            EntityId = record.EntityId,
            RecordCount = record.RecordCount,
            Filters = MaskFilters(record.Filters),
            TraceId = currentUser.TraceId,
            IpAddress = currentUser.IpAddress,
        });

        // Hata yutulmaz: kayit yazilamadiysa cagiran veriyi sunmamalidir (fail-closed).
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Suzgec degerlerini maskeleyerek JSON'a cevirir.
    /// </summary>
    /// <remarks>
    /// Suzgec, kullanicinin yazdigi metni tasir: arama kutusuna girilen bir T.C.
    /// kimlik numarasi maskelenmeseydi erisim kaydina duz metin duserdi (KR-059).
    /// Karar, suzgec <b>adina</b> gore verilir.
    /// </remarks>
    private static string MaskFilters(IReadOnlyDictionary<string, string?>? filters)
    {
        var masked = new JsonObject();

        if (filters is null)
        {
            return masked.ToJsonString();
        }

        foreach (var (name, value) in filters)
        {
            masked[name] = MaskRules.Apply(MaskRules.ForName(name), value);
        }

        return masked.ToJsonString();
    }
}
