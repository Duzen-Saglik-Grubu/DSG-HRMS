using Dsg.Hrms.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentValidationException = FluentValidation.ValidationException;

namespace Dsg.Hrms.Api.ErrorHandling;

/// <summary>
/// Tum hatalari RFC 9457 <c>problem+json</c> bicimine cevirir (ADR-0010 §5).
/// </summary>
/// <remarks>
/// <para>
/// Hata yanitinin tek yerden uretilmesi iki sorunu birden cozer: kullanici tutarli
/// mesajlar gorur ve <b>ic ayrinti sizmasi</b> tek bir yerde engellenir. Her uc
/// noktada ayri yazilsaydi, er ya da gec bir yerde veritabani mesaji veya yigin izi
/// yanita duserdi; sizan ayrinti saldirgana veri modelini tarif eder.
/// </para>
/// <para>
/// Ayrinti kaybolmaz, yalnizca <b>yer degistirir</b>: tamami sunucu gunlugune yazilir
/// ve <c>traceId</c> ile bulunur (ADR-0009 §2).
/// </para>
/// </remarks>
public sealed partial class ProblemDetailsExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    private const string TypeBaseUri = "https://dsg-hrms/errors/";

    /// <inheritdoc />
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        // Istemci baglantiyi kestiginde yanit yazilacak bir yer kalmaz; bu bir
        // uygulama hatasi da degildir.
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var problemDetails = CreateProblemDetails(httpContext, exception);

        Log(exception, problemDetails.Status ?? StatusCodes.Status500InternalServerError);

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,

            // Exception BILEREK verilmez: cerceve, ayrinti uretecine istisnayi
            // aktarirsa yigin izi yanita eklenebilir.
            AdditionalMetadata = null,
        }).ConfigureAwait(false);
    }

    private static ProblemDetails CreateProblemDetails(HttpContext httpContext, Exception exception)
    {
        var problemDetails = exception switch
        {
            FluentValidationException validation => FromValidation(validation),
            HrmsException known => FromKnown(known),
            DbUpdateConcurrencyException => FromConcurrency(),
            _ => FromUnexpected(),
        };

        problemDetails.Instance = httpContext.Request.Path;

        return problemDetails;
    }

    /// <summary>
    /// Dogrulama hatalari alan bazinda doner (ADR-0010 §8).
    /// </summary>
    /// <remarks>
    /// Frontend bu sozlugu dogrudan ilgili form alanina baglar; kullanici hangi
    /// alanin neden reddedildigini gorur.
    /// </remarks>
    private static ProblemDetails FromValidation(FluentValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => ToCamelCase(group.Key),
                group => group.Select(failure => failure.ErrorMessage).ToArray(),
                StringComparer.Ordinal);

        var problemDetails = Create(
            StatusCodes.Status400BadRequest,
            "validation",
            "Dogrulama hatasi",
            "Gonderilen veri gecerli degil.");

        problemDetails.Extensions["errors"] = errors;

        return problemDetails;
    }

    private static ProblemDetails FromKnown(HrmsException exception) =>
        Create(exception.StatusCode, exception.ErrorType, exception.Title, exception.Message);

    /// <summary>
    /// Eszamanlilik cakismasi (ADR-0010 §7).
    /// </summary>
    /// <remarks>
    /// Sessiz uzerine yazma YAPILMAZ: kullaniciya kaydin bu arada degistigi soylenir,
    /// boylece kendi degisikliginin baskasinin degisikligini sildigini fark eder.
    /// </remarks>
    private static ProblemDetails FromConcurrency() =>
        Create(
            StatusCodes.Status409Conflict,
            "concurrency",
            "Kayit guncellenmis",
            "Bu kayit siz goruntulerken baskasi tarafindan guncellendi. " +
            "Lutfen sayfayi yenileyip islemi tekrar deneyin.");

    /// <summary>
    /// Beklenmeyen hata.
    /// </summary>
    /// <remarks>
    /// Mesaj kasitli olarak GENELDIR ve istisnadan hicbir sey tasimaz. Gelistirme
    /// ortaminda da ayni davranir: "yalnizca gelistirmede ayrinti gosterelim"
    /// yaklasimi, ortam degiskeninin yanlis ayarlandigi bir uretim dagitiminda
    /// sizinti uretirdi.
    /// </remarks>
    private static ProblemDetails FromUnexpected() =>
        Create(
            StatusCodes.Status500InternalServerError,
            "unexpected",
            "Beklenmeyen hata",
            "Islem sirasinda beklenmeyen bir hata olustu. " +
            "Sorun devam ederse asagidaki takip numarasiyla birlikte bize ulasin.");

    private static ProblemDetails Create(int status, string errorType, string title, string detail) =>
        new()
        {
            Status = status,
            Type = TypeBaseUri + errorType,
            Title = title,
            Detail = detail,
        };

    private void Log(Exception exception, int statusCode)
    {
        // Beklenen is hatalari uyari, beklenmeyenler hata seviyesindedir: 404'lerin
        // hata olarak birikmesi, gercek sorunlarin gunlukte kaybolmasina yol acardi.
        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            LogUnexpected(logger, exception);
        }
        else
        {
            LogHandled(logger, statusCode, exception.GetType().Name);
        }
    }

    // Kaynak ureteci ile uretilen gunluk metotlari: sicak yolda bicimlendirme
    // maliyeti olusmaz (CA1848).
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Error,
        Message = "Istek beklenmeyen bir hatayla sonuclandi.")]
    private static partial void LogUnexpected(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "Istek {StatusCode} ile sonuclandi: {ExceptionType}")]
    private static partial void LogHandled(ILogger logger, int statusCode, string exceptionType);

    private static string ToCamelCase(string value) =>
        string.IsNullOrEmpty(value) || char.IsLower(value[0])
            ? value
            : char.ToLowerInvariant(value[0]) + value[1..];
}
