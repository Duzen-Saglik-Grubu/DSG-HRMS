using Dsg.Hrms.Domain.Common;

namespace Dsg.Hrms.Domain.Organization;

/// <summary>
/// Firma (ADR-0005 §5).
/// </summary>
/// <remarks>
/// <para>
/// T3 kapsaminda yalnizca istihdamin bagli oldugu firmayi tanimlamak icin gereken
/// alanlar vardir (<c>KR-077</c>). Sube, birim ve tarih aralikli organizasyon
/// yapisi T2 Organizasyon modulunde bu varligi GENISLETIR.
/// </para>
/// <para>
/// Firma LOGO'dan senkronize edilir; <see cref="LogoFirmNumber"/> eslestirme
/// anahtaridir.
/// </para>
/// </remarks>
public sealed class Company : Entity, IAuditable
{
    private Company()
    {
    }

    /// <summary>LOGO firma numarasi (<c>L_CAPIFIRM.NR</c>). Tekildir.</summary>
    public short LogoFirmNumber { get; private set; }

    /// <summary>Firma adi.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public long? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }

    /// <summary>LOGO kaydindan yeni firma olusturur.</summary>
    public static Company FromLogo(short logoFirmNumber, string name) =>
        new() { LogoFirmNumber = logoFirmNumber, Name = name };

    /// <summary>Firma adini gunceller. Degisiklik olduysa <c>true</c> doner.</summary>
    public bool Rename(string name)
    {
        if (string.Equals(Name, name, StringComparison.Ordinal))
        {
            return false;
        }

        Name = name;
        return true;
    }
}
