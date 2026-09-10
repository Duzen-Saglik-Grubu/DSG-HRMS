using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dsg.Hrms.Api.ErrorHandling;
using Dsg.Hrms.Api.Logging;
using Dsg.Hrms.Application.Common.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Dsg.Hrms.Api.IntegrationTests.ErrorHandling;

/// <summary>
/// Hata yanitlarinin RFC 9457 bicimini ve <b>ic ayrinti sizdirmadigini</b> dogrular
/// (ADR-0010 §5-§7).
/// </summary>
/// <remarks>
/// <para>
/// Testler gercek bir HTTP boru hattinda calisir ve uretimdeki <b>ayni</b> kurulum
/// metotlarini (<see cref="ErrorHandlingRegistration"/>) cagirir. Test icin ayri bir
/// yapilandirma yazilsaydi, testler gecerken uretimde farkli davranan bir hata yolu
/// olusabilirdi.
/// </para>
/// <para>
/// Ortam bilincli olarak <c>Development</c> secilmistir: ayrintinin en cok sizmaya
/// egilimli oldugu ortam budur.
/// </para>
/// </remarks>
public sealed class ProblemDetailsTests : IAsyncLifetime
{
    private IHost _host = null!;
    private HttpClient _client = null!;

    /// <summary>Gunluge ve yanita sizmamasi gereken ic ayrinti.</summary>
    private const string InternalDetail =
        "select * from person where national_id = '12345678901'";

    public async Task InitializeAsync()
    {
        _host = await new HostBuilder()
            .ConfigureWebHost(webHost => webHost
                .UseTestServer()
                .UseEnvironment(Environments.Development)
                .ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddHrmsProblemDetails();
                })
                .Configure(app =>
                {
                    app.UseMiddleware<CorrelationIdMiddleware>();
                    app.UseHrmsErrorHandling();
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapGet("/basarili", () => Results.Ok(new { durum = "tamam" }));

                        endpoints.MapGet("/bulunamadi", void () =>
                            throw new NotFoundException());

                        endpoints.MapGet("/yetkisiz", void () =>
                            throw new ForbiddenException());

                        endpoints.MapGet("/cakisma", void () =>
                            throw new ConflictException("Bu talep zaten onaylanmis."));

                        endpoints.MapGet("/is-kurali", void () =>
                            throw new BusinessRuleException("Yillik izin bakiyeniz yetersiz."));

                        endpoints.MapGet("/eszamanlilik", void () =>
                            throw new DbUpdateConcurrencyException());

                        endpoints.MapGet("/dogrulama", void () =>
                            throw new ValidationException(
                            [
                                new ValidationFailure("StartDate", "Baslangic tarihi gecmis olamaz."),
                                new ValidationFailure("StartDate", "Baslangic tarihi zorunludur."),
                                new ValidationFailure("DayCount", "Gun sayisi sifirdan buyuk olmalidir."),
                            ]));

                        endpoints.MapGet("/beklenmeyen", void () =>
                            throw new InvalidOperationException(InternalDetail));
                    });
                }))
            .StartAsync();

        _client = _host.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _host.StopAsync();
        _host.Dispose();
    }

    private async Task<(HttpResponseMessage Response, JsonElement Body)> GetAsync(string path)
    {
        var response = await _client.GetAsync(new Uri(path, UriKind.Relative));
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        return (response, body);
    }

    // ------------------------------------------------------------------
    // Durum kodu eslemesi (ADR-0010 §6)
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("/bulunamadi", HttpStatusCode.NotFound, "not-found")]
    [InlineData("/yetkisiz", HttpStatusCode.Forbidden, "forbidden")]
    [InlineData("/cakisma", HttpStatusCode.Conflict, "conflict")]
    [InlineData("/is-kurali", HttpStatusCode.UnprocessableContent, "business-rule")]
    [InlineData("/eszamanlilik", HttpStatusCode.Conflict, "concurrency")]
    [InlineData("/dogrulama", HttpStatusCode.BadRequest, "validation")]
    [InlineData("/beklenmeyen", HttpStatusCode.InternalServerError, "unexpected")]
    public async Task Exceptions_are_mapped_to_the_agreed_status_codes(
        string path,
        HttpStatusCode expectedStatus,
        string expectedType)
    {
        var (response, body) = await GetAsync(path);

        response.StatusCode.ShouldBe(expectedStatus);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        body.GetProperty("type").GetString().ShouldBe($"https://dsg-hrms/errors/{expectedType}");
        body.GetProperty("status").GetInt32().ShouldBe((int)expectedStatus);
        body.GetProperty("instance").GetString().ShouldBe(path);
    }

    // ------------------------------------------------------------------
    // Ic ayrinti sizmamalidir (ADR-0010 §5)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Unexpected_error_never_leaks_internal_detail()
    {
        // Ortam Development; "yalnizca gelistirmede ayrinti gosterelim" yaklasimi,
        // ortam degiskeninin yanlis ayarlandigi bir uretim dagitiminda sizinti uretirdi.
        var response = await _client.GetAsync(new Uri("/beklenmeyen", UriKind.Relative));
        var raw = await response.Content.ReadAsStringAsync();

        raw.ShouldNotContain(InternalDetail);
        raw.ShouldNotContain("12345678901");
        raw.ShouldNotContain(nameof(InvalidOperationException));
        raw.ShouldNotContain("StackTrace", Case.Insensitive);
        raw.ShouldNotContain("Dsg.Hrms.Api.IntegrationTests");

        // Kullaniciya anlamli, Turkce ve genel bir mesaj doner.
        raw.ShouldContain("beklenmeyen bir hata");
    }

    [Fact]
    public async Task Business_messages_are_shown_but_technical_ones_are_not()
    {
        // Is hatalarinin mesaji kullaniciya YONELIKTIR ve gosterilir...
        var (_, business) = await GetAsync("/is-kurali");
        business.GetProperty("detail").GetString().ShouldBe("Yillik izin bakiyeniz yetersiz.");

        // ...beklenmeyen hatalarin mesaji ise ASLA gosterilmez.
        var (_, unexpected) = await GetAsync("/beklenmeyen");
        unexpected.GetProperty("detail").GetString().ShouldNotBeNull().ShouldNotContain("select");
    }

    [Fact]
    public async Task Out_of_scope_record_returns_404_not_403()
    {
        // Kapsam disi kayit icin 403 donseydi, yanit kaydin VAR OLDUGUNU sizdirirdi
        // (ADR-0007 §4). Kimlik denemesiyle kayit varligi cikarilabilirdi.
        var (response, body) = await GetAsync("/bulunamadi");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        body.GetProperty("detail").GetString().ShouldBe("Kayit bulunamadi.");
    }

    // ------------------------------------------------------------------
    // Izlenebilirlik (ADR-0009 §2, ADR-0010 §5)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Every_error_carries_the_same_trace_id_as_the_response_header()
    {
        _client.DefaultRequestHeaders.Add(CorrelationIdMiddleware.HeaderName, "DESTEK-2026-0042");

        var (response, body) = await GetAsync("/beklenmeyen");

        // Kullanici ekranda gordugu numarayi destek talebinde iletir; kayit bu
        // deger uzerinden bulunur.
        body.GetProperty("traceId").GetString().ShouldBe("DESTEK-2026-0042");
        response.Headers.GetValues(CorrelationIdMiddleware.HeaderName)
            .ShouldContain("DESTEK-2026-0042");
    }

    [Fact]
    public async Task Trace_id_is_present_even_without_an_incoming_header()
    {
        var (_, body) = await GetAsync("/bulunamadi");

        body.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }

    // ------------------------------------------------------------------
    // Dogrulama (ADR-0010 §8)
    // ------------------------------------------------------------------

    [Fact]
    public async Task Validation_errors_are_grouped_by_field_in_camel_case()
    {
        var (_, body) = await GetAsync("/dogrulama");

        var errors = body.GetProperty("errors");

        // Frontend bu sozlugu dogrudan form alanina baglar; alan adlari camelCase.
        errors.GetProperty("startDate").GetArrayLength().ShouldBe(2);
        errors.GetProperty("dayCount").GetArrayLength().ShouldBe(1);
        errors.GetProperty("dayCount")[0].GetString()
            .ShouldBe("Gun sayisi sifirdan buyuk olmalidir.");
    }

    // ------------------------------------------------------------------
    // Hatasiz istekler etkilenmez
    // ------------------------------------------------------------------

    [Fact]
    public async Task A_successful_request_is_not_affected()
    {
        var (response, body) = await GetAsync("/basarili");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        body.GetProperty("durum").GetString().ShouldBe("tamam");
    }

    [Fact]
    public async Task An_unmatched_route_also_returns_problem_details()
    {
        // Cerceve kaynakli govdesiz 404'ler de ayni bicimde donmelidir; aksi hâlde
        // frontend iki farkli hata bicimi ele almak zorunda kalirdi.
        var (response, body) = await GetAsync("/olmayan-yol");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        body.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();
    }
}
