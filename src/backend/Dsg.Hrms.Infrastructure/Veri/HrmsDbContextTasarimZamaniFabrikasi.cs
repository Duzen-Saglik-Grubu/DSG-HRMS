using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Dsg.Hrms.Infrastructure.Veri;

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
public sealed class HrmsDbContextTasarimZamaniFabrikasi : IDesignTimeDbContextFactory<HrmsDbContext>
{
    private const string TasarimZamaniBaglantisi =
        "Host=tasarim-zamani;Database=dsg_hrms;Username=tasarim;Password=tasarim";

    /// <inheritdoc />
    public HrmsDbContext CreateDbContext(string[] args)
    {
        var kurucu = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseNpgsql(TasarimZamaniBaglantisi, npgsql =>
                npgsql.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention();

        return new HrmsDbContext(kurucu.Options);
    }
}
