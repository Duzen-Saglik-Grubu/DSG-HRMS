using Dsg.Hrms.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// Sistem parametresi tablosunun eslemesi (SYG-KMLK-075).
/// </summary>
public sealed class SystemParameterConfiguration : IEntityTypeConfiguration<SystemParameter>
{
    /// <summary>Sistem yonetimi verisinin semasi (ADR-0004 §1).</summary>
    public const string SettingsSchema = "settings";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<SystemParameter> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("system_parameter", SettingsSchema, table =>
        {
            table.HasCheckConstraint("ck_system_parameter_key_format", "key ~ '^PRM-[A-Z]{3}-[0-9]{2}$'");

            // Bir satir ya acik ya sifreli deger tasir; ikisi birden OLAMAZ. Aksi halde
            // bir sirrin acik kopyasi yanlislikla tabloda kalabilirdi.
            table.HasCheckConstraint("ck_system_parameter_single_value", "value IS NULL OR protected_value IS NULL");
        });

        builder.HasKey(parameter => parameter.Id);

        builder.Property(parameter => parameter.Key).IsRequired();
        builder.HasIndex(parameter => parameter.Key).IsUnique();

        builder.MapAuditFields();
    }
}
