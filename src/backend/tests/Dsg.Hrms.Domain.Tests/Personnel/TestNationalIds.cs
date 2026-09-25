namespace Dsg.Hrms.Domain.Tests.Personnel;

/// <summary>
/// Testler icin SENTETIK, gecerli TCKN uretir.
/// </summary>
/// <remarks>
/// Gercek kisilere ait TCKN'ler test verisinde KULLANILMAZ (KVKK). Degerler sagla
/// algoritmasiyla ilk dokuz haneden hesaplanir; herhangi bir gercek kisiyle
/// eslesmeleri amaclanmamistir.
/// </remarks>
internal static class TestNationalIds
{
    /// <summary>Ilk dokuz haneden (ilk hane sifir olmamali) gecerli bir TCKN uretir.</summary>
    public static string From(string firstNine)
    {
        var d = firstNine.Select(c => c - '0').ToArray();
        var tenth = ((((d[0] + d[2] + d[4] + d[6] + d[8]) * 7) - (d[1] + d[3] + d[5] + d[7])) % 10 + 10) % 10;
        var eleventh = (d.Sum() + tenth) % 10;
        return $"{firstNine}{tenth}{eleventh}";
    }

    /// <summary>Sirali numarayla gecerli bir TCKN uretir.</summary>
    public static string Sequential(int index) => From((100000000 + index).ToString(System.Globalization.CultureInfo.InvariantCulture));
}
