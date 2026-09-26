using System.Security.Cryptography;
using System.Text;
using Dsg.Hrms.Application.Identity.Verification;

namespace Dsg.Hrms.Infrastructure.Identity;

/// <summary>
/// HMAC-SHA256 ile kod ozeti (SYG-KMLK-025).
/// </summary>
/// <remarks>
/// Ozet girdisine kodun dis kimligi de katilir: ayni kod iki farkli kayitta farkli ozet
/// uretir. Boylece bir kaydin ozeti bilinse bile baska kayitlar icin onceden
/// hesaplanmis bir tablo kullanilamaz.
/// </remarks>
public sealed class HmacVerificationCodeHasher : IVerificationCodeHasher
{
    private readonly byte[]? _key;

    /// <summary>Yeni ornek olusturur.</summary>
    public HmacVerificationCodeHasher(CodeHashOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (options.IsConfigured)
        {
            _key = Convert.FromBase64String(options.CodeHashKey!);
        }
    }

    /// <inheritdoc />
    public bool IsConfigured => _key is not null;

    /// <inheritdoc />
    public string Hash(Guid codeId, string code) => Convert.ToBase64String(Compute(codeId, code));

    /// <inheritdoc />
    public bool Matches(Guid codeId, string code, string storedHash)
    {
        ArgumentNullException.ThrowIfNull(storedHash);

        byte[] stored;
        try
        {
            stored = Convert.FromBase64String(storedHash);
        }
        catch (FormatException)
        {
            return false;
        }

        // Sabit surede karsilastirma: eslesen bayt sayisi yanit suresinden anlasilamaz.
        return CryptographicOperations.FixedTimeEquals(Compute(codeId, code), stored);
    }

    private byte[] Compute(Guid codeId, string code)
    {
        ArgumentNullException.ThrowIfNull(code);
        var key = _key ?? throw new InvalidOperationException("Dogrulama kodu ozet anahtari (Identity:CodeHashKey) tanimli degil.");

        return HMACSHA256.HashData(key, Encoding.UTF8.GetBytes($"{codeId:N}:{code}"));
    }
}
