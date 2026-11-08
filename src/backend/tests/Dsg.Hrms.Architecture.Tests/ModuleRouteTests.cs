using System.Reflection;
using Dsg.Hrms.Api.Audit;
using Dsg.Hrms.Api.Personnel;
using Dsg.Hrms.Api.Reference;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Dsg.Hrms.Architecture.Tests;

/// <summary>
/// Personel islemleri <c>/api/v1/personnel/</c>, referans veri <c>/api/v1/reference/</c>, denetim
/// kayitlari <c>/api/v1/audit/</c> altinda sunulur (SYG-PERS-074, ADR-0010 §2). Kimlik uclarinin
/// karsiligi <see cref="IdentityRouteTests"/>.
/// </summary>
/// <remarks>
/// Sozlesmenin OpenAPI belgesiyle esitligi CI'da ayrica denetlenir ("OpenAPI belgesi guncel mi").
/// Hatalarin Problem Details bicimi uygulama genelindeki ara katmandandir ve
/// <c>ProblemDetailsTests</c> ile dogrulanir; listelerin sayfalamasi uc testlerinde
/// (<c>PersonListApiTests</c>, <c>AuditEntriesApiTests</c>) sinanir.
/// </remarks>
public sealed class ModuleRouteTests
{
    public static TheoryData<Type, string> Modules => new()
    {
        { typeof(PersonsController), "api/v1/personnel/" },
        { typeof(ReferenceDefinitionsController), "api/v1/reference/" },
        { typeof(AuditEntriesController), "api/v1/audit/" },
    };

    [Theory]
    [MemberData(nameof(Modules))]
    public void Every_endpoint_of_the_module_is_under_its_path(Type sample, string prefix)
    {
        ArgumentNullException.ThrowIfNull(sample);
        var routes = RoutesOf(sample.Namespace!);

        routes.ShouldNotBeEmpty();
        routes.Where(route => !route.StartsWith(prefix, StringComparison.Ordinal)).ShouldBeEmpty();
    }

    /// <summary>Ad alanindaki denetleyicilerin bütün uc yollari, bastaki "/" atilmis ve "/" ile biter.</summary>
    internal static List<string> RoutesOf(string controllerNamespace)
    {
        var controllers = typeof(PersonsController).Assembly.GetTypes()
            .Where(t => t.Namespace == controllerNamespace && typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract)
            .ToList();

        var routes = new List<string>();
        foreach (var controller in controllers)
        {
            var classRoute = controller.GetCustomAttribute<RouteAttribute>()?.Template;
            foreach (var action in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                foreach (var verb in action.GetCustomAttributes<HttpMethodAttribute>())
                {
                    routes.Add(RouteOf(classRoute, verb.Template));
                }
            }
        }

        return routes;
    }

    /// <summary>"/" veya "api/" ile baslayan eylem yolu sinif yolunu ezer (ASP.NET Core kurali).</summary>
    internal static string RouteOf(string? classRoute, string? actionTemplate) =>
        actionTemplate is { } template && (template.StartsWith('/') || template.StartsWith("api/", StringComparison.Ordinal))
            ? template.TrimStart('/').TrimEnd('/') + "/"
            : $"{classRoute}/{actionTemplate}".TrimEnd('/') + "/";
}
