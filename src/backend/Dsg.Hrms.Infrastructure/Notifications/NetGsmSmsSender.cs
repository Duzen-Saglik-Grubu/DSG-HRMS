using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dsg.Hrms.Application.Notifications;
using Dsg.Hrms.Application.Settings;

namespace Dsg.Hrms.Infrastructure.Notifications;

/// <summary>
/// NetGSM standart SMS servisi (ADR-0012 §2, <c>analiz/02</c>, <c>KR-042</c>).
/// </summary>
/// <remarks>
/// <para>
/// Erisim bilgileri ve gonderici adi parametre deposundan her gonderimde okunur
/// (<c>PRM-ENT-01…03</c>); parola degistiginde yeniden baslatma gerekmez.
/// </para>
/// <para>
/// Sonuc kodlari <c>analiz/02</c> §3.6'ya gore eslenir. Istisna metni sonuca YAZILMAZ:
/// NetGSM hata aciklamalari numarayi tasiyabilir.
/// </para>
/// </remarks>
public sealed class NetGsmSmsSender : ISmsSender
{
    /// <summary>Servis adresi.</summary>
    public static readonly Uri BaseAddress = new("https://api.netgsm.com.tr/");

    /// <summary>NetGSM panelinde bu uygulamanin gonderimlerini ayirt eden ad.</summary>
    public const string AppName = "DSG-HRMS";

    private static readonly JsonSerializerOptions Json = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    private readonly HttpClient _http;
    private readonly ISystemParameters _parameters;

    /// <summary>Yeni ornek olusturur.</summary>
    public NetGsmSmsSender(HttpClient http, ISystemParameters parameters)
    {
        _http = http;
        _parameters = parameters;
    }

    /// <inheritdoc />
    public async Task<SendResult> SendAsync(string phone, string messageBody, Guid reference, CancellationToken cancellationToken)
    {
        var credentials = await ReadCredentialsAsync(cancellationToken).ConfigureAwait(false);
        if (credentials is null)
        {
            return SendResult.Misconfigured("not-configured");
        }

        var header = await _parameters.GetAsync(ParameterCatalog.SmsSenderTitle, cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(header))
        {
            return SendResult.Misconfigured("not-configured");
        }

        var body = new SendRequest(
            header,
            [new SendMessage(messageBody, phone)],
            Encoding: "TR",
            IysFilter: "0",
            AppName: AppName,
            ReferenceId: reference.ToString("N", CultureInfo.InvariantCulture));

        using var request = new HttpRequestMessage(HttpMethod.Post, "sms/rest/v2/send")
        {
            Content = JsonContent.Create(body, options: Json),
        };
        request.Headers.Authorization = credentials.Value.BasicHeader;

        try
        {
            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if ((int)response.StatusCode >= 500 || response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                return SendResult.Transient(string.Create(CultureInfo.InvariantCulture, $"http-{(int)response.StatusCode}"));
            }

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return SendResult.Misconfigured(string.Create(CultureInfo.InvariantCulture, $"http-{(int)response.StatusCode}"));
            }

            var result = await ReadJsonAsync<SendResponse>(response, cancellationToken).ConfigureAwait(false);
            if (result?.Code is null)
            {
                return SendResult.Permanent(string.Create(CultureInfo.InvariantCulture, $"http-{(int)response.StatusCode}"));
            }

            return Map(result.Code, result.JobId);
        }
        catch (HttpRequestException)
        {
            return SendResult.Transient("network");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return SendResult.Transient("timeout");
        }
        catch (Polly.Timeout.TimeoutRejectedException)
        {
            return SendResult.Transient("timeout");
        }
    }

    /// <summary>
    /// Bakiye ve gonderici adini ILETI GONDERMEDEN sorgular (kanal sagligi).
    /// </summary>
    public async Task<NetGsmStatus> CheckAsync(CancellationToken cancellationToken)
    {
        var credentials = await ReadCredentialsAsync(cancellationToken).ConfigureAwait(false);
        var header = await _parameters.GetAsync(ParameterCatalog.SmsSenderTitle, cancellationToken).ConfigureAwait(false);
        if (credentials is null || string.IsNullOrWhiteSpace(header))
        {
            return NetGsmStatus.NotConfigured;
        }

        try
        {
            using var headerRequest = new HttpRequestMessage(HttpMethod.Get, "sms/rest/v2/msgheader");
            headerRequest.Headers.Authorization = credentials.Value.BasicHeader;
            using var headerResponse = await _http.SendAsync(headerRequest, cancellationToken).ConfigureAwait(false);
            var headers = await ReadJsonAsync<HeaderResponse>(headerResponse, cancellationToken).ConfigureAwait(false);

            if (headers?.Code != "00")
            {
                return new NetGsmStatus(true, false, headers?.Code ?? $"http-{(int)headerResponse.StatusCode}", false, []);
            }

            var headerDefined = headers.MessageHeaders?.Contains(header, StringComparer.Ordinal) == true;

            using var balanceResponse = await _http.PostAsJsonAsync(
                "balance",
                new BalanceRequest(credentials.Value.UserCode, credentials.Value.Password, 3),
                Json,
                cancellationToken).ConfigureAwait(false);
            var balance = await ReadJsonAsync<JsonElement>(balanceResponse, cancellationToken).ConfigureAwait(false);

            return new NetGsmStatus(true, true, "00", headerDefined, ParseBalance(balance));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or Polly.Timeout.TimeoutRejectedException or JsonException)
        {
            return new NetGsmStatus(true, false, "network", false, []);
        }
    }

    /// <summary>NetGSM sonuc kodunu gonderim sonucuna esler (<c>analiz/02</c> §3.6).</summary>
    public static SendResult Map(string code, string? jobId) => code switch
    {
        "00" or "01" or "02" => SendResult.Sent(code, jobId),
        "30" or "40" => SendResult.Misconfigured(code),
        "80" => SendResult.Transient(code),
        _ => SendResult.Permanent(code),
    };

    private static IReadOnlyList<NetGsmBalance> ParseBalance(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object || !element.TryGetProperty("balance", out var balance))
        {
            return [];
        }

        // stip=3 dizi doner; "amount" bazen sayi, bazen metindir (NetGSM API belgesi ornegi).
        if (balance.ValueKind != JsonValueKind.Array)
        {
            return [new NetGsmBalance("Kredi", balance.ToString())];
        }

        return [.. balance.EnumerateArray().Select(item => new NetGsmBalance(
            item.TryGetProperty("balance_name", out var name) ? name.ToString() : string.Empty,
            item.TryGetProperty("amount", out var amount) ? amount.ToString() : string.Empty))];
    }

    private static async Task<T?> ReadJsonAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            return await response.Content.ReadFromJsonAsync<T>(Json, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private async Task<Credentials?> ReadCredentialsAsync(CancellationToken cancellationToken)
    {
        var userCode = await _parameters.GetAsync(ParameterCatalog.NetGsmUserCode, cancellationToken).ConfigureAwait(false);
        var password = await _parameters.GetAsync(ParameterCatalog.NetGsmPassword, cancellationToken).ConfigureAwait(false);

        return string.IsNullOrWhiteSpace(userCode) || string.IsNullOrEmpty(password)
            ? null
            : new Credentials(userCode, password);
    }

    private readonly record struct Credentials(string UserCode, string Password)
    {
        public AuthenticationHeaderValue BasicHeader =>
            new("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{UserCode}:{Password}")));

        public override string ToString() => UserCode;
    }

    private sealed record SendRequest(
        [property: JsonPropertyName("msgheader")] string MessageHeader,
        [property: JsonPropertyName("messages")] IReadOnlyList<SendMessage> Messages,
        [property: JsonPropertyName("encoding")] string Encoding,
        [property: JsonPropertyName("iysfilter")] string IysFilter,
        [property: JsonPropertyName("appname")] string AppName,
        [property: JsonPropertyName("referansID")] string ReferenceId);

    private sealed record SendMessage(
        [property: JsonPropertyName("msg")] string MessageBody,
        [property: JsonPropertyName("no")] string Number);

    private sealed record SendResponse(
        [property: JsonPropertyName("code")] string? Code,
        [property: JsonPropertyName("jobid")] string? JobId);

    private sealed record HeaderResponse(
        [property: JsonPropertyName("code")] string? Code,
        [property: JsonPropertyName("msgheaders")] IReadOnlyList<string>? MessageHeaders);

    private sealed record BalanceRequest(
        [property: JsonPropertyName("usercode")] string UserCode,
        [property: JsonPropertyName("password")] string Password,
        [property: JsonPropertyName("stip")] int Stip)
    {
        public override string ToString() => UserCode;
    }
}

/// <summary>NetGSM hesap durumu (ileti gondermeden).</summary>
/// <param name="IsConfigured">Erisim bilgileri tanimli mi.</param>
/// <param name="IsReachable">Kimlik dogrulamasi basarili ve servis yanit verdi mi.</param>
/// <param name="ResultCode">NetGSM sonuc kodu veya hata turu.</param>
/// <param name="HeaderDefined">Parametredeki gonderici adi (<c>DUZEN</c>) hesapta tanimli mi.</param>
/// <param name="Balances">Paket/kredi bakiyeleri.</param>
public sealed record NetGsmStatus(bool IsConfigured, bool IsReachable, string? ResultCode, bool HeaderDefined, IReadOnlyList<NetGsmBalance> Balances)
{
    /// <summary>Erisim bilgileri tanimli degil.</summary>
    public static NetGsmStatus NotConfigured { get; } = new(false, false, null, false, []);
}

/// <summary>Tek bir bakiye kalemi.</summary>
/// <param name="Name">Paket veya varlik adi ("Adet SMS", "Kredi Bakiye").</param>
/// <param name="Amount">Miktar.</param>
public sealed record NetGsmBalance(string Name, string Amount);
