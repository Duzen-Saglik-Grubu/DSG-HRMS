namespace Dsg.Hrms.Application.Settings;

/// <summary>
/// Sir parametreleri veritabanina yazilmadan once sifreler (SYG-KMLK-075).
/// </summary>
/// <remarks>
/// Sifreleme anahtari veritabaninda DEGIL, ortam degiskenindedir (ADR-0008). Anahtar
/// veritabaninda olsaydi, veritabani yedegini ele geciren parolalari da cozebilirdi.
/// </remarks>
public interface ISecretProtector
{
    /// <summary>Anahtar tanimli mi. Tanimli degilse sir parametre ekrandan girilemez.</summary>
    bool IsConfigured { get; }

    /// <summary>Degeri sifreler. <paramref name="purpose"/> sifreli metne baglanir.</summary>
    /// <remarks>
    /// Amac olarak parametre kimligi verilir: bir parametrenin sifreli degeri baska bir
    /// parametrenin satirina tasinirsa cozulemez.
    /// </remarks>
    string Protect(string plaintext, string purpose);

    /// <summary>Sifreli degeri cozer.</summary>
    /// <exception cref="System.Security.Cryptography.CryptographicException">Deger bozuksa veya anahtar farkliysa.</exception>
    string Unprotect(string protectedValue, string purpose);
}
