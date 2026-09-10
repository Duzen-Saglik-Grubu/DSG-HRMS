using Dsg.Hrms.Application.Common.Security;
using Dsg.Hrms.Domain.Common;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Api.IntegrationTests.Data;

/// <summary>
/// Yalnizca test amacli varlik.
/// </summary>
/// <remarks>
/// Uretim varliklari henuz tanimlanmadigi icin, ADR-0004'teki ORTAK kurallarin
/// (snake_case, UTC, denetim alanlari, yumusak silme, eszamanlilik) gercekten
/// uygulandigini dogrulamak icin kullanilir.
///
/// Ayni kurallar uretim varliklarina da uygulanacaktir; test, kural kodunun
/// kendisini dogrular.
/// </remarks>
public sealed class SampleRecord : Entity, IAuditable, ISoftDeletable
{
    public string Title { get; set; } = string.Empty;

    public DateOnly ValidOn { get; set; }

    public decimal Amount { get; set; }

    /// <summary>Maskeleme kuralinin denetim izinde de gectigini dogrulamak icin.</summary>
    [PersonalData(PersonalDataKind.NationalId)]
    public string? NationalId { get; set; }

    /// <summary>Sir alanlarin denetim izine hic yazilmadigini dogrulamak icin.</summary>
    [Secret]
    public string? PasswordHash { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public long? CreatedBy { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public long? UpdatedBy { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public long? DeletedBy { get; set; }
}

/// <summary>
/// <see cref="HrmsDbContext"/>'ten turer; boylece uretimdeki ortak kurallar aynen gecerlidir.
/// </summary>
public sealed class SampleDbContext(DbContextOptions<SampleDbContext> options)
    : HrmsDbContext(options)
{
    public DbSet<SampleRecord> SampleRecords => Set<SampleRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<SampleRecord>(builder =>
        {
            builder.ToTable("sample_record");
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Title).IsRequired();
            builder.MapAuditFields();
            builder.MapSoftDeleteFields();
        });

        // Ortak kurallar burada uygulanir.
        base.OnModelCreating(modelBuilder);
    }
}
