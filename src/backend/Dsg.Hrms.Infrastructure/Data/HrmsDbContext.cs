using System.Linq.Expressions;
using Dsg.Hrms.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data;

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
    public HrmsDbContext(DbContextOptions<HrmsDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Turetilmis baglamlar icin. Ortak kurallarin (ADR-0004) turetilmis baglamlarda
    /// da uygulandigini dogrulayan testler bunu kullanir.
    /// </summary>
    protected HrmsDbContext(DbContextOptions options)
        : base(options)
    {
    }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HrmsDbContext).Assembly);

        ApplyCommonConventions(modelBuilder);
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
    private static void ApplyCommonConventions(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            // --- Eszamanlilik denetimi (ADR-0004 §4) ---
            // PostgreSQL'in sistem kolonu xmin, satir surumu olarak kullanilir:
            // her guncellemede veritabani tarafindan otomatik degisir. Ayri bir
            // surum kolonu tutmaya ve elle artirmaya gerek kalmaz.
            //
            // Iki kullanici ayni kaydi duzenlediginde ikincisi 409 alir; sessiz
            // uzerine yazma OLMAZ (ADR-0010 §7).
            if (typeof(Entity).IsAssignableFrom(clrType))
            {
                modelBuilder.Entity(clrType)
                    .Property<uint>("xmin")
                    .HasColumnName("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();

                // Dis kimlik tekil ve dizinli olmalidir; API aramalari bunun uzerinden yapilir.
                modelBuilder.Entity(clrType)
                    .HasIndex(nameof(Entity.PublicId))
                    .IsUnique();
            }

            // --- Zaman damgalari daima UTC (ADR-0004 §3) ---
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset) ||
                    property.ClrType == typeof(DateTimeOffset?))
                {
                    property.SetColumnType("timestamptz");
                }
            }

            // --- Yumusak silme suzgeci (ADR-0004 §5) ---
            // Silinmis kayitlar varsayilan olarak sorgulara GIRMEZ. Her sorguda
            // elle filtre yazmak unutulmaya acik olurdu.
            if (typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                modelBuilder.Entity(clrType).HasQueryFilter(NotDeletedFilter(clrType));

                // Aktif kayit sorgulari icin kismi dizin: silinmis satirlar dizine girmez.
                modelBuilder.Entity(clrType)
                    .HasIndex(nameof(ISoftDeletable.DeletedAt))
                    .HasFilter("deleted_at IS NULL");
            }
        }
    }

    /// <summary>
    /// <c>e =&gt; e.DeletedAt == null</c> ifadesini calisma zamaninda uretir.
    /// </summary>
    private static LambdaExpression NotDeletedFilter(Type entityClrType)
    {
        var parameter = Expression.Parameter(entityClrType, "e");
        var property = Expression.Property(parameter, nameof(ISoftDeletable.DeletedAt));
        var comparison = Expression.Equal(
            property,
            Expression.Constant(null, typeof(DateTimeOffset?)));

        return Expression.Lambda(comparison, parameter);
    }
}

/// <summary>
/// Denetim ve yumusak silme alanlarinin ortak eslemesi.
/// </summary>
/// <remarks>
/// Her varlik yapilandirmasinda cagrilir; kolon adlari ve tipleri tek yerden yonetilir.
/// </remarks>
public static class EntityBuilderExtensions
{
    /// <summary>Denetim alanlarini esler (ADR-0004 §4).</summary>
    public static EntityTypeBuilder<TEntity> MapAuditFields<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, IAuditable
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.CreatedBy);
        builder.Property(e => e.UpdatedAt);
        builder.Property(e => e.UpdatedBy);

        return builder;
    }

    /// <summary>Yumusak silme alanlarini esler (ADR-0004 §5).</summary>
    public static EntityTypeBuilder<TEntity> MapSoftDeleteFields<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : class, ISoftDeletable
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Property(e => e.DeletedAt);
        builder.Property(e => e.DeletedBy);

        return builder;
    }
}
