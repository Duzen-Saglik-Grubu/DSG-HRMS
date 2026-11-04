using Dsg.Hrms.Domain.Personnel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// "IK'ya bildir" duzeltme talepleri (ADR-0022 §3; SYG-PERS-045…049). Ayni kisinin ayni alan icin
/// en fazla bir acik talebi olur: kisit veritabanindadir, es zamanli iki gonderim de yakalanir (AN-08).
/// </summary>
public sealed class CorrectionRequestConfiguration : IEntityTypeConfiguration<CorrectionRequest>
{
    /// <summary>Acik talep tekilliginin dizin adi.</summary>
    public const string OneOpenPerFieldIndex = "ux_correction_request_open_field";

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<CorrectionRequest> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("correction_request", PersonConfiguration.PersonnelSchema, table =>
            // Kapali talepte deger tutulmaz (KR-126): kural veritabaninda da zorlanir.
            table.HasCheckConstraint("ck_correction_request_closed_without_value", "status = 1 OR proposed_value IS NULL"));

        builder.HasKey(request => request.Id);
        builder.HasIndex(request => request.PublicId).IsUnique();
        builder.HasIndex(request => new { request.Status, request.SubmittedAt });
        builder.HasIndex(request => new { request.PersonId, request.Field })
            .HasDatabaseName(OneOpenPerFieldIndex)
            .IsUnique()
            .HasFilter("status = 1");

        builder.HasOne<Person>().WithMany().HasForeignKey(request => request.PersonId).OnDelete(DeleteBehavior.Restrict);

        builder.MapAuditFields();
    }
}
