using System.Reflection;
using NetArchTest.Rules;

namespace Dsg.Hrms.Architecture.Tests;

/// <summary>
/// ADR-0002'de tanimlanan katman bagimlilik kurallarini denetler.
///
/// Bu testler, mimari kurallarin zamanla asinmasini onler: kural ihlali
/// derlemeyi degil TESTI kirar ve Pull Request birlestirilemez.
/// </summary>
public sealed class KatmanBagimlilikTestleri
{
    private const string Application = "Dsg.Hrms.Application";
    private const string Infrastructure = "Dsg.Hrms.Infrastructure";
    private const string Api = "Dsg.Hrms.Api";

    private static Assembly DomainAssembly => typeof(global::Dsg.Hrms.Domain.AssemblyIsareti).Assembly;
    private static Assembly ApplicationAssembly => typeof(global::Dsg.Hrms.Application.AssemblyIsareti).Assembly;
    private static Assembly InfrastructureAssembly => typeof(global::Dsg.Hrms.Infrastructure.AssemblyIsareti).Assembly;

    // ------------------------------------------------------------------
    // Kural 1: Domain hicbir seye bagimli degildir.
    // ------------------------------------------------------------------

    [Fact]
    public void Domain_diger_katmanlara_bagimli_olamaz()
    {
        var sonuc = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Application, Infrastructure, Api)
            .GetResult();

        IhlalMesaji(sonuc, "Domain katmani yalnizca kendi icinde calisir (ADR-0002, Kural 1).")
            .ShouldBeNull();
    }

    [Fact]
    public void Domain_altyapi_kutuphanelerine_bagimli_olamaz()
    {
        // Domain saf is mantigidir: veri erisimi, HTTP ve seri hale getirme
        // ayrintilarini bilmez. Bu, is kurallarinin izole test edilebilmesini saglar.
        var sonuc = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Microsoft.EntityFrameworkCore",
                "Microsoft.AspNetCore",
                "Npgsql",
                "Newtonsoft.Json",
                "System.Text.Json")
            .GetResult();

        IhlalMesaji(sonuc, "Domain katmani altyapi kutuphanelerine bagimli olamaz (ADR-0002, Kural 1).")
            .ShouldBeNull();
    }

    // ------------------------------------------------------------------
    // Kural 2: Application yalnizca Domain'e bagimlidir.
    // ------------------------------------------------------------------

    [Fact]
    public void Application_Infrastructure_veya_Api_ye_bagimli_olamaz()
    {
        var sonuc = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny(Infrastructure, Api)
            .GetResult();

        IhlalMesaji(sonuc,
                "Application katmani dis dunyaya yalnizca arayuzlerle erisir; " +
                "uygulamalari Infrastructure'da bulunur (ADR-0002, Kural 2).")
            .ShouldBeNull();
    }

    [Fact]
    public void Application_veri_erisim_kutuphanelerine_bagimli_olamaz()
    {
        var sonuc = Types.InAssembly(ApplicationAssembly)
            .ShouldNot()
            .HaveDependencyOnAny("Microsoft.EntityFrameworkCore", "Npgsql", "Microsoft.AspNetCore")
            .GetResult();

        IhlalMesaji(sonuc,
                "Application katmani EF Core ve ASP.NET ayrintilarini bilmez; " +
                "aksi hâlde kullanim senaryolari altyapidan bagimsiz test edilemez.")
            .ShouldBeNull();
    }

    // ------------------------------------------------------------------
    // Kural 3: Infrastructure, Api'ye bagimli olamaz.
    // ------------------------------------------------------------------

    [Fact]
    public void Infrastructure_Api_ye_bagimli_olamaz()
    {
        var sonuc = Types.InAssembly(InfrastructureAssembly)
            .ShouldNot()
            .HaveDependencyOn(Api)
            .GetResult();

        IhlalMesaji(sonuc, "Bagimlilik yonu Api -> Infrastructure seklindedir, tersi degil (ADR-0002, Kural 3).")
            .ShouldBeNull();
    }

    // ------------------------------------------------------------------
    // Kural 5: Modul yalitimi.
    // ------------------------------------------------------------------

    [Fact]
    public void Moduller_birbirinin_ic_siniflarina_erisemez()
    {
        // Bir modul, baska bir modulun yalnizca acikca yayimladigi arayuzleri
        // kullanabilir (Modules/<Modul>/Abstractions). Diger her sey icseldir.
        //
        // Modul bulunmadigi surece bu test bos gecer; ilk modul eklendiginde
        // kendiliginden anlamli hâle gelir.
        var ihlaller = new List<string>();

        foreach (var assembly in new[] { DomainAssembly, ApplicationAssembly, InfrastructureAssembly })
        {
            var modulAdlari = ModulAdlariniBul(assembly);

            foreach (var modul in modulAdlari)
            {
                var digerModuller = modulAdlari
                    .Where(m => m != modul)
                    .Select(m => $"{assembly.GetName().Name}.Modules.{m}")
                    .ToArray();

                if (digerModuller.Length == 0)
                {
                    continue;
                }

                var sonuc = Types.InAssembly(assembly)
                    .That().ResideInNamespace($"{assembly.GetName().Name}.Modules.{modul}")
                    .ShouldNot().HaveDependencyOnAny(digerModuller)
                    .GetResult();

                if (!sonuc.IsSuccessful)
                {
                    ihlaller.AddRange(sonuc.FailingTypeNames.Select(t => $"{modul}: {t}"));
                }
            }
        }

        ihlaller.ShouldBeEmpty(
            "Moduller arasi dogrudan erisim yasaktir; yalnizca yayimlanan arayuzler " +
            "uzerinden konusulur (ADR-0002, Kural 5).");
    }

    // ------------------------------------------------------------------
    // Yardimcilar
    // ------------------------------------------------------------------

    private static string[] ModulAdlariniBul(Assembly assembly)
    {
        var onEk = $"{assembly.GetName().Name}.Modules.";

        return assembly.GetTypes()
            .Select(t => t.Namespace)
            .Where(ns => ns is not null && ns.StartsWith(onEk, StringComparison.Ordinal))
            .Select(ns => ns![onEk.Length..].Split('.')[0])
            .Distinct(StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>
    /// Basarisiz kuralda okunabilir bir mesaj uretir; basarili ise <c>null</c> doner.
    /// Amaç, testin "false bekleniyordu true geldi" yerine ihlal eden tip adlarini
    /// ve kuralin gerekcesini gostermesidir.
    /// </summary>
    private static string? IhlalMesaji(TestResult sonuc, string kural)
    {
        if (sonuc.IsSuccessful)
        {
            return null;
        }

        var tipler = string.Join(Environment.NewLine, sonuc.FailingTypeNames.Select(t => $"  - {t}"));
        return $"{kural}{Environment.NewLine}Kurali ihlal eden tipler:{Environment.NewLine}{tipler}";
    }
}
