using Dsg.Hrms.Domain.Personnel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// Fotograf tablosu (ADR-0021 §5): kisi basina tek satir. ADR-0013 §2'nin "dosyalar veritabaninda
/// saklanmaz" kuralinin gerekceli istisnasidir: goruntu LOGO'nun kucuk bir kopyasidir.
/// </summary>
public sealed class PersonPhotoConfiguration : IEntityTypeConfiguration<PersonPhoto>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PersonPhoto> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("person_photo", PersonConfiguration.PersonnelSchema, table =>
            table.HasCheckConstraint("ck_person_photo_source_hash", $"char_length(source_hash) = {PersonPhoto.SourceHashLength}"));
        builder.HasKey(photo => photo.Id);

        builder.Property(photo => photo.Content).IsRequired();
        builder.Property(photo => photo.SourceHash).HasMaxLength(PersonPhoto.SourceHashLength).IsRequired();
        builder.Property(photo => photo.ThumbnailVersion);

        builder.HasIndex(photo => photo.PersonId).IsUnique();
        builder.HasOne<Person>()
            .WithOne()
            .HasForeignKey<PersonPhoto>(photo => photo.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.MapAuditFields();
    }
}
