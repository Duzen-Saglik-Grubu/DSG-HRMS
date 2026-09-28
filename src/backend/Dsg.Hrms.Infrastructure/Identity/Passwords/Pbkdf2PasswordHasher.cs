using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Dsg.Hrms.Application.Identity.Passwords;

namespace Dsg.Hrms.Infrastructure.Identity.Passwords;

/// <summary>
/// PBKDF2-HMAC-SHA512 ile parola ozeti (SYG-KMLK-049, ADR-0006 §6).
/// </summary>
/// <remarks>
/// <para>
/// Bicim: <c>pbkdf2-sha512$&lt;yineleme&gt;$&lt;tuz&gt;$&lt;ozet&gt;</c> (Base64). Yineleme sayisi
/// ozetin icinde saklanir: sayi ileride artirildiginda eski ozetler dogrulanmaya devam
/// eder ve kullanici bir sonraki giriste yeni sayiyla yeniden ozetlenebilir.
/// </para>
/// <para>
/// Yineleme sayisi OWASP'nin PBKDF2-HMAC-SHA512 onerisidir (210.000). SYG-KMLK-049'daki
/// 100–500 ms hedefi UAT donaniminda olculur; gerekirse <see cref="Iterations"/> artirilir.
/// </para>
/// </remarks>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    /// <summary>Yeni ozetlerde kullanilan yineleme sayisi.</summary>
    public const int Iterations = 210_000;

    private const string Prefix = "pbkdf2-sha512";

    private readonly Lazy<string> _dummyHash;

    /// <summary>Yeni ornek olusturur.</summary>
    public Pbkdf2PasswordHasher()
    {
        _dummyHash = new Lazy<string>(() => Hash(Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))));
    }

    /// <inheritdoc />
    public string DummyHash => _dummyHash.Value;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    /// <inheritdoc />
    public string Hash(string normalizedPassword)
    {
        ArgumentNullException.ThrowIfNull(normalizedPassword);

        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Derive(normalizedPassword, salt, Iterations);

        return string.Create(
            CultureInfo.InvariantCulture,
            $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}");
    }

    /// <inheritdoc />
    public bool Verify(string passwordHash, string normalizedPassword)
    {
        ArgumentNullException.ThrowIfNull(passwordHash);
        ArgumentNullException.ThrowIfNull(normalizedPassword);

        var parts = passwordHash.Split('$');
        if (parts.Length != 4
            || parts[0] != Prefix
            || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations)
            || iterations < 100_000)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            return CryptographicOperations.FixedTimeEquals(Derive(normalizedPassword, salt, iterations, expected.Length), expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] Derive(string password, byte[] salt, int iterations, int length = HashSize) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(password), salt, iterations, HashAlgorithmName.SHA512, length);
}
