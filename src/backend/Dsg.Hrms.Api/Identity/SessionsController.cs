using System.Security.Claims;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Identity.Sessions;
using Dsg.Hrms.Domain.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Identity;

/// <summary>
/// Giris, iki adimli dogrulama, jeton yenileme, etkinlik sinyali ve cikis (ADR-0006 §8).
/// </summary>
/// <remarks>
/// Yenileme jetonu yalnizca <c>HttpOnly</c>, <c>Secure</c>, <c>SameSite=Strict</c> cerezde tasinir;
/// yanit govdesine YAZILMAZ ve JavaScript onu goremez (SYG-KMLK-037). Erisim jetonu govdede
/// doner ve istemci onu yalnizca bellekte tutar.
/// </remarks>
[ApiController]
[Route("api/v1/identity/sessions")]
[Produces("application/json")]
public sealed class SessionsController : ControllerBase
{
    /// <summary>Yenileme jetonu cerezinin adi.</summary>
    public const string RefreshCookie = "hrms_refresh";

    /// <summary>Cerezin gonderildigi yol: yalnizca bu denetleyici.</summary>
    public const string RefreshCookiePath = "/api/v1/identity/sessions";

    private readonly SessionService _sessions;
    private readonly ICurrentUser _currentUser;

    /// <summary>Yeni ornek olusturur.</summary>
    public SessionsController(SessionService sessions, ICurrentUser currentUser)
    {
        _sessions = sessions;
        _currentUser = currentUser;
    }

    /// <summary>Kurumsal e-posta ve parolayla giris (SYG-KMLK-031…034).</summary>
    /// <response code="200">Oturum acildi veya iki adimli dogrulama gerekiyor.</response>
    /// <response code="401">E-posta veya parola hatali (hesap olsa da olmasa da ayni yanit).</response>
    /// <response code="403">Hesap kullanima kapali.</response>
    /// <response code="429">Cok fazla hatali deneme; e-posta gecici olarak kilitli.</response>
    [HttpPost]
    [MinimumResponseTime]
    [ProducesResponseType<SignInResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<SignInResponse> SignInAsync(SignInRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await _sessions.SignInAsync(request.Email, request.Password, _currentUser.IpAddress, cancellationToken);
        if (result.Session is not null)
        {
            return SignInResponse.SignedIn(ToResponse(result.Session));
        }

        return SignInResponse.VerificationRequired(new TwoFactorChallengeResponse(
            result.ChallengeId!.Value, RegistrationsController.ToKinds(result.Channels), result.ChallengeExpiresAt!.Value));
    }

    /// <summary>Iki adimli dogrulama icin kod gonderir (SYG-KMLK-034).</summary>
    /// <response code="200">Kod gonderildi.</response>
    /// <response code="404">Giris isleminin suresi doldu.</response>
    /// <response code="422">Kanal kullanilamaz.</response>
    /// <response code="429">Cok fazla kod istendi.</response>
    [HttpPost("challenges/{challengeId:guid}/code")]
    [ProducesResponseType<CodeRequestedResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<CodeRequestedResponse> RequestCodeAsync(Guid challengeId, RequestCodeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var channel = request.Channel == VerificationChannelKind.Email ? RegistrationChannels.Email : RegistrationChannels.Sms;
        return new CodeRequestedResponse(await _sessions.RequestTwoFactorCodeAsync(challengeId, channel, cancellationToken));
    }

    /// <summary>Iki adimli dogrulama kodunu dogrular; dogruysa oturum acilir.</summary>
    /// <response code="200">Dogrulama sonucu; <c>verified</c> ise oturum bilgisi.</response>
    /// <response code="403">Hesap kullanima kapali.</response>
    /// <response code="404">Giris isleminin suresi doldu.</response>
    [HttpPost("challenges/{challengeId:guid}/verification")]
    [ProducesResponseType<TwoFactorVerificationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<TwoFactorVerificationResponse> VerifyAsync(Guid challengeId, VerifyCodeRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var (result, session) = await _sessions.CompleteTwoFactorAsync(challengeId, request.Code, _currentUser.IpAddress, cancellationToken);
        return new TwoFactorVerificationResponse((VerificationOutcome)(int)result, session is null ? null : ToResponse(session));
    }

    /// <summary>
    /// Yenileme jetonuyla (cerez) yeni erisim jetonu alir. Jeton her kullanimda yenilenir
    /// (SYG-KMLK-040). Hareketsizlik sayacini sifirlamaz (SYG-KMLK-038).
    /// </summary>
    /// <response code="200">Yeni oturum bilgisi.</response>
    /// <response code="401">Oturum sona erdi; hata turu nedeni tasir (<c>session-ended/…</c>).</response>
    [HttpPost("refresh")]
    [ProducesResponseType<SessionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<SessionResponse> RefreshAsync(CancellationToken cancellationToken)
    {
        try
        {
            return ToResponse(await _sessions.RefreshAsync(Request.Cookies[RefreshCookie], cancellationToken));
        }
        catch (SessionEndedException)
        {
            ClearRefreshCookie();
            throw;
        }
    }

    /// <summary>
    /// Kullanici etkilesimi veya mesru uzun etkinlik sinyali (SYG-KMLK-038, 039). Dakikada en
    /// fazla iki kez kabul edilir; toplam oturum suresini uzatmaz.
    /// </summary>
    /// <response code="204">Kaydedildi.</response>
    /// <response code="401">Oturum kapali.</response>
    /// <response code="429">Cok sik gonderildi.</response>
    [HttpPost("activity")]
    [Authorize]
    [AllowDuringPasswordChange]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> ActivityAsync(CancellationToken cancellationToken)
    {
        var sid = User.FindFirstValue(AuthenticationRegistration.SessionIdClaim);
        await _sessions.RecordActivityAsync(Guid.Parse(sid!), cancellationToken);
        return NoContent();
    }

    /// <summary>Cikis: oturum ve yenileme jetonu sunucuda iptal edilir (SYG-KMLK-043).</summary>
    /// <response code="204">Cikis yapildi (oturum zaten kapaliysa da).</response>
    [HttpDelete("current")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SignOutAsync(CancellationToken cancellationToken)
    {
        await _sessions.SignOutAsync(Request.Cookies[RefreshCookie], cancellationToken);
        ClearRefreshCookie();
        return NoContent();
    }

    private SessionResponse ToResponse(SessionTokens tokens)
    {
        Response.Cookies.Append(RefreshCookie, tokens.RefreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
            Expires = tokens.SessionExpiresAt,
            IsEssential = true,
        });

        return new SessionResponse(
            tokens.AccessToken,
            tokens.AccessTokenExpiresAt,
            tokens.SessionExpiresAt,
            (int)tokens.IdleTimeout.TotalMinutes,
            new SessionUserResponse(tokens.FirstName, tokens.LastName, tokens.Permissions),
            tokens.PasswordChangeRequired switch
            {
                PasswordChangeReason.FirstSignIn => PasswordChangeReasonKind.FirstSignIn,
                PasswordChangeReason.Expired => PasswordChangeReasonKind.Expired,
                _ => null,
            });
    }

    private void ClearRefreshCookie() =>
        Response.Cookies.Delete(RefreshCookie, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = RefreshCookiePath,
        });
}

/// <summary>Giris istegi.</summary>
/// <param name="Email">Kurumsal e-posta.</param>
/// <param name="Password">Parola.</param>
public sealed record SignInRequest(
    [property: Application.Common.Security.PersonalData(Application.Common.Security.PersonalDataKind.Email)] string Email,
    [property: Application.Common.Security.Secret] string Password)
{
    /// <inheritdoc />
    public override string ToString() => nameof(SignInRequest);
}

/// <summary>Giris yaniti.</summary>
/// <param name="Status">Sonuc.</param>
/// <param name="Session">Oturum acildiysa bilgisi.</param>
/// <param name="Challenge">Iki adimli dogrulama gerekiyorsa bekleyen giris.</param>
public sealed record SignInResponse(SignInStatus Status, SessionResponse? Session, TwoFactorChallengeResponse? Challenge)
{
    /// <summary>Oturum acildi.</summary>
    public static SignInResponse SignedIn(SessionResponse session) => new(SignInStatus.SignedIn, session, null);

    /// <summary>Iki adimli dogrulama gerekiyor.</summary>
    public static SignInResponse VerificationRequired(TwoFactorChallengeResponse challenge) =>
        new(SignInStatus.VerificationRequired, null, challenge);
}

/// <summary>Giris sonucu.</summary>
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<SignInStatus>))]
public enum SignInStatus
{
    /// <summary>Oturum acildi.</summary>
    [System.Text.Json.Serialization.JsonStringEnumMemberName("signedIn")]
    SignedIn = 1,

    /// <summary>Iki adimli dogrulama gerekiyor.</summary>
    [System.Text.Json.Serialization.JsonStringEnumMemberName("verificationRequired")]
    VerificationRequired = 2,
}

/// <summary>Acik oturum. Yenileme jetonu burada DEGIL, HttpOnly cerezdedir.</summary>
/// <param name="AccessToken">Erisim jetonu; istemci yalnizca bellekte tutar.</param>
/// <param name="AccessTokenExpiresAt">Erisim jetonunun gecerlilik sonu.</param>
/// <param name="SessionExpiresAt">Toplam oturum suresi siniri.</param>
/// <param name="IdleTimeoutMinutes">Hareketsizlik suresi (dakika).</param>
/// <param name="User">Kullanici.</param>
/// <param name="PasswordChangeRequired">
/// Oturum parola degisimi bekliyorsa nedeni; beklemiyorsa <c>null</c>. Bu durumda yalnizca parola
/// degistirme ve oturum uclari kullanilabilir, digerleri <c>403 password-change-required</c> doner
/// (SYG-KMLK-046, 050).
/// </param>
public sealed record SessionResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    DateTimeOffset SessionExpiresAt,
    int IdleTimeoutMinutes,
    SessionUserResponse User,
    PasswordChangeReasonKind? PasswordChangeRequired)
{
    /// <inheritdoc />
    public override string ToString() => nameof(SessionResponse);
}

/// <summary>Parola degisiminin zorunlu olma nedeni (SYG-KMLK-046, 050).</summary>
[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<PasswordChangeReasonKind>))]
public enum PasswordChangeReasonKind
{
    /// <summary>Ilk giris (PRM-KML-20).</summary>
    [System.Text.Json.Serialization.JsonStringEnumMemberName("firstSignIn")]
    FirstSignIn = 1,

    /// <summary>Parolanin suresi doldu (PRM-KML-07, PRM-KML-21).</summary>
    [System.Text.Json.Serialization.JsonStringEnumMemberName("expired")]
    Expired = 2,
}

/// <summary>Oturumdaki kullanici.</summary>
/// <param name="FirstName">Ad.</param>
/// <param name="LastName">Soyad.</param>
/// <param name="Permissions">Izinler. Istemci yalnizca GOSTERIM icin kullanir; denetim her istekte sunucudadir (ADR-0007 §3).</param>
public sealed record SessionUserResponse(string FirstName, string LastName, IReadOnlyList<string> Permissions);

/// <summary>Bekleyen iki adimli giris.</summary>
/// <param name="ChallengeId">Kimlik.</param>
/// <param name="Channels">Sunulan kanallar; hedef donmez.</param>
/// <param name="ExpiresAt">Gecerlilik sonu.</param>
public sealed record TwoFactorChallengeResponse(Guid ChallengeId, IReadOnlyList<VerificationChannelKind> Channels, DateTimeOffset ExpiresAt);

/// <summary>Iki adimli dogrulama sonucu.</summary>
/// <param name="Result">Sonuc.</param>
/// <param name="Session">Dogrulandiysa oturum.</param>
public sealed record TwoFactorVerificationResponse(VerificationOutcome Result, SessionResponse? Session);

/// <summary>Giris istegi dogrulamasi.</summary>
public sealed class SignInRequestValidator : AbstractValidator<SignInRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public SignInRequestValidator()
    {
        RuleFor(r => r.Email)
            .NotEmpty().WithMessage("Kurumsal e-posta adresinizi girin.")
            .MaximumLength(254).WithMessage("Geçerli bir e-posta adresi girin.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.");
        RuleFor(r => r.Password)
            .NotEmpty().WithMessage("Parolanızı girin.")
            .MaximumLength(512).WithMessage("Parola en fazla 128 karakter olabilir.");
    }
}
