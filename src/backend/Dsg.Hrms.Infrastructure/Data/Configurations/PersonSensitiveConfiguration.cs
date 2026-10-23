using Dsg.Hrms.Domain.Personnel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// Korumali alanlar tablosu (ADR-0021 §1, SYG-PERS-054): kisi basina tek satir.
/// </summary>
public sealed class PersonSensitiveConfiguration : IEntityTypeConfiguration<PersonSensitive>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PersonSensitive> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("person_sensitive", PersonConfiguration.PersonnelSchema);
        builder.HasKey(row => row.Id);

        builder.HasIndex(row => row.PersonId).IsUnique();
        builder.HasOne<Person>()
            .WithOne()
            .HasForeignKey<PersonSensitive>(row => row.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.MapAuditFields();
    }
}
