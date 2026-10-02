using Dsg.Hrms.Application.Common.Exceptions;

namespace Dsg.Hrms.Api.Networking;

/// <summary>
/// API yalnizca HTTPS ile gelen isteklere hizmet verir (SYG-KMLK-063).
/// </summary>
/// <remarks>
/// <para>
/// TLS nginx'te sonlanir; API'ye istek konteyner aginda duz HTTP ile gelir. Istegin
/// tarayicidan HTTPS ile gelip gelmedigi, <b>guvenilen vekilin</b> <c>X-Forwarded-Proto</c>
/// basligindan okunur (<see cref="ReverseProxyRegistration"/>). Guvenilen aglarin disindan
/// gelen basliga bakilmaz; istemci "https" yazarak denetimi atlatamaz.
/// </para>
/// <para>
/// HTTPS ile gelmeyen istek <b>yonlendirilmez, reddedilir</b>: yonlendirme aninda istegin
/// govdesi (parola, dogrulama kodu) zaten sifresiz gonderilmistir ve tarayici <c>POST</c>'u
/// yonlendirmede <c>GET</c>'e cevirir. nginx sayfa adreslerini yine HTTPS'e yonlendirir;
/// bu denetim, sertifika eksik kaldiginda nginx'in duz HTTP kipine dusmesi gibi durumlarda
/// API'nin sifresiz hizmet vermesini engeller.
/// </para>
/// <para>
/// Yalnizca <c>/api/</c> denetlenir. Saglik uclari konteyner icinden duz HTTP ile cagrilir
/// ve kisisel veri tasimaz. Gelistirme ortaminda (<c>appsettings.Development.json</c>)
/// kapalidir; varsayilan aciktir.
/// </para>
/// </remarks>
public static class HttpsRequirement
{
    /// <summary>Ayarin anahtari.</summary>
    public const string SettingKey = "Security:RequireHttps";

    /// <summary>HTTPS denetimini boru hattina ekler (yapilandirmayla kapatilmadiysa).</summary>
    public static IApplicationBuilder UseHrmsHttpsRequirement(this IApplicationBuilder app, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(configuration);

        if (!configuration.GetValue(SettingKey, defaultValue: true))
        {
            return app;
        }

        return app.Use((context, next) =>
        {
            if (!context.Request.IsHttps && context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                throw new HttpsRequiredException();
            }

            return next(context);
        });
    }
}

/// <summary>Istek HTTPS ile gelmedi (SYG-KMLK-063).</summary>
public sealed class HttpsRequiredException()
    : HrmsException("Bu işlem yalnızca güvenli bağlantı (HTTPS) üzerinden yapılabilir.")
{
    /// <inheritdoc />
    public override int StatusCode => 403;

    /// <inheritdoc />
    public override string ErrorType => "https-required";

    /// <inheritdoc />
    public override string Title => "Güvenli bağlantı gerekli";
}
