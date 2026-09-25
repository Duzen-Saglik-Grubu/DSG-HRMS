using Dsg.Hrms.Domain.Personnel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// Istihdam tablosunun eslemesi (ADR-0005 §1).
/// </summary>
public sealed class EmploymentConfiguration : IEntityTypeConfiguration<Employment>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Employment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("employment", PersonConfiguration.PersonnelSchema, table =>
        {
            // Cikis tarihi giris tarihinden once olamaz.
            table.HasCheckConstraint(
                "ck_employment_termination_after_hire",
                "termination_date IS NULL OR termination_date >= hire_date");
        });

        builder.HasKey(employment => employment.Id);

        builder.Property(employment => employment.RegistryCode).IsRequired();
        builder.Property(employment => employment.HireDate).IsRequired();

        // Senkronizasyonun eslestirme anahtari; LOGO'da veritabani genelinde tekil
        // oldugu 26.09.2026'da dogrulandi.
        builder.HasIndex(employment => employment.RegistryCode).IsUnique();

        // "Kisinin aktif istihdamlari" sorgusu (uyelik on kosulu, SYG-KMLK-013).
        builder.HasIndex(employment => new { employment.PersonId, employment.IsActive });

        builder.HasOne(employment => employment.Company)
            .WithMany()
            .HasForeignKey(employment => employment.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.MapAuditFields();
    }
}
