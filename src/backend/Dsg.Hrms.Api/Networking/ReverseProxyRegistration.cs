using Microsoft.AspNetCore.HttpOverrides;
using IPNetwork = System.Net.IPNetwork;

namespace Dsg.Hrms.Api.Networking;

/// <summary>
/// Ters vekil (nginx) arkasinda gercek istemci IP'sinin okunmasi (#87).
/// </summary>
/// <remarks>
/// <para>
/// API, web konteynerindeki nginx'in arkasinda calisir. Bu ayar olmadan uygulamanin
/// gordugu IP her istekte nginx'in adresidir: denetim izindeki IP anlamsizlasir ve IP
/// basina hiz siniri (SYG-KMLK-059) tum kuruma tek bir sinir olarak uygulanir.
/// </para>
/// <para>
/// <c>X-Forwarded-For</c> yalnizca <see cref="ReverseProxyOptions.TrustedNetworks"/>
/// icinden gelen isteklerde dikkate alinir. Aksi halde herhangi bir istemci basligi
/// kendisi yazarak IP'sini degistirebilir ve hiz sinirini asabilirdi. Liste bossa
/// baslik hic dikkate alinmaz.
/// </para>
/// </remarks>
public static class ReverseProxyRegistration
{
    /// <summary>Ayarlari okur ve kaydeder.</summary>
    public static IServiceCollection AddHrmsReverseProxy(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var networks = ParseNetworks(configuration[$"{ReverseProxyOptions.SectionName}:{nameof(ReverseProxyOptions.TrustedNetworks)}"]);

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            // Tek vekil: nginx. Zincirdeki daha eski adresler istemcinin yazdigidir.
            options.ForwardLimit = 1;
            options.KnownIPNetworks.Clear();
            options.KnownProxies.Clear();

            foreach (var network in networks)
            {
                options.KnownIPNetworks.Add(network);
            }
        });

        return services;
    }

    /// <summary>
    /// Virgulle ayrilmis CIDR listesini okur (<c>172.16.0.0/12</c>).
    /// </summary>
    /// <exception cref="InvalidOperationException">Gecersiz bir ag varsa: acilista durulur (ADR-0008 §4).</exception>
    public static IReadOnlyList<IPNetwork> ParseNetworks(string? value)
    {
        var networks = new List<IPNetwork>();
        foreach (var item in (value ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!IPNetwork.TryParse(item, out var network))
            {
                throw new InvalidOperationException($"'{ReverseProxyOptions.SectionName}:{nameof(ReverseProxyOptions.TrustedNetworks)}' gecersiz bir ag iceriyor: '{item}'.");
            }

            networks.Add(network);
        }

        return networks;
    }
}

/// <summary>Ters vekil ayarlari.</summary>
public sealed class ReverseProxyOptions
{
    /// <summary>Yapilandirma bolumunun adi.</summary>
    public const string SectionName = "ReverseProxy";

    /// <summary>
    /// <c>X-Forwarded-For</c> basligina guvenilen vekil aglari, virgulle ayrilmis CIDR
    /// (Docker koprusu icin <c>172.16.0.0/12</c>). Bossa baslik dikkate alinmaz.
    /// </summary>
    public string? TrustedNetworks { get; init; }
}
