using System.Reflection;
using Dsg.Hrms.Api.Personnel;
using Dsg.Hrms.Application.Personnel.Listing;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Architecture.Tests;

/// <summary>
/// T1'de "Yoneticisi" suzgeci API'de ve uygulama katmaninda yoktur (SYG-PERS-056, PG-PERS-34).
/// Suzgec T2 Organizasyon Yonetimi'nin yonetici iliskisiyle birlikte eklenir (SYG-PERS AN-12);
/// T2 geldiginde bu test bilerek degistirilir.
/// </summary>
/// <remarks>
/// Ekrandaki karsiligi <c>PersonListPage.test.tsx</c> (suzgec panelinde "Yonetici" yok).
/// </remarks>
public sealed class ManagerFilterAbsenceTests
{
    private static readonly string[] ForbiddenTokens = ["manager", "supervisor", "yonetici"];

    [Fact]
    public void Person_list_query_has_no_manager_parameter()
    {
        var names = QueryNamesOf(typeof(PersonListRequest));

        // Bos kume testi anlamsiz kilar: bilinen suzgeclerin okundugunu da dogrula.
        names.ShouldContain("departmentId");
        names.ShouldContain("bloodGroup");
        names.Where(IsManagerLike).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(typeof(PersonListFilters))]
    [InlineData(typeof(PersonFilterOptions))]
    [InlineData(typeof(PersonFilterOptionsResponse))]
    public void Filter_contracts_have_no_manager_member(Type contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        var members = contract.GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(p => p.Name).ToList();

        members.ShouldNotBeEmpty();
        members.Where(IsManagerLike).ShouldBeEmpty();
    }

    private static List<string> QueryNamesOf(Type request) =>
        request.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(p => p.GetCustomAttribute<FromQueryAttribute>()?.Name ?? p.Name)
            .ToList();

    private static bool IsManagerLike(string name) =>
        ForbiddenTokens.Any(token => name.Contains(token, StringComparison.OrdinalIgnoreCase));
}
