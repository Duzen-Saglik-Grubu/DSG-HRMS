using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Domain.Personnel.Sync;
using Dsg.Hrms.Infrastructure.Logo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Dsg.Hrms.Infrastructure.Personnel;

/// <summary>
/// Periyodik LOGO senkronizasyonu (<c>KR-007</c>: 15 dakikada bir, SYG-KMLK-004).
/// </summary>
/// <remarks>
/// <para>
/// Uygulama acilir acilmaz bir calisma yapar, sonra periyotla devam eder. Boylece
/// yeniden baslatmadan sonra veri 15 dakika eski kalmaz.
/// </para>
/// <para>
/// Uygulamanin birden fazla ornegi calisiyorsa her biri zamanlayiciyi calistirir;
/// es zamanli calismayi veritabani kilidi engeller (<see cref="PersonnelSyncStore.LockKey"/>).
/// </para>
/// <para>
/// LOGO baglantisi tanimli degilse hicbir sey yapmaz.
/// </para>
/// </remarks>
public sealed partial class PersonnelSyncWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly PersonnelSyncOptions _options;
    private readonly LogoOptions _logo;
    private readonly ILogger<PersonnelSyncWorker> _logger;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonnelSyncWorker(
        IServiceScopeFactory scopeFactory,
        PersonnelSyncOptions options,
        LogoOptions logo,
        ILogger<PersonnelSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logo = logo;
        _logger = logger;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_logo.IsConfigured)
        {
            LogDisabled(_logger);
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.IntervalMinutes));

        do
        {
            await RunOnceAsync(stoppingToken).ConfigureAwait(false);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<PersonnelSyncService>();
            await service.RunAsync(SyncTrigger.Scheduled, stoppingToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Uygulama kapaniyor.
        }
#pragma warning disable CA1031 // Tek bir calismanin hatasi zamanlayiciyi DURDURMAMALIDIR.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            // Servis hatalari kendisi kaydeder; buraya yalnizca kilit alinamamasi gibi
            // calisma kaydi olusmadan once olan hatalar duser (orn. HRMS veritabani kapali).
            LogRunFailed(_logger, ex);
        }
    }

    [LoggerMessage(EventId = 3300, Level = LogLevel.Warning,
        Message = "LOGO baglantisi tanimli degil (Logo:ConnectionString); personel senkronizasyonu devre disi.")]
    private static partial void LogDisabled(ILogger logger);

    [LoggerMessage(EventId = 3301, Level = LogLevel.Error,
        Message = "Periyodik personel senkronizasyonu calistirilamadi; bir sonraki periyotta yeniden denenecek.")]
    private static partial void LogRunFailed(ILogger logger, Exception exception);
}
