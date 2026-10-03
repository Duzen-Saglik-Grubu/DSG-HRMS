using Dsg.Hrms.Domain.Audit;

namespace Dsg.Hrms.Application.Common.Abstractions;

/// <summary>
/// Kimlik olaylarini denetim izine yazar (SYG-KMLK-060).
/// </summary>
/// <remarks>
/// <para>
/// Olay, isin kendisiyle <b>ayni islemde</b> kaydedilir: <see cref="Record"/> kaydi
/// veritabani baglamina ekler, cagiranin bir sonraki kaydetmesiyle yazilir. Is geri alinirsa
/// olay da yazilmaz; boylece iz, gerceklesmemis bir olayi gostermez. Cagiran her olayi
/// kaydetme cagrisindan ONCE ekler.
/// </para>
/// <para>
/// Zaman, IP ve izleme kimligi uygulama tarafindan doldurulur. Oturum sonlari ayrica, oturum
/// kapandigi an tek noktada yazilir; cagiranin eklemesi gerekmez.
/// </para>
/// </remarks>
public interface ISecurityEventLog
{
    /// <summary>Olayi ekler.</summary>
    /// <param name="type">Olay turu.</param>
    /// <param name="userAccountId">Konu olan hesap; bilinmiyorsa <c>null</c>.</param>
    /// <param name="personId">Konu olan kisi; bilinmiyorsa <c>null</c>.</param>
    /// <param name="detail">Kisa ayrinti kodu. Kisisel veri ICERMEZ.</param>
    void Record(SecurityEventType type, long? userAccountId, long? personId, string? detail = null);
}
