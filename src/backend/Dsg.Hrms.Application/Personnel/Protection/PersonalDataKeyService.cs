namespace Dsg.Hrms.Application.Personnel.Protection;

/// <summary>Bir anahtarla sifreli deger sayisi.</summary>
/// <param name="KeyId">Anahtar kimligi (orn. <c>k2</c>).</param>
/// <param name="Count">Bu anahtarla sifreli deger sayisi.</param>
/// <param name="IsCurrent">Yeni yazimlarda kullanilan anahtar mi.</param>
/// <param name="IsConfigured">Anahtar yapilandirmada tanimli mi; degilse bu degerler cozulemez.</param>
public sealed record KeyUsage(string KeyId, int Count, bool IsCurrent, bool IsConfigured);

/// <summary>Ozel nitelikli verinin anahtar durumu (SYG-PERS-062). Yalnizca sayilar; deger ve kisi yok.</summary>
/// <param name="CurrentKeyId">Guncel anahtar.</param>
/// <param name="Keys">Tanimli anahtarlar ve degerlerde gecen anahtarlar, sayilariyla.</param>
public sealed record PersonalDataKeyStatus(string CurrentKeyId, IReadOnlyList<KeyUsage> Keys);

/// <summary>Yeniden sifreleme isinin sonucu (SYG-PERS-062).</summary>
/// <param name="TargetKeyId">Hedef (guncel) anahtar.</param>
/// <param name="SourceKeyIds">Yeniden yazilan degerlerin onceki anahtarlari.</param>
/// <param name="ReEncrypted">Yeniden yazilan deger sayisi.</param>
/// <param name="Undecryptable">Cozulemedigi icin yeniden yazilamayan deger sayisi (bozuk deger, tanimsiz anahtar, baska yere ait deger).</param>
/// <param name="RemainingOnOldKeys">Is sonunda guncel anahtar disinda bir anahtarla sifreli kalan deger sayisi; eski anahtar ancak 0 iken cikarilir.</param>
public sealed record ReEncryptionResult(
    string TargetKeyId,
    IReadOnlyList<string> SourceKeyIds,
    int ReEncrypted,
    int Undecryptable,
    int RemainingOnOldKeys);

/// <summary>
/// Sifreli degerlerin deposu (ADR-0020 §3). Sifreleme ve cozme yalnizca depo katmanindadir.
/// </summary>
public interface IPersonalDataKeyStore
{
    /// <summary>Degerlerin anahtara gore sayilari.</summary>
    Task<PersonalDataKeyStatus> StatusAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Guncel anahtar disindaki her degeri cozup guncel anahtarla ayni yere bagli olarak yeniden
    /// yazar ve isi tek bir guvenlik olayi olarak kaydeder; hepsi tek islemde.
    /// </summary>
    Task<ReEncryptionResult> ReEncryptAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Ozel nitelikli verinin yeniden sifrelenmesi (SYG-PERS-062; ADR-0020 §3).
/// </summary>
/// <remarks>
/// <para>
/// Anahtar dondurulunce yeni yazimlar yeni anahtarla yapilir; eski degerler kendiliginden
/// degismez, cunku senkronizasyon yalnizca degisen degeri yazar. Bu is onlari acikca yeniden
/// yazar. Kapsam: kisilerin kan grubu ve acik duzeltme taleplerindeki onerilen kan grubu.
/// </para>
/// <para>
/// Deger degismedigi icin denetim izine "degisti" kaydi YAZILMAZ; isin kendisi tek bir guvenlik
/// olayidir. Yetki (<c>system.encryption.update</c>, yalnizca Sistem Yoneticisi; KR-138) cagiran
/// katmanda denetlenir. Is tekrar calistirilabilir: guncel anahtarla sifreli deger yeniden yazilmaz.
/// </para>
/// </remarks>
public sealed class PersonalDataKeyService
{
    private readonly IPersonalDataKeyStore _store;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonalDataKeyService(IPersonalDataKeyStore store)
    {
        _store = store;
    }

    /// <summary>Anahtar durumunu doner.</summary>
    public Task<PersonalDataKeyStatus> StatusAsync(CancellationToken cancellationToken) => _store.StatusAsync(cancellationToken);

    /// <summary>Yeniden sifreleme isini calistirir.</summary>
    public Task<ReEncryptionResult> ReEncryptAsync(CancellationToken cancellationToken) => _store.ReEncryptAsync(cancellationToken);
}
