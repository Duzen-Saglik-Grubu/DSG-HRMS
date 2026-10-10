using System.Text.RegularExpressions;

namespace Dsg.Hrms.Application.Settings;

/// <summary>
/// T3'un kullandigi parametrelerin katalogu (SYG-KMLK-075).
/// </summary>
/// <remarks>
/// <para>
/// Kimlik, tur, aralik ve varsayilan degerler Y4 taslak katalogundan alinmistir. Y4
/// modulu katalogun tamamini getirdiginde bu liste genisler; kimlikler degismez.
/// </para>
/// <para>
/// <b>Katalogda olmayanlar:</b> <c>PRM-GRN-01</c> (logo dosyasi) bir dosya oldugu icin ayri
/// tabloda tutulur (<see cref="BrandingService"/>, <c>KR-091</c>). <c>PRM-BLD-01</c> (bildirim
/// rolu) rol yonetimiyle (T4) birlikte eklenecektir.
/// </para>
/// <para>
/// Yapilandirma anahtarlari Ingilizcedir (<c>KR-058</c>). Ortam degiskeninde
/// <c>Parameters__SmtpPassword</c> bicimiyle verilir.
/// </para>
/// </remarks>
public static partial class ParameterCatalog
{
    private const string Section = "Parameters";

    // ------------------------------------------------------------------ entegrasyon

    /// <summary>NetGSM kullanici kodu.</summary>
    public static readonly ParameterDefinition NetGsmUserCode = new(
        "PRM-ENT-01", $"{Section}:NetGsmUserCode", ParameterType.Text, "NetGSM kullanici kodu");

    /// <summary>NetGSM API parolasi (sir).</summary>
    public static readonly ParameterDefinition NetGsmPassword = new(
        "PRM-ENT-02", $"{Section}:NetGsmPassword", ParameterType.Secret, "NetGSM API parolasi");

    /// <summary>SMS gonderici basligi.</summary>
    public static readonly ParameterDefinition SmsSenderTitle = new(
        "PRM-ENT-03", $"{Section}:SmsSenderTitle", ParameterType.Text, "SMS gonderici basligi", "DUZEN");

    /// <summary>SMTP sunucusu ve portu (<c>sunucu:port</c>).</summary>
    public static readonly ParameterDefinition SmtpServer = new(
        "PRM-ENT-04", $"{Section}:SmtpServer", ParameterType.Text, "SMTP sunucusu ve portu (sunucu:port)",
        Pattern: HostAndPort(), FormatHint: "sunucu:port biçiminde olmalıdır (örneğin mail.duzen.com.tr:587)");

    /// <summary>SMTP kullanici adi (gonderen hesap).</summary>
    public static readonly ParameterDefinition SmtpUserName = new(
        "PRM-ENT-05", $"{Section}:SmtpUserName", ParameterType.Text, "SMTP kullanici adi (gonderen hesap)",
        "ik.bildirim@duzen.com.tr");

    /// <summary>SMTP parolasi (sir).</summary>
    public static readonly ParameterDefinition SmtpPassword = new(
        "PRM-ENT-06", $"{Section}:SmtpPassword", ParameterType.Secret, "SMTP parolasi");

    /// <summary>LOGO senkronizasyon periyodu (dakika).</summary>
    /// <remarks>
    /// Yapilandirma anahtari, parametre deposundan onceki anahtarla aynidir; mevcut
    /// kurulumlarin ayari gecerli kalir.
    /// </remarks>
    public static readonly ParameterDefinition SyncIntervalMinutes = new(
        "PRM-ENT-07", "PersonnelSync:IntervalMinutes", ParameterType.Number, "LOGO senkronizasyon periyodu (dakika)",
        "15", Min: 5, Max: 120);

    // ------------------------------------------------------------------ kimlik

    /// <summary>Kabul edilen kurumsal e-posta alan adlari.</summary>
    public static readonly ParameterDefinition AcceptedEmailDomains = new(
        "PRM-KML-01", $"{Section}:AcceptedEmailDomains", ParameterType.List, "Kabul edilen kurumsal e-posta alan adlari",
        "duzen.com.tr,zeytinim.com,labpt.com.tr", Pattern: DomainName(), FormatHint: "bir alan adı olmalıdır (örneğin duzen.com.tr)");

    /// <summary>Uyelik ve parola sifirlamada kullanilacak kanallar.</summary>
    public static readonly ParameterDefinition VerificationChannels = new(
        "PRM-KML-02", $"{Section}:VerificationChannels", ParameterType.List, "Uyelik ve parola sifirlamada kullanilacak kanallar",
        "email,sms", AllowedItems: ["email", "sms"]);

    /// <summary>Hesap kilitlenmeden onceki basarisiz giris sayisi.</summary>
    public static readonly ParameterDefinition MaxFailedLogins = Integer(
        "PRM-KML-03", "MaxFailedLogins", "Hesap kilitlenmeden onceki basarisiz giris sayisi", 5, 3, 10);

    /// <summary>Hesap kilit suresi (dakika).</summary>
    public static readonly ParameterDefinition LockoutMinutes = Integer(
        "PRM-KML-04", "LockoutMinutes", "Hesap kilit suresi (dakika)", 15, 5, 1440);

    /// <summary>Parola en az karakter sayisi.</summary>
    public static readonly ParameterDefinition MinPasswordLength = Integer(
        "PRM-KML-05", "MinPasswordLength", "Parola en az karakter sayisi", 6, 6, 32);

    /// <summary>Karmasik parola zorunlulugu.</summary>
    public static readonly ParameterDefinition RequireComplexPassword = Boolean(
        "PRM-KML-06", "RequireComplexPassword", "Karmasik parola zorunlulugu", false);

    /// <summary>Zorunlu periyodik parola degistirme.</summary>
    public static readonly ParameterDefinition RequirePeriodicPasswordChange = Boolean(
        "PRM-KML-07", "RequirePeriodicPasswordChange", "Zorunlu periyodik parola degistirme", false);

    /// <summary>Iki adimli dogrulama.</summary>
    public static readonly ParameterDefinition TwoFactorEnabled = Boolean(
        "PRM-KML-08", "TwoFactorEnabled", "Iki adimli dogrulama (2FA)", false);

    /// <summary>Dogrulama kodu uzunlugu.</summary>
    public static readonly ParameterDefinition VerificationCodeLength = Integer(
        "PRM-KML-09", "VerificationCodeLength", "Dogrulama kodu uzunlugu", 6, 4, 8);

    /// <summary>Dogrulama kodu gecerlilik suresi (dakika).</summary>
    public static readonly ParameterDefinition VerificationCodeLifetimeMinutes = Integer(
        "PRM-KML-10", "VerificationCodeLifetimeMinutes", "Dogrulama kodu gecerlilik suresi (dakika)", 5, 1, 30);

    /// <summary>Erisim jetonu omru (dakika).</summary>
    public static readonly ParameterDefinition AccessTokenLifetimeMinutes = Integer(
        "PRM-KML-11", "AccessTokenLifetimeMinutes", "Erisim jetonu omru (dakika)", 15, 5, 60);

    /// <summary>Oturum toplam suresi (saat).</summary>
    public static readonly ParameterDefinition SessionMaxHours = Integer(
        "PRM-KML-12", "SessionMaxHours", "Oturum toplam suresi (saat)", 8, 1, 24);

    /// <summary>Hareketsizlik suresi (dakika).</summary>
    public static readonly ParameterDefinition IdleTimeoutMinutes = Integer(
        "PRM-KML-13", "IdleTimeoutMinutes", "Hareketsizlik suresi (dakika)", 30, 5, 480);

    /// <summary>Tek aktif oturum kurali.</summary>
    public static readonly ParameterDefinition SingleActiveSession = Boolean(
        "PRM-KML-14", "SingleActiveSession", "Tek aktif oturum kurali", true);

    /// <summary>2FA acilmadan once etkilenecek kullanici uyarisi.</summary>
    public static readonly ParameterDefinition TwoFactorImpactWarning = Boolean(
        "PRM-KML-15", "TwoFactorImpactWarning", "2FA acilmadan once etkilenecek kullanici uyarisi", true);

    /// <summary>Dogrulama kodunda en fazla yanlis deneme sayisi.</summary>
    public static readonly ParameterDefinition MaxVerificationAttempts = Integer(
        "PRM-KML-16", "MaxVerificationAttempts", "Dogrulama kodunda en fazla yanlis deneme sayisi", 3, 3, 10);

    /// <summary>Kod gonderim siniri (kisi basina / 15 dakika).</summary>
    public static readonly ParameterDefinition CodeSendLimit = Integer(
        "PRM-KML-17", "CodeSendLimit", "Kod gonderim siniri (kisi basina / 15 dakika)", 10, 1, 15);

    /// <summary>Uyelik deneme siniri (TCKN basina / saat).</summary>
    public static readonly ParameterDefinition RegistrationLimitPerNationalId = Integer(
        "PRM-KML-18", "RegistrationLimitPerNationalId", "Uyelik deneme siniri (TCKN basina / saat)", 10, 3, 20);

    /// <summary>Uyelik deneme siniri (IP basina / saat).</summary>
    public static readonly ParameterDefinition RegistrationLimitPerIp = Integer(
        "PRM-KML-19", "RegistrationLimitPerIp", "Uyelik deneme siniri (IP basina / saat)", 20, 5, 100);

    /// <summary>Ilk giriste parola degistirme zorunlulugu.</summary>
    public static readonly ParameterDefinition RequirePasswordChangeOnFirstLogin = Boolean(
        "PRM-KML-20", "RequirePasswordChangeOnFirstLogin", "Ilk giriste parola degistirme zorunlulugu", false);

    /// <summary>
    /// Periyodik parola degisiminde parolanin en uzun omru (gun). Yalnizca PRM-KML-07 acikken
    /// uygulanir. Y4 taslak katalogunda yoktu; IK karariyla eklendi (01.10.2026, SYG-KMLK AN-24).
    /// </summary>
    public static readonly ParameterDefinition PasswordMaxAgeDays = Integer(
        "PRM-KML-21", "PasswordMaxAgeDays", "Periyodik parola degisim suresi (gun)", 90, 30, 365);

    // ------------------------------------------------------------------ hesap

    /// <summary>IK'nin personele parola olusturma baglantisi gonderebilmesi.</summary>
    public static readonly ParameterDefinition HrInviteEnabled = Boolean(
        "PRM-HSP-01", "HrInviteEnabled", "IK'nin personele parola olusturma baglantisi gonderebilmesi", true);

    /// <summary>Istihdami biten hesabin otomatik pasiflestirilmesi.</summary>
    public static readonly ParameterDefinition AutoDeactivateOnEmploymentEnd = Boolean(
        "PRM-HSP-02", "AutoDeactivateOnEmploymentEnd", "Istihdami biten hesabin otomatik pasiflestirilmesi", true);

    /// <summary>IK'nin kilitli hesabi acabilmesi.</summary>
    public static readonly ParameterDefinition HrCanUnlockAccounts = Boolean(
        "PRM-HSP-03", "HrCanUnlockAccounts", "IK'nin kilitli hesabi acabilmesi", true);

    /// <summary>Parola olusturma baglantisinin gecerlilik suresi (saat).</summary>
    public static readonly ParameterDefinition InviteLinkLifetimeHours = Integer(
        "PRM-HSP-04", "InviteLinkLifetimeHours", "Parola olusturma baglantisinin gecerlilik suresi (saat)", 3, 1, 72);

    // ------------------------------------------------------------------ gorunum ve bildirim

    /// <summary>Destek iletisim bilgisi (uyelik ve giris ekranlari).</summary>
    public static readonly ParameterDefinition SupportContact = new(
        "PRM-GRN-04", $"{Section}:SupportContact", ParameterType.Text, "Destek iletisim bilgisi (uyelik ve giris ekranlari)",
        "Bilgi İşlem");

    /// <summary>Bildirim istisnasinin islemsel iletileri kapsamamasi.</summary>
    public static readonly ParameterDefinition TransactionalMessagesBypassExemption = Boolean(
        "PRM-BLD-03", "TransactionalMessagesBypassExemption", "Bildirim istisnasinin islemsel iletileri kapsamamasi", true);

    /// <summary>Katalogdaki tum parametreler, kimlige gore sirali.</summary>
    public static IReadOnlyList<ParameterDefinition> All { get; } =
    [
        .. new[]
        {
            NetGsmUserCode, NetGsmPassword, SmsSenderTitle, SmtpServer, SmtpUserName, SmtpPassword, SyncIntervalMinutes,
            AcceptedEmailDomains, VerificationChannels, MaxFailedLogins, LockoutMinutes, MinPasswordLength,
            RequireComplexPassword, RequirePeriodicPasswordChange, TwoFactorEnabled, VerificationCodeLength,
            VerificationCodeLifetimeMinutes, AccessTokenLifetimeMinutes, SessionMaxHours, IdleTimeoutMinutes,
            SingleActiveSession, TwoFactorImpactWarning, MaxVerificationAttempts, CodeSendLimit,
            RegistrationLimitPerNationalId, RegistrationLimitPerIp, RequirePasswordChangeOnFirstLogin, PasswordMaxAgeDays,
            HrInviteEnabled, AutoDeactivateOnEmploymentEnd, HrCanUnlockAccounts, InviteLinkLifetimeHours,
            SupportContact, TransactionalMessagesBypassExemption,
        }.OrderBy(p => p.Key, StringComparer.Ordinal),
    ];

    /// <summary>Kimlige gore tanimi bulur; katalogda yoksa <c>null</c>.</summary>
    public static ParameterDefinition? Find(string key) =>
        All.FirstOrDefault(p => string.Equals(p.Key, key, StringComparison.Ordinal));

    private static ParameterDefinition Integer(string key, string name, string description, int @default, int min, int max) =>
        new(key, $"{Section}:{name}", ParameterType.Number, description,
            @default.ToString(System.Globalization.CultureInfo.InvariantCulture), min, max);

    private static ParameterDefinition Boolean(string key, string name, string description, bool @default) =>
        new(key, $"{Section}:{name}", ParameterType.Toggle, description, @default ? "true" : "false");

    [GeneratedRegex(@"^(?=.{1,253}$)([a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z]{2,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex DomainName();

    [GeneratedRegex(@"^([a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?\.)*[a-z0-9]([a-z0-9-]{0,61}[a-z0-9])?:[0-9]{1,5}$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex HostAndPort();
}
