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
    // Kural 5: Modul yalitimi (#171).
    //
    // Moduller her katmanin dogrudan altindaki ad alanidir
    // (Dsg.Hrms.<Katman>.<Modul>). ADR-0002'deki Modules/<Modul>/Abstractions
    // duzeni uygulanmadi; onceki test o ad alanini aradigi icin HICBIR tipi
    // denetlemeden geciyordu. Bugunku kural: moduller arasi her bagimlilik asagidaki
    // tabloda gerekcesiyle BILDIRILIR. Test, olculen bagimlilik kumesini tabloyla
    // birebir karsilastirir:
    //   - bildirilmemis yeni bagimlilik -> test duser (sinir asindi)
    //   - tabloda olup kodda olmayan bagimlilik -> test duser (tablo bayatladi)
    // ------------------------------------------------------------------

    /// <summary>Is modulleri: her katmanin dogrudan altindaki ad alanlari.</summary>
    private static readonly string[] Modules = ["Identity", "Personnel", "Organization", "Notifications", "Settings", "Audit"];

    /// <summary>
    /// Modul olmayan ortak veya teknik ad alanlari. Yeni bir ust ad alani ya modul ya da
    /// ortak olarak siniflandirilmadikca test duser; boylece yeni modul sessizce denetim
    /// disinda kalmaz.
    /// </summary>
    private static readonly string[] SharedNamespaces = ["Common", "Data", "Configuration", "Logging", "Time", "Logo"];

    /// <summary>Bildirilen moduller arasi bagimliliklar (katman: kaynak -> hedef) ve gerekceleri.</summary>
    private static readonly Dictionary<string, string> AllowedModuleDependencies = new(StringComparer.Ordinal)
    {
        // Istihdam bir firmaya baglidir (ADR-0005).
        ["Domain: Personnel -> Organization"] = "istihdam-firma iliskisi",
        ["Application: Personnel -> Organization"] = "senkronizasyon firma ve birimleri yazar",
        ["Infrastructure: Personnel -> Organization"] = "senkronizasyon deposu",

        // Kimlik, kisinin LOGO'dan gelen bilgileriyle dogrulanir (SYG-KMLK-013, 031).
        ["Application: Identity -> Personnel"] = "uyelik ve giriste kisi ve istihdam",
        ["Infrastructure: Identity -> Personnel"] = "hesap islemleri ve uyelik sorgulari",
        ["Infrastructure: Identity -> Organization"] = "hesap islemleri listesinde firma adi (SYG-KMLK-073)",

        // Istihdam bitince hesap pasife alinir (KR-015, SYG-KMLK-054). Identity <-> Personnel
        // karsilikli bagimliligi BILINCLIDIR: T1 cekirdegi T3 icinde yazildi (KR-077).
        ["Application: Personnel -> Identity"] = "senkronizasyonda hesap yasam dongusu",
        ["Infrastructure: Personnel -> Identity"] = "senkronizasyonda hesap yasam dongusu",

        // Parametreler (SYG-KMLK-075) ve iletim (SYG-KMLK-026…030) ortak hizmettir.
        ["Application: Identity -> Settings"] = "kimlik parametreleri",
        ["Application: Identity -> Notifications"] = "dogrulama kodu ve davet iletisi",
        ["Application: Personnel -> Settings"] = "senkronizasyon periyodu",
        ["Infrastructure: Personnel -> Settings"] = "senkronizasyon periyodu",
        ["Infrastructure: Notifications -> Settings"] = "SMTP ve NetGSM parametreleri",
    };

    [Fact]
    public void Every_top_level_namespace_is_classified_as_module_or_shared()
    {
        var unclassified = LayerAssemblies
            .SelectMany(a => TopLevelNamespaces(a).Select(ns => $"{LayerName(a)}.{ns}"))
            .Where(ns => !Modules.Contains(ns.Split('.')[1]) && !SharedNamespaces.Contains(ns.Split('.')[1]))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        unclassified.ShouldBeEmpty(
            "Yeni ust ad alani modul (Modules) veya ortak (SharedNamespaces) olarak siniflandirilmali; " +
            "aksi halde modul siniri denetlenmez (#171).");
    }

    [Fact]
    public void Module_dependencies_match_the_declared_table()
    {
        var measured = MeasureModuleDependencies();

        // Test bos gecmemeli: bugun bildirilen bagimliliklar olculebiliyor olmali.
        measured.ShouldNotBeEmpty("Moduller arasi hicbir bagimlilik olculmedi; test bir seyi denetlemiyor olabilir (#171).");

        var undeclared = measured.Keys.Except(AllowedModuleDependencies.Keys).Select(k => $"{k}: {measured[k]}").ToList();
        undeclared.ShouldBeEmpty(
            "Bildirilmemis moduller arasi bagimlilik (ADR-0002, Kural 5). Bilincliyse gerekcesiyle " +
            "AllowedModuleDependencies tablosuna eklenir; degilse bagimlilik kaldirilir.");

        var stale = AllowedModuleDependencies.Keys.Except(measured.Keys).ToList();
        stale.ShouldBeEmpty("Tabloda olup kodda artik bulunmayan bagimlilik; tablodan cikarilmali.");
    }

    // ------------------------------------------------------------------
    // Yardimcilar
    // ------------------------------------------------------------------

    private static Assembly[] LayerAssemblies => [DomainAssembly, ApplicationAssembly, InfrastructureAssembly];

    private static string LayerName(Assembly assembly) => assembly.GetName().Name!["Dsg.Hrms.".Length..];

    private static IEnumerable<string> TopLevelNamespaces(Assembly assembly)
    {
        var prefix = $"{assembly.GetName().Name}.";
        return assembly.GetTypes()
            .Select(t => t.Namespace)
            .Where(ns => ns is not null && ns.StartsWith(prefix, StringComparison.Ordinal))
            .Select(ns => ns![prefix.Length..].Split('.')[0])
            .Distinct(StringComparer.Ordinal);
    }

    /// <summary>"Katman: kaynak -> hedef" anahtariyla, bagimliligi tasiyan ornek tip adi.</summary>
    private static Dictionary<string, string> MeasureModuleDependencies()
    {
        var edges = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var assembly in LayerAssemblies)
        {
            foreach (var from in Modules)
            {
                foreach (var to in Modules.Where(m => m != from))
                {
                    // Hedef modulun TUM katmanlari: Application.Identity'nin Domain.Personnel'e
                    // bagimliligi da moduller arasi bagimliliktir.
                    var targets = LayerAssemblies.Select(a => $"{a.GetName().Name}.{to}").ToArray();
                    var result = Types.InAssembly(assembly)
                        .That().ResideInNamespace($"{assembly.GetName().Name}.{from}")
                        .ShouldNot().HaveDependencyOnAny(targets)
                        .GetResult();

                    if (!result.IsSuccessful)
                    {
                        edges[$"{LayerName(assembly)}: {from} -> {to}"] = result.FailingTypeNames[0];
                    }
                }
            }
        }

        return edges;
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
