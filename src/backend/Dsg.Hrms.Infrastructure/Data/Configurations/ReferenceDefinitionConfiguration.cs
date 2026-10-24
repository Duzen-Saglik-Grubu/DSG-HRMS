using Dsg.Hrms.Domain.Reference;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// Referans tanimlari tablosu (ADR-0022 §1). Ilk degerler migration ile yuklenir: unvan 1–4
/// (PG-PERS-21) ve kan grubu 1–8 (<c>KR-129</c>, sistem tanimi).
/// </summary>
public sealed class ReferenceDefinitionConfiguration : IEntityTypeConfiguration<ReferenceDefinition>
{
    /// <summary>Referans veri semasi; Y3 bu semayi genisletir.</summary>
    public const string ReferenceSchema = "reference";

    private static readonly DateTimeOffset SeededAt = new(2026, 10, 24, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Sabit ilk degerler; kimlikler migration'lar arasinda degismez.</summary>
    private static readonly (long Id, Guid PublicId, ReferenceType Type, string Code, string Name, bool IsSystem)[] Seed =
    [
        (1, new Guid("0192a3b4-1001-7000-8000-000000000001"), ReferenceType.Title, "1", "Birim Operatörü", false),
        (2, new Guid("0192a3b4-1001-7000-8000-000000000002"), ReferenceType.Title, "2", "Birim Teknik Sorumlusu", false),
        (3, new Guid("0192a3b4-1001-7000-8000-000000000003"), ReferenceType.Title, "3", "Birim Sorumlusu", false),
        (4, new Guid("0192a3b4-1001-7000-8000-000000000004"), ReferenceType.Title, "4", "Yardımcı Hizmetler", false),
        (11, new Guid("0192a3b4-1003-7000-8000-000000000001"), ReferenceType.BloodGroup, "1", "0 Rh+", true),
        (12, new Guid("0192a3b4-1003-7000-8000-000000000002"), ReferenceType.BloodGroup, "2", "0 Rh-", true),
        (13, new Guid("0192a3b4-1003-7000-8000-000000000003"), ReferenceType.BloodGroup, "3", "A Rh+", true),
        (14, new Guid("0192a3b4-1003-7000-8000-000000000004"), ReferenceType.BloodGroup, "4", "A Rh-", true),
        (15, new Guid("0192a3b4-1003-7000-8000-000000000005"), ReferenceType.BloodGroup, "5", "B Rh+", true),
        (16, new Guid("0192a3b4-1003-7000-8000-000000000006"), ReferenceType.BloodGroup, "6", "B Rh-", true),
        (17, new Guid("0192a3b4-1003-7000-8000-000000000007"), ReferenceType.BloodGroup, "7", "AB Rh+", true),
        (18, new Guid("0192a3b4-1003-7000-8000-000000000008"), ReferenceType.BloodGroup, "8", "AB Rh-", true),
    ];

    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<ReferenceDefinition> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("definition", ReferenceSchema, table =>
            table.HasCheckConstraint("ck_definition_type", "type IN (1, 2, 3)"));

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Code).IsRequired().HasMaxLength(20);
        builder.Property(d => d.Name).IsRequired().HasMaxLength(200).UseCollation(DatabaseCollations.Turkish);
        builder.HasIndex(d => new { d.Type, d.Code }).IsUnique();
        builder.HasIndex(d => d.PublicId).IsUnique();

        builder.MapAuditFields();

        // Kimlik dizisi ilk degerlerin ustunden baslar (1000); kullanicinin ekledigi tanim
        // sabit kimliklerle cakismaz.
        builder.Property(d => d.Id).UseIdentityByDefaultColumn().HasIdentityOptions(startValue: 1000);

        builder.HasData(Seed.Select(row => new
        {
            row.Id,
            row.PublicId,
            row.Type,
            row.Code,
            row.Name,
            SortOrder = int.Parse(row.Code, System.Globalization.CultureInfo.InvariantCulture),
            IsActive = true,
            row.IsSystem,
            CreatedAt = SeededAt,
        }));
    }
}
