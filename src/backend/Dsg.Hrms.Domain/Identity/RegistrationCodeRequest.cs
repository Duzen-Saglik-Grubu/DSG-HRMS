namespace Dsg.Hrms.Domain.Identity;

/// <summary>
/// Uyelikte yapilan bir kod istegi (SYG-KMLK-059: kod gonderim hiz siniri).
/// </summary>
/// <remarks>
/// <para>
/// Hiz siniri TCKN'nin ozetine gore sayilir, kisiye gore DEGIL: eslesmeyen denemede kisi
/// yoktur ve sinir kisiye gore sayilsaydi, "429" yaniti yalnizca gercek personelde
/// gorulur ve eslesmeyi ele verirdi (<c>KR-016</c>).
/// </para>
/// <para>
/// Bir kayittir; <c>Entity</c> turunden turemez, denetim izine girmez.
/// </para>
/// </remarks>
public sealed class RegistrationCodeRequest
{
    private RegistrationCodeRequest()
    {
    }

    /// <summary>Veritabani ici birincil anahtar.</summary>
    public long Id { get; private set; }

    /// <summary>TCKN'nin anahtarli ozeti.</summary>
    public string NationalIdHash { get; private set; } = string.Empty;

    /// <summary>Istek ani (UTC).</summary>
    public DateTimeOffset RequestedAt { get; private set; }

    /// <summary>Yeni kayit.</summary>
    public static RegistrationCodeRequest Record(string nationalIdHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(nationalIdHash);
        return new RegistrationCodeRequest { NationalIdHash = nationalIdHash, RequestedAt = now };
    }
}
