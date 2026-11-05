using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Notifications;
using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Application.Tests.Settings;
using Dsg.Hrms.Domain.Notifications;
using Dsg.Hrms.Domain.Personnel.Sync;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Dsg.Hrms.Application.Tests.Personnel.Sync;

/// <summary>Senkronizasyon hatasi ve sema sapmasi bildirimi (SYG-PERS-012).</summary>
public sealed class PersonnelSyncAlertsTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 5, 9, 0, 0, TimeSpan.Zero);

    private readonly IPersonnelSyncAlertStore _store = Substitute.For<IPersonnelSyncAlertStore>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly FakeSystemParameters _parameters = new();
    private readonly List<InboxItem> _added = [];
    private readonly INotificationDispatch _dispatch = Substitute.For<INotificationDispatch>();
    private readonly List<OutboundMessage> _sent = [];
    private PersonnelSyncAlertOptions _options = new(new Uri("https://ik.duzen.com.tr/personnel/sync"));

    public PersonnelSyncAlertsTests()
    {
        _clock.UtcNow.Returns(Now);
        _store.AddAsync(Arg.Do<IReadOnlyCollection<InboxItem>>(_added.AddRange), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _dispatch.TryEnqueue(Arg.Do<OutboundMessage>(_sent.Add)).Returns(true);
    }

    private static SyncAlertRecipient Recipient(long userId, string? email = "yonetici@duzen.com.tr") => new(userId, userId * 10, email);

    private PersonnelSyncAlerts Create() => new(_store, _parameters, _dispatch, _options, _clock, NullLogger<PersonnelSyncAlerts>.Instance);

    private static PersonnelSyncRun Failed(SyncFailureReason reason)
    {
        var run = PersonnelSyncRun.Start(SyncTrigger.Scheduled, Now);
        run.Fail(reason, Now);
        return run;
    }

    [Theory]
    [InlineData(SyncFailureReason.SchemaDrift, InboxItemKind.SyncSchemaDrift)]
    [InlineData(SyncFailureReason.SourceUnavailable, InboxItemKind.SyncFailed)]
    [InlineData(SyncFailureReason.SourceWritable, InboxItemKind.SyncFailed)]
    [InlineData(SyncFailureReason.PersistenceFailed, InboxItemKind.SyncFailed)]
    [InlineData(SyncFailureReason.Unexpected, InboxItemKind.SyncFailed)]
    public async Task Failed_run_is_written_to_every_recipient_with_its_kind(SyncFailureReason reason, InboxItemKind kind)
    {
        _store.ListRecipientsAsync(Arg.Any<IReadOnlyCollection<string>>(), kind, Arg.Any<CancellationToken>()).Returns([Recipient(7), Recipient(9)]);
        var run = Failed(reason);

        await Create().NotifyAsync(run, CancellationToken.None);

        _added.Select(i => (i.RecipientUserId, i.Kind, i.SubjectId, i.CreatedAt)).ShouldBe(
        [
            (7L, kind, run.PublicId, Now),
            (9L, kind, run.PublicId, Now),
        ]);
        _added.ShouldAllBe(i => i.ReadAt == null);
        _sent.Select(m => (m.PersonId, m.Purpose, m.Channel)).ShouldBe(
        [
            (70L, NotificationPurpose.SyncAlert, NotificationChannel.Email),
            (90L, NotificationPurpose.SyncAlert, NotificationChannel.Email),
        ]);
    }

    [Fact]
    public async Task Email_goes_to_each_recipient_with_an_address_and_carries_no_personal_data()
    {
        // KR-134: e-posta, uygulama ici bildirimi alan ve kurumsal adresi olan kullaniciya gider.
        _store.ListRecipientsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<InboxItemKind>(), Arg.Any<CancellationToken>())
            .Returns([Recipient(7, "bir@duzen.com.tr"), Recipient(8, null), Recipient(9, "iki@duzen.com.tr")]);

        await Create().NotifyAsync(Failed(SyncFailureReason.SchemaDrift), CancellationToken.None);

        _added.Count.ShouldBe(3);
        _sent.Select(m => m.Recipient).ShouldBe(["bir@duzen.com.tr", "iki@duzen.com.tr"]);
        _sent.ShouldAllBe(m => m.Subject == PersonnelSyncAlerts.SchemaDriftSubject);
        _sent[0].MessageBody.ShouldContain("LOGO tablolarının yapısı beklenenden farklı");
        _sent[0].MessageBody.ShouldContain("https://ik.duzen.com.tr/personnel/sync");

        // Govde alicidan bagimsizdir: kisiye ozgu bilgi tasimaz.
        _sent[1].MessageBody.ShouldBe(_sent[0].MessageBody);
        _sent[0].MessageBody.ShouldNotContain("bir@");
    }

    [Fact]
    public async Task Email_without_public_address_names_the_screen_instead_of_a_link()
    {
        _options = new PersonnelSyncAlertOptions(null);
        _store.ListRecipientsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<InboxItemKind>(), Arg.Any<CancellationToken>())
            .Returns([Recipient(7)]);

        await Create().NotifyAsync(Failed(SyncFailureReason.SourceUnavailable), CancellationToken.None);

        var message = _sent.ShouldHaveSingleItem();
        message.Subject.ShouldBe(PersonnelSyncAlerts.FailedSubject);
        message.MessageBody.ShouldContain("LOGO'ya bağlanılamadı");
        message.MessageBody.ShouldContain("Personel → Senkronizasyon");
        message.MessageBody.ShouldNotContain("http");
    }

    [Fact]
    public async Task Recipients_come_from_the_alert_role_parameter()
    {
        // PRM-BLD-01: alici tek kisi degil roldur; varsayilan Sistem Yoneticisi.
        await Create().NotifyAsync(Failed(SyncFailureReason.SchemaDrift), CancellationToken.None);
        await _store.Received(1).ListRecipientsAsync(
            Arg.Is<IReadOnlyCollection<string>>(r => r.SequenceEqual(new[] { "system-administrator" })),
            InboxItemKind.SyncSchemaDrift,
            Arg.Any<CancellationToken>());

        _store.ClearReceivedCalls();
        _parameters.With(ParameterCatalog.AlertRecipientRoles, "system-administrator,hr-identity-operations");
        await Create().NotifyAsync(Failed(SyncFailureReason.SchemaDrift), CancellationToken.None);
        await _store.Received(1).ListRecipientsAsync(
            Arg.Is<IReadOnlyCollection<string>>(r => r.SequenceEqual(new[] { "system-administrator", "hr-identity-operations" })),
            InboxItemKind.SyncSchemaDrift,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Run_that_did_not_fail_writes_nothing()
    {
        var succeeded = PersonnelSyncRun.Start(SyncTrigger.Scheduled, Now);
        succeeded.Complete(new SyncCounts(1, 0, 0, 0, 0, 0, 0, 0), [], Now);
        var partial = PersonnelSyncRun.Start(SyncTrigger.Scheduled, Now);
        partial.CompletePartially(new SyncCounts(1, 0, 0, 0, 0, 0, 0, 0), [], SyncFailureReason.SourceUnavailable, Now);
        var running = PersonnelSyncRun.Start(SyncTrigger.Manual, Now);

        foreach (var run in new[] { succeeded, partial, running })
        {
            await Create().NotifyAsync(run, CancellationToken.None);
        }

        await _store.DidNotReceiveWithAnyArgs().ListRecipientsAsync(default!, default, default);
        await _store.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
        _dispatch.DidNotReceiveWithAnyArgs().TryEnqueue(default!);
    }

    [Fact]
    public async Task No_recipient_writes_nothing()
    {
        // Rolde aktif kullanici yok ya da hepsinin ayni turde okunmamis bildirimi var.
        _store.ListRecipientsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<InboxItemKind>(), Arg.Any<CancellationToken>()).Returns(Array.Empty<SyncAlertRecipient>());

        await Create().NotifyAsync(Failed(SyncFailureReason.SourceUnavailable), CancellationToken.None);

        await _store.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Store_failure_does_not_escape()
    {
        // Bildirim yazilamamasi calismanin sonucunu degistirmez.
        _store.ListRecipientsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<InboxItemKind>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("db"));

        await Should.NotThrowAsync(() => Create().NotifyAsync(Failed(SyncFailureReason.SourceUnavailable), CancellationToken.None));
    }
}
