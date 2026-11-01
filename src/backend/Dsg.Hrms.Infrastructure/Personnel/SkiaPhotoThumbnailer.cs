using Dsg.Hrms.Application.Personnel.Sync;
using SkiaSharp;

namespace Dsg.Hrms.Infrastructure.Personnel;

/// <summary>
/// <see cref="IPhotoThumbnailer"/> uygulamasi: SkiaSharp (MIT; ADR-0021 §5). ImageSharp, lisansi
/// ciroya bagli oldugu icin kullanilmaz (<c>KR-025</c> ile ayni ilke).
/// </summary>
/// <remarks>
/// <para>
/// LOGO'daki fotograflarin bir kismi telefonla cekilmistir ve yonu EXIF bilgisindedir
/// (01.11.2026: 237 JPEG). <see cref="SKImage.FromEncodedData(SKData)"/> yonu cozumleme sirasinda
/// uygular; ayrica dondurulmez. Bunu <c>SkiaPhotoThumbnailerTests</c> denetler: kutuphane
/// davranisi degisirse fotograflar yan durur ve test duser.
/// </para>
/// <para>
/// PNG'deki saydam alanlar beyaza boyanir: JPEG saydamlik tasimaz ve siyah zemin olusurdu.
/// </para>
/// </remarks>
public sealed class SkiaPhotoThumbnailer : IPhotoThumbnailer
{
    /// <summary>Uzun kenarin en buyuk boyu (piksel).</summary>
    public const int MaxSize = 400;

    /// <summary>JPEG kalitesi; 400 pikselde fotograf basina yaklasik 20–40 KB.</summary>
    public const int Quality = 85;

    /// <inheritdoc />
    public byte[]? CreateThumbnail(byte[] source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Length == 0)
        {
            return null;
        }

        using var data = SKData.CreateCopy(source);
        using var image = SKImage.FromEncodedData(data);
        if (image is null)
        {
            return null;
        }

        var scale = Math.Min(1f, (float)MaxSize / Math.Max(image.Width, image.Height));
        var width = Math.Max(1, (int)Math.Round(image.Width * scale));
        var height = Math.Max(1, (int)Math.Round(image.Height * scale));

        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul));
        if (surface is null)
        {
            return null;
        }

        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        // Buyuk oranli kucultmede mipmap ile dogrusal ornekleme kirpintisiz sonuc verir.
        canvas.DrawImage(image, new SKRect(0, 0, width, height), new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        canvas.Flush();

        using var snapshot = surface.Snapshot();
        using var encoded = snapshot.Encode(SKEncodedImageFormat.Jpeg, Quality);
        return encoded?.ToArray();
    }
}
