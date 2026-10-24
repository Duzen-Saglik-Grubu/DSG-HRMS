using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Domain.Reference;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// Istihdamin gorev donemleri (ADR-0021 §1–§2; SYG-PERS-019, 042). Silinmemis kayitlarda cakisma
/// <c>EXCLUDE USING gist ... WHERE deleted_at IS NULL</c> ile engellenir; kisit migration'da SQL'dir.
/// </summary>
public sealed class EmploymentDutyConfiguration : IEntityTypeConfiguration<EmploymentDuty>
{
    /// <summary>Cakisma kisitinin adi.</summary>
    public const string NoOverlapConstraint = "ex_employment_duty_no_overlap";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EmploymentDuty> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("employment_duty", PersonConfiguration.PersonnelSchema, table =>
            table.HasCheckConstraint("ck_employment_duty_range", "valid_to IS NULL OR valid_to > valid_from"));

        builder.HasKey(duty => duty.Id);
        builder.Ignore(duty => duty.IsOpen);
        builder.HasIndex(duty => new { duty.EmploymentId, duty.ValidFrom });

        builder.HasOne<Employment>().WithMany().HasForeignKey(duty => duty.EmploymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ReferenceDefinition>().WithMany().HasForeignKey(duty => duty.DutyDefinitionId).OnDelete(DeleteBehavior.Restrict);

        builder.MapAuditFields();
        builder.MapSoftDeleteFields();
    }
}
