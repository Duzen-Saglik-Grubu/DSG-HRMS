using Dsg.Hrms.Api.Identity;
using Dsg.Hrms.Api.Logging;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Infrastructure;
using Dsg.Hrms.Infrastructure.Logging;
using Serilog;

// DSG-HRMS API - uygulama giris noktasi (kompozisyon koku).
//
// Bu dosya, ADR-0002'de tanimlanan katman kurallarinin TEK istisnasidir:
// Infrastructure katmanina yalnizca burada, bagimlilik kaydi icin dokunulur.
//
// Not: OpenAPI, kimlik dogrulama ve hata yonetimi yapilandirmalari
// A1 asamasinin sonraki adimlarinda (WBS 2.7-2.9) eklenecektir.

// Onyukleme gunlugu: yapilandirma okunmadan once olusan hatalar da kaydedilir.
// Aksi hâlde "uygulama aciliyor ama sessizce oluyor" durumu yasanirdi (ADR-0009 §1).
Log.Logger = SerilogConfiguration.CreateBootstrapConfiguration().CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Gunluk yapilandirmasi Infrastructure katmanindadir: maskeleme, hedefler ve
    // saklama suresi tek yerden yonetilir.
    builder.Host.UseSerilog((_, _, loggerConfiguration) =>
        SerilogConfiguration.Configure(loggerConfiguration, builder.Configuration, builder.Environment));

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

    // Yapilandirma dogrulamasi ve veritabani kaydi.
    // Zorunlu bir ayar eksikse uygulama BURADA degil, acilirken durur (ADR-0008 §4).
    builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // Izleme kimligi, gunluk kaydindan ONCE baglanmalidir; boylece istek kaydi da
    // ayni kimligi tasir.
    app.UseMiddleware<CorrelationIdMiddleware>();

    // Istek basina TEK ozet kayit: yontem, yol, durum kodu, sure.
    //
    // Sorgu dizesi (query string) BILINCLI olarak yazilmaz: arama kutusuna girilen
    // bir T.C. kimlik numarasi aksi hâlde duz metin olarak diske duserdi (ADR-0009 §4).
    app.UseSerilogRequestLogging(options =>
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("RemoteIpAddress", httpContext.Connection.RemoteIpAddress?.ToString());
        });

    // Canlilik kontrolu: uygulama ayakta mi?
    app.MapHealthChecks("/health/live");

    // Hazir olma kontrolu: bagimliliklar (veritabani, LOGO, NAS) erisilebilir mi?
    // Bagimlilik kontrolleri ilgili altyapi bilesenleriyle birlikte eklenecektir.
    app.MapHealthChecks("/health/ready");

    await app.RunAsync();
}
catch (Exception exception)
{
    // Acilisi engelleyen hata sessizce kaybolmamalidir.
    Log.Fatal(exception, "Uygulama baslatilamadi.");
    throw;
}
finally
{
    // Arabellekteki kayitlarin diske yazilmasini garanti eder.
    await Log.CloseAndFlushAsync();
}

/// <summary>
/// Entegrasyon testlerinin <c>WebApplicationFactory</c> ile uygulamayi
/// ayaga kaldirabilmesi icin gereklidir (ADR-0011 §3).
/// </summary>
public partial class Program;
