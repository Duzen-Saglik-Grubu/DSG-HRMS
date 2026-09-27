using Dsg.Hrms.Application.Common.Security;
using Dsg.Hrms.Domain.Notifications;

namespace Dsg.Hrms.Application.Notifications;

/// <summary>
/// Gonderilmeyi bekleyen ileti.
/// </summary>
/// <remarks>
/// <para>
/// Yalnizca BELLEKTE yasar; veritabanina yazilmaz. Govde dogrulama kodu tasir ve kod
/// yalnizca ozet olarak saklanabilir (SYG-KMLK-025). Uygulama yeniden baslarsa kuyruktaki
/// ileti kaybolur; kod zaten kisa omurludur ve kullanici yeniden isteyebilir (#85).
/// </para>
/// <para>
/// Bu nesne HICBIR ZAMAN gunluge yazilmaz. Yazilsa bile govde <see cref="SecretAttribute"/>,
/// alici <see cref="PersonalDataAttribute"/> ile isaretlidir.
/// </para>
/// </remarks>
/// <param name="Channel">Kanal.</param>
/// <param name="Purpose">Amac.</param>
/// <param name="PersonId">Alici kisi; kisiye bagli degilse <c>null</c>.</param>
/// <param name="Recipient">E-posta adresi veya <c>5XXXXXXXXX</c> bicimli cep telefonu.</param>
/// <param name="Subject">E-posta konusu; SMS'te <c>null</c>.</param>
/// <param name="MessageBody">Duz metin govde.</param>
/// <param name="QueuedAt">Kuyruga alinma ani (UTC).</param>
public sealed record OutboundMessage(
    NotificationChannel Channel,
    NotificationPurpose Purpose,
    long? PersonId,
    [property: PersonalData(PersonalDataKind.Unspecified)] string Recipient,
    string? Subject,
    [property: Secret] string MessageBody,
    DateTimeOffset QueuedAt)
{
    /// <summary>Alicinin maskeli hali; gunluk ve gonderim kaydi icin.</summary>
    public string MaskedRecipient =>
        (Channel == NotificationChannel.Email ? Mask.Email(Recipient) : Mask.Phone(Recipient)) ?? string.Empty;

    /// <inheritdoc />
    /// <remarks>
    /// Kayit turunun varsayilan <c>ToString</c> ciktisi tum alanlari yazar; bir gunluk
    /// satirinda veya hata iletisinde yanlislikla kullanilirsa kod sizardi (SYG-KMLK-026).
    /// </remarks>
    public override string ToString() => $"{Channel} {Purpose} -> {MaskedRecipient}";
}
