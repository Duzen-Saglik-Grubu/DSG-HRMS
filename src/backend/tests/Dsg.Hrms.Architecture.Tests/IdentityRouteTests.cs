using System.Reflection;
using Dsg.Hrms.Api.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Dsg.Hrms.Architecture.Tests;

/// <summary>
/// Kimlik islemleri <c>/api/v1/identity/</c> altinda sunulur (SYG-KMLK-071, ADR-0010 §2, AN-21).
/// </summary>
/// <remarks>
/// Sozlesmenin OpenAPI belgesiyle esitligi CI'da ayrica denetlenir ("OpenAPI belgesi guncel
/// mi"); hatalarin Problem Details bicimi, her uc testinde yanit govdesiyle dogrulanir.
/// </remarks>
public sealed class IdentityRouteTests
{
    private const string Prefix = "api/v1/identity/";

    [Fact]
    public void Every_identity_endpoint_is_under_the_identity_path()
    {
        var controllers = typeof(SessionsController).Assembly.GetTypes()
            .Where(t => t.Namespace == typeof(SessionsController).Namespace && typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();
        controllers.ShouldNotBeEmpty();

        var routes = new List<string>();
        foreach (var controller in controllers)
        {
            var classRoute = controller.GetCustomAttribute<RouteAttribute>()?.Template;
            foreach (var action in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                foreach (var verb in action.GetCustomAttributes<HttpMethodAttribute>())
                {
                    routes.Add(verb.Template is { } template && template.StartsWith("api/", StringComparison.Ordinal)
                        ? template
                        : $"{classRoute}/{verb.Template}".TrimEnd('/') + "/");
                }
            }
        }

        routes.ShouldNotBeEmpty();
        routes.ShouldAllBe(route => route.StartsWith(Prefix, StringComparison.Ordinal));
    }
}
