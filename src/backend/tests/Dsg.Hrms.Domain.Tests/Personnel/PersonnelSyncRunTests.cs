using Dsg.Hrms.Domain.Personnel.Sync;

namespace Dsg.Hrms.Domain.Tests.Personnel;

public sealed class PersonnelSyncRunTests
{
    private static readonly DateTimeOffset Started = new(2026, 9, 26, 9, 0, 0, TimeSpan.Zero);
    private static readonly SyncCounts Counts = new(10, 1, 2, 3, 4, 5, 1, 1);

    [Fact]
    public void New_run_is_running()
    {
        var run = PersonnelSyncRun.Start(SyncTrigger.Manual, Started);

        run.Status.ShouldBe(SyncStatus.Running);
        run.Trigger.ShouldBe(SyncTrigger.Manual);
        run.StartedAt.ShouldBe(Started);
        run.FinishedAt.ShouldBeNull();
        run.PublicId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Run_without_warnings_succeeds_and_records_counts()
    {
        var run = PersonnelSyncRun.Start(SyncTrigger.Scheduled, Started);

        run.Complete(Counts, [], Started.AddSeconds(30));

        run.Status.ShouldBe(SyncStatus.Succeeded);
        run.RecordsRead.ShouldBe(10);
        run.RecordsSkipped.ShouldBe(1);
        run.PersonsCreated.ShouldBe(2);
        run.PersonsUpdated.ShouldBe(3);
        run.EmploymentsCreated.ShouldBe(4);
        run.EmploymentsUpdated.ShouldBe(5);
        run.EmploymentsDeactivated.ShouldBe(1);
        run.CompaniesChanged.ShouldBe(1);
        run.FinishedAt.ShouldBe(Started.AddSeconds(30));
        run.FailureReason.ShouldBeNull();
    }

    [Fact]
    public void Run_with_warnings_is_completed_with_warnings()
    {
        var run = PersonnelSyncRun.Start(SyncTrigger.Scheduled, Started);
        var warning = PersonnelSyncWarning.Create(SyncWarningCode.MissingNationalId, "00123", "Kartta TCKN yok.");

        run.Complete(Counts, [warning], Started.AddSeconds(30));

        run.Status.ShouldBe(SyncStatus.CompletedWithWarnings);
        run.WarningCount.ShouldBe(1);
        run.Warnings.Single().RegistryCode.ShouldBe("00123");
        run.Warnings.Single().Code.ShouldBe(SyncWarningCode.MissingNationalId);
        run.Warnings.Single().Detail.ShouldBe("Kartta TCKN yok.");
    }

    [Fact]
    public void Failed_run_records_the_reason()
    {
        var run = PersonnelSyncRun.Start(SyncTrigger.Scheduled, Started);

        run.Fail(SyncFailureReason.SourceUnavailable, Started.AddSeconds(5));

        run.Status.ShouldBe(SyncStatus.Failed);
        run.FailureReason.ShouldBe(SyncFailureReason.SourceUnavailable);
        run.FinishedAt.ShouldBe(Started.AddSeconds(5));
    }

    [Fact]
    public void Closed_run_cannot_be_closed_again()
    {
        var run = PersonnelSyncRun.Start(SyncTrigger.Scheduled, Started);
        run.Complete(Counts, [], Started);

        Should.Throw<InvalidOperationException>(() => run.Fail(SyncFailureReason.Unexpected, Started));
        Should.Throw<InvalidOperationException>(() => run.Complete(Counts, [], Started));
    }
}
