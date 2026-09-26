using System.Reflection;
using NetArchTest.Rules;

namespace Dsg.Hrms.Architecture.Tests;

/// <summary>
/// ADR-0002'de tanimlanan katman bagimlilik kurallarini denetler.
///
/// Bu testler, mimari kurallarin zamanla asinmasini onler: kural ihlali
/// derlemeyi degil TESTI kirar ve Pull Request birlestirilemez.
/// </summary>
public sealed class LayerDependencyTests
{
    private const string ApplicationLayer = "Dsg.Hrms.Application";
    private const string InfrastructureLayer = "Dsg.Hrms.Infrastructure";
    private const string ApiLayer = "Dsg.Hrms.Api";

    private static Assembly DomainAssembly => typeof(Dsg.Hrms.Domain.AssemblyMarker).Assembly;
    private static Assembly ApplicationAssembly => typeof(Dsg.Hrms.Application.AssemblyMarker).Assembly;
    private static Assembly InfrastructureAssembly => typeof(Dsg.Hrms.Infrastructure.AssemblyMarker).Assembly;

    // ------------------------------------------------------------------
    // Kural 1: Domain hicbir seye bagimli degildir.
    // ------------------------------------------------------------------

    [Fact]
    public void Domain_should_not_depend_on_other_layers()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(ApplicationLayer, InfrastructureLayer, ApiLayer)
            .GetResult();

        ViolationMessage(result, "Domain katmani yalnizca kendi icinde calisir (ADR-0002, Kural 1).")
            .ShouldBeNull();
    }

    [Fact]
    public void Domain_should_not_depend_on_infrastructure_libraries()
    {
        // Domain saf is mantigidir: veri erisimi, HTTP ve seri hale getirme
        // ayrintilarini bilmez. Bu, is kurallarinin izole test edilebilmesini saglar.
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Npgsql",
                "Newtonsoft.Json",
                "System.Text.Json")
            .GetResult();

        ViolationMessage(result, "Domain katmani altyapi kutuphanelerine bagimli olamaz (ADR-0002, Kural 1).")
            .ShouldBeNull();
    }

    // ------------------------------------------------------------------
    // Kural 2: Application yalnizca Domain'e bagimlidir.
    // ------------------------------------------------------------------

    [Fact]
    public void Application_should_not_depend_on_infrastructure_or_api()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(InfrastructureLayer, ApiLayer)
            .GetResult();

        ViolationMessage(result,
                "Application katmani dis dunyaya yalnizca arayuzlerle erisir; " +
                "uygulamalari Infrastructure'da bulunur (ADR-0002, Kural 2).")
            .ShouldBeNull();
    }

    [Fact]
    public void Application_should_not_depend_on_data_access_libraries()
    {
        var result = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Npgsql", "Microsoft.AspNetCore")
            .GetResult();

        ViolationMessage(result,
                "Application katmani EF Core ve ASP.NET ayrintilarini bilmez; " +
                "aksi hâlde kullanim senaryolari altyapidan bagimsiz test edilemez.")
            .ShouldBeNull();
    }

    // ------------------------------------------------------------------
    // Kural 3: Infrastructure, Api'ye bagimli olamaz.
    // ------------------------------------------------------------------

    [Fact]
    public void Infrastructure_should_not_depend_on_api()
    {
        var result = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(ApiLayer)
            .GetResult();

        ViolationMessage(result, "Bagimlilik yonu Api -> Infrastructure seklindedir, tersi degil (ADR-0002, Kural 3).")
            .ShouldBeNull();
    }

    // ------------------------------------------------------------------
    // Kural 4b: LOGO yalitimi (ADR-0003 §2, KR-003).
    // ------------------------------------------------------------------

    private const string LogoNamespace = "Dsg.Hrms.Infrastructure.Logo";

    /// <summary>LOGO'ya erisim saglayan tipler; yalnizca Logo klasorunde kullanilabilir.</summary>
    private static readonly string[] LogoAccessTypes =
    [
        "Microsoft.Data.SqlClient",
        "Dsg.Hrms.Infrastructure.Logo.LogoDbContext",
        "Dsg.Hrms.Infrastructure.Logo.LogoPersonnelSource",
    ];

    [Fact]
    public void Logo_access_stays_inside_the_logo_folder()
    {
        // LOGO'ya erisen tum kod tek klasorde toplanir; baska bir sinif LOGO baglamini
        // dogrudan kullanirsa salt-okunur kilitler (yetki denetimi, sema denetimi)
        // atlanabilirdi. Kayit noktasi (InfrastructureRegistration) istisnadir.
        var result = Types.InAssembly(InfrastructureAssembly)
            .That().DoNotResideInNamespace(LogoNamespace)
            .And().DoNotHaveName("InfrastructureRegistration")
            .ShouldNot().HaveDependencyOnAny(LogoAccessTypes)
            .GetResult();

        ViolationMessage(result, "LOGO'ya yalnizca Infrastructure/Logo altindan erisilir (ADR-0003 §2).")
            .ShouldBeNull();
    }

    [Fact]
    public void Logo_isolation_rule_is_not_vacuous()
    {
        // Pozitif kontrol: kural gercek bir bagimliligi taniyor. Tanimasaydi yukaridaki
        // test hicbir seyi denetlemeden gecerdi.
        Types.InAssembly(InfrastructureAssembly)
            .That().HaveName("LogoPersonnelSource")
            .Should().HaveDependencyOn("Dsg.Hrms.Infrastructure.Logo.LogoDbContext")
            .GetResult().IsSuccessful.ShouldBeTrue();
    }

    [Fact]
    public void Api_does_not_touch_logo()
    {
        var result = Types.InAssembly(typeof(Program).Assembly)
            .ShouldNot().HaveDependencyOnAny([LogoNamespace, "Microsoft.Data.SqlClient"])
            .GetResult();

        ViolationMessage(result, "Api katmani LOGO'ya erismez; senkronizasyon durumu IPersonnelSyncStatus ile okunur.")
            .ShouldBeNull();
    }

    // ------------------------------------------------------------------
    // Kural 5: Modul yalitimi.
    // ------------------------------------------------------------------

    [Fact]
    public void Modules_should_not_depend_on_each_others_internals()
    {
        // Bir modul, baska bir modulun yalnizca acikca yayimladigi arayuzleri
        // kullanabilir (Modules/<Modul>/Abstractions). Diger her sey icseldir.
        //
        // Modul bulunmadigi surece bu test bos gecer; ilk modul eklendiginde
        // kendiliginden anlamli hâle gelir.
        var violations = new List<string>();

        foreach (var assembly in new[] { DomainAssembly, ApplicationAssembly, InfrastructureAssembly })
        {
            var moduleNames = FindModuleNames(assembly);

            foreach (var module in moduleNames)
            {
                var otherModules = moduleNames
                    .Where(m => m != module)
                    .Select(m => $"{assembly.GetName().Name}.Modules.{m}")
                    .ToArray();

                if (otherModules.Length == 0)
                {
                    continue;
                }

                var result = Types.InAssembly(assembly)
                    .That().ResideInNamespace($"{assembly.GetName().Name}.Modules.{module}")
                    .ShouldNot().HaveDependencyOnAny(otherModules)
                    .GetResult();

                if (!result.IsSuccessful)
                {
                    violations.AddRange(result.FailingTypeNames.Select(t => $"{module}: {t}"));
                }
            }
        }

        violations.ShouldBeEmpty(
            "Moduller arasi dogrudan erisim yasaktir; yalnizca yayimlanan arayuzler " +
            "uzerinden konusulur (ADR-0002, Kural 5).");
    }

    // ------------------------------------------------------------------
    // Yardimcilar
    // ------------------------------------------------------------------

    private static string[] FindModuleNames(Assembly assembly)
    {
        var prefix = $"{assembly.GetName().Name}.Modules.";

        return assembly.GetTypes()
            .Select(t => t.Namespace)
            .Where(ns => ns is not null && ns.StartsWith(prefix, StringComparison.Ordinal))
            .Select(ns => ns![prefix.Length..].Split('.')[0])
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Basarisiz kuralda okunabilir bir mesaj uretir; basarili ise <c>null</c> doner.
    /// Amaç, testin "false bekleniyordu true geldi" yerine ihlal eden tip adlarini
    /// ve kuralin gerekcesini gostermesidir.
    /// </summary>
    private static string? ViolationMessage(TestResult result, string rule)
    {
        if (result.IsSuccessful)
        {
            return null;
        }

        var types = string.Join(Environment.NewLine, result.FailingTypeNames.Select(t => $"  - {t}"));
        return $"{rule}{Environment.NewLine}Kurali ihlal eden tipler:{Environment.NewLine}{types}";
    }
}
