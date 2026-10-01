using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Settings;
using Dsg.Hrms.Infrastructure.Data;
using Dsg.Hrms.Infrastructure.Data.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Settings;

/// <summary>Kurumsal logonun veritabani tarafi (PRM-GRN-01).</summary>
public sealed class BrandLogoStore : IBrandLogoStore
{
    private readonly HrmsDbContext _context;

    /// <summary>Yeni ornek olusturur.</summary>
    public BrandLogoStore(HrmsDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task<BrandLogo?> FindAsync(CancellationToken cancellationToken) =>
        _context.Set<BrandLogo>().OrderBy(l => l.Id).FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public Task<string?> FindVersionAsync(CancellationToken cancellationToken) =>
        _context.Set<BrandLogo>().AsNoTracking().OrderBy(l => l.Id).Select(l => l.Sha256).FirstOrDefaultAsync(cancellationToken);

    /// <inheritdoc />
    public void Add(BrandLogo logo) => _context.Add(logo);

    /// <inheritdoc />
    public void Remove(BrandLogo logo) => _context.Remove(logo);

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken) => _context.SaveChangesAsync(cancellationToken);
}

/// <summary>Kurumsal logo tablosunun eslemesi.</summary>
public sealed class BrandLogoConfiguration : IEntityTypeConfiguration<BrandLogo>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<BrandLogo> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("brand_logo", SystemParameterConfiguration.SettingsSchema, table =>
        {
            table.HasCheckConstraint("ck_brand_logo_size", $"size_bytes BETWEEN 1 AND {BrandLogo.MaxSizeBytes}");
            table.HasCheckConstraint("ck_brand_logo_content_type", "content_type IN ('image/png', 'image/jpeg')");
        });

        builder.HasKey(logo => logo.Id);
        builder.Property(logo => logo.Content).IsRequired();
        builder.Property(logo => logo.ContentType).IsRequired().HasMaxLength(20);
        builder.Property(logo => logo.Sha256).IsRequired().HasMaxLength(64);

        builder.MapAuditFields();
    }
}
