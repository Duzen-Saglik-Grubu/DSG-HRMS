using Dsg.Hrms.Domain.Notifications;

namespace Dsg.Hrms.Application.Notifications;

/// <summary>
/// Bildirim istisnasi: kisiye belirli bir kanaldan ileti gonderilmemesi (KR-056).
/// </summary>
/// <remarks>
/// <para>
/// Istisnanin tanimi T5 Kullanici Yonetimi'nde, uygulamasi Y1 Bildirim Merkezi'ndedir (KR-056).
/// T3 yalnizca kancayi kurar (SYG-KMLK-062): dagitici her gonderimden once bu soyutlamaya
/// sorar. Uygulamasi kayitli degilse istisna yoktur.
/// </para>
/// <para>
/// Islemsel iletiler (<see cref="NotificationPurposeRules.IsTransactional"/>) PRM-BLD-03
/// acikken bu soruya hic tabi tutulmaz.
/// </para>
/// </remarks>
public interface INotificationExemptions
{
    /// <summary>Kisi verilen kanalda verilen anda istisna kapsaminda mi.</summary>
    /// <param name="personId">Kisi; bilinmiyorsa <c>null</c>.</param>
    /// <param name="channel">Kanal.</param>
    /// <param name="at">An.</param>
    /// <param name="cancellationToken">Iptal.</param>
    Task<bool> IsExemptAsync(long? personId, NotificationChannel channel, DateTimeOffset at, CancellationToken cancellationToken);
}
