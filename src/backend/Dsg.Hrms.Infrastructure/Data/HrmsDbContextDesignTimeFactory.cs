using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dsg.Hrms.Infrastructure.Data;

/// <summary>
/// <c>dotnet ef</c> araclarinin tasarim zamaninda kullandigi fabrika.
/// </summary>
/// <remarks>
/// <para>
/// Migration uretmek ve betik cikarmak icin CALISAN bir veritabani gerekmez; yalnizca
/// saglayici bilgisi yeterlidir. Bu fabrika, sahte bir baglanti dizesiyle bunu saglar.
/// </para>
/// <para>
/// Boylece migration uretmek icin gercek baglanti bilgisine veya sirlara ihtiyac
/// duyulmaz (ADR-0008). CI ortami da migration denetimini sirsiz calistirabilir.
/// </para>
/// </remarks>
public sealed class HrmsDbContextDesignTimeFactory : IDesignTimeDbContextFactory<HrmsDbContext>
{
    private const string DesignTimeConnectionString =
        "Host=design-time;Database=dsg_hrms;Username=design;Password=design";

    /// <inheritdoc />
    public HrmsDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseNpgsql(DesignTimeConnectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention();

        return new HrmsDbContext(builder.Options);
    }
}
