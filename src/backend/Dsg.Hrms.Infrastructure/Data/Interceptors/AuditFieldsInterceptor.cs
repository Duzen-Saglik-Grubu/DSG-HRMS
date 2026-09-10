using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dsg.Hrms.Infrastructure.Data.Interceptors;

/// <summary>
/// Denetim ve yumusak silme alanlarini <b>otomatik</b> doldurur (ADR-0004 §4, §5).
/// </summary>
/// <remarks>
/// <para>
/// Bu alanlarin serviste elle doldurulmasi, tek bir yerde unutulunca sessizce
/// bozulan bir denetim izi uretirdi. Merkezi ara katman bu riski ortadan kaldirir.
/// </para>
/// <para>
/// Silme istegi burada <b>guncellemeye donusturulur</b>: EF Core'a "sil" denilse
/// bile satir fiziksel olarak silinmez, <c>DeletedAt</c> isaretlenir.
/// </para>
/// </remarks>
public sealed class AuditFieldsInterceptor(
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        ApplyAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        ApplyAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditFields(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = dateTimeProvider.UtcNow;
        var userId = currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            ConvertDeleteToSoftDelete(entry, now, userId);
            FillAuditFields(entry, now, userId);
        }
    }

    /// <summary>
    /// Fiziksel silme istegini yumusak silmeye cevirir (ADR-0004 §5).
    /// </summary>
    private static void ConvertDeleteToSoftDelete(
        EntityEntry entry,
        DateTimeOffset now,
        long? userId)
    {
        if (entry is { State: EntityState.Deleted, Entity: ISoftDeletable softDeletable })
        {
            entry.State = EntityState.Modified;
            softDeletable.DeletedAt = now;
            softDeletable.DeletedBy = userId;
        }
    }

    private static void FillAuditFields(
        EntityEntry entry,
        DateTimeOffset now,
        long? userId)
    {
        if (entry.Entity is not IAuditable auditable)
        {
            return;
        }

        switch (entry.State)
        {
            case EntityState.Added:
                auditable.CreatedAt = now;
                auditable.CreatedBy = userId;
                break;

            case EntityState.Modified:
                auditable.UpdatedAt = now;
                auditable.UpdatedBy = userId;

                // Olusturma bilgisi degistirilemez: denetim izinin guvenilirligi
                // bunun degismezligine dayanir.
                entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
                entry.Property(nameof(IAuditable.CreatedBy)).IsModified = false;
                break;

            default:
                break;
        }
    }
}
