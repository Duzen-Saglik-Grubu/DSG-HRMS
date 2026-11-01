using SkiaSharp;

namespace Dsg.Hrms.Api.IntegrationTests.Personnel;

/// <summary>
/// Testler icin SENTETIK goruntuler: duz renkli JPEG. Gercek kisilere ait fotograf KULLANILMAZ (KVKK).
/// </summary>
internal static class TestImages
{
    /// <summary>Verilen boyut ve renkte JPEG uretir.</summary>
    public static byte[] Jpeg(int width, int height, SKColor? color = null)
    {
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        surface.Canvas.Clear(color ?? SKColors.SteelBlue);
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }
}
