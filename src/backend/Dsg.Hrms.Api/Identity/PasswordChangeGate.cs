using Dsg.Hrms.Application.Identity.Sessions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Dsg.Hrms.Api.Identity;

/// <summary>
/// Parola degisimi bekleyen oturumda da kullanilabilen uc (SYG-KMLK-046, 050).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
public sealed class AllowDuringPasswordChangeAttribute : Attribute;

/// <summary>
/// Parola degisimi bekleyen oturumu yalnizca izin verilen uclarla sinirlar (SYG-KMLK-046, 050).
/// </summary>
/// <remarks>
/// <para>
/// Oturumun durumu her istekte veritabanindan okunur (<see cref="SessionService.CheckSessionAsync"/>)
/// ve istek baglamina isaretlenir. Kural SUNUCUDA uygulanir: istemci yonlendirmeyi atlasa da
/// parola degismeden baska bir uc kullanilamaz.
/// </para>
/// <para>
/// Yalnizca kimlik dogrulamasi isteyen uclar sinirlanir. Anonim uclar (yenileme, cikis, genel
/// ayarlar, logo) etkilenmez: tarayici jetonu her istege ekler, anonim uclarin kapanmasi giris
/// ekranini bozardi.
/// </para>
/// </remarks>
public sealed class PasswordChangeGateFilter : IAuthorizationFilter
{
    private const string ItemKey = "dsg-hrms:password-change-required";

    /// <summary>Istegin oturumunu parola degisimi bekliyor olarak isaretler.</summary>
    public static void MarkRequired(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Items[ItemKey] = true;
    }

    /// <inheritdoc />
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.HttpContext.Items.ContainsKey(ItemKey))
        {
            return;
        }

        var metadata = context.ActionDescriptor.EndpointMetadata;
        var requiresAuthentication = metadata.OfType<IAuthorizeData>().Any() && !metadata.OfType<IAllowAnonymous>().Any();
        if (requiresAuthentication && !metadata.OfType<AllowDuringPasswordChangeAttribute>().Any())
        {
            throw new PasswordChangeRequiredException();
        }
    }
}
