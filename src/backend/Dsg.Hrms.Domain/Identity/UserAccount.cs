using Dsg.Hrms.Domain.Common;

namespace Dsg.Hrms.Domain.Identity;

/// <summary>
/// Kullanici hesabi (ADR-0006, <c>KR-014</c>). Kisiye baglanir, istihdama degil.
/// </summary>
/// <remarks>
/// <para>
/// Bir kisiye en fazla bir hesap baglanir; bu veritabaninda tekillik kisitiyla
/// zorlanir (SYG-KMLK-021). Kisinin yeni bir istihdami basladiginda yeni hesap
/// olusmaz, ayni hesap yeniden aktiflesir (SYG-KMLK-056).
/// </para>
/// <para>
/// Durum degisiklikleri denetim izine ozellik degisikligi olarak duser: onceki ve
/// sonraki <see cref="Status"/>, <see cref="StatusReason"/> ve <see cref="StatusNote"/>
/// kaydedilir (SYG-KMLK-058).
/// </para>
/// </remarks>
public sealed class UserAccount : Entity, IAuditable
{
    private UserAccount()
    {
    }

    /// <summary>Hesabin bagli oldugu kisinin kimligi. Tekildir.</summary>
    public long PersonId { get; private set; }

    /// <summary>Hesap durumu.</summary>
    public AccountStatus Status { get; private set; }

    /// <summary>Son durum degisikliginin nedeni. Hic degismediyse <c>null</c>.</summary>
    public AccountStatusReason? StatusReason { get; private set; }

    /// <summary>
    /// Elle yapilan durum degisikliginin gerekcesi (SYG-KMLK-057). Otomatik
    /// degisikliklerde <c>null</c>.
    /// </summary>
    public string? StatusNote { get; private set; }

    /// <summary>
    /// Guvenlik damgasi. Hesap pasiflestiginde yenilenir; oturum ve yenileme jetonlari
    /// bu damgaya baglanir ve damga degisince gecersiz olur (SYG-KMLK-054).
    /// </summary>
    public Guid SecurityStamp { get; private set; } = Guid.NewGuid();

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public long? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }

    /// <summary>Kisi icin aktif bir hesap olusturur.</summary>
    public static UserAccount Create(long personId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(personId);
        return new UserAccount { PersonId = personId, Status = AccountStatus.Active };
    }

    /// <summary>
    /// Kisinin tum istihdamlari bittigi icin hesabi pasife alir (SYG-KMLK-054).
    /// Degisiklik olduysa <c>true</c> doner.
    /// </summary>
    /// <remarks>
    /// Elle pasife alinmis hesapta neden "istihdam bitti"ye doner: kisi ayrildiginda
    /// elle verilen karar yerini ayriliga birakir. Boylece kisi yeniden ise girdiginde
    /// hesap REQ-KMLK-035 geregi aktiflesir (<see cref="ReactivateForNewEmployment"/>).
    /// Hesap zaten istihdam bitimiyle pasifse hicbir sey yapmaz.
    /// </remarks>
    public bool DeactivateForEmploymentEnd()
    {
        if (Status == AccountStatus.Passive)
        {
            if (StatusReason == AccountStatusReason.EmploymentEnded)
            {
                return false;
            }

            StatusReason = AccountStatusReason.EmploymentEnded;
            StatusNote = null;
            return true;
        }

        SetPassive(AccountStatusReason.EmploymentEnded, note: null);
        return true;
    }

    /// <summary>
    /// Kisinin yeniden aktif istihdami oldugu icin hesabi aktiflestirir (SYG-KMLK-056).
    /// Degisiklik olduysa <c>true</c> doner.
    /// </summary>
    /// <remarks>
    /// Yalnizca <b>istihdam bitimiyle</b> pasiflesmis hesap otomatik aktiflesir.
    /// Istihdami SURERKEN elle pasife alinan hesap pasif kalir: IK'nin gerekceli karari
    /// (SYG-KMLK-057) 15 dakikada bir calisan senkronizasyonla sessizce geri alinsaydi
    /// elle pasife alma anlamsiz olurdu (<c>KR-080</c>). Boyle bir hesap ancak elle
    /// aktiflesir.
    /// </remarks>
    public bool ReactivateForNewEmployment()
    {
        if (Status != AccountStatus.Passive || StatusReason != AccountStatusReason.EmploymentEnded)
        {
            return false;
        }

        Status = AccountStatus.Active;
        StatusReason = AccountStatusReason.NewEmployment;
        StatusNote = null;
        return true;
    }

    /// <summary>Hesabi gerekceyle elle pasife alir (SYG-KMLK-057).</summary>
    /// <exception cref="ArgumentException">Gerekce bossa.</exception>
    /// <exception cref="InvalidOperationException">Hesap zaten pasifse.</exception>
    public void DeactivateManually(string note)
    {
        var trimmed = RequireNote(note);

        if (Status == AccountStatus.Passive)
        {
            throw new InvalidOperationException("Hesap zaten pasif.");
        }

        SetPassive(AccountStatusReason.Manual, trimmed);
    }

    /// <summary>Hesabi gerekceyle elle aktiflestirir (SYG-KMLK-057).</summary>
    /// <param name="note">Gerekce.</param>
    /// <param name="hasActiveEmployment">Kisinin en az bir aktif istihdami var mi.</param>
    /// <exception cref="ArgumentException">Gerekce bossa.</exception>
    /// <exception cref="InvalidOperationException">
    /// Hesap zaten aktifse veya kisinin aktif istihdami yoksa.
    /// </exception>
    /// <remarks>
    /// Aktif istihdami olmayan kisinin hesabi aktiflestirilemez: bir sonraki
    /// senkronizasyon hesabi yeniden pasife alirdi ve arada gecen surede ayrilmis bir
    /// kisi giris yapabilirdi (<c>KR-015</c>).
    /// </remarks>
    public void ActivateManually(string note, bool hasActiveEmployment)
    {
        var trimmed = RequireNote(note);

        if (Status == AccountStatus.Active)
        {
            throw new InvalidOperationException("Hesap zaten aktif.");
        }

        if (!hasActiveEmployment)
        {
            throw new InvalidOperationException("Aktif istihdami olmayan kisinin hesabi aktiflestirilemez.");
        }

        Status = AccountStatus.Active;
        StatusReason = AccountStatusReason.Manual;
        StatusNote = trimmed;
    }

    private void SetPassive(AccountStatusReason reason, string? note)
    {
        Status = AccountStatus.Passive;
        StatusReason = reason;
        StatusNote = note;

        // Acik oturumlar ve yenileme jetonlari bu damgaya bagli oldugu icin hepsi
        // gecersizlesir (SYG-KMLK-054).
        SecurityStamp = Guid.NewGuid();
    }

    private static string RequireNote(string note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            throw new ArgumentException("Gerekce girilmeden hesap durumu degistirilemez.", nameof(note));
        }

        return note.Trim();
    }
}

/// <summary>Hesap durumu (SYG-KMLK §3.1).</summary>
public enum AccountStatus
{
    /// <summary>Giris yapilabilir.</summary>
    Active = 1,

    /// <summary>Giris yapilamaz (SYG-KMLK-055).</summary>
    Passive = 2,
}

/// <summary>Hesap durumunun son degisme nedeni.</summary>
public enum AccountStatusReason
{
    /// <summary>Kisinin tum istihdamlari bitti (senkronizasyon).</summary>
    EmploymentEnded = 1,

    /// <summary>Kisinin yeniden aktif istihdami basladi (senkronizasyon).</summary>
    NewEmployment = 2,

    /// <summary>Yetkili kullanici elle degistirdi.</summary>
    Manual = 3,
}
