using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Identity.Sessions;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Dsg.Hrms.Api.Identity;

/// <summary>
/// Erisim jetonu (JWT) imzalama ve dogrulama (ADR-0006 §8, SYG-KMLK-037).
/// </summary>
/// <remarks>
/// <para>
/// Imzalama anahtari KAYNAK KODA ve VERITABANINA YAZILMAZ: <c>Identity__JwtSigningKey</c>
/// ortam degiskeni (UAT/uretim) veya User Secrets (gelistirme). Zorunludur; tanimli degilse
/// uygulama ACILMAZ (ADR-0008 §4).
/// </para>
/// <para>
/// Jeton her istekte yalnizca imzasiyla degil, <b>oturumun hala acik olup olmadigiyla</b> da
/// dogrulanir. Boylece cikis, baska cihazdan giris ve hesabin pasiflesmesi jetonun 15
/// dakikalik omrunu beklemeden etkili olur (SYG-KMLK-054).
/// </para>
/// <para>
/// Jetonun omru <b>uygulamanin saatiyle</b> (<see cref="IDateTimeProvider"/>) uretilir ve ayni
/// saatle dogrulanir. Kutuphanenin varsayilani sistem saatidir; uretim ve dogrulama farkli
/// saatlere baksaydi zamana bagli her test calistigi saate gore gecer ya da duserdi (#97).
/// </para>
/// </remarks>
public static class AuthenticationRegistration
{
    /// <summary>Jetonu ureten ve dogrulayan taraf.</summary>
    public const string Issuer = "dsg-hrms";

    /// <summary>Jetonun hedefi.</summary>
    public const string Audience = "dsg-hrms-api";

    /// <summary>Oturum kimliginin tasindigi talep.</summary>
    public const string SessionIdClaim = "sid";

    /// <summary>Saat farki toleransi.</summary>
    public static readonly TimeSpan ClockSkew = TimeSpan.FromSeconds(30);

    /// <summary>Kimlik dogrulamayi kaydeder.</summary>
    public static IServiceCollection AddHrmsAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var options = OptionsRegistration.ReadAndValidate<JwtSigningOptions>(configuration, JwtSigningOptions.SectionName);
        var key = new SymmetricSecurityKey(Convert.FromBase64String(options.JwtSigningKey!));

        services.AddSingleton<IAccessTokenIssuer>(new JwtAccessTokenIssuer(key));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(bearer =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidIssuer = Issuer,
                    ValidAudience = Audience,
                    IssuerSigningKey = key,
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    NameClaimType = HttpContextCurrentUser.UserIdClaimType,
                    ClockSkew = ClockSkew,
                };
                bearer.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var sid = context.Principal?.FindFirstValue(SessionIdClaim);
                        var sessions = context.HttpContext.RequestServices.GetRequiredService<SessionService>();

                        var access = Guid.TryParse(sid, out var sessionId)
                            ? await sessions.CheckSessionAsync(sessionId, context.HttpContext.RequestAborted)
                            : SessionAccess.Closed;

                        if (access == SessionAccess.Closed)
                        {
                            context.Fail("Oturum kapali.");
                        }
                        else if (access == SessionAccess.PasswordChangeRequired)
                        {
                            PasswordChangeGateFilter.MarkRequired(context.HttpContext);
                        }
                    },
                };
            });

        services
            .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IDateTimeProvider>((bearer, clock) =>
                bearer.TokenValidationParameters.LifetimeValidator = (notBefore, expires, _, _) =>
                    IsWithinLifetime(notBefore, expires, clock.UtcNow));

        // Eylem yetkisi (ADR-0007 §3): [HasPermission] politikalari istek aninda uretilir;
        // yetkisiz istek Problem Details ile 403 doner.
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, ProblemDetailsAuthorizationResultHandler>();
        return services;
    }

    /// <summary>
    /// Jeton verilen anda gecerli mi. Bitis zorunludur; saat farki toleransi iki uca da uygulanir.
    /// </summary>
    public static bool IsWithinLifetime(DateTime? notBefore, DateTime? expires, DateTimeOffset now)
    {
        var utcNow = now.UtcDateTime;

        return expires is not null
            && utcNow < expires.Value.ToUniversalTime() + ClockSkew
            && (notBefore is null || utcNow >= notBefore.Value.ToUniversalTime() - ClockSkew);
    }
}

/// <summary>JWT imzalama anahtari.</summary>
public sealed class JwtSigningOptions : IValidatableObject
{
    /// <summary>Yapilandirma bolumunun adi.</summary>
    public const string SectionName = "Identity";

    /// <summary>En az anahtar uzunlugu (bayt; HMAC-SHA256 icin 256 bit).</summary>
    public const int MinKeySizeBytes = 32;

    /// <summary>Base64 bicimli anahtar (<c>openssl rand -base64 32</c>).</summary>
    public string? JwtSigningKey { get; init; }

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        byte[]? bytes = null;
        try
        {
            bytes = string.IsNullOrWhiteSpace(JwtSigningKey) ? null : Convert.FromBase64String(JwtSigningKey);
        }
        catch (FormatException)
        {
            // Asagida anlasilir iletiye donusur; deger iletiye YAZILMAZ.
        }

        if (bytes is null || bytes.Length < MinKeySizeBytes)
        {
            yield return new ValidationResult(
                $"Identity:JwtSigningKey tanimli olmali ve en az {MinKeySizeBytes} baytlik bir anahtarin Base64 bicimi olmalidir (openssl rand -base64 32).",
                [nameof(JwtSigningKey)]);
        }
    }
}

/// <summary>HMAC-SHA256 imzali erisim jetonu ureticisi.</summary>
public sealed class JwtAccessTokenIssuer(SecurityKey key) : IAccessTokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new();
    private readonly SigningCredentials _credentials = new(key, SecurityAlgorithms.HmacSha256);

    /// <inheritdoc />
    public string Issue(long userAccountId, UserSession session, DateTimeOffset issuedAt, DateTimeOffset expiresAt)
    {
        ArgumentNullException.ThrowIfNull(session);

        var id = userAccountId.ToString(System.Globalization.CultureInfo.InvariantCulture);
        return _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = AuthenticationRegistration.Issuer,
            Audience = AuthenticationRegistration.Audience,
            IssuedAt = issuedAt.UtcDateTime,
            NotBefore = issuedAt.UtcDateTime.AddSeconds(-5),
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = _credentials,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = id,
                [HttpContextCurrentUser.UserIdClaimType] = id,
                [AuthenticationRegistration.SessionIdClaim] = session.PublicId.ToString(),
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
            },
        });
    }
}
