namespace Dsg.Hrms.Domain.Audit;

/// <summary>
/// Kimlik olayi kaydi: giris, kod, parola, oturum ve davet olaylari (SYG-KMLK-060).
/// </summary>
/// <remarks>
/// <para>
/// Degisiklik kaydi (<see cref="ChangeLogEntry"/>) bir verinin NASIL degistigini tutar;
/// basarisiz bir giris, gonderilen bir kod veya kapanan bir oturum hicbir is verisini
/// degistirmez ama bir guvenlik incelemesinin ilk sorularidir. Bu kayit o sorulari cevaplar.
/// </para>
/// <para>
/// <b>Kisisel veri tasimaz.</b> Kim oldugu kimlikle (<see cref="UserAccountId"/>,
/// <see cref="PersonId"/>) belirtilir; e-posta, telefon, TCKN, kod veya parola yazilmaz.
/// <see cref="Detail"/> yalnizca makine tarafindan okunur kisa bir koddur.
/// </para>
/// <para>
/// <b>Degistirilemez</b> (KR-060): uygulama yalnizca ekleme yapar, veritabani
/// tetikleyicileri guncelleme ve silmeyi reddeder.
/// </para>
/// </remarks>
public sealed class SecurityEventEntry
{
    /// <summary>Birincil anahtar.</summary>
    public long Id { get; private set; }

    /// <summary>Olayin gerceklestigi an (UTC).</summary>
    public DateTimeOffset OccurredAt { get; init; }

    /// <summary>Olay turu.</summary>
    public SecurityEventType EventType { get; init; }

    /// <summary>Olayin konusu olan hesap; bilinmiyorsa <c>null</c> (ornegin var olmayan e-postayla giris).</summary>
    public long? UserAccountId { get; init; }

    /// <summary>Olayin konusu olan kisi; hesap henuz yoksa veya kisi bilinmiyorsa <c>null</c>.</summary>
    public long? PersonId { get; init; }

    /// <summary>
    /// Islemi yapan oturumun hesabi. Kullanici kendisi icin yaptiginda konuyla aynidir; IK
    /// davet gonderdiginde IK kullanicisidir. Oturum yoksa <c>null</c>.
    /// </summary>
    public long? ActorUserAccountId { get; init; }

    /// <summary>
    /// Kisa ayrinti kodu (ornegin <c>password-reset</c>, <c>idle-timeout</c>, <c>sms</c>).
    /// Kisisel veri ICERMEZ.
    /// </summary>
    public string? Detail { get; init; }

    /// <summary>Uygulama gunlugu ile iliskilendirme kimligi.</summary>
    public string? TraceId { get; init; }

    /// <summary>Istegin geldigi IP adresi.</summary>
    public string? IpAddress { get; init; }
}

/// <summary>Kimlik olayi turu (SYG-KMLK-060).</summary>
public enum SecurityEventType
{
    /// <summary>Uyelik veya parola sifirlama basladi (ayrinti: amac ve eslesme).</summary>
    RegistrationStarted = 1,

    /// <summary>Dogrulama kodu gonderime alindi (ayrinti: amac ve kanal).</summary>
    VerificationCodeSent = 2,

    /// <summary>Dogrulama kodu dogrulandi.</summary>
    VerificationSucceeded = 3,

    /// <summary>Dogrulama kodu reddedildi (ayrinti: amac ve sonuc).</summary>
    VerificationFailed = 4,

    /// <summary>Uyelik sonunda hesap olusturuldu.</summary>
    AccountCreated = 5,

    /// <summary>Giris basarili, oturum acildi (ayrinti: tek veya iki adimli).</summary>
    SignInSucceeded = 6,

    /// <summary>Giris reddedildi (ayrinti: hatali bilgi, kilitli, hesap kapali).</summary>
    SignInFailed = 7,

    /// <summary>Hatali deneme siniri asildi; e-posta kilitlendi.</summary>
    AccountLocked = 8,

    /// <summary>Parola oturum icinde degistirildi.</summary>
    PasswordChanged = 9,

    /// <summary>Oturum icinde parola degisikligi mevcut parola hatali oldugu icin reddedildi.</summary>
    PasswordChangeFailed = 10,

    /// <summary>Parola dogrulanmis kimlikle sifirlandi.</summary>
    PasswordReset = 11,

    /// <summary>Oturum kapandi (ayrinti: kapanma nedeni).</summary>
    SessionEnded = 12,

    /// <summary>Kullanilmis yenileme jetonu tekrar sunuldu; hesabin tum oturumlari kapatildi.</summary>
    TokenReuseDetected = 13,

    /// <summary>IK parola olusturma baglantisi gonderdi.</summary>
    InvitationSent = 14,

    /// <summary>Parola olusturma baglantisi kullanildi (ayrinti: hesap olusturuldu veya parola yenilendi).</summary>
    InvitationAccepted = 15,

    /// <summary>Kullanici kendi hesabinda iki adimli dogrulamayi acti (SYG-KMLK-080).</summary>
    TwoFactorEnabled = 16,

    /// <summary>Kullanici kendi hesabinda iki adimli dogrulamayi kapatti (SYG-KMLK-080).</summary>
    TwoFactorDisabled = 17,

    /// <summary>
    /// Iki adimli dogrulama tercihinin degisikligi mevcut parola hatali oldugu icin reddedildi
    /// (SYG-KMLK-080; ayrinti: acma veya kapatma).
    /// </summary>
    TwoFactorChangeFailed = 18,
}
