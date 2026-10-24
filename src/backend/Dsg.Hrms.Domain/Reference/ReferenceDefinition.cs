using Dsg.Hrms.Domain.Common;

namespace Dsg.Hrms.Domain.Reference;

/// <summary>Referans tanim turu (ADR-0022 §1). Y3 yeni turler ekler.</summary>
public enum ReferenceType
{
    /// <summary>Unvan: LOGO'daki 1–4 kodlarinin karsiliklari (PG-PERS-21).</summary>
    Title = 1,

    /// <summary>Gorev: IK'nin tanimladigi gorevler (PG-PERS-43).</summary>
    Duty = 2,

    /// <summary>Kan grubu: LOGO kodlarinin karsiliklari; sistem tanimi (<c>KR-129</c>).</summary>
    BloodGroup = 3,
}

/// <summary>
/// Referans tanimi (ADR-0022 §1; SYG-PERS-072). Silinmez, pasife alinir (ADR-0004 §5).
/// </summary>
/// <remarks>
/// <para>
/// <b>Sistem tanimi</b> (<see cref="IsSystem"/>) ekrandan degistirilemez ve pasife alinamaz;
/// yalnizca migration ile degisir. Kan grubu tanimlari boyledir: bir adin yanlis duzenlenmesi
/// herkesin saglik verisini sessizce yanlis gosterirdi (<c>KR-129</c>).
/// </para>
/// <para>
/// Pasif tanim yeni secimde listelenmez, ama gecmis kayitlarda adiyla gorunur.
/// </para>
/// </remarks>
public sealed class ReferenceDefinition : Entity, IAuditable
{
    private ReferenceDefinition()
    {
    }

    /// <summary>Tur.</summary>
    public ReferenceType Type { get; private set; }

    /// <summary>Kod; tur icinde tekildir. LOGO kaynakli turlerde LOGO kodudur.</summary>
    [AuditVisible("Tanim kodudur, kisisel veri degildir; degisikligin izlenebilmesi icin gorunmelidir")]
    public string Code { get; private set; } = string.Empty;

    /// <summary>Ekranda gorunen ad.</summary>
    [AuditVisible("Tanim adidir, kisisel veri degildir; ad degisikligi denetim izinde okunabilmelidir")]
    public string Name { get; private set; } = string.Empty;

    /// <summary>Listedeki sira.</summary>
    public int SortOrder { get; private set; }

    /// <summary>Yeni secimde listelenir mi.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Sistem tanimi mi; oyleyse ekrandan degistirilemez.</summary>
    public bool IsSystem { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public long? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }

    /// <summary>Kullanicinin ekledigi yeni tanim (sistem tanimi olamaz).</summary>
    /// <exception cref="InvalidOperationException">Tur yalnizca sistem tanimi tasiyorsa.</exception>
    public static ReferenceDefinition Create(ReferenceType type, string code, string name, int sortOrder)
    {
        if (type == ReferenceType.BloodGroup)
        {
            throw new InvalidOperationException("Kan grubu tanımları yalnızca sistem tarafından tanımlanır.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        var definition = new ReferenceDefinition { Type = type, Code = code.Trim(), SortOrder = sortOrder };
        definition.Rename(name);
        return definition;
    }

    /// <summary>Adi degistirir; degistiyse <c>true</c>.</summary>
    /// <exception cref="InvalidOperationException">Sistem taniminda.</exception>
    public bool Rename(string name)
    {
        EnsureEditable();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var trimmed = name.Trim();
        if (string.Equals(Name, trimmed, StringComparison.Ordinal))
        {
            return false;
        }

        Name = trimmed;
        return true;
    }

    /// <summary>Pasife alir; zaten pasifse <c>false</c>.</summary>
    /// <exception cref="InvalidOperationException">Sistem taniminda.</exception>
    public bool Deactivate()
    {
        EnsureEditable();
        if (!IsActive)
        {
            return false;
        }

        IsActive = false;
        return true;
    }

    private void EnsureEditable()
    {
        if (IsSystem)
        {
            throw new InvalidOperationException("Sistem tanımı ekrandan değiştirilemez.");
        }
    }
}
