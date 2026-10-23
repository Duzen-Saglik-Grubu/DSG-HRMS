using Dsg.Hrms.Domain.Common;

namespace Dsg.Hrms.Domain.Organization;

/// <summary>
/// Sube (LOGO <c>L_CAPIDIV</c>); sehir dahil. Firmaya baglidir (ADR-0021 §3).
/// </summary>
/// <remarks>
/// LOGO'da birim subeye degil firmaya baglidir; "firma → sube → birim" hiyerarsisi LOGO
/// verisinde yoktur. Birimleri birbirine baglayan yapi T2'dedir (<c>organization_unit</c>,
/// ADR-0005 §5); T1 yalnizca iki duz tanimi kurar.
/// </remarks>
public sealed class Branch : Entity, IAuditable
{
    private Branch()
    {
    }

    /// <summary>Firmanin kimligi.</summary>
    public long CompanyId { get; private set; }

    /// <summary>LOGO sube numarasi; firma icinde tekildir.</summary>
    public short LogoNumber { get; private set; }

    /// <summary>Sube adi.</summary>
    [AuditVisible("Kurum tanimidir, kisisel veri degildir; sube adinin degisikligi izlenebilmelidir")]
    public string Name { get; private set; } = string.Empty;

    /// <summary>Subenin sehri; LOGO'da bossa <c>null</c>.</summary>
    [AuditVisible("Kurum tanimidir, kisisel veri degildir; subenin sehri kart ve suzgecte gosterilir")]
    public string? City { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public long? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }

    /// <summary>LOGO tanimindan yeni sube.</summary>
    public static Branch FromLogo(long companyId, short logoNumber, string name, string? city)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(companyId);
        var branch = new Branch { CompanyId = companyId, LogoNumber = logoNumber };
        branch.Apply(name, city);
        return branch;
    }

    /// <summary>Ad ve sehri gunceller; degistiyse <c>true</c>.</summary>
    public bool Apply(string name, string? city)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmedName = name.Trim();
        var trimmedCity = string.IsNullOrWhiteSpace(city) ? null : city.Trim();

        if (string.Equals(Name, trimmedName, StringComparison.Ordinal) && string.Equals(City, trimmedCity, StringComparison.Ordinal))
        {
            return false;
        }

        Name = trimmedName;
        City = trimmedCity;
        return true;
    }
}

/// <summary>Birim (LOGO <c>L_CAPIDEPT</c>). Firmaya baglidir (ADR-0021 §3).</summary>
public sealed class Department : Entity, IAuditable
{
    private Department()
    {
    }

    /// <summary>Firmanin kimligi.</summary>
    public long CompanyId { get; private set; }

    /// <summary>LOGO birim numarasi; firma icinde tekildir.</summary>
    public short LogoNumber { get; private set; }

    /// <summary>Birim adi.</summary>
    [AuditVisible("Kurum tanimidir, kisisel veri degildir; birim adinin degisikligi izlenebilmelidir")]
    public string Name { get; private set; } = string.Empty;

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public long? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }

    /// <summary>LOGO tanimindan yeni birim.</summary>
    public static Department FromLogo(long companyId, short logoNumber, string name)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(companyId);
        var department = new Department { CompanyId = companyId, LogoNumber = logoNumber };
        department.Rename(name);
        return department;
    }

    /// <summary>Adi gunceller; degistiyse <c>true</c>.</summary>
    public bool Rename(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        var trimmed = name.Trim();
        if (string.Equals(Name, trimmed, StringComparison.Ordinal))
        {
            return false;
        }

        Name = trimmed;
        return true;
    }
}
