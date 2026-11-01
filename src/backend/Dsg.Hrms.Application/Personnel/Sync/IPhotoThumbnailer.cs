namespace Dsg.Hrms.Application.Personnel.Sync;

/// <summary>
/// LOGO'daki fotografi saklanacak kucuk hale getirir (ADR-0021 §5, SYG-PERS-014).
/// </summary>
/// <remarks>
/// Uygulama katmani goruntu kutuphanesine bagimli degildir (ADR-0002); uygulamasi altyapi
/// katmanindadir.
/// </remarks>
public interface IPhotoThumbnailer
{
    /// <summary>
    /// Goruntuyu uzun kenari en fazla 400 piksel olacak sekilde kucultur ve JPEG'e cevirir.
    /// Kucuk goruntu buyutulmez. EXIF yonu uygulanir; saydam alanlar beyaz olur.
    /// </summary>
    /// <returns>JPEG baytlari; goruntu cozulemezse <c>null</c>.</returns>
    byte[]? CreateThumbnail(byte[] source);
}
