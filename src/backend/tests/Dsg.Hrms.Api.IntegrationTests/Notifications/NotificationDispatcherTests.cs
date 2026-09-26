using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Notifications;
using Dsg.Hrms.Domain.Notifications;
using Dsg.Hrms.Infrastructure.Data;
using Dsg.Hrms.Infrastructure.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace Dsg.Hrms.Api.IntegrationTests.Notifications;

/// <summary>
/// Dagitici: gonderim kipleri, yeniden deneme ve icerik tasimayan gonderim kaydi
/// (ADR-0012 §5, §6; SYG-KMLK-026, 079). Gondericiler sahtedir.
/// </summary>
public sealed class NotificationDispatcherTests : IAsyncLifetime, IDisposable
{
    private const string Code = "482915";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("dsg_hrms_notification_test")
        .Build();

    private readonly IEmailSender _email = Substitute.For<IEmailSender>();
    private readonly ISmsSender _sms = Substitute.For<ISmsSender>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly CapturingLoggerProvider _logs = new();
    private readonly ILoggerFactory _loggerFactory;

    private DbContextOptions<HrmsDbContext> _options = null!;
    private ServiceProvider _provider = null!;

    public NotificationDispatcherTests()
    {
        _loggerFactory = LoggerFactory.Create(builder => builder.AddProvider(_logs).SetMinimumLevel(LogLevel.Trace));
    }

    public void Dispose()
    {
        _loggerFactory.Dispose();
        _logs.Dispose();
    }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _clock.UtcNow.Returns(new DateTimeOffset(2026, 9, 27, 9, 0, 5, TimeSpan.Zero));

        _options = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseNpgsql(_container.GetConnectionString(), npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .Options;

        await using var context = new HrmsDbContext(_options);
        await context.Database.MigrateAsync();

        var services = new ServiceCollection();
        services.AddScoped(_ => new HrmsDbContext(_options));
        services.AddSingleton(_email);
        services.AddSingleton(_sms);
        _provider = services.BuildServiceProvider();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _container.DisposeAsync();
    }

    private async Task ProcessAsync(NotificationOptions options, OutboundMessage message)
    {
        using var dispatcher = new NotificationDispatcher(
            new InMemoryNotificationDispatch(options),
            _provider.GetRequiredService<IServiceScopeFactory>(),
            options,
            _clock,
            _loggerFactory.CreateLogger<NotificationDispatcher>())
        {
            RetryDelays = [TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(10)],
        };

        await dispatcher.ProcessAsync(message, CancellationToken.None);
    }

    private static NotificationOptions Send => new() { DeliveryMode = DeliveryMode.Send };

    private static OutboundMessage EmailMessage(string recipient = "ahmet.yilmaz@duzen.com.tr") =>
        new(NotificationChannel.Email, NotificationPurpose.RegistrationCode, 7, recipient, "Konu", $"kodunuz: {Code}",
            new DateTimeOffset(2026, 9, 27, 9, 0, 0, TimeSpan.Zero));

    private static OutboundMessage SmsMessage() =>
        new(NotificationChannel.Sms, NotificationPurpose.RegistrationCode, 7, "5321234567", null, $"kodunuz: {Code}",
            new DateTimeOffset(2026, 9, 27, 9, 0, 0, TimeSpan.Zero));

    private async Task<NotificationDelivery> SingleDeliveryAsync()
    {
        await using var context = new HrmsDbContext(_options);
        return await context.Set<NotificationDelivery>().SingleAsync();
    }

    [Fact]
    public async Task Sent_email_is_recorded_without_content_and_with_masked_recipient()
    {
        _email.SendAsync(default!, default!, default!, default).ReturnsForAnyArgs(SendResult.Sent("250", "ABC123"));

        await ProcessAsync(Send, EmailMessage());

        await _email.Received(1).SendAsync("ahmet.yilmaz@duzen.com.tr", "Konu", $"kodunuz: {Code}", Arg.Any<CancellationToken>());
        var delivery = await SingleDeliveryAsync();
        delivery.Status.ShouldBe(DeliveryStatus.Sent);
        delivery.Channel.ShouldBe(NotificationChannel.Email);
        delivery.PersonId.ShouldBe(7);
        delivery.ExternalId.ShouldBe("ABC123");
        delivery.Attempts.ShouldBe(1);
        delivery.RecipientMasked.ShouldNotContain("ahmet.yilmaz");
        delivery.RecipientMasked.ShouldEndWith("@duzen.com.tr");
    }

    [Fact]
    public async Task Log_only_mode_sends_nothing_and_records_suppression()
    {
        await ProcessAsync(new NotificationOptions(), SmsMessage());

        await _sms.DidNotReceiveWithAnyArgs().SendAsync(default!, default!, default, default);
        var delivery = await SingleDeliveryAsync();
        delivery.Status.ShouldBe(DeliveryStatus.Suppressed);
        delivery.ResultCode.ShouldBe("LogOnly");
        delivery.RecipientMasked.ShouldNotContain("5321234567");
    }

    [Fact]
    public async Task Allow_list_mode_suppresses_recipients_outside_the_list()
    {
        var options = new NotificationOptions { DeliveryMode = DeliveryMode.AllowList, AllowedRecipients = "bilgi.islem@duzen.com.tr" };
        _email.SendAsync(default!, default!, default!, default).ReturnsForAnyArgs(SendResult.Sent("250"));
        await ProcessAsync(options, EmailMessage());
        await ProcessAsync(options, EmailMessage("bilgi.islem@duzen.com.tr"));

        await _email.Received(1).SendAsync("bilgi.islem@duzen.com.tr", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await using var context = new HrmsDbContext(_options);
        (await context.Set<NotificationDelivery>().OrderBy(d => d.Id).Select(d => d.Status).ToListAsync())
            .ShouldBe([DeliveryStatus.Suppressed, DeliveryStatus.Sent]);
    }

    [Fact]
    public async Task Transient_failure_is_retried_twice_then_recorded_as_failed()
    {
        _sms.SendAsync(default!, default!, default, default).ReturnsForAnyArgs(SendResult.Transient("network"));

        await ProcessAsync(Send, SmsMessage());

        await _sms.ReceivedWithAnyArgs(3).SendAsync(default!, default!, default, default);
        var delivery = await SingleDeliveryAsync();
        delivery.Status.ShouldBe(DeliveryStatus.Failed);
        delivery.Attempts.ShouldBe(3);
        delivery.ResultCode.ShouldBe("network");
    }

    [Fact]
    public async Task Transient_failure_followed_by_success_is_sent()
    {
        _sms.SendAsync(default!, default!, default, default).ReturnsForAnyArgs(SendResult.Transient("80"), SendResult.Sent("00", "JOB1"));

        await ProcessAsync(Send, SmsMessage());

        var delivery = await SingleDeliveryAsync();
        delivery.Status.ShouldBe(DeliveryStatus.Sent);
        delivery.Attempts.ShouldBe(2);
        delivery.ExternalId.ShouldBe("JOB1");
    }

    [Theory]
    [InlineData(SendOutcome.PermanentFailure, "20")]
    [InlineData(SendOutcome.ConfigurationError, "30")]
    public async Task Permanent_and_configuration_errors_are_not_retried(SendOutcome outcome, string code)
    {
        _sms.SendAsync(default!, default!, default, default).ReturnsForAnyArgs(new SendResult(outcome, code));

        await ProcessAsync(Send, SmsMessage());

        await _sms.ReceivedWithAnyArgs(1).SendAsync(default!, default!, default, default);
        (await SingleDeliveryAsync()).ResultCode.ShouldBe(code);
    }

    [Fact]
    public async Task Configuration_error_is_logged_as_critical()
    {
        _sms.SendAsync(default!, default!, default, default).ReturnsForAnyArgs(SendResult.Misconfigured("40"));

        await ProcessAsync(Send, SmsMessage());

        _logs.Entries.ShouldContain(e => e.Level == LogLevel.Critical && e.Message.Contains("40"));
    }

    [Fact]
    public async Task Code_and_recipient_never_appear_in_logs()
    {
        // SYG-KMLK-026, ADR-0012 §7 (#85): hicbir kipte icerik yazilmaz.
        _sms.SendAsync(default!, default!, default, default).ReturnsForAnyArgs(SendResult.Transient("network"), SendResult.Sent("00"));
        _email.SendAsync(default!, default!, default!, default).ReturnsForAnyArgs(SendResult.Permanent("smtp-550"));

        await ProcessAsync(Send, SmsMessage());
        await ProcessAsync(Send, EmailMessage());
        await ProcessAsync(new NotificationOptions(), SmsMessage());

        // Pozitif kontrol: gunluk bos degil.
        _logs.Entries.Count.ShouldBeGreaterThanOrEqualTo(4);
        _logs.Entries.ShouldAllBe(e => !e.Message.Contains(Code) && !e.Message.Contains("5321234567") && !e.Message.Contains("ahmet.yilmaz"));
    }

    [Fact]
    public async Task Queued_messages_are_processed_by_the_background_loop()
    {
        _email.SendAsync(default!, default!, default!, default).ReturnsForAnyArgs(SendResult.Sent("250"));
        var options = Send;
        var queue = new InMemoryNotificationDispatch(options);
        using var dispatcher = new NotificationDispatcher(
            queue, _provider.GetRequiredService<IServiceScopeFactory>(), options, _clock,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<NotificationDispatcher>.Instance);

        await dispatcher.StartAsync(CancellationToken.None);
        queue.TryEnqueue(EmailMessage()).ShouldBeTrue();

        var deadline = DateTime.UtcNow.AddSeconds(10);
        while (DateTime.UtcNow < deadline)
        {
            await using var context = new HrmsDbContext(_options);
            if (await context.Set<NotificationDelivery>().AnyAsync())
            {
                break;
            }

            await Task.Delay(50);
        }

        await dispatcher.StopAsync(CancellationToken.None);
        (await SingleDeliveryAsync()).Status.ShouldBe(DeliveryStatus.Sent);
    }

    [Fact]
    public void Full_queue_rejects_new_messages()
    {
        var queue = new InMemoryNotificationDispatch(new NotificationOptions { QueueCapacity = 10 });

        for (var i = 0; i < 10; i++)
        {
            queue.TryEnqueue(SmsMessage()).ShouldBeTrue();
        }

        queue.TryEnqueue(SmsMessage()).ShouldBeFalse();
    }

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new Logger(Entries);

        public void Dispose()
        {
        }

        private sealed class Logger(List<(LogLevel, string)> entries) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                lock (entries)
                {
                    entries.Add((logLevel, formatter(state, exception)));
                }
            }
        }
    }
}
