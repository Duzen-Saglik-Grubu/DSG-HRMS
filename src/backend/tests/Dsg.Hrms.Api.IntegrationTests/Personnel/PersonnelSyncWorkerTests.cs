using Dsg.Hrms.Api.IntegrationTests.Settings;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Infrastructure.Logo;
using Dsg.Hrms.Infrastructure.Personnel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Dsg.Hrms.Api.IntegrationTests.Personnel;

/// <summary>
/// Periyodik senkronizasyon calistiricisinin davranisi (SYG-KMLK-004).
/// </summary>
public sealed class PersonnelSyncWorkerTests
{
    private readonly ILogoPersonnelSource _source = Substitute.For<ILogoPersonnelSource>();
    private readonly IPersonnelSyncStore _store = Substitute.For<IPersonnelSyncStore>();

    private PersonnelSyncWorker CreateWorker(bool logoConfigured)
    {
        var services = new ServiceCollection();
        services.AddSingleton(_source);
        services.AddSingleton(_store);
        services.AddSingleton(Substitute.For<IDateTimeProvider>());
        services.AddSingleton(new PersonnelSyncOptions());
        services.AddSingleton<ISystemParameters>(new FakeSystemParameters());
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddScoped<PersonnelSyncService>();
        var provider = services.BuildServiceProvider();

        return new PersonnelSyncWorker(
            provider.GetRequiredService<IServiceScopeFactory>(),
            provider.GetRequiredService<ISystemParameters>(),
            provider.GetRequiredService<IDateTimeProvider>(),
            new LogoOptions { ConnectionString = logoConfigured ? "Server=logo;Database=BORDRO" : null },
            NullLogger<PersonnelSyncWorker>.Instance);
    }

    [Fact]
    public async Task Without_logo_connection_the_worker_does_nothing()
    {
        using var worker = CreateWorker(logoConfigured: false);

        await worker.StartAsync(CancellationToken.None);
        await worker.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));

        await _store.DidNotReceiveWithAnyArgs().TryOpenSessionAsync(default);
    }

    [Fact]
    public async Task With_logo_connection_the_first_run_starts_immediately()
    {
        // Yeniden baslatmadan sonra veri 15 dakika eski kalmamalidir.
        var started = new TaskCompletionSource();
        _store.TryOpenSessionAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            started.TrySetResult();
            return Task.FromResult<IPersonnelSyncSession?>(null); // baska calisma suruyor gibi
        });
        using var worker = CreateWorker(logoConfigured: true);

        await worker.StartAsync(CancellationToken.None);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await worker.StopAsync(CancellationToken.None);

        await _store.Received(1).TryOpenSessionAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_failing_run_does_not_stop_the_worker()
    {
        // Tek bir calismanin hatasi zamanlayiciyi oldurseydi, senkronizasyon sessizce
        // dururdu ve bir sonraki periyotta yeniden denenmezdi.
        var attempted = new TaskCompletionSource();
        _store.TryOpenSessionAsync(Arg.Any<CancellationToken>()).Returns(_ =>
        {
            attempted.TrySetResult();
            return Task.FromException<IPersonnelSyncSession?>(new InvalidOperationException("HRMS veritabani kapali"));
        });
        using var worker = CreateWorker(logoConfigured: true);

        await worker.StartAsync(CancellationToken.None);
        await attempted.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await Task.Delay(200);

        worker.ExecuteTask!.IsCompleted.ShouldBeFalse();
        await worker.StopAsync(CancellationToken.None);
    }
}
