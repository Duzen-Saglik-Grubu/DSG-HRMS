using Microsoft.OpenApi;

namespace Dsg.Hrms.Api.OpenApi;

/// <summary>
/// API sozlesmesinin (OpenAPI) uretimi (ADR-0010 §1).
/// </summary>
/// <remarks>
/// <para>
/// OpenAPI belgesi <b>tek dogruluk kaynagidir</b>. Frontend TypeScript tipleri bundan
/// otomatik uretilir; boylece backend'de degisen bir alan frontend'de <b>derleme
/// hatasi</b> olarak ortaya cikar, calisma zamaninda degil.
/// </para>
/// <para>
/// Belge ayrica <c>docs/api/openapi-v1.json</c> altinda depoda tutulur ve derleme
/// zamaninda yeniden uretilir. Amac, sozlesme degisikliginin PR farkinda
/// <b>okunabilir</b> olmasidir: kirici bir degisiklik gozden kacmaz.
/// </para>
/// </remarks>
public static class OpenApiRegistration
{
    /// <summary>Su anki API surumu (ADR-0010 §2).</summary>
    public const string ApiVersion = "v1";

    /// <summary>OpenAPI belgesinin adi; uretilen dosya adini da belirler.</summary>
    public const string DocumentName = ApiVersion;

    /// <summary>OpenAPI uretimini kaydeder.</summary>
    public static IServiceCollection AddHrmsOpenApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOpenApi(DocumentName, options =>
        {
            // XML belge yorumlarindan gelen satir sonlari kaynak dosyanin satir sonuna baglidir:
            // Windows calisma kopyasi CRLF, CI LF uretir ve depodaki belge surekli "degismis"
            // gorunurdu. Aciklamalar her platformda LF'ye getirilir.
            options.AddOperationTransformer((operation, _, _) =>
            {
                operation.Summary = NormalizeLineEndings(operation.Summary);
                operation.Description = NormalizeLineEndings(operation.Description);
                if (operation.Responses is not null)
                {
                    foreach (var response in operation.Responses.Values.OfType<OpenApiResponse>())
                    {
                        response.Description = NormalizeLineEndings(response.Description);
                    }
                }

                return Task.CompletedTask;
            });
            options.AddSchemaTransformer((schema, _, _) =>
            {
                schema.Description = NormalizeLineEndings(schema.Description);
                if (schema.Properties is not null)
                {
                    foreach (var property in schema.Properties.Values.OfType<OpenApiSchema>())
                    {
                        property.Description = NormalizeLineEndings(property.Description);
                    }
                }

                return Task.CompletedTask;
            });

            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "DSG-HRMS API",
                    Version = ApiVersion,
                    Description =
                        "Duzen Saglik Grubu Insan Kaynaklari Yonetim Sistemi API sozlesmesi. " +
                        "Bu belge otomatik uretilir; elle duzenlenmez.",
                    Contact = new OpenApiContact
                    {
                        Name = "Duzen Saglik Grubu - Bilgi Islem",
                    },
                };

                return Task.CompletedTask;
            });
        });

        return services;
    }

    private static string? NormalizeLineEndings(string? text) =>
        text?.Replace("\r\n", "\n", StringComparison.Ordinal);

    /// <summary>
    /// OpenAPI belgesini ve gelistirme arayuzunu yayimlar.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Swagger arayuzu yalnizca <b>gelistirme</b> ortaminda acilir. Uygulama yerel
    /// aga aciktir ve arayuz kimlik dogrulamasi istemez; uretimde acik birakilmasi,
    /// tum uc noktalari ve veri modelini kesfedilebilir hâle getirirdi.
    /// </para>
    /// <para>
    /// Belgenin kendisi (<c>/openapi/v1.json</c>) her ortamda yayimlanir: frontend
    /// derlemesi ve sozlesme denetimi bunu kullanir.
    /// </para>
    /// </remarks>
    public static WebApplication UseHrmsOpenApi(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapOpenApi();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint($"/openapi/{DocumentName}.json", $"DSG-HRMS API {ApiVersion}");
                options.DocumentTitle = "DSG-HRMS API";
            });
        }

        return app;
    }
}
