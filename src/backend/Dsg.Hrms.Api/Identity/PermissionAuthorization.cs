using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Identity.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.Extensions.Options;

namespace Dsg.Hrms.Api.Identity;

/// <summary>
/// Uc noktanin gerektirdigi izin (ADR-0007 §3, SYG-KMLK-074).
/// </summary>
/// <remarks>
/// Kimlik dogrulamasini da gerektirir. Izin yoksa <c>403</c> doner; bu yalnizca ROL kaynakli
/// yetkisizliktir. Kapsam disi bir kayit ise <c>404</c> ile karsilanir (ADR-0007 §4).
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    /// <summary>Politika adlarinin oneki.</summary>
    public const string PolicyPrefix = "permission:";

    /// <summary>Yeni ornek olusturur.</summary>
    /// <param name="permission">Izin kodu (<see cref="Permissions"/>).</param>
    public HasPermissionAttribute(string permission)
        : base(PolicyPrefix + permission)
    {
        if (!Permissions.All.ContainsKey(permission))
        {
            throw new ArgumentException($"'{permission}' sabit izin listesinde yok.", nameof(permission));
        }

        Permission = permission;
    }

    /// <summary>Izin kodu.</summary>
    public string Permission { get; }
}

/// <summary>Izin gereksinimi.</summary>
public sealed record PermissionRequirement(string Permission) : IAuthorizationRequirement;

/// <summary>
/// <c>permission:</c> onekli politikalari istek aninda uretir; boylece her izin icin ayri
/// politika kaydi gerekmez.
/// </summary>
public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        ArgumentNullException.ThrowIfNull(policyName);

        if (!policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.Ordinal))
        {
            return _fallback.GetPolicyAsync(policyName);
        }

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(policyName[HasPermissionAttribute.PolicyPrefix.Length..]))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    /// <inheritdoc />
    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    /// <inheritdoc />
    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}

/// <summary>
/// Izni hesabin rollerinden denetler. Izinler istek basina bir kez okunur; erisim jetonunda
/// tasinmaz, boylece rolu kaldirilan kullanici jetonun suresini beklemeden yetkisini kaybeder.
/// </summary>
public sealed class PermissionHandler(AccessControlService access) : AuthorizationHandler<PermissionRequirement>
{
    private static readonly object CacheKey = new();

    /// <inheritdoc />
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        var userId = context.User.FindFirst(HttpContextCurrentUser.UserIdClaimType)?.Value;
        if (!long.TryParse(userId, System.Globalization.CultureInfo.InvariantCulture, out var accountId))
        {
            return;
        }

        var http = context.Resource as HttpContext;
        IReadOnlySet<string> permissions;
        if (http?.Items[CacheKey] is IReadOnlySet<string> cached)
        {
            permissions = cached;
        }
        else
        {
            permissions = await access.GetPermissionsAsync(accountId, http?.RequestAborted ?? CancellationToken.None);
            http?.Items[CacheKey] = permissions;
        }

        if (permissions.Contains(requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}

/// <summary>
/// Yetkisiz istegi (<c>403</c>) diger hatalarla ayni Problem Details bicimine cevirir.
/// Cerceve varsayilaninda govdesiz <c>403</c> doner; istemci kullaniciya ne oldugunu
/// soyleyemezdi.
/// </summary>
public sealed class ProblemDetailsAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    /// <inheritdoc />
    public Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        ArgumentNullException.ThrowIfNull(authorizeResult);

        // Hata isleyici ara katmani istisnayi Problem Details'e cevirir (ADR-0010 §5).
        if (authorizeResult.Forbidden)
        {
            throw new ForbiddenException();
        }

        return _default.HandleAsync(next, context, policy, authorizeResult);
    }
}
