using Dsg.Hrms.Domain.Personnel.Sync;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// Veri kalitesi uyarilarinin eslemesi (ADR-0003 §5).
/// </summary>
public sealed class PersonnelSyncWarningConfiguration : IEntityTypeConfiguration<PersonnelSyncWarning>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PersonnelSyncWarning> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("sync_warning", PersonConfiguration.PersonnelSchema);
        builder.HasKey(warning => warning.Id);

        builder.Property(warning => warning.Code).HasConversion<string>().HasMaxLength(40).IsRequired();
        builder.Property(warning => warning.RegistryCode).IsRequired();
        builder.Property(warning => warning.Detail).IsRequired();

        // Veri kalitesi raporu: "bu sicil icin hangi uyarilar var?"
        builder.HasIndex(warning => new { warning.RunId, warning.RegistryCode });
    }
}
