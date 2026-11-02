using Dsg.Hrms.Infrastructure.Personnel;
using SkiaSharp;

namespace Dsg.Hrms.Infrastructure.Tests.Personnel;

/// <summary>
/// Fotograf kucultme (SYG-PERS-014; ADR-0021 §5). Goruntuler test icinde uretilir; gercek kisilere
/// ait fotograf KULLANILMAZ (KVKK).
/// </summary>
public sealed class SkiaPhotoThumbnailerTests
{
    private readonly SkiaPhotoThumbnailer _thumbnailer = new();

    [Fact]
    public void Large_image_is_scaled_to_400_pixels_on_the_long_side_as_jpeg()
    {
        var thumbnail = _thumbnailer.CreateThumbnail(Encode(Halves(1200, 600, SKColors.Red, SKColors.Blue), SKEncodedImageFormat.Png))!;

        thumbnail[..2].ShouldBe(new byte[] { 0xFF, 0xD8 });
        using var bitmap = SKBitmap.Decode(thumbnail);
        (bitmap.Width, bitmap.Height).ShouldBe((400, 200));
    }

    [Fact]
    public void Small_image_is_not_enlarged()
    {
        var thumbnail = _thumbnailer.CreateThumbnail(Encode(Halves(120, 160, SKColors.Red, SKColors.Blue), SKEncodedImageFormat.Jpeg))!;

        using var bitmap = SKBitmap.Decode(thumbnail);
        (bitmap.Width, bitmap.Height).ShouldBe((120, 160));
    }

    [Fact]
    public void Exif_orientation_is_applied()
    {
        // Telefonla cekilen fotografta yon EXIF'tedir (6: saat yonunde 90 derece). Sol yari kirmizi
        // olan yatay goruntu, dondurulunce dikey olur ve kirmizi yari uste gelir.
        var rotated = WithExifOrientation(Encode(Halves(600, 200, SKColors.Red, SKColors.Blue), SKEncodedImageFormat.Jpeg), 6);

        using var bitmap = SKBitmap.Decode(_thumbnailer.CreateThumbnail(rotated)!);

        (bitmap.Width, bitmap.Height).ShouldBe((133, 400));
        IsReddish(bitmap.GetPixel(66, 20)).ShouldBeTrue();
        IsReddish(bitmap.GetPixel(66, 380)).ShouldBeFalse();
    }

    [Fact]
    public void Transparent_areas_become_white()
    {
        using var surface = SKSurface.Create(new SKImageInfo(50, 50, SKColorType.Rgba8888, SKAlphaType.Premul));
        surface.Canvas.Clear(SKColors.Transparent);
        using var image = surface.Snapshot();
        using var png = image.Encode(SKEncodedImageFormat.Png, 100);

        using var bitmap = SKBitmap.Decode(_thumbnailer.CreateThumbnail(png.ToArray())!);

        var pixel = bitmap.GetPixel(25, 25);
        (pixel.Red, pixel.Green, pixel.Blue).ShouldBe(((byte)255, (byte)255, (byte)255));
    }

    [Theory]
    [InlineData(new byte[0])]
    [InlineData(new byte[] { 0x00, 0x01, 0x02, 0x03 })]
    public void Undecodable_input_returns_null(byte[] source) =>
        _thumbnailer.CreateThumbnail(source).ShouldBeNull();

    private static bool IsReddish(SKColor color) => color.Red > 200 && color.Blue < 60;

    /// <summary>Sol yarisi bir renk, sag yarisi diger renk olan goruntu.</summary>
    private static SKImage Halves(int width, int height, SKColor left, SKColor right)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        using var paint = new SKPaint { Color = right };
        surface.Canvas.Clear(left);
        surface.Canvas.DrawRect(width / 2f, 0, width / 2f, height, paint);
        return surface.Snapshot();
    }

    private static byte[] Encode(SKImage image, SKEncodedImageFormat format)
    {
        using (image)
        using (var data = image.Encode(format, 95))
        {
            return data.ToArray();
        }
    }

    /// <summary>
    /// JPEG'e yalnizca yon etiketi tasiyan en kucuk EXIF bolumunu (APP1) ekler: TIFF basligi
    /// (buyuk endian), tek girdili IFD, etiket 0x0112.
    /// </summary>
    private static byte[] WithExifOrientation(byte[] jpeg, byte orientation)
    {
        byte[] app1 =
        [
            0xFF, 0xE1, 0x00, 0x22,
            (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0x00, 0x00,
            (byte)'M', (byte)'M', 0x00, 0x2A, 0x00, 0x00, 0x00, 0x08,
            0x00, 0x01,
            0x01, 0x12, 0x00, 0x03, 0x00, 0x00, 0x00, 0x01, 0x00, orientation, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00,
        ];

        return [.. jpeg[..2], .. app1, .. jpeg[2..]];
    }
}
