using Dsg.Hrms.Domain.Common;

namespace Dsg.Hrms.Domain.Identity;

/// <summary>
/// IK'nin gonderdigi tek kullanimlik parola olusturma baglantisi (SYG-KMLK-051…053).
/// </summary>
/// <remarks>
/// <para>
/// Jetonun kendisi SAKLANMAZ; yalnizca SHA-256 ozeti tutulur (<see cref="TokenHash"/>).
/// Veritabanini okuyan biri baglantiyi yeniden uretemez. Baglanti tek kullanimliktir ve
/// suresi sonunda gecersizlesir. Yeni baglanti gonderildiginde oncekiler geri alinir
/// (SYG-KMLK-052).
/// </para>
/// <para>
/// Gonderen (<see cref="CreatedBy"/>), alici kisi, gerekce ve zaman denetim izine yazilir
/// (SYG-KMLK-053). Gerekce olmadan davet olusturulamaz.
/// </para>
/// </remarks>
public sealed class AccountInvitation : Entity, IAuditable
{
    /// <summary>Gerekcenin en fazla uzunlugu.</summary>
    public const int MaxReasonLength = 500;

    private AccountInvitation()
    {
    }

    /// <summary>Davet edilen kisi.</summary>
    public long PersonId { get; private set; }

    /// <summary>Jetonun SHA-256 ozeti (Base64).</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>Gonderim gerekcesi.</summary>
    public string Reason { get; private set; } = string.Empty;

    /// <summary>Gecerlilik sonu.</summary>
    public DateTimeOffset ExpiresAt { get; private set; }

    /// <summary>Kullanildigi an; kullanilmadiysa <c>null</c>.</summary>
    public DateTimeOffset? UsedAt { get; private set; }

    /// <summary>Yeni baglantiyla geri alindigi an; alinmadiysa <c>null</c>.</summary>
    public DateTimeOffset? RevokedAt { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public long? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }

    /// <summary>Yeni davet olusturur.</summary>
    /// <exception cref="ArgumentException">Gerekce bos veya cok uzunsa, ozet bossa.</exception>
    public static AccountInvitation Issue(long personId, string tokenHash, string reason, DateTimeOffset now, TimeSpan lifetime)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(personId);
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenHash);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Gerekce girilmeden davet gonderilemez.", nameof(reason));
        }

        var trimmed = reason.Trim();
        if (trimmed.Length > MaxReasonLength)
        {
            throw new ArgumentException($"Gerekce en fazla {MaxReasonLength} karakter olabilir.", nameof(reason));
        }

        return new AccountInvitation
        {
            PersonId = personId,
            TokenHash = tokenHash,
            Reason = trimmed,
            ExpiresAt = now + lifetime,
        };
    }

    /// <summary>Baglanti kullanilabilir mi (kullanilmamis, geri alinmamis, suresi dolmamis).</summary>
    public bool IsUsable(DateTimeOffset now) => UsedAt is null && RevokedAt is null && now < ExpiresAt;

    /// <summary>Baglanti kullanildi.</summary>
    /// <exception cref="InvalidOperationException">Baglanti kullanilamaz durumdaysa.</exception>
    public void Use(DateTimeOffset now)
    {
        if (!IsUsable(now))
        {
            throw new InvalidOperationException("Kullanilamayan davet kullanilamaz.");
        }

        UsedAt = now;
    }

    /// <summary>Yeni baglanti gonderildi; bu baglanti gecersizlesir.</summary>
    public void Revoke(DateTimeOffset now)
    {
        if (UsedAt is null && RevokedAt is null)
        {
            RevokedAt = now;
        }
    }
}
