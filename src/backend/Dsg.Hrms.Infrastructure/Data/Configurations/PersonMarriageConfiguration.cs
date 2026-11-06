using Dsg.Hrms.Domain.Personnel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// Evlilik bilgisi (ADR-0021 §1, §6; SYG-PERS-036, 054): kisi basina silinmemis tek kayit, yumusak
/// silinir.
/// </summary>
public sealed class PersonMarriageConfiguration : IEntityTypeConfiguration<PersonMarriage>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PersonMarriage> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("person_marriage", PersonConfiguration.PersonnelSchema);
        builder.HasKey(marriage => marriage.Id);

        // Kisi basina en fazla bir gecerli kayit; silinmis kayitlar yeniden girisi engellemez.
        builder.HasIndex(marriage => marriage.PersonId).IsUnique().HasFilter("deleted_at IS NULL");
        builder.HasOne<Person>().WithMany().HasForeignKey(marriage => marriage.PersonId).OnDelete(DeleteBehavior.Restrict);

        builder.MapAuditFields();
        builder.MapSoftDeleteFields();
    }
}
