namespace Dsg.Hrms.Application.Common.Abstractions;

/// <summary>
/// Kurumun yerel saati (Europe/Istanbul). Zaman damgalari UTC tutulur (ADR-0004 §3); bir
/// dosyaya veya iletiye yazilan saat yerel saattir.
/// </summary>
public static class CorporateTime
{
    /// <summary>Kurumun saat dilimi.</summary>
    public static TimeZoneInfo Zone { get; } = Resolve();

    /// <summary>Ani yerel saate cevirir.</summary>
    public static DateTimeOffset ToLocal(DateTimeOffset instant) => TimeZoneInfo.ConvertTime(instant, Zone);

    /// <summary>
    /// Saat dilimi kimligi isletim sistemine gore degisir: Linux'ta IANA
    /// ("Europe/Istanbul"), Windows'ta ("Turkey Standard Time"). Uretim Linux,
    /// gelistirme Windows oldugu icin ikisi de denenir.
    /// </summary>
    private static TimeZoneInfo Resolve()
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
