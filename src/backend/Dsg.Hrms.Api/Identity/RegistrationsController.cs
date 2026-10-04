using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Application.Identity.Registration;
using Dsg.Hrms.Domain.Identity;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Identity;

/// <summary>
/// Uyelik akisi (ADR-0006 §1, SYG-KMLK-013…021, 071).
/// </summary>
/// <remarks>
/// <para>
/// Uc noktalar kimlik dogrulamasi ISTEMEZ: kullanicinin henuz hesabi yoktur. Kotuye
/// kullanima karsi korumalar hiz sinirlari (SYG-KMLK-059) ve eslesme gizliligidir.
/// </para>
/// <para>
/// Ilk uc adim eslesme olsa da olmasa da ayni durum kodunu ve ayni govde bicimini
/// dondurur; yanit en az <see cref="RegistrationTimingOptions.MinimumResponseTime"/> surer
/// (SYG-KMLK-015).
/// </para>
/// </remarks>
[ApiController]
[Route("api/v1/identity/registrations")]
[Produces("application/json")]
public sealed class RegistrationsController : ControllerBase
{
    private readonly RegistrationService _registrations;
    private readonly ICurrentUser _currentUser;

    /// <summary>Yeni ornek olusturur.</summary>
    public RegistrationsController(RegistrationService registrations, ICurrentUser currentUser)
    {
        _registrations = registrations;
        _currentUser = currentUser;
    }

    /// <summary>Uyeligi baslatir: TCKN, dogum tarihi ve kurumsal e-posta.</summary>
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
            new StartRegistration(request.NationalId.Trim(), request.BirthDate, request.Email.Trim()),
            _currentUser.IpAddress,
            cancellationToken);

        return new RegistrationStartedResponse(started.RegistrationId, ToKinds(started.Channels), started.ExpiresAt);
    }

    /// <summary>Secilen kanala kod gonderir. Kanal degisimi ve tekrar gonderim de bu uctur.</summary>
    /// <response code="200">Kod gonderildi (eslesme yoksa gonderilmez; yanit aynidir).</response>
    /// <response code="404">Uyelik islemi bulunamadi veya suresi doldu.</response>
    /// <response code="422">Kanal bu islemde kullanilamaz.</response>
    /// <response code="429">Cok fazla kod istendi.</response>
    [HttpPost("{registrationId:guid}/code")]
    [MinimumResponseTime]
    [ProducesResponseType<CodeRequestedResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<CodeRequestedResponse> RequestCodeAsync(Guid registrationId, RequestCodeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var channel = request.Channel == VerificationChannelKind.Email ? RegistrationChannels.Email : RegistrationChannels.Sms;
        var requested = await _registrations.RequestCodeAsync(registrationId, channel, cancellationToken);

        return new CodeRequestedResponse(requested.CodeExpiresAt);
    }

    /// <summary>Kodu dogrular.</summary>
    /// <response code="200">Dogrulama sonucu.</response>
    /// <response code="404">Uyelik islemi bulunamadi veya suresi doldu.</response>
    [HttpPost("{registrationId:guid}/verification")]
    [MinimumResponseTime]
    [ProducesResponseType<VerificationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<VerificationResponse> VerifyAsync(Guid registrationId, VerifyCodeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var verification = await _registrations.VerifyAsync(registrationId, request.Code, cancellationToken);

        return new VerificationResponse((VerificationOutcome)(int)verification.Result, verification.AccountExists);
    }

    /// <summary>Parolayi belirler ve hesabi olusturur.</summary>
    /// <response code="201">Hesap olusturuldu; kullanici giris yapabilir.</response>
    /// <response code="400">Parola kurallara uymuyor (<c>errors.password</c>).</response>
    /// <response code="404">Uyelik islemi bulunamadi, dogrulanmadi veya suresi doldu.</response>
    /// <response code="409">Kisinin zaten hesabi var.</response>
    [HttpPost("{registrationId:guid}/account")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CompleteAsync(Guid registrationId, CompleteRegistrationRequest request, [FromServices] PasswordPolicy passwordPolicy, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var violations = await _registrations.CompleteAsync(registrationId, request.Password, cancellationToken);
        if (violations.Count > 0)
        {
            var minLength = await passwordPolicy.MinimumLengthAsync(cancellationToken);
            throw new ValidationException(violations.Select(v => new ValidationFailure("password", PasswordMessages.For(v, minLength))));
        }

        return StatusCode(StatusCodes.Status201Created);
    }

    /// <summary>Kanal bayraklarini API degerlerine cevirir.</summary>
    public static List<VerificationChannelKind> ToKinds(RegistrationChannels channels)
    {
        var kinds = new List<VerificationChannelKind>();
        if (channels.HasFlag(RegistrationChannels.Email))
        {
            kinds.Add(VerificationChannelKind.Email);
        }

        if (channels.HasFlag(RegistrationChannels.Sms))
        {
            kinds.Add(VerificationChannelKind.Sms);
        }

        return kinds;
    }
}

/// <summary>Parola kurali ihlallerinin kullaniciya gosterilen iletileri (REQ-KMLK-043: Turkce, teknik terimsiz).</summary>
public static class PasswordMessages
{
    /// <summary>Ihlalin iletisi.</summary>
    /// <param name="violation">Ihlal.</param>
    /// <param name="minLength">Parolanin en az karakter sayisi (PRM-KML-05); iletide soylenir (SYG-KMLK-064, B-02).</param>
    public static string For(PasswordViolation violation, int minLength) => violation switch
    {
        PasswordViolation.TooShort => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"Parola en az {minLength} karakter olmalıdır."),
        PasswordViolation.TooLong => "Parola en fazla 128 karakter olabilir.",
        PasswordViolation.NotComplex => "Parola büyük harf, küçük harf, rakam ve simge içermelidir.",
        PasswordViolation.Common => "Bu parola çok yaygın ve kolay tahmin edilir. Daha az bilinen bir parola seçin.",
        PasswordViolation.ContainsPersonalOrOrganizationWord => "Parola adınızdan, e-posta adresinizden veya kurum adından türetilmemelidir.",
        PasswordViolation.SameAsCurrent => "Yeni parola mevcut parolanızdan farklı olmalıdır.",
        _ => "Parola kabul edilmedi.",
    };
}
