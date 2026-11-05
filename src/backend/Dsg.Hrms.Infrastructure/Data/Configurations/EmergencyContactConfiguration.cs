using Dsg.Hrms.Domain.Personnel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// Acil durum kisileri (ADR-0021 §1, §6; SYG-PERS-043, 054): kisi basina birden cok, yumusak silinir.
/// </summary>
public sealed class EmergencyContactConfiguration : IEntityTypeConfiguration<EmergencyContact>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<EmergencyContact> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("emergency_contact", PersonConfiguration.PersonnelSchema);
        builder.HasKey(contact => contact.Id);

        builder.Property(contact => contact.Relationship).HasMaxLength(EmergencyContact.MaxRelationshipLength).IsRequired();
        builder.Property(contact => contact.FirstName).HasMaxLength(EmergencyContact.MaxNameLength).IsRequired();
        builder.Property(contact => contact.LastName).HasMaxLength(EmergencyContact.MaxNameLength).IsRequired();
        builder.Property(contact => contact.MobilePhone).HasMaxLength(10).IsRequired();

        builder.HasIndex(contact => contact.PersonId);
        builder.HasOne<Person>().WithMany().HasForeignKey(contact => contact.PersonId).OnDelete(DeleteBehavior.Restrict);

        builder.MapAuditFields();
        builder.MapSoftDeleteFields();
    }
}
