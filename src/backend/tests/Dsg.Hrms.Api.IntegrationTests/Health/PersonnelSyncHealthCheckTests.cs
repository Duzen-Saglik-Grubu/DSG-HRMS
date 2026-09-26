using Dsg.Hrms.Api.Health;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Domain.Personnel.Sync;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;

namespace Dsg.Hrms.Api.IntegrationTests.Health;

/// <summary>
/// Senkronizasyon sagliginin durum eslemesi (SYG-KMLK-010).
/// </summary>
public sealed class PersonnelSyncHealthCheckTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 26, 12, 0, 0, TimeSpan.Zero);

    private readonly IPersonnelSyncStatus _status = Substitute.For<IPersonnelSyncStatus>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    public PersonnelSyncHealthCheckTests()
    {
        _clock.UtcNow.Returns(Now);
        _status.IsEnabled.Returns(true);
    }

    private Task<HealthCheckResult> CheckAsync() =>
        new PersonnelSyncHealthCheck(_status, _clock, new PersonnelSyncOptions { IntervalMinutes = 15 })
            .CheckHealthAsync(new HealthCheckContext());

    private void LastRun(SyncStatus status, int minutesAgo, SyncFailureReason? reason = null) =>
        _status.GetLastRunAsync(Arg.Any<CancellationToken>()).Returns(
            new PersonnelSyncRunSummary(status, Now.AddMinutes(-minutesAgo - 1), Now.AddMinutes(-minutesAgo), reason));

    [Fact]
    public async Task Disabled_sync_is_healthy_because_it_is_a_deliberate_choice()
    {
        _status.IsEnabled.Returns(false);

        (await CheckAsync()).Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task No_run_yet_is_degraded()
    {
        _status.GetLastRunAsync(Arg.Any<CancellationToken>()).Returns((PersonnelSyncRunSummary?)null);

        (await CheckAsync()).Status.ShouldBe(HealthStatus.Degraded);
    }

    [Theory]
    [InlineData(SyncStatus.Succeeded)]
    [InlineData(SyncStatus.CompletedWithWarnings)]
    public async Task Recent_completed_run_is_healthy(SyncStatus status)
    {
        // Veri kalitesi uyarilari sistemin sagligini bozmaz; IK'nin duzeltecegi veridir.
        LastRun(status, minutesAgo: 10);

        (await CheckAsync()).Status.ShouldBe(HealthStatus.Healthy);
    }

    [Fact]
    public async Task Failed_run_is_unhealthy_and_names_the_reason()
    {
        LastRun(SyncStatus.Failed, minutesAgo: 5, SyncFailureReason.SourceWritable);

        var result = await CheckAsync();

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        result.Description.ShouldNotBeNull();
        result.Description.ShouldContain(nameof(SyncFailureReason.SourceWritable));
    }

    [Fact]
    public async Task Run_older_than_three_intervals_is_degraded()
    {
        LastRun(SyncStatus.Succeeded, minutesAgo: 46);

        (await CheckAsync()).Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task Run_within_three_intervals_is_healthy()
    {
        LastRun(SyncStatus.Succeeded, minutesAgo: 44);

        (await CheckAsync()).Status.ShouldBe(HealthStatus.Healthy);
    }
}
