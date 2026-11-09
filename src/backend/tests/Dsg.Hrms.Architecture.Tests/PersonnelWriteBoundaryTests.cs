using System.Reflection;
using Dsg.Hrms.Api.Personnel;
using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Personnel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Hosting;
using NetArchTest.Rules;

namespace Dsg.Hrms.Architecture.Tests;

/// <summary>
/// LOGO kaynakli alanlarla HRMS'te girilen alanlarin siniri (SYG-PERS-006, KR-112) ve tek
/// senkronizasyon zamanlayicisi (SYG-PERS-007).
/// </summary>
/// <remarks>
/// LOGO'dan gelen alanlar icin yazma ucu yoktur; HRMS'te girilen alanlar (gorev, evlilik bilgisi,
/// acil durum kisisi) ayri tablolardadir. Senkronizasyon bu tablolara yazmaz; tek istisna, istihdam
/// bitince acik gorevin bitis tarihinin kapanmasidir (SYG-PERS-021, AN-29). Gorevin kendisi
/// degismez, boylece senkronizasyon IK'nin girdigini ezemez.
/// </remarks>
public sealed class PersonnelWriteBoundaryTests
{
    /// <summary>
    /// Personel modulundeki yazma uclari ve gerekceleri. Yeni bir yazma ucu buraya gerekcesiyle
    /// eklenmeden test duser; LOGO kaynakli bir alana yazan uc bu listeye giremez.
    /// </summary>
    private static readonly Dictionary<string, string> WriteEndpoints = new()
    {
        ["POST api/v1/personnel/corrections/me/"] = "IK'ya bildir: duzeltme talebi ayri tabloya yazilir; LOGO alani degismez (SYG-PERS-045)",
        ["POST api/v1/personnel/corrections/{requestId:guid}/complete/"] = "Talebi kapatir; duzeltme LOGO'da yapilir (SYG-PERS-048)",
        ["POST api/v1/personnel/persons/{personId:guid}/emergency-contacts/"] = "Acil durum kisisi: HRMS'te girilen alan (KR-112)",
        ["PUT api/v1/personnel/persons/{personId:guid}/emergency-contacts/{contactId:guid}/"] = "Acil durum kisisi: HRMS'te girilen alan (KR-112)",
        ["DELETE api/v1/personnel/persons/{personId:guid}/emergency-contacts/{contactId:guid}/"] = "Acil durum kisisi: HRMS'te girilen alan (KR-112)",
        ["POST api/v1/personnel/persons/{personId:guid}/employments/{registryCode}/duties/"] = "Gorev: HRMS'te girilen alan (KR-112)",
        ["DELETE api/v1/personnel/persons/{personId:guid}/employments/{registryCode}/duties/{dutyId:guid}/"] = "Gorev: HRMS'te girilen alan (KR-112)",
        ["PUT api/v1/personnel/persons/{personId:guid}/marriage/"] = "Evlilik bilgisi: HRMS'te girilen alan (KR-112)",
        ["DELETE api/v1/personnel/persons/{personId:guid}/marriage/"] = "Evlilik bilgisi: HRMS'te girilen alan (KR-112)",
        ["POST api/v1/personnel/encryption/re-encryption/"] = "Kan grubunu degeri degistirmeden yeni anahtarla yeniden sifreler (SYG-PERS-062, KR-138)",
        ["POST api/v1/personnel/sync-runs/"] = "Senkronizasyonu tetikler; LOGO kaynakli tablolara yalnizca senkronizasyon yazar (SYG-PERS-008)",
    };

    [Fact]
    public void Every_personnel_write_endpoint_is_listed_with_its_reason()
    {
        var writes = new List<string>();
        foreach (var controller in typeof(PersonsController).Assembly.GetTypes()
            .Where(t => t.Namespace == typeof(PersonsController).Namespace && typeof(ControllerBase).IsAssignableFrom(t) && !t.IsAbstract))
        {
            var classRoute = controller.GetCustomAttribute<RouteAttribute>()?.Template;
            foreach (var action in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                foreach (var verb in action.GetCustomAttributes<HttpMethodAttribute>())
                {
                    foreach (var method in verb.HttpMethods.Where(m => m != "GET"))
                    {
                        writes.Add($"{method} {ModuleRouteTests.RouteOf(classRoute, verb.Template)}");
                    }
                }
            }
        }

        writes.ShouldNotBeEmpty();
        writes.Except(WriteEndpoints.Keys).ShouldBeEmpty("Listede olmayan yazma ucu: gerekcesiyle WriteEndpoints'e eklenmeli.");
        WriteEndpoints.Keys.Except(writes).ShouldBeEmpty("Listede olup kodda olmayan uc: liste bayatladi.");
    }

    [Fact]
    public void Synchronization_does_not_depend_on_hrms_entered_fields()
    {
        // Acil durum kisisi ve evlilik bilgisi senkronizasyonun hicbir sinifinda gecmez.
        var dependants = SyncDependantsOn(typeof(EmergencyContact), typeof(PersonMarriage));

        dependants.ShouldBeEmpty("Senkronizasyon HRMS'te girilen alanlara yazmaz (SYG-PERS-006).");
    }

    [Fact]
    public void Synchronization_touches_duties_only_to_close_them_on_termination()
    {
        // Gorev yalnizca istihdam bitince kapatilmak uzere yuklenir (SYG-PERS-021, AN-29).
        string[] allowed =
        [
            typeof(PersonnelDetailsSynchronizer).FullName!,
            typeof(PersonnelDetailsSnapshot).FullName!,
            typeof(PersonnelSyncStore).FullName!,
        ];
        var dependants = SyncDependantsOn(typeof(EmploymentDuty));

        // Pozitif kontrol: kural gercek bagimliligi taniyor (bossa hicbir sey denetlenmiyordur).
        dependants.ShouldContain(typeof(PersonnelDetailsSynchronizer).FullName!);
        dependants.Except(allowed).ShouldBeEmpty();
    }

    [Fact]
    public void Synchronization_runs_on_the_single_t3_scheduler()
    {
        // SYG-PERS-007: T1 alanlari T3'un periyodik calismasinda senkronize edilir; ayri bir
        // zamanlayici kurulmaz. Uygulamadaki arka plan isleri bunlardir.
        var workers = typeof(PersonnelSyncWorker).Assembly.GetTypes()
            .Where(t => typeof(BackgroundService).IsAssignableFrom(t) && !t.IsAbstract)
            .Select(t => t.Name)
            .Order(StringComparer.Ordinal)
            .ToList();

        workers.ShouldBe(["NotificationDispatcher", "PersonnelSyncWorker"]);
    }

    /// <summary>
    /// Verilen tiplere bagimli senkronizasyon siniflari (ic siniflar dis sinifin adiyla): uygulama
    /// akisi (<c>Application.Personnel.Sync</c>) ve altyapidaki <c>PersonnelSync*</c> siniflari.
    /// </summary>
    private static List<string> SyncDependantsOn(params Type[] types) =>
        Types.InAssemblies([typeof(PersonnelSyncService).Assembly, typeof(PersonnelSyncStore).Assembly])
            .That().HaveDependencyOnAny([.. types.Select(t => t.FullName!)])
            .GetTypes()
            .Select(t => (t.DeclaringType ?? t).FullName!)
            .Where(name => name.StartsWith(typeof(PersonnelSyncService).Namespace + ".", StringComparison.Ordinal)
                || name.StartsWith(typeof(PersonnelSyncStore).Namespace + ".PersonnelSync", StringComparison.Ordinal))
            .Distinct()
            .Order(StringComparer.Ordinal)
            .ToList();
}
