using Dsg.Hrms.Api.Networking;

namespace Dsg.Hrms.Api.IntegrationTests.Networking;

/// <summary>Guvenilen vekil aglarinin okunmasi (#87).</summary>
public sealed class ReverseProxyRegistrationTests
{
    [Fact]
    public void Empty_setting_trusts_no_proxy()
    {
        // Liste bossa X-Forwarded-For hic dikkate alinmaz: istemci IP'sini kendisi yazamaz.
        ReverseProxyRegistration.ParseNetworks(null).ShouldBeEmpty();
        ReverseProxyRegistration.ParseNetworks(" ").ShouldBeEmpty();
    }

    [Fact]
    public void Comma_separated_networks_are_parsed()
    {
        var networks = ReverseProxyRegistration.ParseNetworks("172.16.0.0/12, 10.0.0.0/8");

        networks.Select(n => n.ToString()).ShouldBe(["172.16.0.0/12", "10.0.0.0/8"]);
    }

    [Theory]
    [InlineData("172.16.0.0/40")]
    [InlineData("nginx")]
    public void Invalid_network_fails_at_startup(string value)
    {
        Should.Throw<InvalidOperationException>(() => ReverseProxyRegistration.ParseNetworks(value))
            .Message.ShouldContain("ReverseProxy:TrustedNetworks");
    }
}
