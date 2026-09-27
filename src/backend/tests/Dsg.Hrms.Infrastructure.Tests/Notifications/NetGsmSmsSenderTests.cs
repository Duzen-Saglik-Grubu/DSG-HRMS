using System.Net;
using System.Text;
using System.Text.Json;
using Dsg.Hrms.Application.Notifications;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Infrastructure.Notifications;

namespace Dsg.Hrms.Infrastructure.Tests.Notifications;

/// <summary>
/// NetGSM istemcisi: istek bicimi ve <c>analiz/02</c> §3.6 hata kodlarinin tamami
/// (SYG-KMLK-029). Gercek gonderim YAPILMAZ; yanitlar sahtedir.
/// </summary>
public sealed class NetGsmSmsSenderTests : IDisposable
{
    private readonly StubHandler _handler = new();
    private readonly StubParameters _parameters = new();
    private readonly HttpClient _http;

    public NetGsmSmsSenderTests()
    {
        _http = new HttpClient(_handler) { BaseAddress = NetGsmSmsSender.BaseAddress };
    }

    public void Dispose()
    {
        _http.Dispose();
        _handler.Dispose();
    }

    private NetGsmSmsSender CreateSender() => new(_http, _parameters);

    private Task<SendResult> SendAsync() =>
        CreateSender().SendAsync("5321234567", "DSG-HRMS dogrulama kodunuz: 482915", Guid.Parse("0199a3b2-0000-7000-8000-000000000001"), CancellationToken.None);

    [Fact]
    public async Task Request_follows_the_documented_format()
    {
        _handler.Respond(HttpStatusCode.OK, """{"code":"00","jobid":"17377215342605050417149344","description":"queued"}""");

        var result = await SendAsync();

        result.ShouldBe(SendResult.Sent("00", "17377215342605050417149344"));

        var request = _handler.Requests.Single();
        request.Method.ShouldBe(HttpMethod.Post);
        request.Uri.ShouldBe(new Uri("https://api.netgsm.com.tr/sms/rest/v2/send"));
        request.Authorization.ShouldBe("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("8503020000:sahteparola")));

        using var body = JsonDocument.Parse(request.Body!);
        var root = body.RootElement;
        root.GetProperty("msgheader").GetString().ShouldBe("DUZEN");
        root.GetProperty("encoding").GetString().ShouldBe("TR");
        root.GetProperty("iysfilter").GetString().ShouldBe("0");
        root.GetProperty("appname").GetString().ShouldBe("DSG-HRMS");
        root.GetProperty("referansID").GetString().ShouldBe("0199a3b2000070008000000000000001");
        var message = root.GetProperty("messages").EnumerateArray().Single();
        message.GetProperty("no").GetString().ShouldBe("5321234567");
        message.GetProperty("msg").GetString().ShouldBe("DSG-HRMS dogrulama kodunuz: 482915");
    }

    [Theory]
    [InlineData("00", SendOutcome.Sent)]
    [InlineData("01", SendOutcome.Sent)]
    [InlineData("02", SendOutcome.Sent)]
    [InlineData("20", SendOutcome.PermanentFailure)]
    [InlineData("30", SendOutcome.ConfigurationError)]
    [InlineData("40", SendOutcome.ConfigurationError)]
    [InlineData("50", SendOutcome.PermanentFailure)]
    [InlineData("51", SendOutcome.PermanentFailure)]
    [InlineData("70", SendOutcome.PermanentFailure)]
    [InlineData("80", SendOutcome.TransientFailure)]
    [InlineData("85", SendOutcome.PermanentFailure)]
    [InlineData("99", SendOutcome.PermanentFailure)]
    public async Task Every_documented_result_code_is_mapped(string code, SendOutcome expected)
    {
        _handler.Respond(HttpStatusCode.OK, $$"""{"code":"{{code}}","jobid":null,"description":"x"}""");

        var result = await SendAsync();

        result.Outcome.ShouldBe(expected);
        result.ResultCode.ShouldBe(code);
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError, SendOutcome.TransientFailure)]
    [InlineData(HttpStatusCode.BadGateway, SendOutcome.TransientFailure)]
    [InlineData(HttpStatusCode.TooManyRequests, SendOutcome.TransientFailure)]
    [InlineData(HttpStatusCode.Unauthorized, SendOutcome.ConfigurationError)]
    [InlineData(HttpStatusCode.Forbidden, SendOutcome.ConfigurationError)]
    [InlineData(HttpStatusCode.BadRequest, SendOutcome.PermanentFailure)]
    public async Task Http_status_codes_are_mapped(HttpStatusCode status, SendOutcome expected)
    {
        _handler.Respond(status, "<html>hata</html>");

        (await SendAsync()).Outcome.ShouldBe(expected);
    }

    [Fact]
    public async Task Network_error_is_transient_and_does_not_leak_the_exception_text()
    {
        _handler.Throw(new HttpRequestException("5321234567 numarasina baglanilamadi"));

        var result = await SendAsync();

        result.ShouldBe(SendResult.Transient("network"));
    }

    [Fact]
    public async Task Timeout_is_transient()
    {
        _handler.Throw(new TaskCanceledException("zaman asimi"));

        (await SendAsync()).ShouldBe(SendResult.Transient("timeout"));
    }

    [Fact]
    public async Task Missing_credentials_are_a_configuration_error_and_nothing_is_sent()
    {
        _parameters.Values.Remove(ParameterCatalog.NetGsmPassword.Key);

        (await SendAsync()).ShouldBe(SendResult.Misconfigured("not-configured"));
        _handler.Requests.ShouldBeEmpty();
    }

    // ------------------------------------------------------------------ yoklama (ileti gondermeden)

    [Fact]
    public async Task Check_queries_header_and_balance_without_sending()
    {
        _handler.Respond(HttpStatusCode.OK, """{"code":"00","msgheaders":["DUZEN","DIGER"],"description":"success"}""");
        _handler.Respond(HttpStatusCode.OK, """{"balance":[{"amount":20,"balance_name":"Adet SMS"},{"amount":"15","balance_name":"Kredi Bakiye"}]}""");

        var status = await CreateSender().CheckAsync(CancellationToken.None);

        status.IsConfigured.ShouldBeTrue();
        status.IsReachable.ShouldBeTrue();
        status.HeaderDefined.ShouldBeTrue();
        status.Balances.ShouldBe([new NetGsmBalance("Adet SMS", "20"), new NetGsmBalance("Kredi Bakiye", "15")]);

        _handler.Requests.Select(r => r.Uri.AbsolutePath).ShouldBe(["/sms/rest/v2/msgheader", "/balance"]);
        _handler.Requests.ShouldNotContain(r => r.Uri.AbsolutePath.Contains("send"));

        using var balanceBody = JsonDocument.Parse(_handler.Requests[1].Body!);
        balanceBody.RootElement.GetProperty("usercode").GetString().ShouldBe("8503020000");
        balanceBody.RootElement.GetProperty("stip").GetInt32().ShouldBe(3);
    }

    [Fact]
    public async Task Check_reports_an_undefined_sender_header()
    {
        _handler.Respond(HttpStatusCode.OK, """{"code":"00","msgheaders":["BASKA"]}""");
        _handler.Respond(HttpStatusCode.OK, """{"code":"00","balance":"15"}""");

        var status = await CreateSender().CheckAsync(CancellationToken.None);

        status.HeaderDefined.ShouldBeFalse();
        status.Balances.ShouldBe([new NetGsmBalance("Kredi", "15")]);
    }

    [Fact]
    public async Task Check_reports_wrong_credentials()
    {
        _handler.Respond(HttpStatusCode.OK, """{"code":"30","msgheaders":null}""");

        var status = await CreateSender().CheckAsync(CancellationToken.None);

        status.IsReachable.ShouldBeFalse();
        status.ResultCode.ShouldBe("30");
    }

    [Fact]
    public async Task Check_reports_unreachable_service()
    {
        _handler.Throw(new HttpRequestException("dns"));

        (await CreateSender().CheckAsync(CancellationToken.None)).ResultCode.ShouldBe("network");
    }

    [Fact]
    public async Task Check_without_credentials_reports_not_configured()
    {
        _parameters.Values.Remove(ParameterCatalog.NetGsmUserCode.Key);

        (await CreateSender().CheckAsync(CancellationToken.None)).ShouldBe(NetGsmStatus.NotConfigured);
        _handler.Requests.ShouldBeEmpty();
    }

    // ------------------------------------------------------------------ sahteler

    private sealed class StubParameters : ISystemParameters
    {
        public Dictionary<string, string?> Values { get; } = new()
        {
            [ParameterCatalog.NetGsmUserCode.Key] = "8503020000",
            [ParameterCatalog.NetGsmPassword.Key] = "sahteparola",
            [ParameterCatalog.SmsSenderTitle.Key] = "DUZEN",
        };

        public ValueTask<string?> GetAsync(ParameterDefinition parameter, CancellationToken cancellationToken) =>
            ValueTask.FromResult(Values.GetValueOrDefault(parameter.Key));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Queue<Func<HttpResponseMessage>> _responses = new();

        public List<RecordedRequest> Requests { get; } = [];

        public void Respond(HttpStatusCode status, string body) =>
            _responses.Enqueue(() => new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });

        public void Throw(Exception exception) => _responses.Enqueue(() => throw exception);

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RecordedRequest(request.Method, request.RequestUri!, request.Headers.Authorization?.ToString(), body));
            return _responses.Dequeue()();
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, Uri Uri, string? Authorization, string? Body);
}
