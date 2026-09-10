using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Security;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Dsg.Hrms.Infrastructure.Data.Interceptors;

/// <summary>
/// Is kayitlarindaki degisiklikleri denetim izine yazar (ADR-0009 §2).
/// </summary>
/// <remarks>
/// <para>
/// Denetim kaydi, degisikligi yapan islemle <b>ayni veritabani isleminde</b> yazilir:
/// is kaydi basarili olup denetim kaydi olusmayan bir durum mumkun degildir. Kayitlar
/// <c>SaveChanges</c> calismadan once degisiklik izleyiciye eklenir.
/// </para>
/// <para>
/// Kayit ara katmanda uretilir, serviste elle yazilmaz. Elle yazim yaklasimi tek bir
/// yerde unutuldugunda sessizce denetimsiz kalan bir alan birakirdi.
/// </para>
/// </remarks>
public sealed class AuditTrailInterceptor(
    ICurrentUser currentUser,
    IDateTimeProvider dateTimeProvider) : SaveChangesInterceptor
{
    private static readonly HashSet<string> AuditFieldNames = new(StringComparer.Ordinal)
    {
        nameof(IAuditable.CreatedAt),
        nameof(IAuditable.CreatedBy),
        nameof(IAuditable.UpdatedAt),
        nameof(IAuditable.UpdatedBy),
    };

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        WriteAuditTrail(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        WriteAuditTrail(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void WriteAuditTrail(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        GuardAgainstAuditTampering(context);

        var occurredAt = dateTimeProvider.UtcNow;
        var userId = currentUser.UserId;
        var traceId = currentUser.TraceId;
        var ipAddress = currentUser.IpAddress;

        // Once toplanir, sonra eklenir: koleksiyon uzerinde gezinirken degisiklik
        // izleyiciye yeni varlik eklemek gezinmeyi bozar.
        var entries = context.ChangeTracker
            .Entries()
            .Where(entry => entry.Entity is Entity)
            .Select(entry => TryCreateEntry(entry, occurredAt, userId, traceId, ipAddress))
            .OfType<ChangeLogEntry>()
            .ToList();

        if (entries.Count > 0)
        {
            context.Set<ChangeLogEntry>().AddRange(entries);
        }
    }

    /// <summary>
    /// Denetim kaydinin uygulama uzerinden degistirilmesini engeller.
    /// </summary>
    /// <remarks>
    /// Veritabani tetikleyicisi son savunma hattidir; buradaki denetim ayni hatayi
    /// veritabanina gitmeden, anlasilir bir mesajla yakalar.
    /// </remarks>
    private static void GuardAgainstAuditTampering(DbContext context)
    {
        var tampered = context.ChangeTracker
            .Entries<ChangeLogEntry>()
            .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted);

        if (tampered)
        {
            throw new InvalidOperationException(
                "Denetim izi kayitlari degistirilemez ve silinemez (ADR-0009 §2). " +
                "Bu tabloya yalnizca ekleme yapilabilir.");
        }
    }

    private static ChangeLogEntry? TryCreateEntry(
        EntityEntry entry,
        DateTimeOffset occurredAt,
        long? userId,
        string? traceId,
        string? ipAddress)
    {
        var operation = ResolveOperation(entry);

        if (operation is null)
        {
            return null;
        }

        var changes = BuildChanges(entry, operation.Value);

        // Yalnizca denetim alanlari degismisse anlamli bir is degisikligi yoktur;
        // denetim izinde gurultu uretmeyiz.
        if (operation == AuditOperation.Update && changes.Count == 0)
        {
            return null;
        }

        return new ChangeLogEntry
        {
            OccurredAt = occurredAt,
            UserAccountId = userId,
            EntityName = entry.Metadata.ClrType.Name,
            EntityId = ((Entity)entry.Entity).PublicId,
            Operation = operation.Value,
            Changes = changes.ToJsonString(),
            TraceId = traceId,
            IpAddress = ipAddress,
        };
    }

    /// <summary>
    /// Varlik durumunu is anlamindaki islem turune cevirir.
    /// </summary>
    /// <remarks>
    /// Yumusak silme, veritabani acisindan bir guncellemedir (ADR-0004 §5). Denetim
    /// izini okuyan kisi bunu <b>silme</b> olarak gormelidir; aksi hâlde "kaydi kim
    /// sildi" sorusu denetim izinden cevaplanamazdi.
    /// </remarks>
    private static AuditOperation? ResolveOperation(EntityEntry entry) => entry.State switch
    {
        EntityState.Added => AuditOperation.Insert,
        EntityState.Deleted => AuditOperation.Delete,
        EntityState.Modified when IsSoftDeletion(entry) => AuditOperation.Delete,
        EntityState.Modified => AuditOperation.Update,
        _ => null,
    };

    private static bool IsSoftDeletion(EntityEntry entry)
    {
        if (entry.Entity is not ISoftDeletable)
        {
            return false;
        }

        var property = entry.Property(nameof(ISoftDeletable.DeletedAt));

        return property.IsModified
            && property.OriginalValue is null
            && property.CurrentValue is not null;
    }

    /// <summary>
    /// Degisen alanlarin eski ve yeni degerlerini uretir.
    /// </summary>
    private static JsonObject BuildChanges(EntityEntry entry, AuditOperation operation)
    {
        var changes = new JsonObject();

        foreach (var property in entry.Properties)
        {
            if (ShouldSkip(property, operation))
            {
                continue;
            }

            var decision = ResolveDecision(property.Metadata);

            changes[property.Metadata.Name] = new JsonObject
            {
                ["old"] = operation == AuditOperation.Insert
                    ? null
                    : Format(property.OriginalValue, decision),
                ["new"] = operation == AuditOperation.Delete
                    ? null
                    : Format(property.CurrentValue, decision),
            };
        }

        return changes;
    }

    private static bool ShouldSkip(PropertyEntry property, AuditOperation operation)
    {
        var metadata = property.Metadata;

        // Golge ozellikler (ornegin xmin) ve birincil anahtar denetim izinde yer almaz;
        // kaydin kimligi zaten ayri bir kolonda tutulur.
        if (metadata.IsShadowProperty() || metadata.IsPrimaryKey())
        {
            return true;
        }

        // Denetim alanlari (kim, ne zaman) denetim kaydinin kendi kolonlarinda zaten
        // vardir; ikinci kez yazmak gurultu uretir.
        if (AuditFieldNames.Contains(metadata.Name))
        {
            return true;
        }

        // Guncellemede yalnizca DEGISEN alanlar yazilir; degismeyenleri de yazmak
        // denetim izini okunamaz hâle getirirdi.
        return operation == AuditOperation.Update && !property.IsModified;
    }

    /// <summary>
    /// Alanin maskeleme kararini uretir.
    /// </summary>
    /// <remarks>
    /// Denetim izi de bir kayittir ve kisisel veriyi duz metin barindirmamalidir:
    /// "neyin degistigi" bilgisi, degerin kendisini ikinci bir yerde saklamayi
    /// gerektirmez (KR-059).
    /// </remarks>
    private static MaskDecision ResolveDecision(IProperty property) =>
        property.PropertyInfo is { } propertyInfo
            ? MaskRules.For(propertyInfo)
            : MaskRules.ForName(property.Name);

    private static JsonNode? Format(object? value, MaskDecision decision)
    {
        if (value is null)
        {
            return null;
        }

        // Maskeleme gerektirmeyen degerler kendi turlerinde yazilir; boylece denetim
        // izi uzerinde sayisal ve mantiksal sorgu yapilabilir.
        if (decision.Action == MaskAction.None)
        {
            return JsonSerializer.SerializeToNode(value, value.GetType());
        }

        return MaskRules.Apply(decision, Convert.ToString(value, CultureInfo.InvariantCulture));
    }
}
