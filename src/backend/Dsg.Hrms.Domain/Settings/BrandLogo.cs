using Dsg.Hrms.Domain.Common;

namespace Dsg.Hrms.Domain.Settings;

/// <summary>
/// Kurumsal logo (PRM-GRN-01, SYG-KMLK-069, 076). Tabloda en fazla bir satir bulunur.
/// </summary>
/// <remarks>
/// <para>
/// Logo yuklenmemisse istemci depodaki varsayilan logoyu (<c>assets/duzen_logo.png</c>) kullanir.
/// Yalnizca PNG ve JPEG kabul edilir: SVG, icine betik gomulebildigi icin kabul edilmez.
/// </para>
/// <para>
/// Degisiklik denetim izine yazilir; dosyanin kendisi degil, ozeti ve boyutu yazilir
/// (<see cref="Content"/> ad kuraliyla dislanir).
/// </para>
/// </remarks>
public sealed class BrandLogo : Entity, IAuditable
{
    /// <summary>Kabul edilen en buyuk dosya boyutu (bayt).</summary>
    public const int MaxSizeBytes = 512 * 1024;

    private BrandLogo()
    {
    }

    /// <summary>Dosyanin icerigi.</summary>
    public byte[] Content { get; private set; } = [];

    /// <summary>Icerik turu (<c>image/png</c> veya <c>image/jpeg</c>).</summary>
    public string ContentType { get; private set; } = string.Empty;

    /// <summary>Icerigin SHA-256 ozeti (onaltilik); tarayici onbellegi icin surum.</summary>
    public string Sha256 { get; private set; } = string.Empty;

    /// <summary>Boyut (bayt).</summary>
    public int SizeBytes { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public long? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }

    /// <summary>Yeni logo olusturur.</summary>
    public static BrandLogo Create(byte[] content, string contentType, string sha256)
    {
        var logo = new BrandLogo();
        logo.Replace(content, contentType, sha256);
        return logo;
    }

    /// <summary>Logoyu degistirir.</summary>
    /// <exception cref="ArgumentException">Icerik bos, cok buyuk veya turu desteklenmiyorsa.</exception>
    public void Replace(byte[] content, string contentType, string sha256)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(sha256);

        if (content.Length == 0 || content.Length > MaxSizeBytes)
        {
            throw new ArgumentException("Logo bos olamaz ve boyut sinirini asamaz.", nameof(content));
        }

        if (contentType is not ("image/png" or "image/jpeg"))
        {
            throw new ArgumentException("Yalnizca PNG ve JPEG kabul edilir.", nameof(contentType));
        }

        Content = content;
        ContentType = contentType;
        Sha256 = sha256;
        SizeBytes = content.Length;
    }
}
