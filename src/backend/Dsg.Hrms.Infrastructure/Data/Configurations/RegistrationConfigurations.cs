using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>Uyelik denemesi tablosunun eslemesi (SYG-KMLK-013…021).</summary>
public sealed class RegistrationAttemptConfiguration : IEntityTypeConfiguration<RegistrationAttempt>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RegistrationAttempt> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("registration_attempt", UserAccountConfiguration.IdentitySchema, table =>
        {
            // HMAC-SHA256 ozeti (Base64, 44 karakter). Duz TCKN (11 hane) yazilamaz.
            table.HasCheckConstraint("ck_registration_attempt_national_id_hash_length", "length(national_id_hash) = 44");
            table.HasCheckConstraint("ck_registration_attempt_channels", "offered_channels BETWEEN 1 AND 3");

            // Yalnizca uyelik (1) ve parola sifirlama (2); iki adimli giris kendi kaydini kullanir.
            table.HasCheckConstraint("ck_registration_attempt_purpose", "purpose IN (1, 2)");
        });

        builder.HasKey(attempt => attempt.Id);
        builder.Property(attempt => attempt.NationalIdHash).IsRequired();

        // Hiz siniri sorgulari (SYG-KMLK-059): TCKN basina ve IP basina, son bir saat.
        builder.HasIndex(attempt => new { attempt.NationalIdHash, attempt.CreatedAt });
        builder.HasIndex(attempt => new { attempt.IpAddress, attempt.CreatedAt });

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(attempt => attempt.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.MapAuditFields();
    }
}

/// <summary>Uyelik kod istegi tablosunun eslemesi (SYG-KMLK-059).</summary>
public sealed class RegistrationCodeRequestConfiguration : IEntityTypeConfiguration<RegistrationCodeRequest>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RegistrationCodeRequest> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("registration_code_request", UserAccountConfiguration.IdentitySchema, table =>
            table.HasCheckConstraint("ck_registration_code_request_hash_length", "length(national_id_hash) = 44"));

        builder.HasKey(request => request.Id);
        builder.Property(request => request.NationalIdHash).IsRequired();
        builder.HasIndex(request => new { request.NationalIdHash, request.RequestedAt });
    }
}
