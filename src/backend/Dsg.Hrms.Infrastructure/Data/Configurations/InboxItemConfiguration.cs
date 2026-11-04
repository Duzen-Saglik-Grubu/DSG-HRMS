using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>Uygulama ici bildirim kutusu (ADR-0022 §3; SYG-PERS-046).</summary>
public sealed class InboxItemConfiguration : IEntityTypeConfiguration<InboxItem>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<InboxItem> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("inbox_item", NotificationDeliveryConfiguration.NotificationSchema);

        builder.HasKey(item => item.Id);
        builder.HasIndex(item => item.PublicId).IsUnique();

        // "Kullanicinin en yeni bildirimleri" ve okunmamis sayisi.
        builder.HasIndex(item => new { item.RecipientUserId, item.CreatedAt });
        builder.HasIndex(item => item.RecipientUserId).HasFilter("read_at IS NULL").HasDatabaseName("ix_inbox_item_unread");

        builder.HasOne<UserAccount>().WithMany().HasForeignKey(item => item.RecipientUserId).OnDelete(DeleteBehavior.Cascade);
    }
}
