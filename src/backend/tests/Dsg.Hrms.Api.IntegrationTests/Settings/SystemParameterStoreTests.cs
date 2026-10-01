using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Settings;
using Dsg.Hrms.Infrastructure.Data;
using Dsg.Hrms.Infrastructure.Data.Interceptors;
using Dsg.Hrms.Infrastructure.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using NSubstitute;
using Testcontainers.PostgreSql;

namespace Dsg.Hrms.Api.IntegrationTests.Settings;

/// <summary>
/// Parametre deposunun <b>gercek PostgreSQL</b> uzerinde dogrulanmasi (SYG-KMLK-075, 076; <c>KR-071</c>).
/// </summary>
public sealed class SystemParameterStoreTests : IAsyncLifetime
{
    private const string SmtpPassword = "sahteparola";

    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("dsg_hrms_parameter_test")
        .Build();

    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly string _key = ParameterStoreFactory.NewKey();
    private readonly List<ServiceProvider> _providers = [];

    private DateTimeOffset _now = new(2026, 9, 26, 9, 0, 0, TimeSpan.Zero);
    private DbContextOptions<HrmsDbContext> _options = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        _clock.UtcNow.Returns(_ => _now);
        _currentUser.UserId.Returns(42L);

        _options = new DbContextOptionsBuilder<HrmsDbContext>()
            .UseNpgsql(_container.GetConnectionString(), npgsql => npgsql.MigrationsHistoryTable("__ef_migrations_history", "public"))
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(
                new AuditFieldsInterceptor(_currentUser, _clock),
                new AuditTrailInterceptor(_currentUser, _clock))
            .Options;

        await using var context = new HrmsDbContext(_options);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        foreach (var provider in _providers)
        {
            await provider.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    private SystemParameters CreateParameters(IDictionary<string, string?>? configuration = null, string? key = "default")
    {
        var (parameters, provider) = ParameterStoreFactory.Create(_options, _clock, configuration, key == "default" ? _key : key);
        _providers.Add(provider);
        return parameters;
    }

    private async Task UpdateAsync(SystemParameters parameters, ParameterDefinition parameter, string value, string? key = "default")
    {
        await using var context = new HrmsDbContext(_options);
        var protector = new AesGcmSecretProtector(new SecretProtectionOptions { Key = key == "default" ? _key : key });
        await new SystemParameterEditor(context, parameters, protector).UpdateAsync(parameter.Key, value, CancellationToken.None);
    }

    private HrmsDbContext Read() => new(_options);

    // ------------------------------------------------------------------ cozumleme sirasi

    [Fact]
    public async Task Value_resolves_from_database_then_configuration_then_default()
    {
        var parameters = CreateParameters(new Dictionary<string, string?> { ["Parameters:MaxFailedLogins"] = "7" });
        var other = CreateParameters();

        (await other.GetIntegerAsync(ParameterCatalog.MaxFailedLogins, CancellationToken.None)).ShouldBe(5);      // varsayilan
        (await parameters.GetIntegerAsync(ParameterCatalog.MaxFailedLogins, CancellationToken.None)).ShouldBe(7); // yapilandirma

        await UpdateAsync(parameters, ParameterCatalog.MaxFailedLogins, "9");

        (await parameters.GetIntegerAsync(ParameterCatalog.MaxFailedLogins, CancellationToken.None)).ShouldBe(9); // veritabani
    }

    [Fact]
    public async Task Change_takes_effect_on_other_instances_within_one_minute()
    {
        // SYG-KMLK-075: yeniden dagitim gerekmeden en gec 1 dakika. Degisikligi yapan
        // ornek hemen, digerleri onbellek suresi dolunca yeni degeri gorur.
        var editorInstance = CreateParameters();
        var otherInstance = CreateParameters();
        (await otherInstance.GetIntegerAsync(ParameterCatalog.MaxFailedLogins, CancellationToken.None)).ShouldBe(5);

        await UpdateAsync(editorInstance, ParameterCatalog.MaxFailedLogins, "8");

        (await editorInstance.GetIntegerAsync(ParameterCatalog.MaxFailedLogins, CancellationToken.None)).ShouldBe(8);
        (await otherInstance.GetIntegerAsync(ParameterCatalog.MaxFailedLogins, CancellationToken.None)).ShouldBe(5);

        _now += SystemParameters.CacheDuration;

        (await otherInstance.GetIntegerAsync(ParameterCatalog.MaxFailedLogins, CancellationToken.None)).ShouldBe(8);
    }

    [Fact]
    public async Task Stored_value_outside_the_current_range_falls_back()
    {
        // Katalog araligi sonradan daralirsa eski deger kullanilmaz.
        await using (var context = Read())
        {
            var row = SystemParameter.Create(ParameterCatalog.MaxFailedLogins.Key);
            row.SetValue("99");
            context.Add(row);
            await context.SaveChangesAsync();
        }

        (await CreateParameters().GetIntegerAsync(ParameterCatalog.MaxFailedLogins, CancellationToken.None)).ShouldBe(5);
    }

    // ------------------------------------------------------------------ dogrulama ve denetim izi

    [Fact]
    public async Task Invalid_value_is_rejected_and_nothing_is_saved()
    {
        var parameters = CreateParameters();

        var ex = await Should.ThrowAsync<BusinessRuleException>(() => UpdateAsync(parameters, ParameterCatalog.MaxFailedLogins, "50"));
        ex.Message.ShouldBe("Değer 3–10 aralığında olmalıdır.");

        await using var context = Read();
        (await context.Set<SystemParameter>().CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task Unknown_parameter_is_not_found()
    {
        await using var context = Read();
        var editor = new SystemParameterEditor(context, CreateParameters(), new AesGcmSecretProtector(new SecretProtectionOptions()));

        await Should.ThrowAsync<NotFoundException>(() => editor.UpdateAsync("PRM-XXX-01", "1", CancellationToken.None));
    }

    [Fact]
    public async Task Change_is_audited_with_old_and_new_value_and_the_user()
    {
        var parameters = CreateParameters();
        await UpdateAsync(parameters, ParameterCatalog.IdleTimeoutMinutes, "45");
        await UpdateAsync(parameters, ParameterCatalog.IdleTimeoutMinutes, "60");

        await using var context = Read();
        var update = await context.ChangeLog.SingleAsync(e => e.EntityName == nameof(SystemParameter) && e.Operation == AuditOperation.Update);
        var changes = update.Changes.ToString();

        changes.ShouldContain("45");
        changes.ShouldContain("60");
        update.UserAccountId.ShouldBe(42L);
    }

    [Fact]
    public async Task Saving_the_same_value_adds_nothing_to_the_audit_trail()
    {
        var parameters = CreateParameters();
        await UpdateAsync(parameters, ParameterCatalog.IdleTimeoutMinutes, "45");
        await UpdateAsync(parameters, ParameterCatalog.IdleTimeoutMinutes, " 45 ");

        await using var context = Read();
        (await context.ChangeLog.CountAsync(e => e.EntityName == nameof(SystemParameter))).ShouldBe(1);
    }

    // ------------------------------------------------------------------ sirlar (KR-071)

    [Fact]
    public async Task Secret_is_encrypted_in_the_database_and_readable_by_the_application()
    {
        var parameters = CreateParameters();

        await UpdateAsync(parameters, ParameterCatalog.SmtpPassword, SmtpPassword);

        // Veritabaninda acik metin YOK.
        await using (var context = Read())
        {
            var raw = await context.Database
                .SqlQuery<string>($"SELECT coalesce(value, '') || '|' || coalesce(protected_value, '') AS \"Value\" FROM settings.system_parameter")
                .SingleAsync();
            raw.ShouldNotContain(SmtpPassword);
            raw.ShouldStartWith("|v1:");
        }

        (await parameters.GetAsync(ParameterCatalog.SmtpPassword, CancellationToken.None)).ShouldBe(SmtpPassword);
    }

    [Fact]
    public async Task Secret_never_appears_in_the_audit_trail()
    {
        var parameters = CreateParameters();
        await UpdateAsync(parameters, ParameterCatalog.SmtpPassword, SmtpPassword);
        await UpdateAsync(parameters, ParameterCatalog.SmtpPassword, SmtpPassword + "-yeni");

        await using var context = Read();
        var entries = await context.ChangeLog.Where(e => e.EntityName == nameof(SystemParameter)).ToListAsync();

        // Pozitif kontrol: degisiklik kayitli ve alan adi gorunuyor.
        entries.Count.ShouldBe(2);
        entries.ShouldAllBe(e => e.Changes.ToString().Contains("ProtectedValue"));

        foreach (var entry in entries)
        {
            var changes = entry.Changes.ToString();
            changes.ShouldNotContain(SmtpPassword);
            changes.ShouldNotContain("v1:");
        }
    }

    [Fact]
    public async Task Secret_value_is_never_listed()
    {
        var parameters = CreateParameters();
        await UpdateAsync(parameters, ParameterCatalog.SmtpPassword, SmtpPassword);

        await using var context = Read();
        var list = await new SystemParameterEditor(context, parameters, new AesGcmSecretProtector(new SecretProtectionOptions { Key = _key }))
            .ListAsync(CancellationToken.None);

        var smtp = list.Single(p => p.Key == ParameterCatalog.SmtpPassword.Key);
        smtp.Value.ShouldBeNull();
        smtp.IsSet.ShouldBeTrue();
        smtp.Source.ShouldBe(ParameterSource.Database);

        var netgsm = list.Single(p => p.Key == ParameterCatalog.NetGsmPassword.Key);
        netgsm.IsSet.ShouldBeFalse();
        netgsm.Source.ShouldBe(ParameterSource.None);

        var lockout = list.Single(p => p.Key == ParameterCatalog.LockoutMinutes.Key);
        lockout.Value.ShouldBe("15");
        lockout.Source.ShouldBe(ParameterSource.Default);
        lockout.Min.ShouldBe(5);
        lockout.Max.ShouldBe(1440);

        list.Count.ShouldBe(ParameterCatalog.All.Count);
    }

    [Fact]
    public async Task Secret_from_configuration_is_used_when_nothing_is_stored_and_never_listed()
    {
        // Ilk kurulum ekransiz: sir ortam degiskeninden gelir (ADR-0008).
        var parameters = CreateParameters(new Dictionary<string, string?> { ["Parameters:SmtpPassword"] = SmtpPassword });

        (await parameters.GetAsync(ParameterCatalog.SmtpPassword, CancellationToken.None)).ShouldBe(SmtpPassword);

        await using var context = Read();
        var smtp = (await new SystemParameterEditor(context, parameters, new AesGcmSecretProtector(new SecretProtectionOptions()))
            .ListAsync(CancellationToken.None)).Single(p => p.Key == ParameterCatalog.SmtpPassword.Key);
        smtp.Value.ShouldBeNull();
        smtp.Source.ShouldBe(ParameterSource.Configuration);
    }

    [Fact]
    public async Task Secret_cannot_be_saved_without_an_encryption_key()
    {
        var parameters = CreateParameters(key: null);

        var ex = await Should.ThrowAsync<BusinessRuleException>(() => UpdateAsync(parameters, ParameterCatalog.SmtpPassword, SmtpPassword, key: null));
        ex.Message.ShouldContain("ParameterProtection:Key");
        ex.Message.ShouldNotContain(SmtpPassword);
    }

    [Fact]
    public async Task Empty_secret_is_rejected()
    {
        await Should.ThrowAsync<BusinessRuleException>(() => UpdateAsync(CreateParameters(), ParameterCatalog.SmtpPassword, "  "));
    }

    // ------------------------------------------------------------------ veritabani kisitlari

    [Fact]
    public async Task Database_rejects_a_row_with_both_plain_and_protected_value()
    {
        await using var context = Read();

        var ex = await Should.ThrowAsync<PostgresException>(() => context.Database.ExecuteSqlAsync(
            $"INSERT INTO settings.system_parameter (key, value, protected_value, created_at, public_id) VALUES ('PRM-ENT-06', 'acik', 'v1:x', now(), gen_random_uuid())"));

        ex.ConstraintName.ShouldBe("ck_system_parameter_single_value");
    }

    [Fact]
    public async Task Database_rejects_duplicate_parameter_rows()
    {
        await using var context = Read();
        await context.Database.ExecuteSqlAsync(
            $"INSERT INTO settings.system_parameter (key, value, created_at, public_id) VALUES ('PRM-KML-03', '5', now(), gen_random_uuid())");

        var ex = await Should.ThrowAsync<PostgresException>(() => context.Database.ExecuteSqlAsync(
            $"INSERT INTO settings.system_parameter (key, value, created_at, public_id) VALUES ('PRM-KML-03', '6', now(), gen_random_uuid())"));

        ex.ConstraintName.ShouldBe("ix_system_parameter_key");
    }
}
