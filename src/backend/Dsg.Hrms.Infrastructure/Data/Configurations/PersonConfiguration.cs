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

            // Enum kolonlari tamsayidir ve tanimli degerlerle sinirlidir (ADR-0021 §6).
            table.HasCheckConstraint("ck_person_gender", "gender IS NULL OR gender IN (1, 2)");
            table.HasCheckConstraint("ck_person_marital_status", "marital_status IS NULL OR marital_status IN (1, 2)");

            // Onceki soyadi yalnizca kadin personelde tutulur (PG-PERS-19); uygulamadaki bir
            // hata erkek personelde deger birakamaz.
            table.HasCheckConstraint("ck_person_previous_surname_female", "previous_surname IS NULL OR gender = 2");
        });

        builder.HasKey(person => person.Id);

        builder.Property(person => person.NationalId).IsRequired();
        // Turkce siralama ve buyuk/kucuk harf duyarsiz arama (ADR-0004 §10, #244 B-05): LOGO adlari
        // tamamen buyuk harfle yazar (IŞIK, İSMAİL); "ışık" aramasi ve Ç/İ/Ö/Ş/Ü sirasi bu kuralla dogru calisir.
        builder.Property(person => person.FirstName).IsRequired().UseCollation(DatabaseCollations.Turkish);
        builder.Property(person => person.LastName).IsRequired().UseCollation(DatabaseCollations.Turkish);
        builder.Property(person => person.BirthDate).IsRequired();
        builder.Property(person => person.PreviousSurname).UseCollation(DatabaseCollations.Turkish);
        builder.Property(person => person.EducationName).UseCollation(DatabaseCollations.Turkish);

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
