using System.Security.Claims;
using Dsg.Hrms.Application.Identity.Sessions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Identity;

/// <summary>
/// Oturumdaki kullanicinin kendi hesabi (SYG-KMLK-048).
/// </summary>
[ApiController]
[Authorize]
[Route("api/v1/identity/account")]
[Produces("application/json")]
public sealed class AccountController : ControllerBase
{
    private readonly SessionService _sessions;

    /// <summary>Yeni ornek olusturur.</summary>
    public AccountController(SessionService sessions)
    {
        _sessions = sessions;
    }

    /// <summary>
    /// Parolayi degistirir. Mevcut parola istenir; degisiklikten sonra hesabin DIGER oturumlari
    /// kapanir, bu oturum acik kalir (SYG-KMLK-048).
    /// </summary>
    /// <response code="204">Parola degisti.</response>
    /// <response code="400">Mevcut parola hatali (<c>errors.currentPassword</c>) veya yeni parola kurallara uymuyor (<c>errors.newPassword</c>).</response>
    /// <response code="401">Oturum kapali.</response>
    /// <remarks>Parola degisimi bekleyen oturumda da kullanilabilir; degisince kisit kalkar (SYG-KMLK-046, 050).</remarks>
    /// <response code="429">Cok fazla hatali deneme; e-posta gecici olarak kilitli.</response>
    [HttpPost("password")]
    [AllowDuringPasswordChange]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sid = Guid.Parse(User.FindFirstValue(AuthenticationRegistration.SessionIdClaim)!);
        var result = await _sessions.ChangePasswordAsync(sid, request.CurrentPassword, request.NewPassword, cancellationToken);

        if (result.CurrentPasswordInvalid)
        {
            throw new ValidationException([new ValidationFailure("currentPassword", "Mevcut parolanız hatalı.")]);
        }

        if (result.Violations.Count > 0)
        {
            throw new ValidationException(result.Violations.Select(v => new ValidationFailure("newPassword", PasswordMessages.For(v))));
        }

        return NoContent();
    }
}
