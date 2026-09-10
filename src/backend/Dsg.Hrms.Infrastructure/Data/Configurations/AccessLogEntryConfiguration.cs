using Dsg.Hrms.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// Erisim kaydi tablosunun eslemesi (ADR-0009 §3).
/// </summary>
public sealed class AccessLogEntryConfiguration : IEntityTypeConfiguration<AccessLogEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<AccessLogEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("access_log", ChangeLogEntryConfiguration.AuditSchema);

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.EntityName).HasMaxLength(200).IsRequired();
        builder.Property(entry => entry.TraceId).HasMaxLength(64);
        builder.Property(entry => entry.IpAddress).HasMaxLength(45);

        // Erisim turu metin olarak saklanir: veritabanini dogrudan sorgulayan bir
        // denetci sayisal kod yerine "Export" gormelidir.
        builder.Property(entry => entry.AccessType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(entry => entry.Filters)
            .HasColumnType("jsonb")
            .IsRequired();

        // "Bu kullanici bugun kimin verisine baktı?"
        builder.HasIndex(entry => new { entry.UserAccountId, entry.OccurredAt });

        // "Bu kaydi kimler goruntuledi?"
        builder.HasIndex(entry => new { entry.EntityName, entry.EntityId });

        // Sizinti incelemesinin ilk sorgusu: toplu disa aktarmalar.
        // Kismi dizin: tekil goruntulemeler dizine girmez, dizin kucuk kalir.
        builder.HasIndex(entry => new { entry.AccessType, entry.OccurredAt })
            .HasFilter("access_type IN ('Export', 'Report')");

        builder.HasIndex(entry => entry.TraceId);
    }
}
