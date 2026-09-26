using Dsg.Hrms.Application.Settings;

namespace Dsg.Hrms.Api.IntegrationTests.Settings;

/// <summary>
/// Katalog varsayilanlarini donduren, istenen parametreleri ezilebilen sahte depo.
/// </summary>
public sealed class FakeSystemParameters : ISystemParameters
{
    private readonly Dictionary<string, string?> _overrides = new(StringComparer.Ordinal);

    /// <summary>Parametreye test degeri verir.</summary>
    public FakeSystemParameters With(ParameterDefinition parameter, string? value)
    {
        _overrides[parameter.Key] = value;
        return this;
    }

    /// <inheritdoc />
    public ValueTask<string?> GetAsync(ParameterDefinition parameter, CancellationToken cancellationToken) =>
        ValueTask.FromResult(_overrides.TryGetValue(parameter.Key, out var value) ? value : parameter.DefaultValue);
}
