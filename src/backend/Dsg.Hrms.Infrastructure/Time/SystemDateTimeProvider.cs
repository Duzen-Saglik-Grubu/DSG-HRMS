using Dsg.Hrms.Application.Common.Abstractions;

namespace Dsg.Hrms.Infrastructure.Time;

/// <summary>
/// Sistem saatini kullanan <see cref="IDateTimeProvider"/> uygulamasi.
/// </summary>
/// <remarks>
/// <para>
/// Zaman damgalari DAIMA UTC uretilir (ADR-0004 §3). Kullaniciya gosterim,
/// sunum katmaninda Europe/Istanbul saatine cevrilir.
/// </para>
/// <para>
/// <see cref="Today"/> ise takvim kavramidir ve kurumun yerel takvimine gore
/// hesaplanir: Istanbul'da 01 Ocak sabahi 02:00 iken UTC hâlâ 31 Aralik'tir.
/// Izin gunu gibi takvim tabanli kurallarda UTC tarihini kullanmak bir gun
/// kaymaya yol acardi.
/// </para>
/// </remarks>
public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    private static readonly TimeZoneInfo CorporateTimeZone = ResolveTimeZone();

    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public DateOnly Today =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, CorporateTimeZone).Date);

    /// <summary>
    /// Saat dilimi kimligi isletim sistemine gore degisir: Linux'ta IANA
    /// ("Europe/Istanbul"), Windows'ta ("Turkey Standard Time"). Uretim Linux,
    /// gelistirme Windows oldugu icin ikisi de denenir.
    /// </summary>
    private static TimeZoneInfo ResolveTimeZone()
    {
        foreach (var id in new[] { "Europe/Istanbul", "Turkey Standard Time" })
        {
            if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var timeZone))
            {
                return timeZone;
            }
        }

        throw new TimeZoneNotFoundException(
            "Europe/Istanbul saat dilimi bulunamadi. Konteyner imajinda saat dilimi " +
            "verisi (tzdata) eksik olabilir; InvariantGlobalization ayari da kontrol edilmeli.");
    }
}
