using System.Security.Claims;
using Dsg.Hrms.Application.Common.Security;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Application.Identity.Sessions;
using Dsg.Hrms.Domain.Identity;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Identity;

/// <summary>
/// Oturumdaki kullanicinin kendi hesabi: parola degisikligi ve iki adimli dogrulama tercihi
/// (SYG-KMLK-048, 080).
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
    public async Task<IActionResult> ChangePasswordAsync(ChangePasswordRequest request, [FromServices] PasswordPolicy passwordPolicy, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await _sessions.ChangePasswordAsync(SessionId(), request.CurrentPassword, request.NewPassword, cancellationToken);

        if (result.CurrentPasswordInvalid)
        {
            throw CurrentPasswordInvalid();
        }

        if (result.Violations.Count > 0)
        {
            var minLength = await passwordPolicy.MinimumLengthAsync(cancellationToken);
            throw new ValidationException(result.Violations.Select(v => new ValidationFailure("newPassword", PasswordMessages.For(v, minLength))));
        }

        return NoContent();
    }

    /// <summary>Hesabin iki adimli dogrulama durumu (SYG-KMLK-080).</summary>
    /// <response code="200">Durum.</response>
    /// <response code="401">Oturum kapali.</response>
    [HttpGet("two-factor")]
    [ProducesResponseType<TwoFactorStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<TwoFactorStatusResponse> GetTwoFactorAsync(CancellationToken cancellationToken)
    {
        var status = await _sessions.GetTwoFactorPreferenceAsync(SessionId(), cancellationToken);
        return new TwoFactorStatusResponse(status.Available, status.Enabled, RegistrationsController.ToKinds(status.Channels));
    }

    /// <summary>
    /// Iki adimli dogrulamayi acmayi baslatir: mevcut parola denetlenir ve secilen kanala kod gonderilir (SYG-KMLK-080).
    /// </summary>
    /// <response code="200">Kod gonderildi.</response>
    /// <response code="400">Mevcut parola hatali (<c>errors.currentPassword</c>) veya istek gecersiz.</response>
    /// <response code="401">Oturum kapali.</response>
    /// <response code="422">Iki adimli dogrulama sistemde kapali, hesapta zaten acik veya secilen kanal kullanilamaz.</response>
    /// <response code="429">Cok fazla hatali deneme veya cok fazla kod istendi.</response>
    [HttpPost("two-factor/setup")]
    [ProducesResponseType<TwoFactorSetupResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<TwoFactorSetupResponse> StartTwoFactorSetupAsync(TwoFactorSetupRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var channel = request.Channel == VerificationChannelKind.Email ? RegistrationChannels.Email : RegistrationChannels.Sms;
        var result = await _sessions.StartTwoFactorSetupAsync(SessionId(), request.CurrentPassword, channel, cancellationToken);
        if (result.CurrentPasswordInvalid)
        {
            throw CurrentPasswordInvalid();
        }

        return new TwoFactorSetupResponse(result.CodeId!.Value, result.CodeExpiresAt!.Value);
    }

    /// <summary>Iki adimli dogrulamayi acma kodunu dogrular; dogruysa hesapta iki adimli dogrulama acilir (SYG-KMLK-080).</summary>
    /// <response code="200">Dogrulama sonucu; <c>verified</c> ise acildi.</response>
    /// <response code="400">Istek gecersiz.</response>
    /// <response code="401">Oturum kapali.</response>
    /// <response code="422">Iki adimli dogrulama sistemde kapatildi.</response>
    [HttpPost("two-factor/setup/verification")]
    [ProducesResponseType<TwoFactorSetupVerificationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<TwoFactorSetupVerificationResponse> VerifyTwoFactorSetupAsync(TwoFactorSetupVerificationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await _sessions.CompleteTwoFactorSetupAsync(SessionId(), request.CodeId, request.Code, cancellationToken);
        return new TwoFactorSetupVerificationResponse((VerificationOutcome)(int)result);
    }

    /// <summary>Hesapta iki adimli dogrulamayi kapatir; mevcut parola istenir (SYG-KMLK-080).</summary>
    /// <response code="204">Kapatildi (zaten kapaliysa da).</response>
    /// <response code="400">Mevcut parola hatali (<c>errors.currentPassword</c>) veya istek gecersiz.</response>
    /// <response code="401">Oturum kapali.</response>
    /// <response code="429">Cok fazla hatali deneme; e-posta gecici olarak kilitli.</response>
    [HttpPost("two-factor/disable")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> DisableTwoFactorAsync(TwoFactorDisableRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await _sessions.DisableTwoFactorAsync(SessionId(), request.CurrentPassword, cancellationToken))
        {
            throw CurrentPasswordInvalid();
        }

        return NoContent();
    }

    private static ValidationException CurrentPasswordInvalid() =>
        new([new ValidationFailure("currentPassword", "Mevcut parolanız hatalı.")]);

    private Guid SessionId() => Guid.Parse(User.FindFirstValue(AuthenticationRegistration.SessionIdClaim)!);
}

/// <summary>Hesabin iki adimli dogrulama durumu (SYG-KMLK-080).</summary>
/// <param name="Available">Iki adimli dogrulama sistemde kullaniliyor mu (PRM-KML-08); kullanilmiyorsa acilamaz.</param>
/// <param name="Enabled">Kullanicinin kendi tercihi acik mi; giriste kod ancak sistemde de aciksa istenir.</param>
/// <param name="Channels">Kisinin kullanabilecegi kanallar; bossa kayitli kurumsal e-posta veya cep telefonu yoktur ve acilamaz. Hedef donmez.</param>
public sealed record TwoFactorStatusResponse(bool Available, bool Enabled, IReadOnlyList<VerificationChannelKind> Channels);

/// <summary>Iki adimli dogrulamayi acma istegi (SYG-KMLK-080).</summary>
/// <param name="CurrentPassword">Mevcut parola.</param>
/// <param name="Channel">Kodun gonderilecegi kanal.</param>
public sealed record TwoFactorSetupRequest([property: Secret] string CurrentPassword, VerificationChannelKind Channel)
{
    /// <inheritdoc />
    public override string ToString() => nameof(TwoFactorSetupRequest);
}

/// <summary>Iki adimli dogrulamayi acma kodu gonderildi.</summary>
/// <param name="CodeId">Kodun kimligi; dogrulamada geri gonderilir.</param>
/// <param name="CodeExpiresAt">Kodun gecerlilik sonu (geri sayim, SYG-KMLK-028).</param>
public sealed record TwoFactorSetupResponse(Guid CodeId, DateTimeOffset CodeExpiresAt);

/// <summary>Iki adimli dogrulamayi acma kodunun dogrulama istegi.</summary>
/// <param name="CodeId">Gonderilen kodun kimligi.</param>
/// <param name="Code">Girilen kod.</param>
public sealed record TwoFactorSetupVerificationRequest(Guid CodeId, [property: Secret] string Code)
{
    /// <inheritdoc />
    public override string ToString() => nameof(TwoFactorSetupVerificationRequest);
}

/// <summary>Iki adimli dogrulamayi acma kodunun dogrulama sonucu.</summary>
/// <param name="Result">Sonuc; <c>verified</c> ise hesapta iki adimli dogrulama acildi.</param>
public sealed record TwoFactorSetupVerificationResponse(VerificationOutcome Result);

/// <summary>Iki adimli dogrulamayi kapatma istegi (SYG-KMLK-080).</summary>
/// <param name="CurrentPassword">Mevcut parola.</param>
public sealed record TwoFactorDisableRequest([property: Secret] string CurrentPassword)
{
    /// <inheritdoc />
    public override string ToString() => nameof(TwoFactorDisableRequest);
}

/// <summary>Iki adimli dogrulamayi acma istegi dogrulamasi.</summary>
public sealed class TwoFactorSetupRequestValidator : AbstractValidator<TwoFactorSetupRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public TwoFactorSetupRequestValidator()
    {
        RuleFor(r => r.CurrentPassword)
            .NotEmpty().WithMessage("Mevcut parolanızı girin.")
            .MaximumLength(512).WithMessage("Mevcut parolanız hatalı.");

        RuleFor(r => r.Channel).IsInEnum().WithMessage("Bir doğrulama yöntemi seçin.");
    }
}

/// <summary>Iki adimli dogrulamayi acma kodunun dogrulama istegi dogrulamasi.</summary>
public sealed class TwoFactorSetupVerificationRequestValidator : AbstractValidator<TwoFactorSetupVerificationRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public TwoFactorSetupVerificationRequestValidator()
    {
        RuleFor(r => r.CodeId).NotEmpty().WithMessage("Doğrulama kodu bulunamadı. Yeni kod isteyin.");

        RuleFor(r => r.Code)
            .NotEmpty().WithMessage("Doğrulama kodunu girin.")
            .MaximumLength(12).WithMessage("Doğrulama kodu yalnızca rakamlardan oluşur. E-posta veya SMS ile gelen kodu girin.");
    }
}

/// <summary>Iki adimli dogrulamayi kapatma istegi dogrulamasi.</summary>
public sealed class TwoFactorDisableRequestValidator : AbstractValidator<TwoFactorDisableRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public TwoFactorDisableRequestValidator()
    {
        RuleFor(r => r.CurrentPassword)
            .NotEmpty().WithMessage("Mevcut parolanızı girin.")
            .MaximumLength(512).WithMessage("Mevcut parolanız hatalı.");
    }
}
