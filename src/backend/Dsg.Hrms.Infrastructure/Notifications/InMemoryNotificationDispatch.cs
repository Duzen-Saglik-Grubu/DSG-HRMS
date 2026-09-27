using System.Threading.Channels;
using Dsg.Hrms.Application.Notifications;

namespace Dsg.Hrms.Infrastructure.Notifications;

/// <summary>
/// Bellek ici bildirim kuyrugu (SYG-KMLK-015).
/// </summary>
/// <remarks>
/// Kodlu ileti veritabanina yazilmaz (SYG-KMLK-025, #85). Uygulama yeniden baslarsa
/// kuyruktaki iletiler kaybolur; kodlar zaten kisa omurludur ve kullanici "tekrar gonder"
/// ile yeni kod ister.
/// </remarks>
public sealed class InMemoryNotificationDispatch : INotificationDispatch
{
    private readonly Channel<OutboundMessage> _channel;

    /// <summary>Yeni ornek olusturur.</summary>
    public InMemoryNotificationDispatch(NotificationOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _channel = Channel.CreateBounded<OutboundMessage>(new BoundedChannelOptions(options.QueueCapacity)
        {
            // Wait kipinde TryWrite kuyruk doluyken false doner. DropWrite kipi ise
            // iletiyi sessizce atip true dondururdu; kaybolan kod fark edilmezdi.
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
        });
    }

    /// <summary>Dagiticinin okudugu uc.</summary>
    public ChannelReader<OutboundMessage> Reader => _channel.Reader;

    /// <inheritdoc />
    public bool TryEnqueue(OutboundMessage message)
    {
        ArgumentNullException.ThrowIfNull(message);
        return _channel.Writer.TryWrite(message);
    }
}
