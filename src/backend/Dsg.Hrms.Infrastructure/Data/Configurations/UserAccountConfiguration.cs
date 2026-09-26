using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// Kullanici hesabi tablosunun eslemesi (ADR-0006, SYG-KMLK-021).
/// </summary>
public sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    /// <summary>Kimlik verisinin semasi (ADR-0004 §1).</summary>
    public const string IdentitySchema = "identity";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("user_account", IdentitySchema, table =>
        {
            // Elle yapilan durum degisikligi gerekcesiz kaydedilemez (SYG-KMLK-057).
            table.HasCheckConstraint(
                "ck_user_account_manual_note",
                $"status_reason IS DISTINCT FROM {(int)AccountStatusReason.Manual} OR (status_note IS NOT NULL AND btrim(status_note) <> '')");
        });

        builder.HasKey(account => account.Id);

        builder.Property(account => account.Status).IsRequired();
        builder.Property(account => account.SecurityStamp).IsRequired();

        // Bir kisiye en fazla bir hesap (SYG-KMLK-021). Uygulama kodundaki bir hata veya
        // es zamanli iki uyelik istegi ikinci hesabi olusturamaz.
        builder.HasIndex(account => account.PersonId).IsUnique();

        builder.HasOne<Person>()
            .WithOne()
            .HasForeignKey<UserAccount>(account => account.PersonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.MapAuditFields();
    }
}
