namespace Dsg.Hrms.Domain.Personnel;

/// <summary>
/// Yas hesabi (SYG-PERS-035). Dogum tarihinden hesaplanir, saklanmaz: saklanan yas her yil
/// yanlisa doner.
/// </summary>
public static class Age
{
    /// <summary>
    /// Verilen gundeki yas. Kisi dogum gununde bir yas buyur.
    /// </summary>
    /// <remarks>
    /// 29 Subat dogumlu kisi artik yil olmayan yillarda 1 Mart'ta buyur: dogum gunu o yil
    /// takvimde yoksa henuz gelmemis sayilir (AN-05). Ay ve gun karsilastirmasi bunu
    /// kendiliginden saglar: 28 Subat, 29 Subat'tan oncedir.
    /// </remarks>
    /// <param name="birthDate">Dogum tarihi.</param>
    /// <param name="today">Bugun (Europe/Istanbul).</param>
    public static int On(DateOnly birthDate, DateOnly today)
    {
        var age = today.Year - birthDate.Year;
        if (today.Month < birthDate.Month || (today.Month == birthDate.Month && today.Day < birthDate.Day))
        {
            age--;
        }

        return Math.Max(age, 0);
    }
}
