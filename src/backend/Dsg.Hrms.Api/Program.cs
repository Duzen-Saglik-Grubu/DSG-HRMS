using Dsg.Hrms.Api.Identity;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Infrastructure;

// DSG-HRMS API - uygulama giris noktasi (kompozisyon koku).
//
// Bu dosya, ADR-0002'de tanimlanan katman kurallarinin TEK istisnasidir:
// Infrastructure katmanina yalnizca burada, bagimlilik kaydi icin dokunulur.
//
// Not: Loglama, OpenAPI, kimlik dogrulama ve hata yonetimi yapilandirmalari
// A1 asamasinin sonraki adimlarinda (WBS 2.6-2.9) eklenecektir.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

// Yapilandirma dogrulamasi ve veritabani kaydi.
// Zorunlu bir ayar eksikse uygulama BURADA degil, acilirken durur (ADR-0008 §4).
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);

builder.Services.AddHealthChecks();

var app = builder.Build();

// Canlilik kontrolu: uygulama ayakta mi?
app.MapHealthChecks("/health/live");

// Hazir olma kontrolu: bagimliliklar (veritabani, LOGO, NAS) erisilebilir mi?
// Bagimlilik kontrolleri ilgili altyapi bilesenleriyle birlikte eklenecektir.
app.MapHealthChecks("/health/ready");

await app.RunAsync();

/// <summary>
/// Entegrasyon testlerinin <c>WebApplicationFactory</c> ile uygulamayi
/// ayaga kaldirabilmesi icin gereklidir (ADR-0011 §3).
/// </summary>
public partial class Program;
