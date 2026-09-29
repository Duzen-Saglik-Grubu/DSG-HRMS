using Dsg.Hrms.Domain.Common;

namespace Dsg.Hrms.Domain.Identity;

/// <summary>
/// Rol: izinlerin adlandirilmis kumesi (ADR-0007 §1, SYG-KMLK-074).
/// </summary>
/// <remarks>
/// <para>
/// Izinler kodda sabittir; roller yonetilebilir. T3 yalnizca iki hazir rolu migration ile
/// yukler (<see cref="SystemAdministratorCode"/>, <see cref="HrIdentityOperationsCode"/>). Rol
/// yonetim ekrani T4 kapsamindadir.
/// </para>
/// <para>
/// Reddetme (deny) izni yoktur: kullanicinin izinleri rollerinin izinlerinin birlesimidir.
/// </para>
/// </remarks>
public sealed class Role : Entity, IAuditable
{
    /// <summary>Sistem Yoneticisi: tum izinler.</summary>
    public const string SystemAdministratorCode = "system-administrator";

    /// <summary>IK Kimlik Islemleri: hesap islemleri, davet ve senkronizasyon durumu.</summary>
    public const string HrIdentityOperationsCode = "hr-identity-operations";

    private Role()
    {
    }

    /// <summary>Degismez kod; kodda ve yapilandirmada rolu bu ad temsil eder.</summary>
    public string Code { get; private set; } = string.Empty;

    /// <summary>Ekranda gosterilen ad.</summary>
    public string Name { get; private set; } = string.Empty;

    /// <summary>Hazir rol mu; hazir roller silinemez (T4).</summary>
    public bool IsSystem { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public long? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }
}

/// <summary>Rolun sahip oldugu izin (tablo: <c>role_permission</c>). Izin kodu sabit listeden biridir.</summary>
public sealed class RoleGrant
{
    private RoleGrant()
    {
    }

    /// <summary>Rol.</summary>
    public long RoleId { get; private set; }

    /// <summary>Izin kodu (<c>&lt;modul&gt;.&lt;kaynak&gt;.&lt;eylem&gt;</c>).</summary>
    public string Permission { get; private set; } = string.Empty;
}

/// <summary>
/// Hesaba atanmis rol (ADR-0007 §1). Atama ve kaldirma denetim izine yazilir.
/// </summary>
public sealed class UserRole : Entity, IAuditable
{
    private UserRole()
    {
    }

    /// <summary>Hesap.</summary>
    public long UserAccountId { get; private set; }

    /// <summary>Rol.</summary>
    public long RoleId { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public long? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }

    /// <summary>Hesaba rol atar.</summary>
    public static UserRole Assign(long userAccountId, long roleId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(userAccountId);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(roleId);

        return new UserRole { UserAccountId = userAccountId, RoleId = roleId };
    }
}
