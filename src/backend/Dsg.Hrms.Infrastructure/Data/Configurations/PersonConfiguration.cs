using Dsg.Hrms.Domain.Personnel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// Kisi tablosunun eslemesi (ADR-0005 §1).
/// </summary>
public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    /// <summary>Personel verisinin semasi (ADR-0004 §1).</summary>
    public const string PersonnelSchema = "personnel";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("person", PersonnelSchema, table =>
        {
            // Son savunma hatti: uygulama kodunda bir hata olsa bile bicimi gecersiz
            // bir TCKN veritabanina GIREMEZ. Sagla algoritmasi uygulamada denetlenir;
            // burada bicim (11 hane, ilk hane sifir degil) zorlanir.
            table.HasCheckConstraint("ck_person_national_id_format", "national_id ~ '^[1-9][0-9]{10}$'");

            // Cep telefonu normallestirilmis bicimde saklanir (SYG-KMLK-009).
            table.HasCheckConstraint("ck_person_mobile_phone_format", "mobile_phone IS NULL OR mobile_phone ~ '^5[0-9]{9}$'");

            // E-posta normallestirilmis (kucuk harf) saklanir; buyuk harfli bir kayit
            // uyelik eslestirmesinde ayni adresin iki adres sayilmasina yol acardi.
            table.HasCheckConstraint("ck_person_email_lowercase", "email IS NULL OR email = lower(email)");
        });

        builder.HasKey(person => person.Id);

        builder.Property(person => person.NationalId).IsRequired();
        builder.Property(person => person.FirstName).IsRequired();
        builder.Property(person => person.LastName).IsRequired();
        builder.Property(person => person.BirthDate).IsRequired();

        builder.HasIndex(person => person.NationalId).IsUnique();

        // Uyelik ve giris e-postayla eslesir (SYG-KMLK-013, 031). TEKIL DEGILDIR:
        // paylasilan adresler vardir ve isaretlenerek saklanir (SYG-KMLK-008).
        builder.HasIndex(person => person.Email);

        builder.HasMany(person => person.Employments)
            .WithOne(employment => employment.Person)
            .HasForeignKey(employment => employment.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(person => person.Employments)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.MapAuditFields();
    }
}
