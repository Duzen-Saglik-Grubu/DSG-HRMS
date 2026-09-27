using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Dsg.Hrms.Api.Identity;

/// <summary>
/// Yaniti sabit bir alt sureye tamamlar (SYG-KMLK-015, KPÖ-KMLK-2).
/// </summary>
/// <remarks>
/// <para>
/// Eslesen istek eslesmeyenden daha fazla is yapar (kod uretimi, kuyruga alma). Fark
/// yanit suresinden olculebilseydi, ayni ekran ve ayni ileti eslesmeyi gizleyemezdi.
/// Yanit, isin suresinden bagimsiz olarak en az <see cref="RegistrationTimingOptions.MinimumResponseTime"/>
/// sonra doner; hata yaniti da dahil.
/// </para>
/// <para>
/// Is bu sureyi asarsa bekleme eklenmez: alt sinir, ust sinir degildir.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class MinimumResponseTimeAttribute : Attribute, IAsyncActionFilter
{
    /// <inheritdoc />
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var minimum = context.HttpContext.RequestServices.GetRequiredService<IOptions<RegistrationTimingOptions>>().Value.MinimumResponseTime;
        var stopwatch = Stopwatch.StartNew();

        await next();

        // Zamanlayici birkac milisaniye erken donebilir (Linux'ta olculdu); alt sinir
        // gercekten saglanana kadar beklenir.
        for (var remaining = minimum - stopwatch.Elapsed; remaining > TimeSpan.Zero; remaining = minimum - stopwatch.Elapsed)
        {
            await Task.Delay(remaining, CancellationToken.None);
        }
    }
}

/// <summary>Uyelik yanit suresi ayari.</summary>
public sealed class RegistrationTimingOptions
{
    /// <summary>Yapilandirma bolumunun adi.</summary>
    public const string SectionName = "Registration";

    /// <summary>Uyelik adimlarinin en kisa yanit suresi. Varsayilan 1 saniye.</summary>
    public TimeSpan MinimumResponseTime { get; set; } = TimeSpan.FromSeconds(1);
}
