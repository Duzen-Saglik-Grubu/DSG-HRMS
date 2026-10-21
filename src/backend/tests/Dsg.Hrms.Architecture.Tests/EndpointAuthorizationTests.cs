using System.Reflection;
using Dsg.Hrms.Api.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Dsg.Hrms.Architecture.Tests;

/// <summary>
/// Her uc noktanin yetki karari acikca yazilidir (#244, tasarim gozden gecirmesi B-04).
/// </summary>
/// <remarks>
/// <para>
/// Varsayilan politika oturum ister; isaretini unutan yeni bir uc anonim kalmaz. Bu test
/// ikinci kilittir: her eylem ya <c>[AllowAnonymous]</c> ya da <c>[Authorize]</c> /
/// <c>[HasPermission]</c> tasir (eylemde veya sinifta). Boylece kararin verildigi PR'da
/// gorunur ve incelenir.
/// </para>
/// <para>
/// Anonim uclarin listesi asagida sabittir. Yeni bir anonim uc eklemek bu listeyi
/// degistirmeyi gerektirir; liste degisikligi PR incelemesinde guvenlik karari olarak
/// gorulur.
/// </para>
/// </remarks>
public sealed class EndpointAuthorizationTests
{
    /// <summary>Oturum gerektirmeyen uclar: <c>Denetleyici.Eylem</c>.</summary>
    private static readonly string[] AnonymousEndpoints =
    [
        // Uyelik ve parola sifirlama: kullanicinin henuz oturumu yoktur (SYG-KMLK-013, 047).
        "RegistrationsController.StartAsync",
        "RegistrationsController.RequestCodeAsync",
        "RegistrationsController.VerifyAsync",
        "RegistrationsController.CompleteAsync",
        "PasswordResetsController.StartAsync",
        "PasswordResetsController.RequestCodeAsync",
        "PasswordResetsController.VerifyAsync",
        "PasswordResetsController.ResetAsync",
        // Giris, iki adimli dogrulama, yenileme (cerezle) ve cikis (SYG-KMLK-031, 034, 040, 043).
        "SessionsController.SignInAsync",
        "SessionsController.RequestCodeAsync",
        "SessionsController.VerifyAsync",
        "SessionsController.RefreshAsync",
        "SessionsController.SignOutAsync",
        // Davet baglantisi: jetonla dogrulanir (SYG-KMLK-051).
        "InvitationsController.LookupAsync",
        "InvitationsController.AcceptAsync",
        // Giris ekraninin herkese acik ayarlari ve logosu (SYG-KMLK-069, 070).
        "PublicSettingsController.GetAsync",
        "BrandingController.GetAsync",
    ];

    [Fact]
    public void Every_action_states_its_authorization_explicitly()
    {
        var unmarked = Actions()
            .Where(a => !IsAnonymous(a) && !HasAuthorizeData(a))
            .Select(Name)
            .ToList();

        unmarked.ShouldBeEmpty("Her eylem [AllowAnonymous] veya [Authorize]/[HasPermission] tasimali");
    }

    [Fact]
    public void Anonymous_endpoints_match_the_reviewed_list()
    {
        Actions().Where(IsAnonymous).Select(Name)
            .ShouldBe(AnonymousEndpoints, ignoreOrder: true);
    }

    [Fact]
    public void Class_level_anonymous_is_not_mixed_with_action_level_authorization()
    {
        // [AllowAnonymous] sinifta ise eylemdeki [Authorize] UYGULANMAZ; bu birlesim tuzaktir.
        var trapped = Actions()
            .Where(a => a.DeclaringType!.GetCustomAttribute<AllowAnonymousAttribute>() is not null
                        && a.GetCustomAttributes().OfType<IAuthorizeData>().Any())
            .Select(Name)
            .ToList();

        trapped.ShouldBeEmpty();
    }

    [Fact]
    public void Action_discovery_is_not_empty()
    {
        // Denetimin bos gecmesini onler: kesif bozulursa yukaridaki testler hicbir sey denetlemez.
        Actions().Count.ShouldBeGreaterThan(30);
        Actions().Select(Name).ShouldContain("AccountsController.DeactivateAsync");
    }

    private static List<MethodInfo> Actions() =>
        typeof(SessionsController).Assembly.GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .SelectMany(t => t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(m => m.GetCustomAttributes<HttpMethodAttribute>().Any())
            .ToList();

    private static bool IsAnonymous(MethodInfo action) =>
        action.GetCustomAttributes().OfType<IAllowAnonymous>().Any()
        || action.DeclaringType!.GetCustomAttributes().OfType<IAllowAnonymous>().Any();

    private static bool HasAuthorizeData(MethodInfo action) =>
        action.GetCustomAttributes().OfType<IAuthorizeData>().Any()
        || action.DeclaringType!.GetCustomAttributes().OfType<IAuthorizeData>().Any();

    private static string Name(MethodInfo action) => $"{action.DeclaringType!.Name}.{action.Name}";
}
