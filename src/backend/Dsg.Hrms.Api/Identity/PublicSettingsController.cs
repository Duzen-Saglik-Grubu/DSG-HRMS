using Dsg.Hrms.Application.Settings;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Identity;

/// <summary>
/// Giris ve uyelik ekranlarinin kimlik dogrulamasi olmadan okudugu ayarlar (SYG-KMLK-070).
/// </summary>
/// <remarks>
/// <para>
/// Yalnizca ekranda gosterilmek icin zaten herkese acik olan bilgiler doner: destek
/// iletisim bilgisi ve parola kurallari. SIR veya kisisel veri DONMEZ; yeni bir alan
/// eklenirken bu kural korunur.
/// </para>
/// <para>
/// Parola kurallari ekranda yalnizca YARDIM icindir; asil denetim sunucudadir
/// (SYG-KMLK-044, 045).
/// </para>
/// </remarks>
[ApiController]
[Route("api/v1/identity/public-settings")]
[Produces("application/json")]
public sealed class PublicSettingsController : ControllerBase
{
    private readonly ISystemParameters _parameters;
    private readonly BrandingService _branding;

    /// <summary>Yeni ornek olusturur.</summary>
    public PublicSettingsController(ISystemParameters parameters, BrandingService branding)
    {
        _parameters = parameters;
        _branding = branding;
    }

    /// <summary>Giris ve uyelik ekranlarinin ayarlarini dondurur.</summary>
    /// <response code="200">Ayarlar.</response>
    [HttpGet]
    [ProducesResponseType<PublicSettingsResponse>(StatusCodes.Status200OK)]
    public async Task<PublicSettingsResponse> GetAsync(CancellationToken cancellationToken) =>
        new(
            await _parameters.GetAsync(ParameterCatalog.SupportContact, cancellationToken) ?? string.Empty,
            new PasswordRulesResponse(
                await _parameters.GetIntegerAsync(ParameterCatalog.MinPasswordLength, cancellationToken),
                Application.Identity.Passwords.PasswordPolicy.MaxLength,
                await _parameters.GetBooleanAsync(ParameterCatalog.RequireComplexPassword, cancellationToken)),
            await _parameters.GetIntegerAsync(ParameterCatalog.VerificationCodeLength, cancellationToken),
            await _branding.GetLogoVersionAsync(cancellationToken));
}

/// <summary>Giris ve uyelik ekranlarinin ayarlari.</summary>
/// <param name="SupportContact">Sorun yasayan kullanicinin basvuracagi birim ve iletisim bilgisi (PRM-GRN-04).</param>
/// <param name="PasswordRules">Parola kurallari (ekranda yardim icin).</param>
/// <param name="VerificationCodeLength">Dogrulama kodunun hane sayisi (PRM-KML-09).</param>
/// <param name="LogoVersion">Yuklu kurumsal logonun surumu (PRM-GRN-01); yuklenmemisse bos ve varsayilan logo kullanilir.</param>
public sealed record PublicSettingsResponse(string SupportContact, PasswordRulesResponse PasswordRules, int VerificationCodeLength, string? LogoVersion);

/// <summary>Parola kurallari.</summary>
/// <param name="MinLength">En az uzunluk (PRM-KML-05).</param>
/// <param name="MaxLength">En fazla uzunluk.</param>
/// <param name="RequireComplexity">Buyuk/kucuk harf, rakam ve simge zorunlu mu (PRM-KML-06).</param>
public sealed record PasswordRulesResponse(int MinLength, int MaxLength, bool RequireComplexity);
