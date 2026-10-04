using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Application.Identity.Registration;
using Dsg.Hrms.Domain.Identity;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Identity;

/// <summary>
/// Parola sifirlama (ADR-0006 §6, SYG-KMLK-047).
/// </summary>
/// <remarks>
/// <para>
/// Sifirlama uyelikle AYNI eslestirme, kanal ve kod akisini kullanir; bu denetleyici uyelik
/// servisine yalnizca "parola sifirlama" amaciyla baslar. Ilk uc adim eslesme olsa da olmasa
/// da ayni yaniti dondurur ve en az <see cref="RegistrationTimingOptions.MinimumResponseTime"/>
/// surer (<c>KR-085</c>). Hiz sinirlari uyelikle ortaktir (SYG-KMLK-059).
/// </para>
/// <para>
/// Uyelikte "hesabiniz zaten var" sonucunu alan kisi, ayni islemin kimligiyle dogrudan
/// <c>{id}/password</c> ucuna gelebilir: kimligi zaten dogrulanmistir.
/// </para>
/// </remarks>
[ApiController]
[Route("api/v1/identity/password-resets")]
[Produces("application/json")]
public sealed class PasswordResetsController : ControllerBase
{
    private readonly RegistrationService _registrations;
    private readonly ICurrentUser _currentUser;

    /// <summary>Yeni ornek olusturur.</summary>
    public PasswordResetsController(RegistrationService registrations, ICurrentUser currentUser)
    {
        _registrations = registrations;
        _currentUser = currentUser;
    }

    /// <summary>Parola sifirlamayi baslatir: TCKN, dogum tarihi ve kurumsal e-posta.</summary>
    /// <response code="200">Kanal secimi. Eslesme olmasa da ayni yanit doner.</response>
    /// <response code="400">Bicim hatasi (ornegin gecersiz TCKN).</response>
    /// <response code="429">Cok fazla deneme.</response>
    [HttpPost]
    [MinimumResponseTime]
    [ProducesResponseType<RegistrationStartedResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<RegistrationStartedResponse> StartAsync(StartRegistrationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var started = await _registrations.StartAsync(
            new StartRegistration(request.NationalId.Trim(), request.BirthDate, request.Email.Trim(), VerificationPurpose.PasswordReset),
            _currentUser.IpAddress,
            cancellationToken);

        return new RegistrationStartedResponse(started.RegistrationId, RegistrationsController.ToKinds(started.Channels), started.ExpiresAt);
    }

    /// <summary>Secilen kanala kod gonderir. Kanal degisimi ve tekrar gonderim de bu uctur.</summary>
    /// <response code="200">Kod gonderildi (eslesme yoksa gonderilmez; yanit aynidir).</response>
    /// <response code="404">Islem bulunamadi veya suresi doldu.</response>
    /// <response code="422">Kanal bu islemde kullanilamaz.</response>
    /// <response code="429">Cok fazla kod istendi.</response>
    [HttpPost("{resetId:guid}/code")]
    [MinimumResponseTime]
    [ProducesResponseType<CodeRequestedResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<CodeRequestedResponse> RequestCodeAsync(Guid resetId, RequestCodeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var channel = request.Channel == VerificationChannelKind.Email ? RegistrationChannels.Email : RegistrationChannels.Sms;
        var requested = await _registrations.RequestCodeAsync(resetId, channel, cancellationToken);

        return new CodeRequestedResponse(requested.CodeExpiresAt);
    }

    /// <summary>Kodu dogrular. Hesabin olup olmadigi YALNIZCA dogrulamadan sonra soylenir.</summary>
    /// <response code="200">Dogrulama sonucu.</response>
    /// <response code="404">Islem bulunamadi veya suresi doldu.</response>
    [HttpPost("{resetId:guid}/verification")]
    [MinimumResponseTime]
    [ProducesResponseType<VerificationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<VerificationResponse> VerifyAsync(Guid resetId, VerifyCodeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var verification = await _registrations.VerifyAsync(resetId, request.Code, cancellationToken);

        return new VerificationResponse((VerificationOutcome)(int)verification.Result, verification.AccountExists);
    }

    /// <summary>Yeni parolayi belirler; hesabin tum oturumlari kapanir ve giris kilidi kalkar.</summary>
    /// <response code="204">Parola degisti; kullanici yeni parolasiyla giris yapabilir.</response>
    /// <response code="400">Parola kurallara uymuyor (<c>errors.password</c>).</response>
    /// <response code="403">Hesap kullanima kapali.</response>
    /// <response code="404">Islem bulunamadi, dogrulanmadi veya suresi doldu.</response>
    /// <response code="422">Kisinin hesabi yok; uye olmasi gerekir.</response>
    [HttpPost("{resetId:guid}/password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ResetAsync(Guid resetId, ResetPasswordRequest request, [FromServices] PasswordPolicy passwordPolicy, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var violations = await _registrations.ResetPasswordAsync(resetId, request.Password, cancellationToken);
        if (violations.Count > 0)
        {
            var minLength = await passwordPolicy.MinimumLengthAsync(cancellationToken);
            throw new ValidationException(violations.Select(v => new ValidationFailure("password", PasswordMessages.For(v, minLength))));
        }

        return NoContent();
    }
}
