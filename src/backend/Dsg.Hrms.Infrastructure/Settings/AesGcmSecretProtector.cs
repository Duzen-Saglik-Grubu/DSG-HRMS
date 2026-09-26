using System.Security.Cryptography;
using System.Text;
using Dsg.Hrms.Application.Settings;

namespace Dsg.Hrms.Infrastructure.Settings;

/// <summary>
/// AES-256-GCM ile sir sifreleme (SYG-KMLK-075).
/// </summary>
/// <remarks>
/// <para>
/// Bicim: <c>v1:</c> + Base64(nonce[12] | etiket[16] | sifreli metin). Surum oneki,
/// anahtar veya algoritma degistiginde eski degerlerin taninmasi icindir.
/// </para>
/// <para>
/// GCM, gizliligin yaninda <b>butunlugu</b> de saglar: sifreli metin veritabaninda
/// degistirilirse cozme basarisiz olur, bozuk bir parola sessizce kullanilmaz. Amac
/// (parametre kimligi) ek dogrulanmis veri olarak baglanir.
/// </para>
/// </remarks>
public sealed class AesGcmSecretProtector : ISecretProtector
{
    private const string Prefix = "v1:";
    private const int NonceSize = 12;
    private const int TagSize = 16;

    private readonly byte[]? _key;

    /// <summary>Yeni ornek olusturur.</summary>
    public AesGcmSecretProtector(SecretProtectionOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.IsConfigured)
        {
            _key = Convert.FromBase64String(options.Key!);
        }
    }

    /// <inheritdoc />
    public bool IsConfigured => _key is not null;

    /// <inheritdoc />
    public string Protect(string plaintext, string purpose)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        var key = RequireKey();

        var plain = Encoding.UTF8.GetBytes(plaintext);
        var payload = new byte[NonceSize + TagSize + plain.Length];
        var nonce = payload.AsSpan(0, NonceSize);
        var tag = payload.AsSpan(NonceSize, TagSize);
        var cipher = payload.AsSpan(NonceSize + TagSize);

        RandomNumberGenerator.Fill(nonce);

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plain, cipher, tag, Encoding.UTF8.GetBytes(purpose));

        return Prefix + Convert.ToBase64String(payload);
    }

    /// <inheritdoc />
    public string Unprotect(string protectedValue, string purpose)
    {
        ArgumentNullException.ThrowIfNull(protectedValue);
        var key = RequireKey();

        if (!protectedValue.StartsWith(Prefix, StringComparison.Ordinal))
        {
            throw new CryptographicException("Sifreli deger taninan bicimde degil.");
        }

        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(protectedValue[Prefix.Length..]);
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Sifreli deger taninan bicimde degil.", ex);
        }

        if (payload.Length < NonceSize + TagSize)
        {
            throw new CryptographicException("Sifreli deger taninan bicimde degil.");
        }

        var plain = new byte[payload.Length - NonceSize - TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(
            payload.AsSpan(0, NonceSize),
            payload.AsSpan(NonceSize + TagSize),
            payload.AsSpan(NonceSize, TagSize),
            plain,
            Encoding.UTF8.GetBytes(purpose));

        return Encoding.UTF8.GetString(plain);
    }

    private byte[] RequireKey() =>
        _key ?? throw new InvalidOperationException(
            "Sir parametre sifreleme anahtari (ParameterProtection:Key) tanimli degil.");
}
