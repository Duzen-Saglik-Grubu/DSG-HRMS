using System.Globalization;

namespace Dsg.Hrms.Application.Settings;

/// <summary>
/// Parametre degerlerini okur (SYG-KMLK-075).
/// </summary>
/// <remarks>
/// <para>Deger su sirayla cozulur:</para>
/// <list type="number">
///   <item>Veritabanindaki deger (parametre ekranindan girilen).</item>
///   <item>Yapilandirma (<see cref="ParameterDefinition.ConfigurationKey"/>; ortam degiskeni dahil).</item>
///   <item>Katalogdaki varsayilan.</item>
/// </list>
/// <para>
/// Veritabani degerleri en fazla 1 dakika onbellekte tutulur: parametre ekranindan
/// yapilan degisiklik, yeniden dagitim gerekmeden en gec 1 dakikada etkili olur.
/// </para>
/// </remarks>
public interface ISystemParameters
{
    /// <summary>
    /// Parametrenin cozulmus kanonik degerini dondurur; hicbir kaynakta yoksa <c>null</c>.
    /// Sir parametrelerde cozulmus (acik) deger doner; bu deger yalnizca iletisim
    /// istemcisine verilir, gunluge ve yanita YAZILMAZ.
    /// </summary>
    ValueTask<string?> GetAsync(ParameterDefinition parameter, CancellationToken cancellationToken);
}

/// <summary>Turlu okuma yardimcilari.</summary>
public static class SystemParameterExtensions
{
    /// <summary>Tam sayi parametreyi okur.</summary>
    public static async ValueTask<int> GetIntegerAsync(
        this ISystemParameters parameters, ParameterDefinition parameter, CancellationToken cancellationToken)
    {
        EnsureType(parameter, ParameterType.Number);
        var value = await Required(parameters, parameter, cancellationToken).ConfigureAwait(false);
        return int.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
    }

    /// <summary>Acik/Kapali parametreyi okur.</summary>
    public static async ValueTask<bool> GetBooleanAsync(
        this ISystemParameters parameters, ParameterDefinition parameter, CancellationToken cancellationToken)
    {
        EnsureType(parameter, ParameterType.Toggle);
        var value = await Required(parameters, parameter, cancellationToken).ConfigureAwait(false);
        return string.Equals(value, "true", StringComparison.Ordinal);
    }

    /// <summary>Liste parametreyi okur. Tanimli degilse bos liste doner.</summary>
    public static async ValueTask<IReadOnlyList<string>> GetListAsync(
        this ISystemParameters parameters, ParameterDefinition parameter, CancellationToken cancellationToken)
    {
        EnsureType(parameter, ParameterType.List);
        var value = await parameters.GetAsync(parameter, cancellationToken).ConfigureAwait(false);
        return value is null ? [] : value.Split(',');
    }

    private static async ValueTask<string> Required(
        ISystemParameters parameters, ParameterDefinition parameter, CancellationToken cancellationToken) =>
        await parameters.GetAsync(parameter, cancellationToken).ConfigureAwait(false)
        ?? throw new InvalidOperationException($"{parameter.Key} parametresinin degeri yok.");

    private static void EnsureType(ParameterDefinition parameter, ParameterType expected)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        if (parameter.Type != expected)
        {
            throw new InvalidOperationException($"{parameter.Key} parametresi {expected} turunde degil ({parameter.Type}).");
        }
    }
}
