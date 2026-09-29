using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Application.Settings;
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
/// Periyot parametre deposundan okunur (<c>PRM-ENT-07</c>) ve beklerken dakikada bir
/// yeniden okunur: yonetici periyodu 120 dakikadan 5 dakikaya indirdiginde yeni deger
/// eski periyodun bitmesini beklemeden en gec 1 dakikada etkili olur (SYG-KMLK-075).
/// </para>
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
    /// <summary>Periyot beklenirken parametrenin yeniden okunma araligi.</summary>
    public static readonly TimeSpan PollInterval = TimeSpan.FromMinutes(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ISystemParameters _parameters;
    private readonly IDateTimeProvider _clock;
    private readonly LogoOptions _logo;
    private readonly PersonnelSyncTrigger _trigger;
    private readonly ILogger<PersonnelSyncWorker> _logger;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonnelSyncWorker(
        IServiceScopeFactory scopeFactory,
        ISystemParameters parameters,
        IDateTimeProvider clock,
        LogoOptions logo,
        PersonnelSyncTrigger trigger,
        ILogger<PersonnelSyncWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _parameters = parameters;
        _clock = clock;
        _logo = logo;
        _trigger = trigger;
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

        try
        {
            var trigger = SyncTrigger.Scheduled;
            while (!stoppingToken.IsCancellationRequested)
            {
                var startedAt = _clock.UtcNow;
                await RunOnceAsync(trigger, stoppingToken).ConfigureAwait(false);

                // Elle istenen calisma (SYG-KMLK-072) bekleyisi kisaltir; sonraki periyot o
                // calismanin basindan sayilir.
                trigger = await WaitForNextRunAsync(startedAt, stoppingToken).ConfigureAwait(false)
                    ? SyncTrigger.Manual
                    : SyncTrigger.Scheduled;
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Uygulama kapaniyor.
        }
    }

    /// <returns>Elle calisma istendiyse <c>true</c>; periyot dolduysa <c>false</c>.</returns>
    private async Task<bool> WaitForNextRunAsync(DateTimeOffset startedAt, CancellationToken stoppingToken)
    {
        while (true)
        {
            var interval = await ReadIntervalAsync(stoppingToken).ConfigureAwait(false);
            var remaining = startedAt + interval - _clock.UtcNow;

            if (remaining <= TimeSpan.Zero)
            {
                return false;
            }

            if (await _trigger.WaitAsync(remaining < PollInterval ? remaining : PollInterval, stoppingToken).ConfigureAwait(false))
            {
                return true;
            }
        }
    }

    private async Task<TimeSpan> ReadIntervalAsync(CancellationToken stoppingToken)
    {
        try
        {
            var minutes = await _parameters
                .GetIntegerAsync(ParameterCatalog.SyncIntervalMinutes, stoppingToken)
                .ConfigureAwait(false);
            return TimeSpan.FromMinutes(minutes);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
#pragma warning disable CA1031 // Parametre okunamazsa (HRMS veritabani kapali) zamanlayici DURMAMALIDIR.
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogIntervalUnavailable(_logger, ex);
            return TimeSpan.FromMinutes(int.Parse(ParameterCatalog.SyncIntervalMinutes.DefaultValue!, System.Globalization.CultureInfo.InvariantCulture));
        }
    }

    private async Task RunOnceAsync(SyncTrigger trigger, CancellationToken stoppingToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<PersonnelSyncService>();
            await service.RunAsync(trigger, stoppingToken).ConfigureAwait(false);
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

    [LoggerMessage(EventId = 3302, Level = LogLevel.Warning,
        Message = "Senkronizasyon periyodu (PRM-ENT-07) okunamadi; varsayilan periyot kullaniliyor.")]
    private static partial void LogIntervalUnavailable(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3301, Level = LogLevel.Error,
        Message = "Periyodik personel senkronizasyonu calistirilamadi; bir sonraki periyotta yeniden denenecek.")]
    private static partial void LogRunFailed(ILogger logger, Exception exception);
}
