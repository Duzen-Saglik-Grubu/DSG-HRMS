using Dsg.Hrms.Api.Identity;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Personnel.Protection;
using Microsoft.AspNetCore.Mvc;

namespace Dsg.Hrms.Api.Personnel;

/// <summary>
/// Ozel nitelikli verinin sifrelemesi: anahtar durumu ve yeniden sifreleme isi (SYG-PERS-062;
/// ADR-0020 §3). Yalnizca <c>system.encryption.update</c>; bu izin yalnizca Sistem Yoneticisindedir (KR-138).
/// </summary>
/// <remarks>
/// Yanitlarda yalnizca anahtar kimlikleri ve sayilar vardir; deger, anahtar ve kisi donmez.
/// </remarks>
[ApiController]
[Route("api/v1/personnel/encryption")]
[Produces("application/json")]
public sealed class PersonalDataEncryptionController : ControllerBase
{
    private readonly PersonalDataKeyService _keys;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonalDataEncryptionController(PersonalDataKeyService keys)
    {
        _keys = keys;
    }

    /// <summary>Sifreli degerlerin anahtara gore sayilari.</summary>
    /// <response code="200">Durum.</response>
    /// <response code="403">Yetki yok (<c>system.encryption.update</c>).</response>
    [HttpGet]
    [HasPermission(Permissions.EncryptionUpdate)]
    [ProducesResponseType<EncryptionStatusResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<EncryptionStatusResponse> StatusAsync(CancellationToken cancellationToken)
    {
        var status = await _keys.StatusAsync(cancellationToken);
        return new EncryptionStatusResponse(
            status.CurrentKeyId,
            [.. status.Keys.Select(k => new EncryptionKeyResponse(k.KeyId, k.Count, k.IsCurrent, k.IsConfigured))]);
    }

    /// <summary>
    /// Guncel anahtar disindaki degerleri guncel anahtarla yeniden yazar. Denetim izine "degisti"
    /// yazilmaz; is tek bir guvenlik olayidir. Tekrar calistirmak guvenlidir.
    /// </summary>
    /// <response code="200">Sonuc.</response>
    /// <response code="403">Yetki yok (<c>system.encryption.update</c>).</response>
    [HttpPost("re-encryption")]
    [HasPermission(Permissions.EncryptionUpdate)]
    [ProducesResponseType<ReEncryptionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<ReEncryptionResponse> ReEncryptAsync(CancellationToken cancellationToken)
    {
        var result = await _keys.ReEncryptAsync(cancellationToken);
        return new ReEncryptionResponse(result.TargetKeyId, result.SourceKeyIds, result.ReEncrypted, result.Undecryptable, result.RemainingOnOldKeys);
    }
}

/// <summary>Anahtar durumu.</summary>
/// <param name="CurrentKeyId">Yeni yazimlarda kullanilan anahtar.</param>
/// <param name="Keys">Anahtarlar ve sayilari.</param>
public sealed record EncryptionStatusResponse(string CurrentKeyId, IReadOnlyList<EncryptionKeyResponse> Keys);

/// <summary>Bir anahtar.</summary>
/// <param name="KeyId">Kimlik.</param>
/// <param name="Count">Bu anahtarla sifreli deger sayisi.</param>
/// <param name="IsCurrent">Guncel anahtar mi.</param>
/// <param name="IsConfigured">Yapilandirmada tanimli mi; degilse degerler cozulemez.</param>
public sealed record EncryptionKeyResponse(string KeyId, int Count, bool IsCurrent, bool IsConfigured);

/// <summary>Yeniden sifreleme sonucu.</summary>
/// <param name="TargetKeyId">Hedef anahtar.</param>
/// <param name="SourceKeyIds">Yeniden yazilan degerlerin onceki anahtarlari.</param>
/// <param name="ReEncrypted">Yeniden yazilan deger sayisi.</param>
/// <param name="Undecryptable">Cozulemeyen deger sayisi.</param>
/// <param name="RemainingOnOldKeys">Guncel anahtar disinda kalan deger sayisi; eski anahtar ancak 0 iken cikarilir.</param>
public sealed record ReEncryptionResponse(
    string TargetKeyId,
    IReadOnlyList<string> SourceKeyIds,
    int ReEncrypted,
    int Undecryptable,
    int RemainingOnOldKeys);
