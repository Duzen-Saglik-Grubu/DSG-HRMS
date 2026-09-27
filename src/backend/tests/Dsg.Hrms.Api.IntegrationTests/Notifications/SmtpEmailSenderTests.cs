using System.Net.Http.Json;
using System.Text.Json;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Dsg.Hrms.Application.Notifications;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Infrastructure.Notifications;

namespace Dsg.Hrms.Api.IntegrationTests.Notifications;

/// <summary>
/// SMTP gondericisinin <b>gercek bir SMTP sunucusuna</b> karsi dogrulanmasi (SYG-KMLK-030).
/// </summary>
/// <remarks>
/// Sunucu Mailpit'tir: iletileri teslim etmez, yalnizca yakalar ve HTTP arayuzunden
/// okunmasina izin verir. Kurum sunucusuna hicbir ileti GITMEZ.
/// </remarks>
public sealed class SmtpEmailSenderTests : IAsyncLifetime, IDisposable
{
    private const int SmtpPort = 1025;
    private const int HttpPort = 8025;

    private readonly IContainer _mailpit = new ContainerBuilder("axllent/mailpit:v1.27")
        .WithPortBinding(SmtpPort, true)
        .WithPortBinding(HttpPort, true)
        .WithEnvironment("MP_SMTP_AUTH_ACCEPT_ANY", "true")
        .WithEnvironment("MP_SMTP_AUTH_ALLOW_INSECURE", "true")
        .WithWaitStrategy(Wait.ForUnixContainer().UntilHttpRequestIsSucceeded(request => request.ForPort(HttpPort).ForPath("/api/v1/info")))
        .Build();

    private readonly HttpClient _http = new();

    public async Task InitializeAsync()
    {
        await _mailpit.StartAsync();
        _http.BaseAddress = new Uri($"http://{_mailpit.Hostname}:{_mailpit.GetMappedPublicPort(HttpPort)}/");
    }

    public async Task DisposeAsync() => await _mailpit.DisposeAsync();

    public void Dispose() => _http.Dispose();

    private SmtpEmailSender CreateSender(int? port = null, string? password = "sahteparola") =>
        new(new StubParameters(new Dictionary<string, string?>
        {
            [ParameterCatalog.SmtpServer.Key] = $"{_mailpit.Hostname}:{port ?? _mailpit.GetMappedPublicPort(SmtpPort)}",
            [ParameterCatalog.SmtpUserName.Key] = "ik.bildirim@duzen.com.tr",
            [ParameterCatalog.SmtpPassword.Key] = password,
        }));

    [Fact]
    public async Task Email_is_delivered_with_sender_name_subject_and_body()
    {
        var result = await CreateSender().SendAsync(
            "ahmet.yilmaz@duzen.com.tr", "DSG-HRMS üyelik doğrulama kodu", "Kodunuz: 482915", CancellationToken.None);

        result.Outcome.ShouldBe(SendOutcome.Sent);

        using var list = JsonDocument.Parse(await _http.GetStringAsync("api/v1/messages"));
        var message = list.RootElement.GetProperty("messages").EnumerateArray().Single();
        message.GetProperty("From").GetProperty("Address").GetString().ShouldBe("ik.bildirim@duzen.com.tr");
        message.GetProperty("From").GetProperty("Name").GetString().ShouldBe("Düzen İK Sistemi");
        message.GetProperty("To").EnumerateArray().Single().GetProperty("Address").GetString().ShouldBe("ahmet.yilmaz@duzen.com.tr");
        message.GetProperty("Subject").GetString().ShouldBe("DSG-HRMS üyelik doğrulama kodu");

        var id = message.GetProperty("ID").GetString();
        var detail = await _http.GetFromJsonAsync<JsonElement>($"api/v1/message/{id}");
        detail.GetProperty("Text").GetString()!.ShouldContain("Kodunuz: 482915");
    }

    [Fact]
    public async Task Check_authenticates_without_sending()
    {
        (await CreateSender().CheckAsync(CancellationToken.None)).ShouldBe(SendResult.Sent("auth-ok"));

        using var list = JsonDocument.Parse(await _http.GetStringAsync("api/v1/messages"));
        list.RootElement.GetProperty("total").GetInt32().ShouldBe(0);
    }

    [Fact]
    public async Task Unreachable_server_is_transient()
    {
        // Mailpit'in HTTP portu SMTP konusmaz; kapali bir port gibi davranir.
        var result = await CreateSender(port: 1).SendAsync("a@duzen.com.tr", "k", "g", CancellationToken.None);

        result.Outcome.ShouldBe(SendOutcome.TransientFailure);
    }

    [Fact]
    public async Task Missing_password_is_a_configuration_error_and_nothing_is_sent()
    {
        (await CreateSender(password: null).SendAsync("a@duzen.com.tr", "k", "g", CancellationToken.None))
            .ShouldBe(SendResult.Misconfigured("not-configured"));
    }

    private sealed class StubParameters(Dictionary<string, string?> values) : ISystemParameters
    {
        public ValueTask<string?> GetAsync(ParameterDefinition parameter, CancellationToken cancellationToken) =>
            ValueTask.FromResult(values.GetValueOrDefault(parameter.Key));
    }
}
