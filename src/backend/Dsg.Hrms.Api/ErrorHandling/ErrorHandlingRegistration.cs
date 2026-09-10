using Dsg.Hrms.Api.Logging;

namespace Dsg.Hrms.Api.ErrorHandling;

/// <summary>
/// Hata yonetiminin kaydi ve ara katman sirasi (ADR-0010 §5).
/// </summary>
/// <remarks>
/// Kurulum tek bir yerde toplanmistir; boylece hem uygulama hem testler <b>ayni</b>
/// yapilandirmayi calistirir. Test icin ayri bir kurulum yazilsaydi, testler gecerken
/// uretimde farkli davranan bir hata yolu olusabilirdi.
/// </remarks>
public static class ErrorHandlingRegistration
{
    /// <summary>Problem Details uretimini ve istisna isleyicisini kaydeder.</summary>
    public static IServiceCollection AddHrmsProblemDetails(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;

                // Kullanici hata ekraninda bu numarayi gorur ve destek talebinde
                // iletir; gunluk kaydiyla eslesme bunun uzerinden yapilir.
                context.ProblemDetails.Extensions["traceId"] =
                    CorrelationIdMiddleware.GetCorrelationId(context.HttpContext);

                // Cerceve, gelistirme ortaminda istisna ayrintisi ekleyebilir.
                // Bu alan yanittan HER ORTAMDA cikarilir (ADR-0010 §5).
                context.ProblemDetails.Extensions.Remove("exception");
            });

        services.AddExceptionHandler<ProblemDetailsExceptionHandler>();

        return services;
    }

    /// <summary>
    /// Hata ara katmanlarini boru hattina ekler.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Sira onemlidir.</b> Istisna isleyici, istek gunlugunden <i>sonra</i> gelir:
    /// boylece istisna Serilog'un istek ara katmanina ulasmaz ve ayni hata iki kez
    /// kaydedilmez. Istek yine de dogru durum koduyla gunluge duser.
    /// </para>
    /// <para>
    /// <c>UseStatusCodePages</c>, govdesiz donen durum kodlarini da (ornegin
    /// eslesmeyen bir yol icin <c>404</c>) Problem Details bicimine cevirir; boylece
    /// frontend tek bir hata bicimi bekler.
    /// </para>
    /// </remarks>
    public static IApplicationBuilder UseHrmsErrorHandling(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseExceptionHandler();
        app.UseStatusCodePages();

        return app;
    }
}
