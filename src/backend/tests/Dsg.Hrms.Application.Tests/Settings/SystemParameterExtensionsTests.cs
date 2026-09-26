using Dsg.Hrms.Application.Settings;

namespace Dsg.Hrms.Application.Tests.Settings;

/// <summary>Turlu parametre okuma yardimcilari.</summary>
public sealed class SystemParameterExtensionsTests
{
    private readonly FakeSystemParameters _parameters = new();

    [Fact]
    public async Task Typed_readers_parse_canonical_values()
    {
        _parameters.With(ParameterCatalog.MaxFailedLogins, "7")
            .With(ParameterCatalog.TwoFactorEnabled, "true")
            .With(ParameterCatalog.AcceptedEmailDomains, "duzen.com.tr,labpt.com.tr");

        (await _parameters.GetIntegerAsync(ParameterCatalog.MaxFailedLogins, CancellationToken.None)).ShouldBe(7);
        (await _parameters.GetBooleanAsync(ParameterCatalog.TwoFactorEnabled, CancellationToken.None)).ShouldBeTrue();
        (await _parameters.GetListAsync(ParameterCatalog.AcceptedEmailDomains, CancellationToken.None))
            .ShouldBe(["duzen.com.tr", "labpt.com.tr"]);
    }

    [Fact]
    public async Task Missing_list_is_empty()
    {
        _parameters.With(ParameterCatalog.AcceptedEmailDomains, null);

        (await _parameters.GetListAsync(ParameterCatalog.AcceptedEmailDomains, CancellationToken.None)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Missing_required_value_fails_loudly()
    {
        _parameters.With(ParameterCatalog.MaxFailedLogins, null);

        await Should.ThrowAsync<InvalidOperationException>(
            async () => await _parameters.GetIntegerAsync(ParameterCatalog.MaxFailedLogins, CancellationToken.None));
    }

    [Fact]
    public async Task Reading_with_the_wrong_type_fails()
    {
        // Yanlis turle okuma bir programlama hatasidir; sessizce yanlis deger donmemelidir.
        await Should.ThrowAsync<InvalidOperationException>(
            async () => await _parameters.GetBooleanAsync(ParameterCatalog.MaxFailedLogins, CancellationToken.None));
        await Should.ThrowAsync<InvalidOperationException>(
            async () => await _parameters.GetIntegerAsync(ParameterCatalog.TwoFactorEnabled, CancellationToken.None));
        await Should.ThrowAsync<InvalidOperationException>(
            async () => await _parameters.GetListAsync(ParameterCatalog.SupportContact, CancellationToken.None));
    }
}
