using System.Security.Cryptography;
using System.Text;

namespace Dsg.Hrms.Infrastructure.Personnel.Protection;

/// <summary>
/// Ozel nitelikli verinin AES-256-GCM ile sifrelenmesi (ADR-0020 §1; SYG-PERS-060).
/// </summary>
/// <remarks>
/// <para>
/// Bicim: <c>k&lt;n&gt;:&lt;nonce&gt;:&lt;sifreli metin&gt;</c>; nonce ve sifreli metin (etiketiyle)
/// Base64'tur. Anahtar kimligi, anahtar dondurulurken eski degerlerin hala cozulebilmesi icindir.
/// Her deger icin rastgele nonce kullanilir: ayni kan grubu iki kiside farkli sifreli metin
/// uretir ve veritabanina erisen biri degerleri gruplayamaz.
/// </para>
/// <para>
/// Sifreleme, degerin <b>yerine</b> baglanir (<see cref="ProtectedField"/>: tablo, alan ve kayit
/// sahibinin <c>public_id</c>'si, AAD). Bir kisinin sifreli degeri baska bir kisinin satirina
/// veya baska bir kolona kopyalanirsa cozulemez.
/// </para>
/// <para>
/// Yalnizca depo katmani (senkronizasyon deposu ve kapsamli sorgu bileseni) kullanir; uygulama
/// katmani duz degeri gorur, veritabani yalnizca sifreli degeri.
/// </para>
/// </remarks>
public sealed class PersonalDataProtector
{
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly Dictionary<string, byte[]> _keys;
    private readonly string _currentKeyId;

    /// <summary>Yeni ornek olusturur; secenekler dogrulanmis olmalidir.</summary>
    public PersonalDataProtector(PersonalDataProtectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _keys = options.Keys.ToDictionary(k => k.Key, k => Convert.FromBase64String(k.Value), StringComparer.Ordinal);
        _currentKeyId = options.CurrentKeyId;
    }

    /// <summary>Yeni yazimlarda kullanilan anahtarin kimligi.</summary>
    public string CurrentKeyId => _currentKeyId;

    /// <summary>Degeri guncel anahtarla sifreler.</summary>
    public string Protect(string plaintext, ProtectedField field)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ArgumentNullException.ThrowIfNull(field);

        var plain = Encoding.UTF8.GetBytes(plaintext);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var sealedBytes = new byte[plain.Length + TagSize];

        using var aes = new AesGcm(_keys[_currentKeyId], TagSize);
        aes.Encrypt(nonce, plain, sealedBytes.AsSpan(0, plain.Length), sealedBytes.AsSpan(plain.Length), field.AssociatedData);

        return $"{_currentKeyId}:{Convert.ToBase64String(nonce)}:{Convert.ToBase64String(sealedBytes)}";
    }

    /// <summary>
    /// Sifreli degeri cozer. Bicim gecersizse, anahtar bilinmiyorsa veya deger bu yere ait
    /// degilse (AAD tutmuyorsa) <c>null</c> doner; ayrinti disariya verilmez.
    /// </summary>
    public string? TryUnprotect(string protectedValue, ProtectedField field)
    {
        ArgumentNullException.ThrowIfNull(protectedValue);
        ArgumentNullException.ThrowIfNull(field);

        var parts = protectedValue.Split(':');
        if (parts.Length != 3 || !_keys.TryGetValue(parts[0], out var key))
        {
            return null;
        }

        try
        {
            var nonce = Convert.FromBase64String(parts[1]);
            var sealedBytes = Convert.FromBase64String(parts[2]);
            if (nonce.Length != NonceSize || sealedBytes.Length < TagSize)
            {
                return null;
            }

            var length = sealedBytes.Length - TagSize;
            var plain = new byte[length];
            using var aes = new AesGcm(key, TagSize);
            aes.Decrypt(nonce, sealedBytes.AsSpan(0, length), sealedBytes.AsSpan(length), plain, field.AssociatedData);
            return Encoding.UTF8.GetString(plain);
        }
        catch (FormatException)
        {
            return null;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    /// <summary>Degerin hangi anahtarla sifrelendigi; bicim gecersizse <c>null</c>.</summary>
    public static string? KeyIdOf(string protectedValue)
    {
        ArgumentNullException.ThrowIfNull(protectedValue);
        var separator = protectedValue.IndexOf(':', StringComparison.Ordinal);
        return separator > 0 ? protectedValue[..separator] : null;
    }
}

/// <summary>
/// Sifreli degerin yeri: tablo, alan ve kayit sahibinin dis kimligi (ADR-0020 §1, AAD).
/// </summary>
/// <param name="Table">Tablo adi (sema dahil).</param>
/// <param name="Column">Kolon adi.</param>
/// <param name="OwnerPublicId">Kayit sahibinin <c>public_id</c>'si.</param>
public sealed record ProtectedField(string Table, string Column, Guid OwnerPublicId)
{
    /// <summary>Kisinin kan grubu (<c>personnel.person_sensitive.blood_group</c>).</summary>
    public static ProtectedField BloodGroup(Guid personPublicId) =>
        new("personnel.person_sensitive", "blood_group", personPublicId);

    /// <summary>
    /// Duzeltme talebinde onerilen kan grubu (<c>personnel.correction_request.proposed_value</c>;
    /// SYG-PERS-049). Sahibi talebin kendisidir: deger baska bir talebe kopyalanirsa cozulemez.
    /// </summary>
    public static ProtectedField CorrectionValue(Guid requestPublicId) =>
        new("personnel.correction_request", "proposed_value", requestPublicId);

    /// <summary>Sifrelemeye baglanan ilisiklendirilmis veri.</summary>
    internal byte[] AssociatedData => Encoding.UTF8.GetBytes($"{Table}|{Column}|{OwnerPublicId:D}");
}
