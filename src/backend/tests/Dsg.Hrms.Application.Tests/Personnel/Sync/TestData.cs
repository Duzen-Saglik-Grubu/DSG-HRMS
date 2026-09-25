using System.Globalization;
using Dsg.Hrms.Application.Personnel.Sync;

namespace Dsg.Hrms.Application.Tests.Personnel.Sync;

/// <summary>
/// Senkronizasyon testleri icin SENTETIK LOGO kartlari.
/// </summary>
/// <remarks>
/// Gercek kisilere ait veri KULLANILMAZ (KVKK). TCKN'ler sagla algoritmasiyla
/// uretilir; ad, e-posta ve telefonlar uydurmadir.
/// </remarks>
internal static class TestData
{
    public static readonly DateOnly Today = new(2026, 9, 26);

    /// <summary>Sirali numarayla gecerli bir TCKN uretir.</summary>
    public static string NationalId(int index)
    {
        var firstNine = (100000000 + index).ToString(CultureInfo.InvariantCulture);
        var d = firstNine.Select(c => c - '0').ToArray();
        var tenth = ((((d[0] + d[2] + d[4] + d[6] + d[8]) * 7) - (d[1] + d[3] + d[5] + d[7])) % 10 + 10) % 10;
        var eleventh = (d.Sum() + tenth) % 10;
        return $"{firstNine}{tenth}{eleventh}";
    }

    public static LogoPersonnelRecord Card(
        string registryCode,
        string? nationalId,
        int logoRef = 1,
        string firstName = "Ahmet",
        string lastName = "Yilmaz",
        DateOnly? birthDate = null,
        DateOnly? hireDate = null,
        DateOnly? terminationDate = null,
        short firm = 1,
        string? firmName = "Duzen Laboratuvarlar",
        string[]? emails = null,
        string[]? phones = null) =>
        new(
            logoRef,
            registryCode,
            nationalId,
            firstName,
            lastName,
            birthDate ?? new DateOnly(1985, 4, 12),
            hireDate ?? new DateOnly(2020, 1, 1),
            terminationDate,
            firm,
            firmName,
            emails ?? [$"{registryCode}@duzen.com.tr"],
            phones ?? ["05321234567"]);
}
