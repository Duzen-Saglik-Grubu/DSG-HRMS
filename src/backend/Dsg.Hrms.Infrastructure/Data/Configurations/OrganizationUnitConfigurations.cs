using Dsg.Hrms.Domain.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>Sube tablosu (ADR-0021 §1, §3).</summary>
public sealed class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("branch", CompanyConfiguration.OrganizationSchema);
        builder.HasKey(branch => branch.Id);
        builder.Property(branch => branch.Name).IsRequired().UseCollation(DatabaseCollations.Turkish);
        builder.Property(branch => branch.City).UseCollation(DatabaseCollations.Turkish);

        // Senkronizasyonun eslestirme anahtari: LOGO'da numara firma icinde tekildir.
        builder.HasIndex(branch => new { branch.CompanyId, branch.LogoNumber }).IsUnique();
        builder.HasOne<Company>().WithMany().HasForeignKey(branch => branch.CompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.MapAuditFields();
    }
}

/// <summary>Birim tablosu (ADR-0021 §1, §3).</summary>
public sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("department", CompanyConfiguration.OrganizationSchema);
        builder.HasKey(department => department.Id);
        builder.Property(department => department.Name).IsRequired().UseCollation(DatabaseCollations.Turkish);

        builder.HasIndex(department => new { department.CompanyId, department.LogoNumber }).IsUnique();
        builder.HasOne<Company>().WithMany().HasForeignKey(department => department.CompanyId).OnDelete(DeleteBehavior.Restrict);

        builder.MapAuditFields();
    }
}
