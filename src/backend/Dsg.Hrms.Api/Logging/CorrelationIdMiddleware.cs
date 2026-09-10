using System.Diagnostics;
using Serilog.Context;

namespace Dsg.Hrms.Api.Logging;

/// <summary>
/// Her istege izlenebilir bir kimlik (<c>CorrelationId</c>) baglar (ADR-0009 §2).
/// </summary>
/// <remarks>
/// <para>
/// Bir kullanici "saat 14:20'de hata aldim" dediginde, o tek istegin urettigi tum
/// kayitlar — uygulama gunlugu, hata kaydi, kullaniciya donen yanit — ayni kimlikle
/// bulunabilmelidir. Kimlik, kullaniciya donen hata yanitinda da yer alir
/// (RFC 9457, <c>traceId</c>); boylece destek talebi ile gunluk kaydi eslesir.
/// </para>
/// <para>
/// Kimlik oncelikle W3C izleme baglamindan (<see cref="Activity"/>) alinir; boylece
/// ileride OpenTelemetry ile toplanan izlerle ayni deger kullanilir.
/// </para>
/// </remarks>
public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    /// <summary>Istek ve yanitta kullanilan baslik adi.</summary>
    public const string HeaderName = "X-Correlation-Id";

    /// <summary>Istek boyunca izleme kimligini tasiyan anahtar.</summary>
    public const string ItemKey = "Dsg.Hrms.CorrelationId";

    private const string LogPropertyName = "CorrelationId";
    private const int MaximumLength = 64;

    /// <summary>Ara katmani calistirir.</summary>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var correlationId = ResolveCorrelationId(context);

        // Istek boyunca erisilebilir olmalidir: denetim izi de ayni kimligi kullanir,
        // boylece bir denetim kaydindan teknik gunluk satirlarina ulasilabilir.
        context.Items[ItemKey] = correlationId;

        // Yanit basligi, govde yazilmaya BASLAMADAN once eklenmelidir.
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty(LogPropertyName, correlationId))
        {
            await next(context);
        }
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        // Cagiran taraf kendi kimligini gonderebilir; bu, istegin birden fazla
        // sistemden gectigi durumlarda ucdan uca izlemeyi mumkun kilar.
        if (context.Request.Headers.TryGetValue(HeaderName, out var incoming) &&
            IsAcceptable(incoming.ToString()))
        {
            return incoming.ToString();
        }

        return Activity.Current?.TraceId.ToString()
            ?? context.TraceIdentifier;
    }

    /// <summary>
    /// Disaridan gelen kimligi dogrular.
    /// </summary>
    /// <remarks>
    /// Baslik degeri saldirgan denetimindedir. Dogrulanmadan gunluge yazilmasi
    /// <b>gunluk enjeksiyonu</b>na yol acar: satir sonu karakteri iceren bir deger,
    /// gunluk dosyasina sahte kayitlar eklemek icin kullanilabilir. Bu yuzden
    /// yalnizca harf, rakam ve tire kabul edilir; uygun olmayan deger sessizce
    /// yok sayilir ve kimlik uretilir.
    /// </remarks>
    private static bool IsAcceptable(string value)
    {
        if (value.Length is 0 or > MaximumLength)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (!char.IsAsciiLetterOrDigit(character) && character != '-')
            {
                return false;
            }
        }

        return true;
    }
}
