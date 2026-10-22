using System.Globalization;

namespace Dsg.Hrms.Domain.Personnel;

/// <summary>
/// Bir istihdamin kidem suresi: takvime gore yil, ay ve gun (SYG-PERS-039). Hesaplanir,
/// saklanmaz.
/// </summary>
/// <param name="Years">Tam yil.</param>
/// <param name="Months">Yildan arta kalan tam ay.</param>
/// <param name="Days">Aydan arta kalan gun.</param>
public sealed record Seniority(int Years, int Months, int Days)
{
    /// <summary>
    /// Kidemi hesaplar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Bitis:</b> Aktif istihdamda bugun SAYILMAZ (gun henuz tamamlanmadi). Ayrilan
    /// istihdamda cikis gunu SAYILIR: cikis tarihi son calisma gunudur, bu yuzden sure cikis
    /// tarihinin ertesi gunune kadar hesaplanir. Giris 01.01.2020, cikis 31.12.2020 ise tam
    /// bir yildir (AN-04, IK teyidi 22.10.2026, <c>KR-127</c>).
    /// </para>
    /// <para>
    /// <b>Ay sonu:</b> Giris tarihine once tam yillar, sonra tam aylar eklenir; eklenen ayda o
    /// gun yoksa ayin son gunu alinir (31.01'e bir ay = 28.02 veya 29.02). Yil ve ay her
    /// seferinde giris tarihinden eklenir; ardisik ekleme ay sonunda gun kaydirirdi.
    /// </para>
    /// </remarks>
    /// <param name="hireDate">Ise giris tarihi.</param>
    /// <param name="terminationDate">Isten cikis tarihi (son calisma gunu); aktifse <c>null</c>.</param>
    /// <param name="today">Bugun (Europe/Istanbul).</param>
    public static Seniority Calculate(DateOnly hireDate, DateOnly? terminationDate, DateOnly today)
    {
        var end = terminationDate is { } last ? last.AddDays(1) : today;
        if (end <= hireDate)
        {
            return new Seniority(0, 0, 0);
        }

        var totalMonths = ((end.Year - hireDate.Year) * 12) + end.Month - hireDate.Month;
        if (hireDate.AddMonths(totalMonths) > end)
        {
            totalMonths--;
        }

        var days = end.DayNumber - hireDate.AddMonths(totalMonths).DayNumber;
        return new Seniority(totalMonths / 12, totalMonths % 12, days);
    }

    /// <summary>
    /// Ekranda gosterilen metin: "X Yil Y Ay Z Gun". Sayilar sifirla doldurulmaz. Yil sifirsa
    /// yil, yil ve ay sifirsa ay da yazilmaz (PG-PERS-21).
    /// </summary>
    public string Text => (Years, Months) switch
    {
        ( > 0, _) => string.Create(CultureInfo.InvariantCulture, $"{Years} Yıl {Months} Ay {Days} Gün"),
        (0, > 0) => string.Create(CultureInfo.InvariantCulture, $"{Months} Ay {Days} Gün"),
        _ => string.Create(CultureInfo.InvariantCulture, $"{Days} Gün"),
    };
}
