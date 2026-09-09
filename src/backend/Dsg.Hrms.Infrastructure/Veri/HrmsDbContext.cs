using System.Linq.Expressions;
using Dsg.Hrms.Domain.Ortak;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Veri;

/// <summary>
/// HRMS veritabani baglami.
/// </summary>
/// <remarks>
/// ADR-0004'te tanimlanan veritabani standartlarini <b>merkezi olarak</b> uygular.
/// Her varlik yapilandirmasinda tekrar edilmesi gereken kurallar burada bir kez
/// yazilir; boylece unutulma ihtimali ortadan kalkar.
/// </remarks>
public class HrmsDbContext : DbContext
{
    /// <summary>Uygulama tarafindan kullanilan kurucu.</summary>
    public HrmsDbContext(DbContextOptions<HrmsDbContext> secenekler)
        : base(secenekler)
    {
    }

    /// <summary>
    /// Turetilmis baglamlar icin. Ortak kurallarin (ADR-0004) turetilmis baglamlarda
    /// da uygulandigini dogrulayan testler bunu kullanir.
    /// </summary>
    protected HrmsDbContext(DbContextOptions secenekler)
        : base(secenekler)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HrmsDbContext).Assembly);

        OrtakKurallariUygula(modelBuilder);
    }

    /// <inheritdoc />
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        ArgumentNullException.ThrowIfNull(configurationBuilder);
        base.ConfigureConventions(configurationBuilder);

        // Metin alanlari icin varsayilan tip: text.
        // PostgreSQL'de varchar(n) performans avantaji saglamaz; uzunluk kisiti
        // gerektiginde CHECK ile konur (ADR-0004 §3).
        configurationBuilder.Properties<string>().HaveColumnType("text");

        // Para: kayan noktali tip KULLANILMAZ. Yuvarlama hatalari bordro ve izin
        // hesaplarinda kabul edilemez.
        configurationBuilder.Properties<decimal>().HavePrecision(19, 4);
    }

    /// <summary>
    /// Tum varliklara uygulanan ortak kurallar (ADR-0004).
    /// </summary>
    private static void OrtakKurallariUygula(ModelBuilder modelKurucu)
    {
        foreach (var varlikTipi in modelKurucu.Model.GetEntityTypes())
        {
            var clrTipi = varlikTipi.ClrType;

            // --- Eszamanlilik denetimi (ADR-0004 §4) ---
            // PostgreSQL'in sistem kolonu xmin, satir surumu olarak kullanilir:
            // her guncellemede veritabani tarafindan otomatik degisir. Ayri bir
            // surum kolonu tutmaya ve elle artirmaya gerek kalmaz.
            //
            // Iki kullanici ayni kaydi duzenlediginde ikincisi 409 alir; sessiz
            // uzerine yazma OLMAZ (ADR-0010 §7).
            if (typeof(Varlik).IsAssignableFrom(clrTipi))
            {
                modelKurucu.Entity(clrTipi)
                    .Property<uint>("xmin")
                    .HasColumnName("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();

                // Dis kimlik tekil ve dizinli olmalidir; API aramalari bunun uzerinden yapilir.
                modelKurucu.Entity(clrTipi)
                    .HasIndex(nameof(Varlik.GenelId))
                    .IsUnique();
            }

            // --- Zaman damgalari daima UTC (ADR-0004 §3) ---
            foreach (var ozellik in varlikTipi.GetProperties())
            {
                if (ozellik.ClrType == typeof(DateTimeOffset) ||
                    ozellik.ClrType == typeof(DateTimeOffset?))
                {
                    ozellik.SetColumnType("timestamptz");
                }
            }

            // --- Yumusak silme suzgeci (ADR-0004 §5) ---
            // Silinmis kayitlar varsayilan olarak sorgulara GIRMEZ. Her sorguda
            // elle filtre yazmak unutulmaya acik olurdu.
            if (typeof(ISilinebilir).IsAssignableFrom(clrTipi))
            {
                modelKurucu.Entity(clrTipi).HasQueryFilter(SilinmemisSuzgeci(clrTipi));

                // Aktif kayit sorgulari icin kismi dizin: silinmis satirlar dizine girmez.
                modelKurucu.Entity(clrTipi)
                    .HasIndex(nameof(ISilinebilir.SilinmeAni))
                    .HasFilter("silinme_ani IS NULL");
            }
        }
    }

    /// <summary>
    /// <c>e =&gt; e.SilinmeAni == null</c> ifadesini calisma zamaninda uretir.
    /// </summary>
    private static LambdaExpression SilinmemisSuzgeci(Type varlikTipi)
    {
        var parametre = Expression.Parameter(varlikTipi, "e");
        var ozellik = Expression.Property(parametre, nameof(ISilinebilir.SilinmeAni));
        var karsilastirma = Expression.Equal(
            ozellik,
            Expression.Constant(null, typeof(DateTimeOffset?)));

        return Expression.Lambda(karsilastirma, parametre);
    }
}

/// <summary>
/// Denetim ve yumusak silme alanlarinin ortak eslemesi.
/// </summary>
/// <remarks>
/// Her varlik yapilandirmasinda cagrilir; kolon adlari ve tipleri tek yerden yonetilir.
/// </remarks>
public static class OrtakAlanEslemesi
{
    /// <summary>Denetim alanlarini esler (ADR-0004 §4).</summary>
    public static EntityTypeBuilder<TVarlik> DenetimAlanlariniEsle<TVarlik>(
        this EntityTypeBuilder<TVarlik> kurucu)
        where TVarlik : class, IDenetlenebilir
    {
        ArgumentNullException.ThrowIfNull(kurucu);

        kurucu.Property(v => v.OlusturmaAni).IsRequired();
        kurucu.Property(v => v.OlusturanKullaniciId);
        kurucu.Property(v => v.GuncellemeAni);
        kurucu.Property(v => v.GuncelleyenKullaniciId);

        return kurucu;
    }

    /// <summary>Yumusak silme alanlarini esler (ADR-0004 §5).</summary>
    public static EntityTypeBuilder<TVarlik> SilmeAlanlariniEsle<TVarlik>(
        this EntityTypeBuilder<TVarlik> kurucu)
        where TVarlik : class, ISilinebilir
    {
        ArgumentNullException.ThrowIfNull(kurucu);

        kurucu.Property(v => v.SilinmeAni);
        kurucu.Property(v => v.SilenKullaniciId);

        return kurucu;
    }
}
