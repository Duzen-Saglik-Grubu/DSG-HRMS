namespace Dsg.Hrms.Application.Notifications;

/// <summary>
/// Iletileri arka planda gonderilmek uzere kuyruga alir (SYG-KMLK-015, ADR-0012 §5).
/// </summary>
/// <remarks>
/// Gonderim istegin icinde yapilmaz: SMTP veya NetGSM'in yanit suresi kullaniciya
/// yansimaz ve bir gonderim hatasi is isleminin hatasi sayilmaz.
/// </remarks>
public interface INotificationDispatch
{
    /// <summary>Iletiyi kuyruga alir. Kuyruk doluysa <c>false</c> doner.</summary>
    bool TryEnqueue(OutboundMessage message);
}
