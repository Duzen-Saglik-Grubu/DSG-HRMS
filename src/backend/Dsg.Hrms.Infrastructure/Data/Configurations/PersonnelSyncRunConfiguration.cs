using Dsg.Hrms.Domain.Personnel.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// Senkronizasyon calisma kayitlarinin eslemesi (SYG-KMLK-005).
/// </summary>
public sealed class PersonnelSyncRunConfiguration : IEntityTypeConfiguration<PersonnelSyncRun>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PersonnelSyncRun> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("sync_run", PersonConfiguration.PersonnelSchema);
        builder.HasKey(run => run.Id);

        builder.HasIndex(run => run.PublicId).IsUnique();

        // Durum ve neden metin olarak saklanir: veritabanini dogrudan sorgulayan
        // biri sayisal kod yerine "Failed" / "SourceUnavailable" gormelidir.
        builder.Property(run => run.Trigger).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(run => run.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(run => run.FailureReason).HasConversion<string>().HasMaxLength(30);

        // En sik soru: "son calisma ne zaman, sonucu ne?"
        builder.HasIndex(run => run.StartedAt).IsDescending();

        builder.HasMany(run => run.Warnings)
            .WithOne()
            .HasForeignKey(warning => warning.RunId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(run => run.Warnings)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
