using System.Security.Cryptography;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Domain.Settings;
using Microsoft.Extensions.Logging;

namespace Dsg.Hrms.Application.Settings;

/// <summary>Kurumsal logonun veritabani tarafi.</summary>
public interface IBrandLogoStore
{
    /// <summary>Logoyu bulur (izlenen); yuklenmemisse <c>null</c>.</summary>
    Task<BrandLogo?> FindAsync(CancellationToken cancellationToken);

    /// <summary>Yuklu logonun ozeti; icerik okunmaz. Yuklenmemisse <c>null</c>.</summary>
    Task<string?> FindVersionAsync(CancellationToken cancellationToken);

    /// <summary>Yeni kaydi ekler.</summary>
    void Add(BrandLogo logo);

    /// <summary>Kaydi siler.</summary>
    void Remove(BrandLogo logo);

    /// <summary>Degisiklikleri kaydeder.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Kurumsal logo (PRM-GRN-01, SYG-KMLK-069, 076).
/// </summary>
/// <remarks>
/// Dosya turu uzantiya veya istemcinin bildirdigi ture gore DEGIL, dosyanin ilk baytlarina gore
/// belirlenir: uzantisi <c>.png</c> olan bir HTML dosyasi logo olarak kabul edilmez.
/// </remarks>
public sealed partial class BrandingService
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];

    private readonly IBrandLogoStore _store;
    private readonly ILogger<BrandingService> _logger;

    /// <summary>Yeni ornek olusturur.</summary>
    public BrandingService(IBrandLogoStore store, ILogger<BrandingService> logger)
    {
        _store = store;
        _logger = logger;
    }

    /// <summary>Yuklu logo; yoksa <c>null</c> (istemci varsayilan logoyu kullanir).</summary>
    public Task<BrandLogo?> GetLogoAsync(CancellationToken cancellationToken) => _store.FindAsync(cancellationToken);

    /// <summary>
    /// Yuklu logonun surumu (icerik ozeti); yuklenmemisse <c>null</c>. Giris ekrani logoyu bu
    /// surumle ister: logo degisince tarayici yenisini alir, yuklenmemisse hic istemez.
    /// </summary>
    public Task<string?> GetLogoVersionAsync(CancellationToken cancellationToken) => _store.FindVersionAsync(cancellationToken);

    /// <summary>Logoyu yukler veya degistirir.</summary>
    /// <exception cref="BusinessRuleException">Dosya bos, cok buyuk veya PNG/JPEG degilse.</exception>
    public async Task SetLogoAsync(byte[] content, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (content.Length == 0)
        {
            throw new BusinessRuleException("Dosya boş.");
        }

        if (content.Length > BrandLogo.MaxSizeBytes)
        {
            throw new BusinessRuleException($"Logo en fazla {BrandLogo.MaxSizeBytes / 1024} KB olabilir.");
        }

        var contentType = content.AsSpan().StartsWith(PngSignature) ? "image/png"
            : content.AsSpan().StartsWith(JpegSignature) ? "image/jpeg"
            : throw new BusinessRuleException("Logo PNG veya JPEG biçiminde olmalıdır.");

        var sha256 = Convert.ToHexStringLower(SHA256.HashData(content));
        var logo = await _store.FindAsync(cancellationToken).ConfigureAwait(false);
        if (logo is null)
        {
            _store.Add(BrandLogo.Create(content, contentType, sha256));
        }
        else
        {
            logo.Replace(content, contentType, sha256);
        }

        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogReplaced(_logger, content.Length, contentType);
    }

    /// <summary>Yuklu logoyu kaldirir; istemci varsayilan logoya doner.</summary>
    public async Task RemoveLogoAsync(CancellationToken cancellationToken)
    {
        var logo = await _store.FindAsync(cancellationToken).ConfigureAwait(false);
        if (logo is null)
        {
            return;
        }

        _store.Remove(logo);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogRemoved(_logger);
    }

    [LoggerMessage(EventId = 3970, Level = LogLevel.Information, Message = "Kurumsal logo degistirildi: {Size} bayt, {ContentType} (PRM-GRN-01).")]
    private static partial void LogReplaced(ILogger logger, int size, string contentType);

    [LoggerMessage(EventId = 3971, Level = LogLevel.Information, Message = "Kurumsal logo kaldirildi; varsayilan logo kullanilacak (PRM-GRN-01).")]
    private static partial void LogRemoved(ILogger logger);
}
