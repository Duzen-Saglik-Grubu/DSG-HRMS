using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Dsg.Hrms.Api.IntegrationTests.Identity;

/// <summary>
/// Varsayilan yetki politikasi: isareti olmayan uc oturum ister (#244, B-04; ADR-0007 §3).
/// Hangi ucun anonim oldugu mimari testinde (EndpointAuthorizationTests) sabitlenir; bu test
/// politikanin calisan uygulamada gercekten kurulu oldugunu ve anonim uclarin acik kaldigini
/// dogrular.
/// </summary>
[Collection(ApiHostGroup.Name)]
public sealed class FallbackAuthorizationTests : IClassFixture<RegistrationApiFixture>
{
    private readonly RegistrationApiFixture _fixture;

    public FallbackAuthorizationTests(RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Fallback_policy_requires_an_authenticated_user()
    {
        var fallback = await _fixture.WithServicesAsync(services =>
            services.GetRequiredService<IAuthorizationPolicyProvider>().GetFallbackPolicyAsync());

        fallback.ShouldNotBeNull();
        fallback.Requirements.OfType<DenyAnonymousAuthorizationRequirement>().ShouldNotBeEmpty();
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/openapi/v1.json")]
    [InlineData("/api/v1/identity/public-settings")]
    public async Task Anonymous_endpoints_stay_reachable_without_a_session(string path)
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync(new Uri(path, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Session_endpoint_without_a_token_returns_401()
    {
        using var client = _fixture.CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/identity/account/two-factor", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
