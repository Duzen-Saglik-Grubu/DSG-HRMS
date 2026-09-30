using Dsg.Hrms.Application.Common.Security;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Identity.Invitations;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Identity;

/// <summary>
/// IK davet baglantisi (SYG-KMLK-051…053).
/// </summary>
/// <remarks>
/// <para>
/// Gonderim IK yetkisi ister (<c>identity.invite.create</c>). Baglantinin kullanimi kimlik
/// dogrulamasi istemez: baglantiyi alan kisinin henuz parolasi yoktur. Jeton 256 bit
/// rastgeledir; tahmin edilemez.
/// </para>
/// <para>
/// Jeton istek GOVDESINDE gelir, adreste degil: adresteki deger sunucu ve vekil gunluklerine
/// yazilirdi.
/// </para>
/// </remarks>
[ApiController]
[Produces("application/json")]
public sealed class InvitationsController : ControllerBase
{
    /// <summary>Baglantinin actigi sayfanin yolu (istemcideki <c>routes.invite</c>).</summary>
    public const string InvitePagePath = "/invite";

    private readonly InvitationService _invitations;
    private readonly IConfiguration _configuration;

    /// <summary>Yeni ornek olusturur.</summary>
    public InvitationsController(InvitationService invitations, IConfiguration configuration)
    {
        _invitations = invitations;
        _configuration = configuration;
    }

    /// <summary>Kisiye tek kullanimlik parola olusturma baglantisi gonderir.</summary>
    /// <remarks>
    /// Baglanti yalnizca kisinin LOGO'daki kurumsal e-postasina gider; IK adres giremez.
    /// Kisinin onceki baglantilari gecersizlesir.
    /// </remarks>
    /// <response code="202">Baglanti gonderim kuyruguna alindi.</response>
    /// <response code="400">Gerekce girilmedi.</response>
    /// <response code="403">Yetki yok (<c>identity.invite.create</c>).</response>
    /// <response code="404">Kisi bulunamadi.</response>
    /// <response code="422">Davet kapali veya kisinin uygun kurumsal e-postasi, aktif calisma kaydi yok ya da hesabi pasif.</response>
    [HttpPost("api/v1/identity/accounts/{personId:guid}/invitations")]
    [HasPermission(Permissions.InviteCreate)]
    [ProducesResponseType<InvitationSentResponse>(StatusCodes.Status202Accepted)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> SendAsync(Guid personId, InvitationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var sent = await _invitations.SendAsync(personId, request.Reason, InvitePageUri(), cancellationToken);
        return Accepted(new InvitationSentResponse(sent.ExpiresAt));
    }

    /// <summary>Baglantinin gecerli olup olmadigini okur.</summary>
    /// <response code="200">Baglanti gecerli.</response>
    /// <response code="404">Baglanti gecersiz, kullanilmis veya suresi dolmus.</response>
    [HttpPost("api/v1/identity/invitations/lookup")]
    [ProducesResponseType<InvitationInfoResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<InvitationInfoResponse> LookupAsync(InvitationTokenRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var info = await _invitations.LookupAsync(request.Token, cancellationToken);
        return new InvitationInfoResponse(info.FirstName, info.AccountExists, info.ExpiresAt);
    }

    /// <summary>
    /// Parolayi belirler. Hesap yoksa olusturulur; varsa parola yenilenir ve acik oturumlar
    /// kapanir. Baglanti tek kullanimliktir.
    /// </summary>
    /// <response code="204">Parola belirlendi; kullanici giris yapabilir.</response>
    /// <response code="400">Parola kurallara uymuyor (<c>errors.password</c>).</response>
    /// <response code="403">Hesap kullanima kapali.</response>
    /// <response code="404">Baglanti gecersiz, kullanilmis veya suresi dolmus.</response>
    [HttpPost("api/v1/identity/invitations/acceptance")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AcceptAsync(AcceptInvitationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var violations = await _invitations.AcceptAsync(request.Token, request.Password, cancellationToken);
        if (violations.Count > 0)
        {
            throw new ValidationException(violations.Select(v => new ValidationFailure("password", PasswordMessages.For(v))));
        }

        return NoContent();
    }

    /// <summary>
    /// Baglantinin sayfa adresi. <c>Identity:PublicBaseUrl</c> tanimliysa o kullanilir (UAT ve
    /// uretimde tanimlanir); degilse istegin adresi (gelistirme). Tanimli adres, istek basligiyla
    /// oynanarak e-postaya baska bir sitenin yazdirilmasini onler.
    /// </summary>
    private Uri InvitePageUri()
    {
        var configured = _configuration["Identity:PublicBaseUrl"];
        var baseUrl = string.IsNullOrWhiteSpace(configured) ? $"{Request.Scheme}://{Request.Host}" : configured.TrimEnd('/');
        return new Uri(baseUrl + InvitePagePath);
    }
}

/// <summary>Davet gonderim istegi.</summary>
/// <param name="Reason">Gerekce (zorunlu; denetim izine yazilir, SYG-KMLK-053).</param>
public sealed record InvitationRequest(string Reason);

/// <summary>Davet gonderildi.</summary>
/// <param name="ExpiresAt">Baglantinin gecerlilik sonu.</param>
public sealed record InvitationSentResponse(DateTimeOffset ExpiresAt);

/// <summary>Baglanti sorgusu.</summary>
/// <param name="Token">Baglantidaki jeton.</param>
public sealed record InvitationTokenRequest([property: Secret] string Token)
{
    /// <inheritdoc />
    public override string ToString() => nameof(InvitationTokenRequest);
}

/// <summary>Baglanti bilgisi.</summary>
/// <param name="FirstName">Kisinin adi (karsilama icin).</param>
/// <param name="AccountExists">Hesap var mi; varsa parola yenilenir, yoksa hesap olusur.</param>
/// <param name="ExpiresAt">Gecerlilik sonu.</param>
public sealed record InvitationInfoResponse(string FirstName, bool AccountExists, DateTimeOffset ExpiresAt);

/// <summary>Parola belirleme istegi.</summary>
/// <param name="Token">Baglantidaki jeton.</param>
/// <param name="Password">Parola.</param>
public sealed record AcceptInvitationRequest([property: Secret] string Token, [property: Secret] string Password)
{
    /// <inheritdoc />
    public override string ToString() => nameof(AcceptInvitationRequest);
}

/// <summary>Davet gonderim istegi dogrulamasi.</summary>
public sealed class InvitationRequestValidator : AbstractValidator<InvitationRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public InvitationRequestValidator()
    {
        RuleFor(r => r.Reason)
            .Must(reason => !string.IsNullOrWhiteSpace(reason)).WithMessage("Gerekçe girin.")
            .MaximumLength(500).WithMessage("Gerekçe en fazla 500 karakter olabilir.");
    }
}

/// <summary>Baglanti sorgusu dogrulamasi.</summary>
public sealed class InvitationTokenRequestValidator : AbstractValidator<InvitationTokenRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public InvitationTokenRequestValidator()
    {
        RuleFor(r => r.Token).NotEmpty().MaximumLength(100).WithMessage("Bağlantı geçersiz.");
    }
}

/// <summary>Parola belirleme istegi dogrulamasi.</summary>
public sealed class AcceptInvitationRequestValidator : AbstractValidator<AcceptInvitationRequest>
{
    /// <summary>Yeni ornek olusturur.</summary>
    public AcceptInvitationRequestValidator()
    {
        RuleFor(r => r.Token).NotEmpty().MaximumLength(100).WithMessage("Bağlantı geçersiz.");
        RuleFor(r => r.Password)
            .NotEmpty().WithMessage("Parolanızı girin.")
            .MaximumLength(512).WithMessage("Parola en fazla 128 karakter olabilir.");
    }
}
