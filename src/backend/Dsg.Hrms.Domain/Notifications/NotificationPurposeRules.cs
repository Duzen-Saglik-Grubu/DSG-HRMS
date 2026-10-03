namespace Dsg.Hrms.Domain.Notifications;

/// <summary>Ileti amaclarinin kurallari.</summary>
public static class NotificationPurposeRules
{
    /// <summary>
    /// Ileti islemsel/guvenlik iletisi mi (SYG-KMLK-062, KR-056).
    /// </summary>
    /// <remarks>
    /// Islemsel ileti, kisinin kendi baslattigi bir islemin parcasidir: dogrulama kodu, parola
    /// sifirlama, davet baglantisi. Bildirim istisnasi kapsamindaki kisi bunlari alamasaydi
    /// sisteme hic giremezdi; bu yuzden istisnadan muaftirlar (PRM-BLD-03). Yeni bir amac
    /// eklendiginde burada siniflandirilmak ZORUNDADIR: siniflandirilmamis amac derlemeyi
    /// bozmaz ama birim testi duser.
    /// </remarks>
    public static bool IsTransactional(this NotificationPurpose purpose) => purpose switch
    {
        NotificationPurpose.RegistrationCode => true,
        NotificationPurpose.PasswordResetCode => true,
        NotificationPurpose.TwoFactorCode => true,
        NotificationPurpose.Invitation => true,
        _ => throw new ArgumentOutOfRangeException(nameof(purpose), purpose, "Ileti amaci siniflandirilmamis (SYG-KMLK-062)."),
    };
}
