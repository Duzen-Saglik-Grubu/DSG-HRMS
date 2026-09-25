using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Domain.Personnel.Sync;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using static Dsg.Hrms.Application.Tests.Personnel.Sync.TestData;

namespace Dsg.Hrms.Application.Tests.Personnel.Sync;

/// <summary>
/// Senkronizasyon akisinin hata davranislari (SYG-KMLK-004, 005, 010, 011).
/// </summary>
public sealed class PersonnelSyncServiceTests
{
    private readonly ILogoPersonnelSource _source = Substitute.For<ILogoPersonnelSource>();
    private readonly IPersonnelSyncStore _store = Substitute.For<IPersonnelSyncStore>();
    private readonly IPersonnelSyncSession _session = Substitute.For<IPersonnelSyncSession>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly List<(SyncStatus Status, SyncFailureReason? Reason)> _savedRuns = [];

    public PersonnelSyncServiceTests()
    {
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 9, 26, 9, 0, 0, TimeSpan.Zero));
        _clock.Today.Returns(Today);

        _store.TryOpenSessionAsync(Arg.Any<CancellationToken>()).Returns(_session);
        _store.SaveRunAsync(Arg.Do<PersonnelSyncRun>(run => _savedRuns.Add((run.Status, run.FailureReason))), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _session.LoadAsync(Arg.Any<CancellationToken>()).Returns(new PersonnelSnapshot([], [], []));

        _source.VerifyReadOnlyAccessAsync(Arg.Any<CancellationToken>()).Returns(new LogoAccessCheckResult(true, []));
        _source.VerifySchemaAsync(Arg.Any<CancellationToken>()).Returns(new LogoSchemaCheckResult(true, []));
        _source.GetAllAsync(Arg.Any<CancellationToken>()).Returns([Card("00001", NationalId(1))]);
    }

    private PersonnelSyncService CreateService() =>
        new(_source, _store, _clock, new PersonnelSyncOptions(), NullLogger<PersonnelSyncService>.Instance);

    [Fact]
    public async Task Successful_run_applies_changes_and_records_the_run()
    {
        var result = await CreateService().RunAsync(SyncTrigger.Scheduled, CancellationToken.None);

        result.Status.ShouldBe(SyncStatus.Succeeded);
        result.RunId.ShouldNotBeNull();
        _session.Received(1).Add(
            Arg.Any<IEnumerable<Domain.Organization.Company>>(),
            Arg.Is<IEnumerable<Domain.Personnel.Person>>(p => p.Count() == 1),
            Arg.Is<IEnumerable<Domain.Personnel.Employment>>(e => e.Count() == 1));
        await _session.Received(1).CommitAsync(Arg.Any<CancellationToken>());

        // Once "suruyor", sonra sonuc yazilir.
        _savedRuns.Select(r => r.Status).ShouldBe([SyncStatus.Running, SyncStatus.Succeeded]);
    }

    [Fact]
    public async Task Concurrent_run_is_skipped_without_touching_logo()
    {
        _store.TryOpenSessionAsync(Arg.Any<CancellationToken>()).Returns((IPersonnelSyncSession?)null);

        var result = await CreateService().RunAsync(SyncTrigger.Manual, CancellationToken.None);

        result.ShouldBe(PersonnelSyncResult.AlreadyRunning);
        await _source.DidNotReceiveWithAnyArgs().GetAllAsync(default);
        _savedRuns.ShouldBeEmpty();
    }

    [Fact]
    public async Task Writable_logo_session_is_refused_before_any_read()
    {
        // KR-004: oturumda yazma yetkisi bulunmamalidir. Bu bir guvenlik olayidir.
        _source.VerifyReadOnlyAccessAsync(Arg.Any<CancellationToken>())
            .Returns(new LogoAccessCheckResult(false, ["LH_001_PERSON:UPDATE"]));

        var result = await CreateService().RunAsync(SyncTrigger.Scheduled, CancellationToken.None);

        result.Status.ShouldBe(SyncStatus.Failed);
        _savedRuns.Last().Reason.ShouldBe(SyncFailureReason.SourceWritable);
        await _source.DidNotReceiveWithAnyArgs().VerifySchemaAsync(default);
        await _source.DidNotReceiveWithAnyArgs().GetAllAsync(default);
        await _session.DidNotReceiveWithAnyArgs().CommitAsync(default);
    }

    [Fact]
    public async Task Schema_drift_stops_the_run_before_reading()
    {
        _source.VerifySchemaAsync(Arg.Any<CancellationToken>())
            .Returns(new LogoSchemaCheckResult(false, ["LH_001_PERSON.TTFNO: bulunamadi"]));

        var result = await CreateService().RunAsync(SyncTrigger.Scheduled, CancellationToken.None);

        result.Status.ShouldBe(SyncStatus.Failed);
        _savedRuns.Last().Reason.ShouldBe(SyncFailureReason.SchemaDrift);
        await _source.DidNotReceiveWithAnyArgs().GetAllAsync(default);
    }

    [Fact]
    public async Task Unreachable_logo_is_recorded_and_nothing_is_committed()
    {
        // SYG-KMLK-011: sistem son basarili anlik goruntuyle calismaya devam eder.
        _source.GetAllAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new LogoUnavailableException());

        var result = await CreateService().RunAsync(SyncTrigger.Scheduled, CancellationToken.None);

        result.Status.ShouldBe(SyncStatus.Failed);
        _savedRuns.Last().Reason.ShouldBe(SyncFailureReason.SourceUnavailable);
        await _session.DidNotReceiveWithAnyArgs().CommitAsync(default);
    }

    [Fact]
    public async Task Persistence_failure_is_recorded()
    {
        _session.CommitAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new SyncPersistenceException());

        var result = await CreateService().RunAsync(SyncTrigger.Scheduled, CancellationToken.None);

        result.Status.ShouldBe(SyncStatus.Failed);
        _savedRuns.Last().Reason.ShouldBe(SyncFailureReason.PersistenceFailed);
    }

    [Fact]
    public async Task Unexpected_failure_is_recorded_instead_of_escaping()
    {
        _session.LoadAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new InvalidOperationException("beklenmeyen"));

        var result = await CreateService().RunAsync(SyncTrigger.Scheduled, CancellationToken.None);

        result.Status.ShouldBe(SyncStatus.Failed);
        _savedRuns.Last().Reason.ShouldBe(SyncFailureReason.Unexpected);
    }

    [Fact]
    public async Task Cancelled_run_is_closed_and_not_left_running()
    {
        // "Suruyor" olarak asili kalan bir calisma, sagligi yanlis gosterirdi.
        using var cts = new CancellationTokenSource();
        _source.GetAllAsync(Arg.Any<CancellationToken>()).Returns(async _ =>
        {
            await cts.CancelAsync();
            throw new OperationCanceledException(cts.Token);
        });

        var result = await CreateService().RunAsync(SyncTrigger.Scheduled, cts.Token);

        result.Status.ShouldBe(SyncStatus.Failed);
        _savedRuns.Last().Status.ShouldBe(SyncStatus.Failed);
    }

    [Fact]
    public async Task Session_is_always_disposed()
    {
        _source.GetAllAsync(Arg.Any<CancellationToken>()).ThrowsAsync(new LogoUnavailableException());

        await CreateService().RunAsync(SyncTrigger.Scheduled, CancellationToken.None);

        // Onaylanmamis islemin geri alinmasi ve kilidin birakilmasi buna baglidir.
        await _session.Received(1).DisposeAsync();
    }
}
