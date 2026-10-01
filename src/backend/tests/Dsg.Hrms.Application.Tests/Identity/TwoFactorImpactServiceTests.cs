using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Identity.Sessions;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Application.Tests.Settings;
using NSubstitute;

namespace Dsg.Hrms.Application.Tests.Identity;

/// <summary>2FA acilmadan once etki uyarisi (SYG-KMLK-035).</summary>
public sealed class TwoFactorImpactServiceTests
{
    private readonly ITwoFactorImpactStore _store = Substitute.For<ITwoFactorImpactStore>();

    private TwoFactorImpactService Service(FakeSystemParameters parameters) => new(_store, parameters);

    [Theory]
    [InlineData("email,sms", true, true)]
    [InlineData("sms", false, true)]
    [InlineData("email", true, false)]
    public async Task Channels_are_counted_with_the_same_rule_as_sign_in(string channels, bool email, bool sms)
    {
        _store.CountWithoutChannelAsync(email, sms, Arg.Any<CancellationToken>()).Returns(2);

        var impact = await Service(new FakeSystemParameters().With(ParameterCatalog.VerificationChannels, channels))
            .EvaluateAsync(CancellationToken.None);

        impact.AffectedCount.ShouldBe(2);
        impact.ConfirmationRequired.ShouldBeTrue();
    }

    [Fact]
    public async Task Enabling_without_confirmation_is_refused_with_the_count()
    {
        _store.CountWithoutChannelAsync(Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(3);
        var service = Service(new FakeSystemParameters());

        var error = await Should.ThrowAsync<BusinessRuleException>(() =>
            service.EnsureConfirmedAsync(ParameterCatalog.TwoFactorEnabled.Key, "TRUE", confirmed: false, CancellationToken.None));

        error.Message.ShouldContain("3 aktif hesap sahibi");
        await service.EnsureConfirmedAsync(ParameterCatalog.TwoFactorEnabled.Key, "true", confirmed: true, CancellationToken.None);
    }

    [Fact]
    public async Task Other_changes_need_no_confirmation()
    {
        var service = Service(new FakeSystemParameters());

        await service.EnsureConfirmedAsync(ParameterCatalog.TwoFactorEnabled.Key, "false", confirmed: false, CancellationToken.None);
        await service.EnsureConfirmedAsync(ParameterCatalog.MaxFailedLogins.Key, "true", confirmed: false, CancellationToken.None);
        await service.EnsureConfirmedAsync(
            ParameterCatalog.TwoFactorEnabled.Key, "true", confirmed: false,
            CancellationToken.None).ShouldThrowAsync<BusinessRuleException>();

        // Zaten aciksa degisiklik yoktur; uyari kapaliysa onay istenmez.
        await Service(new FakeSystemParameters().With(ParameterCatalog.TwoFactorEnabled, "true"))
            .EnsureConfirmedAsync(ParameterCatalog.TwoFactorEnabled.Key, "true", confirmed: false, CancellationToken.None);
        await Service(new FakeSystemParameters().With(ParameterCatalog.TwoFactorImpactWarning, "false"))
            .EnsureConfirmedAsync(ParameterCatalog.TwoFactorEnabled.Key, "true", confirmed: false, CancellationToken.None);
    }
}
