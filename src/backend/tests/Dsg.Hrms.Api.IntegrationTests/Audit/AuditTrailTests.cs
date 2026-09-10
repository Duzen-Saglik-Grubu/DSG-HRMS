using System.Text.Json;
using Dsg.Hrms.Api.IntegrationTests.Data;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Infrastructure.Data.Interceptors;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace Dsg.Hrms.Api.IntegrationTests.Audit;

/// <summary>
/// Denetim izinin <b>gercek PostgreSQL</b> uzerinde dogrulanmasi (ADR-0009 §2).
/// </summary>
/// <remarks>
/// Denetim izi, KVKK denetiminde ilk istenen kayittir. "Yaziliyor" varsayimiyla
/// yetinilemez: burada kaydin gercekten olustugu, dogru islem turunu tasidigi,
/// kisisel veriyi maskeledigi ve <b>degistirilemez</b> oldugu olculur.
/// </remarks>
public sealed class AuditTrailTests : IAsyncLifetime
{
    private const string NationalIdValue = "12345678901";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("dsg_hrms_audit_test")
        .Build();

    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

    private static readonly DateTimeOffset FixedInstant =
        new(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _dateTimeProvider.UtcNow.Returns(FixedInstant);
        _currentUser.UserId.Returns((long?)42);
        _currentUser.TraceId.Returns("00112233445566778899aabbccddeeff");
        _currentUser.IpAddress.Returns("192.168.3.77");

        await using var context = CreateContext();
        await context.Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync() => await _container.DisposeAsync();

    private SampleDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<SampleDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                new AuditFieldsInterceptor(_currentUser, _dateTimeProvider),
                new AuditTrailInterceptor(_currentUser, _dateTimeProvider))
            .Options;

        return new SampleDbContext(options);
    }

    private static async Task<ChangeLogEntry> SingleEntryForAsync(SampleDbContext context, Guid publicId) =>
        await context.ChangeLog.AsNoTracking().SingleAsync(entry => entry.EntityId == publicId);

    // ------------------------------------------------------------------
    // Kayit uretimi
    // ------------------------------------------------------------------

    [Fact]
    public async Task Insert_is_recorded_with_the_request_context()
    {
        await using var context = CreateContext();
        var record = new SampleRecord { Title = "Yeni kayit" };

        context.SampleRecords.Add(record);
        await context.SaveChangesAsync();

        var entry = await SingleEntryForAsync(context, record.PublicId);

        entry.Operation.ShouldBe(AuditOperation.Insert);
        entry.EntityName.ShouldBe(nameof(SampleRecord));
        entry.OccurredAt.ShouldBe(FixedInstant);
        entry.UserAccountId.ShouldBe(42);

        // Bir denetim kaydindan ayni istegin teknik gunluk satirlarina ulasilabilmelidir.
        entry.TraceId.ShouldBe("00112233445566778899aabbccddeeff");
        entry.IpAddress.ShouldBe("192.168.3.77");
    }

    [Fact]
    public async Task Update_records_only_the_fields_that_actually_changed()
    {
        await using var context = CreateContext();
        var record = new SampleRecord { Title = "Ilk hâli", Amount = 100m };
        context.SampleRecords.Add(record);
        await context.SaveChangesAsync();

        record.Title = "Guncellenmis hâli";
        await context.SaveChangesAsync();

        var updateEntry = await context.ChangeLog
            .AsNoTracking()
            .SingleAsync(entry => entry.EntityId == record.PublicId
                && entry.Operation == AuditOperation.Update);

        using var changes = JsonDocument.Parse(updateEntry.Changes);
        var root = changes.RootElement;

        // Degismeyen alanlari da yazmak denetim izini okunamaz hâle getirirdi.
        root.TryGetProperty(nameof(SampleRecord.Amount), out _).ShouldBeFalse();

        var title = root.GetProperty(nameof(SampleRecord.Title));
        title.GetProperty("old").GetString().ShouldBe("Ilk hâli");
        title.GetProperty("new").GetString().ShouldBe("Guncellenmis hâli");
    }

    [Fact]
    public async Task Soft_delete_is_recorded_as_a_deletion_not_an_update()
    {
        // Yumusak silme veritabani acisindan guncellemedir. Denetim izinde silme
        // olarak gorunmezse "kaydi kim sildi" sorusu cevaplanamaz.
        await using var context = CreateContext();
        var record = new SampleRecord { Title = "Silinecek" };
        context.SampleRecords.Add(record);
        await context.SaveChangesAsync();

        context.SampleRecords.Remove(record);
        await context.SaveChangesAsync();

        var operations = await context.ChangeLog
            .AsNoTracking()
            .Where(entry => entry.EntityId == record.PublicId)
            .Select(entry => entry.Operation)
            .ToListAsync();

        operations.ShouldBe([AuditOperation.Insert, AuditOperation.Delete]);
    }

    [Fact]
    public async Task Saving_without_a_real_change_does_not_produce_noise()
    {
        await using var context = CreateContext();
        var record = new SampleRecord { Title = "Kayit" };
        context.SampleRecords.Add(record);
        await context.SaveChangesAsync();

        // Degisiklik yapmadan yeniden kaydetme.
        await context.SaveChangesAsync();

        var count = await context.ChangeLog
            .AsNoTracking()
            .CountAsync(entry => entry.EntityId == record.PublicId);

        count.ShouldBe(1);
    }

    // ------------------------------------------------------------------
    // KVKK — denetim izi de bir kayittir
    // ------------------------------------------------------------------

    [Fact]
    public async Task Personal_data_is_masked_in_the_audit_trail()
    {
        await using var context = CreateContext();
        var record = new SampleRecord
        {
            Title = "Kisisel veri denemesi",
            NationalId = NationalIdValue,
            PasswordHash = "cok-gizli-ozet",
        };

        context.SampleRecords.Add(record);
        await context.SaveChangesAsync();

        var entry = await SingleEntryForAsync(context, record.PublicId);

        // Denetim izi "neyin degistigini" gosterir; veriyi ikinci bir yerde saklamaz.
        entry.Changes.ShouldNotContain(NationalIdValue);
        entry.Changes.ShouldNotContain("cok-gizli-ozet");
        entry.Changes.ShouldContain("123*****901");
    }

    [Fact]
    public async Task Audit_fields_are_not_duplicated_into_the_change_payload()
    {
        await using var context = CreateContext();
        var record = new SampleRecord { Title = "Kayit" };
        context.SampleRecords.Add(record);
        await context.SaveChangesAsync();

        var entry = await SingleEntryForAsync(context, record.PublicId);

        // Kim ve ne zaman bilgisi denetim kaydinin kendi kolonlarindadir.
        entry.Changes.ShouldNotContain(nameof(SampleRecord.CreatedAt));
        entry.Changes.ShouldNotContain(nameof(SampleRecord.CreatedBy));
    }

    // ------------------------------------------------------------------
    // Butunluk
    // ------------------------------------------------------------------

    [Fact]
    public async Task Audit_entry_shares_the_transaction_of_the_business_change()
    {
        // Is kaydi geri alinip denetim kaydinin kalmasi (veya tersi) denetim izini
        // guvenilmez kilardi.
        await using var context = CreateContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var record = new SampleRecord { Title = "Geri alinacak" };
        context.SampleRecords.Add(record);
        await context.SaveChangesAsync();

        await transaction.RollbackAsync();

        await using var verification = CreateContext();

        (await verification.SampleRecords.AnyAsync(r => r.PublicId == record.PublicId))
            .ShouldBeFalse();
        (await verification.ChangeLog.AnyAsync(entry => entry.EntityId == record.PublicId))
            .ShouldBeFalse("Is kaydi geri alindiysa denetim kaydi da kalmamalidir.");
    }

    [Fact]
    public async Task Application_refuses_to_modify_an_audit_entry()
    {
        await using var context = CreateContext();
        context.SampleRecords.Add(new SampleRecord { Title = "Kayit" });
        await context.SaveChangesAsync();

        var entry = await context.ChangeLog.FirstAsync();
        context.Entry(entry).Property(e => e.IpAddress).CurrentValue = "10.0.0.1";

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            async () => await context.SaveChangesAsync());

        exception.Message.ShouldContain("degistirilemez");
    }

    [Fact]
    public async Task Application_refuses_to_delete_an_audit_entry()
    {
        await using var context = CreateContext();
        context.SampleRecords.Add(new SampleRecord { Title = "Kayit" });
        await context.SaveChangesAsync();

        context.ChangeLog.Remove(await context.ChangeLog.FirstAsync());

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await context.SaveChangesAsync());
    }
}
