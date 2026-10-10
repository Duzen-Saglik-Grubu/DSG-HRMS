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

    /// <summary>
    /// Parola ozeti (PBKDF2-HMAC-SHA512, SYG-KMLK-049). Parola belirlenmemisse <c>null</c>.
    /// Ad bilincli secildi: denetim izinde ad tabanli kural bu alani HIC yazmaz.
    /// </summary>
    public string? PasswordHash { get; private set; }

    /// <summary>Parolanin son belirlendigi an (UTC).</summary>
    public DateTimeOffset? PasswordChangedAt { get; private set; }

    /// <summary>
    /// Hatali giris siniri asildiginda kilidin kalkacagi an (SYG-KMLK-033). Kilitli degilse
    /// <c>null</c>. Kilit girilen e-postaya gore tutulur; bu alan yalnizca kilitlenme ve
    /// kilit kalkmasinin denetim izine dusmesi icindir (SYG-KMLK-058).
    /// </summary>
    public DateTimeOffset? LockedUntil { get; private set; }

    /// <summary>Ilk basarili girisin ani (SYG-KMLK-050). Hic giris yapilmadiysa <c>null</c>.</summary>
    public DateTimeOffset? FirstSignedInAt { get; private set; }

    /// <summary>
    /// Ilk giriste parola degisimi bekleniyor (SYG-KMLK-050). Ilk giris, parametre acikken
    /// yapildiysa isaretlenir; parola degisince kalkar. Kullanici degistirmeden cikip yeniden
    /// girerse zorunluluk surer.
    /// </summary>
    public bool FirstPasswordChangePending { get; private set; }

    /// <summary>
    /// Kullanicinin kendi iki adimli dogrulama tercihi (SYG-KMLK-080). Varsayilan kapali.
    /// Giriste kod yalnizca sistem parametresi (PRM-KML-08) ve bu tercih birlikte acikken
    /// istenir.
    /// </summary>
    public bool TwoFactorEnabled { get; private set; }

    /// <summary>
    /// IK'nin iki adimli dogrulamayi kapatma gerekcesi (SYG-KMLK-081, R-27). Kullanici
    /// tercihi yeniden actiginda <c>null</c> olur; hic kapatilmadiysa <c>null</c>.
    /// </summary>
    public string? TwoFactorResetNote { get; private set; }

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

    /// <summary>Uyelik sonunda parolasi belirlenmis aktif hesap olusturur (SYG-KMLK-013).</summary>
    public static UserAccount Register(long personId, string passwordHash, DateTimeOffset now)
    {
        var account = Create(personId);
        account.SetPassword(passwordHash, now);
        return account;
    }

    /// <summary>
    /// Parolayi degistirir ve guvenlik damgasini yeniler: mevcut oturumlar gecersizlesir
    /// (SYG-KMLK-048).
    /// </summary>
    public void SetPassword(string passwordHash, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        PasswordHash = passwordHash;
        PasswordChangedAt = now;
        FirstPasswordChangePending = false;
        SecurityStamp = Guid.NewGuid();
    }

    /// <summary>
    /// Basarili girisi kaydeder. Yalnizca ILK giris iz birakir (SYG-KMLK-050): her giriste
    /// hesap satiri degisseydi denetim izi her giriste buyurdu.
    /// </summary>
    /// <param name="now">Giris ani.</param>
    /// <param name="requirePasswordChange">Ilk giriste parola degisimi zorunlu mu (PRM-KML-20).</param>
    public void RecordSignIn(DateTimeOffset now, bool requirePasswordChange)
    {
        if (FirstSignedInAt is not null)
        {
            return;
        }

        FirstSignedInAt = now;
        FirstPasswordChangePending = requirePasswordChange;
    }

    /// <summary>
    /// Bu giriste parola degisimi gerekiyor mu; gerekiyorsa nedeni (SYG-KMLK-046, 050).
    /// Girisi kaydettikten (<see cref="RecordSignIn"/>) SONRA cagrilir.
    /// </summary>
    /// <param name="firstSignInRule">Ilk giriste degisim kurali acik mi (PRM-KML-20).</param>
    /// <param name="maxPasswordAge">Periyodik degisim aciksa parolanin en uzun omru (PRM-KML-07, 21); kapaliysa <c>null</c>.</param>
    /// <param name="now">Simdiki an.</param>
    /// <remarks>
    /// Parametre sonradan kapatilirsa bekleyen ilk giris degisimi de istenmez. Parolanin
    /// belirlenme ani bilinmiyorsa suresi dolmus sayilir.
    /// </remarks>
    public PasswordChangeReason? RequiredPasswordChange(bool firstSignInRule, TimeSpan? maxPasswordAge, DateTimeOffset now)
    {
        if (firstSignInRule && FirstPasswordChangePending)
        {
            return PasswordChangeReason.FirstSignIn;
        }

        if (maxPasswordAge is { } maxAge && (PasswordChangedAt is null || now - PasswordChangedAt.Value >= maxAge))
        {
            return PasswordChangeReason.Expired;
        }

        return null;
    }

    /// <summary>
    /// Iki adimli dogrulamayi kullanicinin kendi tercihiyle acar (SYG-KMLK-080). Degisiklik
    /// olduysa <c>true</c> doner.
    /// </summary>
    /// <remarks>
    /// Guvenlik damgasi YENILENMEZ: tercih bir sonraki giristen itibaren gecerlidir; acik
    /// oturumlar kapanmaz.
    /// </remarks>
    public bool EnableTwoFactor()
    {
        if (TwoFactorEnabled)
        {
            return false;
        }

        TwoFactorEnabled = true;

        // IK'nin onceki kapatma gerekcesi artik gecerli durumu anlatmaz (SYG-KMLK-081).
        TwoFactorResetNote = null;
        return true;
    }

    /// <summary>
    /// Iki adimli dogrulamayi kullanicinin kendi tercihiyle kapatir (SYG-KMLK-080).
    /// Degisiklik olduysa <c>true</c> doner. Guvenlik damgasi yenilenmez.
    /// </summary>
    public bool DisableTwoFactor()
    {
        if (!TwoFactorEnabled)
        {
            return false;
        }

        TwoFactorEnabled = false;
        return true;
    }

    /// <summary>
    /// Iki adimli dogrulamayi IK gerekcesiyle kapatir (SYG-KMLK-081, R-27): e-posta ve
    /// telefon erisimini kaybeden kisi yeniden yalnizca parolasiyla giris yapabilir.
    /// </summary>
    /// <param name="reason">Gerekce.</param>
    /// <exception cref="ArgumentException">Gerekce bossa.</exception>
    /// <exception cref="InvalidOperationException">Tercih zaten kapaliysa.</exception>
    /// <remarks>
    /// Guvenlik damgasi yenilenmez: kapatma kisinin yetkisini daraltmaz, acik oturumlar
    /// surer. Gerekce denetim izine ozellik degisikligi olarak duser (SYG-KMLK-058).
    /// </remarks>
    public void ResetTwoFactor(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Gerekce girilmeden iki adimli dogrulama kapatilamaz.", nameof(reason));
        }

        if (!TwoFactorEnabled)
        {
            throw new InvalidOperationException("Iki adimli dogrulama zaten kapali.");
        }

        TwoFactorEnabled = false;
        TwoFactorResetNote = reason.Trim();
    }

    /// <summary>Kilitlenmeyi kaydeder (SYG-KMLK-058).</summary>
    public void RecordLockout(DateTimeOffset until) => LockedUntil = until;

    /// <summary>Kilit kalktiysa kaydi temizler; degisiklik olduysa <c>true</c>.</summary>
    public bool ClearLockout()
    {
        if (LockedUntil is null)
        {
            return false;
        }

        LockedUntil = null;
        return true;
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

/// <summary>Parola degisiminin zorunlu olma nedeni (SYG-KMLK-046, 050).</summary>
public enum PasswordChangeReason
{
    /// <summary>Ilk giris (PRM-KML-20).</summary>
    FirstSignIn = 1,

    /// <summary>Parolanin suresi doldu (PRM-KML-07, PRM-KML-21).</summary>
    Expired = 2,
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
