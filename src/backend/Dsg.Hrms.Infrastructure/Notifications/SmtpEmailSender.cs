using System.Globalization;
using System.Net.Sockets;
using Dsg.Hrms.Application.Notifications;
using Dsg.Hrms.Application.Settings;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace Dsg.Hrms.Infrastructure.Notifications;

/// <summary>
/// Kurum SMTP sunucusu uzerinden e-posta (ADR-0012 §3, SYG-KMLK-030).
/// </summary>
/// <remarks>
/// <para>
/// Sunucu, kullanici ve parola parametre deposundan her gonderimde okunur
/// (<c>PRM-ENT-04…06</c>). Gonderen adres SMTP kullanici adidir; gorunen ad
/// <see cref="SenderDisplayName"/>'dir.
/// </para>
/// <para>
/// Baglanti, sunucu destekliyorsa STARTTLS ile sifrelenir; 465 portunda dogrudan TLS
/// kullanilir. Sertifika dogrulamasi KAPATILMAZ.
/// </para>
/// </remarks>
public sealed class SmtpEmailSender : IEmailSender
{
    /// <summary>Baglanti ve komut zaman asimi (ADR-0012 §5).</summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gonderenin gorunen adi (ADR-0012 §3). Personel kodu beklerken gondereni hemen
    /// tanimali: "IK" iletinin nereden geldigini, "Sistem" otomatik gonderildigini soyler.
    /// </summary>
    public const string SenderDisplayName = "Düzen İK Sistemi";

    private readonly ISystemParameters _parameters;

    /// <summary>Yeni ornek olusturur.</summary>
    public SmtpEmailSender(ISystemParameters parameters)
    {
        _parameters = parameters;
    }

    /// <inheritdoc />
    public async Task<SendResult> SendAsync(string address, string subject, string messageBody, CancellationToken cancellationToken)
    {
        var settings = await ReadSettingsAsync(cancellationToken).ConfigureAwait(false);
        if (settings is null)
        {
            return SendResult.Misconfigured("not-configured");
        }

        using var message = new MimeMessage();
        message.From.Add(new MailboxAddress(SenderDisplayName, settings.Value.UserName));
        message.To.Add(MailboxAddress.Parse(address));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = messageBody };

        return await ExecuteAsync(settings.Value, async client =>
        {
            var response = await client.SendAsync(message, cancellationToken).ConfigureAwait(false);
            return SendResult.Sent("250", ExtractQueueId(response));
        }, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sunucuya baglanip kimlik dogrular ve ILETI GONDERMEDEN ayrilir (kanal sagligi).
    /// </summary>
    public async Task<SendResult> CheckAsync(CancellationToken cancellationToken)
    {
        var settings = await ReadSettingsAsync(cancellationToken).ConfigureAwait(false);
        if (settings is null)
        {
            return SendResult.Misconfigured("not-configured");
        }

        return await ExecuteAsync(settings.Value, _ => Task.FromResult(SendResult.Sent("auth-ok")), cancellationToken).ConfigureAwait(false);
    }

    private static async Task<SendResult> ExecuteAsync(
        SmtpSettings settings,
        Func<SmtpClient, Task<SendResult>> action,
        CancellationToken cancellationToken)
    {
        using var client = new SmtpClient { Timeout = (int)Timeout.TotalMilliseconds };

        try
        {
            var security = settings.Port == 465 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;
            await client.ConnectAsync(settings.Host, settings.Port, security, cancellationToken).ConfigureAwait(false);
            await client.AuthenticateAsync(settings.UserName, settings.Password, cancellationToken).ConfigureAwait(false);

            var result = await action(client).ConfigureAwait(false);

            await client.DisconnectAsync(quit: true, cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (AuthenticationException)
        {
            return SendResult.Misconfigured("smtp-auth");
        }
        catch (SslHandshakeException)
        {
            return SendResult.Misconfigured("smtp-tls");
        }
        catch (SmtpCommandException ex)
        {
            // 4xx gecici (sunucu mesgul, kota), 5xx kalici (alici reddedildi).
            var code = string.Create(CultureInfo.InvariantCulture, $"smtp-{(int)ex.StatusCode}");
            return (int)ex.StatusCode >= 500 ? SendResult.Permanent(code) : SendResult.Transient(code);
        }
        catch (SmtpProtocolException)
        {
            return SendResult.Transient("smtp-protocol");
        }
        catch (Exception ex) when (ex is SocketException or IOException or TimeoutException or ServiceNotConnectedException)
        {
            return SendResult.Transient("network");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return SendResult.Transient("timeout");
        }
    }

    /// <summary>Sunucu yanitindaki kuyruk kimligini ("250 2.0.0 Ok: queued as ABC123") ayiklar.</summary>
    private static string? ExtractQueueId(string? response)
    {
        const string marker = "queued as ";
        var index = response?.IndexOf(marker, StringComparison.OrdinalIgnoreCase) ?? -1;
        return index < 0 ? null : response![(index + marker.Length)..].Trim();
    }

    private async Task<SmtpSettings?> ReadSettingsAsync(CancellationToken cancellationToken)
    {
        var server = await _parameters.GetAsync(ParameterCatalog.SmtpServer, cancellationToken).ConfigureAwait(false);
        var userName = await _parameters.GetAsync(ParameterCatalog.SmtpUserName, cancellationToken).ConfigureAwait(false);
        var password = await _parameters.GetAsync(ParameterCatalog.SmtpPassword, cancellationToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(userName) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        // Katalog bicimi "sunucu:port" olarak dogrular (PRM-ENT-04).
        var separator = server.LastIndexOf(':');
        return new SmtpSettings(
            server[..separator],
            int.Parse(server[(separator + 1)..], NumberStyles.None, CultureInfo.InvariantCulture),
            userName,
            password);
    }

    private readonly record struct SmtpSettings(string Host, int Port, string UserName, string Password)
    {
        public override string ToString() => $"{Host}:{Port}";
    }
}
