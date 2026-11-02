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
    /// Kucultme kuralinin surumu. Kural degisince artar; eski surumle saklanan fotograf, LOGO'da
    /// degismese de bir sonraki senkronizasyonda yeniden okunur ve kucultulur (#315).
    /// </summary>
    int Version { get; }

    /// <summary>
    /// Goruntuyu uzun kenari en fazla 400 piksel olacak sekilde kucultur ve JPEG'e cevirir.
    /// Kucuk goruntu buyutulmez. EXIF yonu uygulanmaz: goruntu LOGO'da nasil gorunuyorsa oyle
    /// kalir (#315). Saydam alanlar beyaz olur.
    /// </summary>
    /// <returns>JPEG baytlari; goruntu cozulemezse <c>null</c>.</returns>
    byte[]? CreateThumbnail(byte[] source);
}
