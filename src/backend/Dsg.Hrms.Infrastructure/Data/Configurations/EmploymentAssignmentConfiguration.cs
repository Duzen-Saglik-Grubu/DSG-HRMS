using Dsg.Hrms.Domain.Organization;
using Dsg.Hrms.Domain.Personnel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// Tarih aralikli istihdam nitelikleri (ADR-0021 §2; SYG-PERS-018, 019).
/// </summary>
/// <remarks>
/// Ayni istihdamda cakisan aralik <c>EXCLUDE USING gist</c> kisitiyla engellenir. EF bu kisiti
/// modelde ifade edemez; migration'da SQL olarak kurulur (<see cref="NoOverlapConstraint"/>).
/// </remarks>
public sealed class EmploymentAssignmentConfiguration : IEntityTypeConfiguration<EmploymentAssignment>
{
    /// <summary>Cakisma kisitinin adi; 23P01 hatasinda bu ad doner.</summary>
    public const string NoOverlapConstraint = "ex_employment_assignment_no_overlap";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EmploymentAssignment> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("employment_assignment", PersonConfiguration.PersonnelSchema, table =>
        {
            // Bos veya ters aralik olamaz (valid_to haric ust sinirdir).
            table.HasCheckConstraint("ck_employment_assignment_range", "valid_to IS NULL OR valid_to > valid_from");
        });

        builder.HasKey(assignment => assignment.Id);
        builder.Ignore(assignment => assignment.Values);
        builder.Ignore(assignment => assignment.IsOpen);

        builder.HasIndex(assignment => new { assignment.EmploymentId, assignment.ValidFrom });

        builder.HasOne<Employment>().WithMany().HasForeignKey(assignment => assignment.EmploymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Company>().WithMany().HasForeignKey(assignment => assignment.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Branch>().WithMany().HasForeignKey(assignment => assignment.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Department>().WithMany().HasForeignKey(assignment => assignment.DepartmentId).OnDelete(DeleteBehavior.Restrict);

        builder.MapAuditFields();
    }
}
