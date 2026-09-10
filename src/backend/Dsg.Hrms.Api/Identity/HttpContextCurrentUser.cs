using System.Globalization;
using System.Security.Claims;
using Dsg.Hrms.Api.Logging;
using Dsg.Hrms.Application.Common.Abstractions;

namespace Dsg.Hrms.Api.Identity;

/// <summary>
/// Oturum acmis kullaniciyi ve istek baglamini HTTP baglamindan okur.
/// </summary>
/// <remarks>
/// <para>
/// Kimlik dogrulama altyapisi henuz kurulmadigi icin <see cref="UserId"/> su an daima
/// <c>null</c> doner; bu, islemlerin "sistem" adina yapildigi anlamina gelir. Kimlik
/// Yonetimi modulu (T3) tamamlandiginda talep (claim) okuma devreye girecektir.
/// </para>
/// <para>
/// Arka plan islerinde HTTP baglami bulunmaz; tum ozellikler <c>null</c> doner.
/// </para>
/// </remarks>
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    /// <summary>Kullanici kimliginin tasindigi talep (claim) adi.</summary>
    public const string UserIdClaimType = "hrms:user_id";

    /// <inheritdoc />
    public long? UserId
    {
        get
        {
            var value = httpContextAccessor.HttpContext?.User?.FindFirstValue(UserIdClaimType);

            return long.TryParse(value, CultureInfo.InvariantCulture, out var id) ? id : null;
        }
    }

    /// <inheritdoc />
    public string? IpAddress =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    /// <inheritdoc />
    public string? TraceId
    {
        get
        {
            var context = httpContextAccessor.HttpContext;

            // Ara katmanin urettigi (veya cagirandan alip dogruladigi) kimlik kullanilir;
            // uygulama gunlugundeki CorrelationId ile ayni deger olmalidir.
            return context is null ? null : CorrelationIdMiddleware.GetCorrelationId(context);
        }
    }
}
