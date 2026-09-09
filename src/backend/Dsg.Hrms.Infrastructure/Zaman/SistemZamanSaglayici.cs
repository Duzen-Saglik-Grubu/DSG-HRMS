using Dsg.Hrms.Application.Ortak.Soyutlamalar;

namespace Dsg.Hrms.Infrastructure.Zaman;

/// <summary>
/// Sistem saatini kullanan <see cref="IZamanSaglayici"/> uygulamasi.
/// </summary>
/// <remarks>
/// <para>
/// Zaman damgalari DAIMA UTC uretilir (ADR-0004 §3). Kullaniciya gosterim,
/// sunum katmaninda Europe/Istanbul saatine cevrilir.
/// </para>
/// <para>
/// <see cref="Bugun"/> ise takvim kavramidir ve kurumun yerel takvimine gore
/// hesaplanir: Istanbul'da 01 Ocak sabahi 02:00 iken UTC hâlâ 31 Aralik'tir.
/// Izin gunu gibi takvim tabanli kurallarda UTC tarihini kullanmak bir gun
/// kaymaya yol acardi.
/// </para>
/// </remarks>
public sealed class SistemZamanSaglayici : IZamanSaglayici
{
    private static readonly TimeZoneInfo KurumSaatDilimi = SaatDiliminiBul();

    /// <inheritdoc />
    public DateTimeOffset SuAn => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public DateOnly Bugun =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, KurumSaatDilimi).Date);

    /// <summary>
    /// Saat dilimi kimligi isletim sistemine gore degisir: Linux'ta IANA
    /// ("Europe/Istanbul"), Windows'ta ("Turkey Standard Time"). Uretim Linux,
    /// gelistirme Windows oldugu icin ikisi de denenir.
    /// </summary>
    private static TimeZoneInfo SaatDiliminiBul()
    {
        foreach (var kimlik in new[] { "Europe/Istanbul", "Turkey Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(kimlik, out var dilim))
            {
                return dilim;
            }
        }

        throw new TimeZoneNotFoundException(
            "Europe/Istanbul saat dilimi bulunamadi. Konteyner imajinda saat dilimi " +
            "verisi (tzdata) eksik olabilir; InvariantGlobalization ayari da kontrol edilmeli.");
    }
}
