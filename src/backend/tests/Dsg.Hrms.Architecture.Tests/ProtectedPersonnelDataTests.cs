using Dsg.Hrms.Domain.Personnel;
using NetArchTest.Rules;

namespace Dsg.Hrms.Architecture.Tests;

/// <summary>
/// Korumali kisi alanlarina yalnizca listelenen siniflar erisir (ADR-0019 §1, §3; SYG-PERS-054).
/// </summary>
/// <remarks>
/// <para>
/// Korumali alanlar (adres; sonra kan grubu, evlilik bilgisi, acil durum kisileri) kisi
/// tablosunda degil ayri tiplerdedir. Bu tiplere bagimli olabilen siniflar asagida sabittir:
/// kapsamli sorgu bileseni, senkronizasyon ve EF eslemesi. Kimlik modulu (hesap listesi, oturum
/// deposu) hicbirine bagimli olamaz; olsaydi T1'in alan politikasini atlayan bir yol acilirdi.
/// </para>
/// <para>
/// Listeye sinif eklemek PR incelemesinde gorunen bir guvenlik kararidir.
/// </para>
/// </remarks>
public sealed class ProtectedPersonnelDataTests
{
    private static readonly string[] ProtectedTypes = [typeof(PersonSensitive).FullName!];

    /// <summary>Korumali tiplere bagimli olabilen siniflar (ic ice tipler dis sinifla sayilir).</summary>
    private static readonly string[] Allowed =
    [
        // Alan modeli: tipin kendisi.
        "Dsg.Hrms.Domain.Personnel.PersonSensitive",
        // Tek giris noktasi: kart ve liste okumalari (ADR-0019 §1).
        "Dsg.Hrms.Infrastructure.Personnel.PersonnelQuery",
        // Senkronizasyon: LOGO'dan gelen degerin yazilmasi (SYG-PERS-003).
        "Dsg.Hrms.Application.Personnel.Sync.PersonnelDetailsSynchronizer",
        "Dsg.Hrms.Application.Personnel.Sync.PersonnelDetailsSnapshot",
        "Dsg.Hrms.Application.Personnel.Sync.DetailsOutcome",
        "Dsg.Hrms.Infrastructure.Personnel.PersonnelSyncStore",
        // EF eslemesi.
        "Dsg.Hrms.Infrastructure.Data.Configurations.PersonSensitiveConfiguration",
    ];

    [Fact]
    public void Only_the_listed_classes_depend_on_protected_personnel_types()
    {
        var dependants = Types.InAssemblies([
                typeof(Dsg.Hrms.Domain.AssemblyMarker).Assembly,
                typeof(Dsg.Hrms.Application.AssemblyMarker).Assembly,
                typeof(Dsg.Hrms.Infrastructure.AssemblyMarker).Assembly,
                typeof(Program).Assembly,
            ])
            .That().HaveDependencyOnAny(ProtectedTypes)
            .GetTypes()
            .Select(t => (t.DeclaringType ?? t).FullName!)
            .Where(name => !name.Contains(".Migrations.", StringComparison.Ordinal))
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToList();

        // Denetimin kendisi: bilinen bir bagimliyi bulamiyorsa hicbir seyi denetlemiyordur.
        dependants.ShouldContain("Dsg.Hrms.Infrastructure.Personnel.PersonnelQuery");
        dependants.Except(Allowed).ShouldBeEmpty("Korumali kisi tiplerine yeni bir bagimlilik eklendi; ADR-0019 §1 listesi bilincli olarak guncellenmeli");
    }

    [Fact]
    public void Identity_module_never_depends_on_protected_personnel_types()
    {
        var result = Types.InAssemblies([
                typeof(Dsg.Hrms.Application.AssemblyMarker).Assembly,
                typeof(Dsg.Hrms.Infrastructure.AssemblyMarker).Assembly,
                typeof(Program).Assembly,
            ])
            .That().ResideInNamespaceMatching(@"^Dsg\.Hrms\.[A-Za-z]+\.Identity(\..*)?$")
            .ShouldNot().HaveDependencyOnAny(ProtectedTypes)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(string.Join(", ", result.FailingTypeNames ?? []));
    }
}
