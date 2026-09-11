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

    /// <summary>
    /// Gercek baglantinin okundugu ortam degiskeni.
    /// </summary>
    /// <remarks>
    /// Uygulamanin kullandigi anahtarla aynidir (<c>Database:Hrms</c>); boylece
    /// gelistirici tek bir deger tanimlar.
    /// </remarks>
    private const string ConnectionEnvironmentVariable = "Database__Hrms";

    /// <inheritdoc />
    public HrmsDbContext CreateDbContext(string[] args)
    {
        // Migration URETMEK icin calisan bir veritabani gerekmez; sahte dize yeter.
        // Ancak "dotnet ef database update" GERCEK baglanti ister. Ortam degiskeni
        // tanimliysa o kullanilir; boylece komut satirinda her seferinde
        // --connection yazmak gerekmez.
        var connectionString =
            Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable)
            ?? DesignTimeConnectionString;

        var builder = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseNpgsql(connectionString, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention();

        return new HrmsDbContext(builder.Options);
    }
}
