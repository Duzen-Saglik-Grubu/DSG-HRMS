using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Identity.Sessions;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Application.Tests.Settings;
using NSubstitute;

namespace Dsg.Hrms.Application.Tests.Identity;

/// <summary>2FA acilmadan once etki uyarisi (SYG-KMLK-035, 080).</summary>
public sealed class TwoFactorImpactServiceTests
{
    private readonly ITwoFactorImpactStore _store = Substitute.For<ITwoFactorImpactStore>();

    private TwoFactorImpactService Service(FakeSystemParameters parameters) => new(_store, parameters);

    [Fact]
    public async Task Impact_is_the_number_of_active_accounts_with_their_own_preference_on()
    {
        // SYG-KMLK-080: parametre acilinca yalnizca kendi tercihi acik olanlardan kod istenir.
        _store.CountWithTwoFactorPreferenceAsync(Arg.Any<CancellationToken>()).Returns(2);

        var impact = await Service(new FakeSystemParameters()).EvaluateAsync(CancellationToken.None);

        impact.AffectedCount.ShouldBe(2);
        impact.ConfirmationRequired.ShouldBeTrue();
    }

    [Fact]
    public async Task No_confirmation_is_required_when_nobody_is_affected()
    {
        _store.CountWithTwoFactorPreferenceAsync(Arg.Any<CancellationToken>()).Returns(0);
        var service = Service(new FakeSystemParameters());

        var impact = await service.EvaluateAsync(CancellationToken.None);

        impact.AffectedCount.ShouldBe(0);
        impact.ConfirmationRequired.ShouldBeFalse();
        await service.EnsureConfirmedAsync(ParameterCatalog.TwoFactorEnabled.Key, "true", confirmed: false, CancellationToken.None);
    }

    [Fact]
    public async Task Enabling_without_confirmation_is_refused_with_the_count()
    {
        _store.CountWithTwoFactorPreferenceAsync(Arg.Any<CancellationToken>()).Returns(3);
        var service = Service(new FakeSystemParameters());

        var error = await Should.ThrowAsync<BusinessRuleException>(() =>
            service.EnsureConfirmedAsync(ParameterCatalog.TwoFactorEnabled.Key, "TRUE", confirmed: false, CancellationToken.None));

        error.Message.ShouldContain("açmış 3 kişi");
        error.Message.ShouldContain("girişte kod girecek");
        await service.EnsureConfirmedAsync(ParameterCatalog.TwoFactorEnabled.Key, "true", confirmed: true, CancellationToken.None);
    }

    [Fact]
    public async Task Other_changes_need_no_confirmation()
    {
        _store.CountWithTwoFactorPreferenceAsync(Arg.Any<CancellationToken>()).Returns(1);
        var service = Service(new FakeSystemParameters());

        await service.EnsureConfirmedAsync(ParameterCatalog.TwoFactorEnabled.Key, "false", confirmed: false, CancellationToken.None);
        await service.EnsureConfirmedAsync(ParameterCatalog.MaxFailedLogins.Key, "true", confirmed: false, CancellationToken.None);
        await service.EnsureConfirmedAsync(
            ParameterCatalog.TwoFactorEnabled.Key, "true", confirmed: false,
            CancellationToken.None).ShouldThrowAsync<BusinessRuleException>();

        // Zaten aciksa degisiklik yoktur; uyari kapaliysa onay istenmez.
        await Service(new FakeSystemParameters().With(ParameterCatalog.TwoFactorEnabled, "true"))
            .EnsureConfirmedAsync(ParameterCatalog.TwoFactorEnabled.Key, "true", confirmed: false, CancellationToken.None);

        var withoutWarning = Service(new FakeSystemParameters().With(ParameterCatalog.TwoFactorImpactWarning, "false"));
        (await withoutWarning.EvaluateAsync(CancellationToken.None)).ConfirmationRequired.ShouldBeFalse();
        await withoutWarning.EnsureConfirmedAsync(ParameterCatalog.TwoFactorEnabled.Key, "true", confirmed: false, CancellationToken.None);
    }
}
