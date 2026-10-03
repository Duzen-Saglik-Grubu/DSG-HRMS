using Dsg.Hrms.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// Kimlik olayi tablosunun eslemesi (SYG-KMLK-060).
/// </summary>
public sealed class SecurityEventEntryConfiguration : IEntityTypeConfiguration<SecurityEventEntry>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SecurityEventEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("security_event", ChangeLogEntryConfiguration.AuditSchema);

        builder.HasKey(entry => entry.Id);

        // Olay turu metin olarak saklanir: veritabanini dogrudan sorgulayan bir denetci
        // sayisal kod yerine "SignInFailed" gormelidir.
        builder.Property(entry => entry.EventType)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(entry => entry.Detail).HasMaxLength(100);
        builder.Property(entry => entry.TraceId).HasMaxLength(64);
        builder.Property(entry => entry.IpAddress).HasMaxLength(45);

        // "Bu hesabin son olaylari": olay incelemesinin ilk sorgusu.
        builder.HasIndex(entry => new { entry.UserAccountId, entry.OccurredAt });

        // "Son bir saatte basarisiz girisler" gibi tur bazli sorgular.
        builder.HasIndex(entry => new { entry.EventType, entry.OccurredAt });

        // Bir IP'den gelen denemeler (kaba kuvvet incelemesi).
        builder.HasIndex(entry => new { entry.IpAddress, entry.OccurredAt });

        builder.HasIndex(entry => entry.TraceId);
    }
}
