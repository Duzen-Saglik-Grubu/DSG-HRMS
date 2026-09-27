using Dsg.Hrms.Api.ErrorHandling;
using Dsg.Hrms.Api.Health;
using Dsg.Hrms.Api.Identity;
using Dsg.Hrms.Api.Logging;
using Dsg.Hrms.Api.Networking;
using Dsg.Hrms.Api.Observability;
using Dsg.Hrms.Api.OpenApi;
using Dsg.Hrms.Api.Validation;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Infrastructure;
using Dsg.Hrms.Infrastructure.Logging;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    builder.Services.AddHrmsProblemDetails();
    builder.Services.AddHrmsOpenApi();
    builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
    builder.Services.TryAddSingleton(TimeProvider.System);

    // Denetleyiciler ve istek dogrulamasi (ADR-0010: FluentValidation tek dogruluk kaynagi).
    builder.Services.AddControllers(options => options.Filters.Add<ValidateRequestsFilter>());
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();
    builder.Services.Configure<RegistrationTimingOptions>(builder.Configuration.GetSection(RegistrationTimingOptions.SectionName));
    builder.Services.AddHrmsReverseProxy(builder.Configuration);

    // Yapilandirma dogrulamasi ve veritabani kaydi.
    // Zorunlu bir ayar eksikse uygulama BURADA degil, acilirken durur (ADR-0008 §4).
    builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

    builder.Services.AddHrmsHealthChecks(builder.Configuration);
    builder.Services.AddHrmsObservability(builder.Configuration);

    var app = builder.Build();

    // Gercek istemci IP'si ters vekilden okunur; izleme kimligi ve istek gunlugu bunu
    // kullanacagi icin en basta calisir (#87).
    app.UseForwardedHeaders();

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

    // Hata yonetimi istek gunlugunden SONRA gelir; gerekcesi
    // ErrorHandlingRegistration icinde yazilidir.
    app.UseHrmsErrorHandling();

    // API sozlesmesi: frontend tipleri bu belgeden uretilir (ADR-0010 §1).
    app.UseHrmsOpenApi();

    // Canlilik ve hazir olma uc noktalari (ADR-0011).
    // LOGO ve NAS kontrolleri, ilgili altyapi bilesenleriyle birlikte eklenecektir.
    app.MapHrmsHealthChecks();
    app.MapControllers();

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
