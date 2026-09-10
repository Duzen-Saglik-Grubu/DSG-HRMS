using Dsg.Hrms.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// Denetim izi tablosunun eslemesi (ADR-0009 §2).
/// </summary>
public sealed class ChangeLogEntryConfiguration : IEntityTypeConfiguration<ChangeLogEntry>
{
    /// <summary>Denetim kayitlarinin tutuldugu sema.</summary>
    public const string AuditSchema = "audit";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ChangeLogEntry> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Denetim kayitlari AYRI bir semada tutulur: is tablolarindan farkli bir
        // saklama suresi, farkli yetki ve farkli yedekleme politikasi vardir.
        builder.ToTable("change_log", AuditSchema);

        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.EntityName).HasMaxLength(200).IsRequired();
        builder.Property(entry => entry.TraceId).HasMaxLength(64);
        builder.Property(entry => entry.IpAddress).HasMaxLength(45);

        // Islem turu metin olarak saklanir: veritabanini dogrudan sorgulayan bir
        // denetci, sayisal kod yerine "Delete" gormelidir.
        builder.Property(entry => entry.Operation)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // jsonb: alan bazli sorgu ve dizin mumkun olur (json tipinde degildir).
        builder.Property(entry => entry.Changes)
            .HasColumnType("jsonb")
            .IsRequired();

        // En sik sorulan soru: "bu kaydin gecmisi nedir?"
        builder.HasIndex(entry => new { entry.EntityName, entry.EntityId });

        // Ikinci soru: "bu kullanici bugun ne yapti?"
        builder.HasIndex(entry => new { entry.UserAccountId, entry.OccurredAt });

        // Ucuncu soru: "bu istekte neler degisti?" — uygulama gunlugundeki
        // CorrelationId ile denetim izini birlestirir.
        builder.HasIndex(entry => entry.TraceId);
    }
}
