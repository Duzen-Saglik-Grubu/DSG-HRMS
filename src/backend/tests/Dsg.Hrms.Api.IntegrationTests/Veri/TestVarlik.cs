using Dsg.Hrms.Domain.Ortak;
using Dsg.Hrms.Infrastructure.Veri;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Api.IntegrationTests.Veri;

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
public sealed class DenemeKaydi : Varlik, IDenetlenebilir, ISilinebilir
{
    public string Baslik { get; set; } = string.Empty;

    public DateOnly GecerlilikTarihi { get; set; }

    public decimal Tutar { get; set; }

    public DateTimeOffset OlusturmaAni { get; set; }

    public long? OlusturanKullaniciId { get; set; }

    public DateTimeOffset? GuncellemeAni { get; set; }

    public long? GuncelleyenKullaniciId { get; set; }

    public DateTimeOffset? SilinmeAni { get; set; }

    public long? SilenKullaniciId { get; set; }
}

/// <summary>
/// <see cref="HrmsDbContext"/>'ten turer; boylece uretimdeki ortak kurallar aynen gecerlidir.
/// </summary>
public sealed class DenemeDbContext(DbContextOptions<DenemeDbContext> secenekler)
    : HrmsDbContext(secenekler)
{
    public DbSet<DenemeKaydi> DenemeKayitlari => Set<DenemeKaydi>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<DenemeKaydi>(kurucu =>
        {
            kurucu.ToTable("deneme_kaydi");
            kurucu.HasKey(v => v.Id);
            kurucu.Property(v => v.Baslik).IsRequired();
            kurucu.DenetimAlanlariniEsle();
            kurucu.SilmeAlanlariniEsle();
        });

        // Ortak kurallar burada uygulanir.
        base.OnModelCreating(modelBuilder);
    }
}
