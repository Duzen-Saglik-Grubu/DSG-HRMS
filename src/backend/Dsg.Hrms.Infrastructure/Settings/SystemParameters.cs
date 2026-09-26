using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Settings;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Dsg.Hrms.Infrastructure.Settings;

/// <summary>
/// <see cref="ISystemParameters"/> uygulamasi: veritabani, yapilandirma, varsayilan.
/// </summary>
/// <remarks>
/// <para>
/// Tekil (singleton) omurludur ve veritabani satirlarini <see cref="CacheDuration"/>
/// boyunca onbellekte tutar. Parametre her okundugunda sorgu atilsaydi, giris gibi sik
/// cagrilan islemler her istekte birkac ek sorgu uretirdi.
/// </para>
/// <para>
/// Uygulamanin birden fazla ornegi calisiyorsa degisikligi yapan ornek onbellegini
/// hemen bosaltir; digerleri en gec <see cref="CacheDuration"/> sonra yeni degeri okur
/// (SYG-KMLK-075: "en gec 1 dakika").
/// </para>
/// </remarks>
public sealed partial class SystemParameters : ISystemParameters, IDisposable
{
    /// <summary>Onbellek suresi.</summary>
    public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ISecretProtector _protector;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<SystemParameters> _logger;
    private readonly SemaphoreSlim _loadLock = new(1, 1);

    private volatile CachedRows? _cache;

    /// <summary>Yeni ornek olusturur.</summary>
    public SystemParameters(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ISecretProtector protector,
        IDateTimeProvider clock,
        ILogger<SystemParameters> logger)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _protector = protector;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>
    /// Yapilandirmadaki parametre degerlerini dogrular. Uygulama acilirken cagrilir.
    /// </summary>
    /// <exception cref="InvalidOperationException">Gecersiz deger varsa; ileti degeri icermez.</exception>
    /// <remarks>
    /// Yapilandirmadaki gecersiz bir deger (orn. periyot 0) ilk kullanildigi anda degil,
    /// uygulama acilirken fark edilmelidir (ADR-0008 §4).
    /// </remarks>
    public static void ValidateConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var errors = ParameterCatalog.All
            .Where(p => !p.IsSecret)
            .Select(p => (p, value: configuration[p.ConfigurationKey]))
            .Where(x => !string.IsNullOrWhiteSpace(x.value))
            .Select(x => (x.p, result: x.p.Validate(x.value)))
            .Where(x => !x.result.IsValid)
            .Select(x => $"  - {x.p.ConfigurationKey} ({x.p.Key}): {x.result.Error}")
            .ToList();

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Parametre yapilandirmasi gecersiz:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    /// <inheritdoc />
    public async ValueTask<string?> GetAsync(ParameterDefinition parameter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        var rows = await GetRowsAsync(cancellationToken).ConfigureAwait(false);
        return Resolve(parameter, rows, revealSecret: true).Value;
    }

    /// <summary>Onbellegi bosaltir; bir sonraki okuma veritabanina gider.</summary>
    public void Invalidate() => _cache = null;

    /// <inheritdoc />
    public void Dispose() => _loadLock.Dispose();

    /// <summary>
    /// Parametreyi verilen satirlara gore cozer.
    /// </summary>
    /// <param name="parameter">Katalog tanimi.</param>
    /// <param name="rows">Veritabani satirlari (kimlige gore).</param>
    /// <param name="revealSecret">
    /// <c>false</c> ise sir parametrenin degeri cozulmez ve <c>null</c> doner; kaynak yine belirtilir.
    /// </param>
    internal ResolvedParameter Resolve(
        ParameterDefinition parameter,
        IReadOnlyDictionary<string, SystemParameterRow> rows,
        bool revealSecret)
    {
        if (rows.TryGetValue(parameter.Key, out var row))
        {
            if (parameter.IsSecret && row.ProtectedValue is not null)
            {
                return new ResolvedParameter(
                    revealSecret ? _protector.Unprotect(row.ProtectedValue, parameter.Key) : null,
                    ParameterSource.Database);
            }

            if (!parameter.IsSecret && row.Value is not null)
            {
                // Katalog araligi sonradan daralmis olabilir: gecersizlesen deger
                // kullanilmaz, bir sonraki kaynaga gecilir ve uyari yazilir.
                var validation = parameter.Validate(row.Value);
                if (validation.IsValid)
                {
                    return new ResolvedParameter(validation.CanonicalValue, ParameterSource.Database);
                }

                LogInvalidStoredValue(_logger, parameter.Key);
            }
        }

        var configured = _configuration[parameter.ConfigurationKey];
        if (!string.IsNullOrWhiteSpace(configured))
        {
            if (parameter.IsSecret)
            {
                return new ResolvedParameter(revealSecret ? configured : null, ParameterSource.Configuration);
            }

            // Acilista dogrulandi (ValidateConfiguration).
            return new ResolvedParameter(parameter.Validate(configured).CanonicalValue, ParameterSource.Configuration);
        }

        return parameter.DefaultValue is not null
            ? new ResolvedParameter(parameter.DefaultValue, ParameterSource.Default)
            : new ResolvedParameter(null, ParameterSource.None);
    }

    /// <summary>Satirlari veritabanindan okur (onbellek kullanmaz).</summary>
    internal static async Task<IReadOnlyDictionary<string, SystemParameterRow>> LoadRowsAsync(
        HrmsDbContext context,
        CancellationToken cancellationToken) =>
        await context.Set<SystemParameter>()
            .AsNoTracking()
            .ToDictionaryAsync(
                p => p.Key,
                p => new SystemParameterRow(p.Value, p.ProtectedValue),
                StringComparer.Ordinal,
                cancellationToken)
            .ConfigureAwait(false);

    private async ValueTask<IReadOnlyDictionary<string, SystemParameterRow>> GetRowsAsync(CancellationToken cancellationToken)
    {
        var cache = _cache;
        if (cache is not null && _clock.UtcNow - cache.LoadedAt < CacheDuration)
        {
            return cache.Rows;
        }

        await _loadLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Kilidi beklerken baska bir cagri yuklemis olabilir.
            cache = _cache;
            if (cache is not null && _clock.UtcNow - cache.LoadedAt < CacheDuration)
            {
                return cache.Rows;
            }

            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<HrmsDbContext>();
            var rows = await LoadRowsAsync(context, cancellationToken).ConfigureAwait(false);

            _cache = new CachedRows(rows, _clock.UtcNow);
            return rows;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    [LoggerMessage(EventId = 3400, Level = LogLevel.Warning,
        Message = "{Key} parametresinin kayitli degeri katalogdaki kurala uymuyor; yapilandirma veya varsayilan deger kullaniliyor.")]
    private static partial void LogInvalidStoredValue(ILogger logger, string key);

    private sealed record CachedRows(IReadOnlyDictionary<string, SystemParameterRow> Rows, DateTimeOffset LoadedAt);
}

/// <summary>Veritabanindaki parametre satirinin degerleri.</summary>
/// <param name="Value">Sir olmayan deger.</param>
/// <param name="ProtectedValue">Sifreli sir deger.</param>
internal sealed record SystemParameterRow(string? Value, string? ProtectedValue);

/// <summary>Cozulmus parametre.</summary>
/// <param name="Value">Deger; yoksa veya sir gizlendiyse <c>null</c>.</param>
/// <param name="Source">Kaynak.</param>
internal sealed record ResolvedParameter(string? Value, ParameterSource Source);
