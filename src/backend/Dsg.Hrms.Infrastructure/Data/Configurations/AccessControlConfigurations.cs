using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsg.Hrms.Infrastructure.Data.Configurations;

/// <summary>
/// Eylem yetkisi tablolari (ADR-0007 §1, SYG-KMLK-074).
/// </summary>
/// <remarks>
/// Izinler ve hazir roller <b>migration ile</b> yuklenir (<c>HasData</c>). Izin listesi kodda
/// sabittir (<see cref="Permissions"/>); veritabanindaki <c>permission</c> tablosu, bir role
/// listede olmayan bir izin atanmasini yabanci anahtarla engeller.
/// </remarks>
internal static class AccessControlSeed
{
    /// <summary>Hazir rollerin sabit kimlikleri; migration'lar arasinda degismez.</summary>
    public static readonly (long Id, Guid PublicId, string Code, string Name)[] Roles =
    [
        (1, new Guid("0192a3b4-0001-7000-8000-000000000001"), Role.SystemAdministratorCode, "Sistem Yöneticisi"),
        (2, new Guid("0192a3b4-0002-7000-8000-000000000002"), Role.HrIdentityOperationsCode, "İK Kimlik İşlemleri"),
    ];

    /// <summary>Yukleme ani (denetim alani zorunludur).</summary>
    public static readonly DateTimeOffset SeededAt = new(2026, 9, 29, 0, 0, 0, TimeSpan.Zero);
}

/// <summary>Izin tablosu: kodda sabit listenin veritabani karsiligi.</summary>
public sealed class PermissionDefinition
{
    /// <summary>Izin kodu.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Aciklama.</summary>
    public string Description { get; set; } = string.Empty;
}

/// <summary>Izin tablosunun eslemesi.</summary>
public sealed class PermissionDefinitionConfiguration : IEntityTypeConfiguration<PermissionDefinition>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<PermissionDefinition> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("permission", UserAccountConfiguration.IdentitySchema);
        builder.HasKey(permission => permission.Code);
        builder.Property(permission => permission.Code).HasMaxLength(100);
        builder.Property(permission => permission.Description).IsRequired().HasMaxLength(200);

        builder.HasData(Permissions.All.Select(item => new PermissionDefinition { Code = item.Key, Description = item.Value }));
    }
}

/// <summary>Rol tablosunun eslemesi.</summary>
public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("role", UserAccountConfiguration.IdentitySchema);
        builder.HasKey(role => role.Id);
        builder.Property(role => role.Code).IsRequired().HasMaxLength(100);
        builder.Property(role => role.Name).IsRequired().HasMaxLength(200);
        builder.HasIndex(role => role.Code).IsUnique();
        builder.HasIndex(role => role.PublicId).IsUnique();

        builder.MapAuditFields();

        builder.HasData(AccessControlSeed.Roles.Select(role => new
        {
            role.Id,
            role.PublicId,
            role.Code,
            role.Name,
            IsSystem = true,
            CreatedAt = AccessControlSeed.SeededAt,
        }));
    }
}

/// <summary>Rol izni tablosunun eslemesi.</summary>
public sealed class RoleGrantConfiguration : IEntityTypeConfiguration<RoleGrant>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<RoleGrant> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("role_permission", UserAccountConfiguration.IdentitySchema);
        builder.HasKey(item => new { item.RoleId, item.Permission });
        builder.Property(item => item.Permission).HasMaxLength(100);

        builder.HasOne<Role>().WithMany().HasForeignKey(item => item.RoleId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PermissionDefinition>().WithMany().HasForeignKey(item => item.Permission).OnDelete(DeleteBehavior.Restrict);

        builder.HasData(
            from role in AccessControlSeed.Roles
            from permission in Permissions.ByRole[role.Code]
            select new { RoleId = role.Id, Permission = permission });
    }
}

/// <summary>Kullanici rolu tablosunun eslemesi.</summary>
public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("user_role", UserAccountConfiguration.IdentitySchema);
        builder.HasKey(item => item.Id);

        // Bir hesaba ayni rol bir kez atanir.
        builder.HasIndex(item => new { item.UserAccountId, item.RoleId }).IsUnique();

        builder.HasOne<UserAccount>().WithMany().HasForeignKey(item => item.UserAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Role>().WithMany().HasForeignKey(item => item.RoleId).OnDelete(DeleteBehavior.Restrict);

        builder.MapAuditFields();
    }
}
