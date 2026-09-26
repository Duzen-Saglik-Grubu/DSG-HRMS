using Dsg.Hrms.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// Gonderim kaydi tablosunun eslemesi (ADR-0012 §6).
/// </summary>
public sealed class NotificationDeliveryConfiguration : IEntityTypeConfiguration<NotificationDelivery>
{
    /// <summary>Bildirim verisinin semasi (ADR-0004 §1).</summary>
    public const string NotificationSchema = "notification";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("delivery", NotificationSchema);

        builder.HasKey(delivery => delivery.Id);
        builder.HasIndex(delivery => delivery.PublicId).IsUnique();

        builder.Property(delivery => delivery.RecipientMasked).IsRequired();

        // "Bu kisiye gonderilenler" ve "son basarisiz gonderimler" sorgulari (R-14).
        builder.HasIndex(delivery => new { delivery.PersonId, delivery.QueuedAt });
        builder.HasIndex(delivery => new { delivery.Status, delivery.QueuedAt });
    }
}
