using Dsg.Hrms.Domain.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// Firma tablosunun eslemesi (ADR-0005 §5).
/// </summary>
public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    /// <summary>Organizasyon verisinin semasi (ADR-0004 §1).</summary>
    public const string OrganizationSchema = "organization";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("company", OrganizationSchema);
        builder.HasKey(company => company.Id);

        builder.Property(company => company.Name).IsRequired();

        // Senkronizasyonun eslestirme anahtari.
        builder.HasIndex(company => company.LogoFirmNumber).IsUnique();

        builder.MapAuditFields();
    }
}
