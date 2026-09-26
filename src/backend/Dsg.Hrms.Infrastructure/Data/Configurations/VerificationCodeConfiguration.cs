using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// Dogrulama kodu tablosunun eslemesi (SYG-KMLK-022…027).
/// </summary>
public sealed class VerificationCodeConfiguration : IEntityTypeConfiguration<VerificationCode>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<VerificationCode> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("verification_code", UserAccountConfiguration.IdentitySchema, table =>
        {
            // HMAC-SHA256 ozeti Base64 olarak 44 karakterdir. Bu kisit, kodun kendisinin
            // (6-8 hane) yanlislikla ozet yerine yazilmasini veritabaninda engeller.
            table.HasCheckConstraint("ck_verification_code_hash_length", "length(code_hash) = 44");
            table.HasCheckConstraint("ck_verification_code_attempts", "failed_attempts >= 0 AND max_failed_attempts > 0");
        });

        builder.HasKey(code => code.Id);

        builder.Property(code => code.CodeHash).IsRequired();

        // Hiz siniri sorgusu (kisi + zaman) ve acik kodlarin bulunmasi (kisi + amac + durum).
        builder.HasIndex(code => new { code.PersonId, code.CreatedAt });
        builder.HasIndex(code => new { code.PersonId, code.Purpose, code.Status });

        builder.HasOne<Person>()
            .WithMany()
            .HasForeignKey(code => code.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.MapAuditFields();
    }
}
