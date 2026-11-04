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
    /// <inheritdoc />
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;

    /// <inheritdoc />
    public DateOnly Today =>
        DateOnly.FromDateTime(CorporateTime.ToLocal(DateTimeOffset.UtcNow).Date);
}
