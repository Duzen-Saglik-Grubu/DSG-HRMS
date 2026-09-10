using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using Dsg.Hrms.Application.Common.Security;
using Serilog.Core;
using Serilog.Events;

namespace Dsg.Hrms.Infrastructure.Logging;

/// <summary>
/// Gunluge yazilan nesnelerin kisisel veri iceren alanlarini maskeler (ADR-0009 §4).
/// </summary>
/// <remarks>
/// <para>
/// Serilog, <c>{@Nesne}</c> bicimindeki bir sablonu gordugunde nesneyi ozelliklerine
/// ayirir. Bu ilke o ayristirmanin arasina girer: her ozellik icin
/// <see cref="MaskRules"/> karar verir, deger maskeli olarak yazilir.
/// </para>
/// <para>
/// <b>Kapsam yalnizca DSG-HRMS tipleridir.</b> Cerceve tiplerine (istisna, koleksiyon,
/// yapilandirma nesneleri) dokunulmaz; Serilog'un kendi davranisi bozulmaz.
/// </para>
/// <para>
/// <b>Sinir:</b> Bu ilke yalnizca nesne olarak yazilan degerleri kapsar. Ham bir metin
/// dogrudan yazilirsa (<c>logger.Information("TCKN: {Value}", tckn)</c>) yakalanamaz.
/// Bu yuzden kisisel veri gunluge <b>daima nesne olarak</b> verilir; kural
/// <c>CONTRIBUTING.md</c> §3.3'te yazilidir ve kod incelemesinde denetlenir.
/// </para>
/// </remarks>
public sealed class MaskingDestructuringPolicy : IDestructuringPolicy
{
    private const string AssemblyPrefix = "Dsg.Hrms";

    /// <summary>Ozellik okunamadiginda yazilan deger.</summary>
    private const string UnreadableValue = "<okunamadi>";

    // Yansima (reflection) her kayitta degil, tip basina BIR KEZ calisir.
    // Gunluk yolu sicak yoldur; buradaki maliyet tum isteklere yansir.
    private static readonly ConcurrentDictionary<Type, PropertyPlan[]> PlanCache = new();

    /// <inheritdoc />
    public bool TryDestructure(
        object value,
        ILogEventPropertyValueFactory propertyValueFactory,
        out LogEventPropertyValue result)
    {
        ArgumentNullException.ThrowIfNull(propertyValueFactory);

        result = null!;

        if (value is null || !IsApplicationType(value.GetType()))
        {
            return false;
        }

        var type = value.GetType();
        var plans = PlanCache.GetOrAdd(type, BuildPlan);

        var properties = new List<LogEventProperty>(plans.Length);

        foreach (var plan in plans)
        {
            properties.Add(new LogEventProperty(
                plan.Name,
                Render(plan, value, propertyValueFactory)));
        }

        result = new StructureValue(properties, type.Name);
        return true;
    }

    private static LogEventPropertyValue Render(
        PropertyPlan plan,
        object owner,
        ILogEventPropertyValueFactory propertyValueFactory)
    {
        // Sir alanlarda deger HIC OKUNMAZ: okunmayan deger sizamaz.
        if (plan.Decision.Action == MaskAction.Exclude)
        {
            return new ScalarValue(Mask.SecretPlaceholder);
        }

        object? propertyValue;

        try
        {
            propertyValue = plan.Property.GetValue(owner);
        }
        catch (TargetInvocationException)
        {
            // Hesaplanan bir ozellik hata firlatabilir. Gunluk yazmak, uygulamayi
            // durdurmak icin gecerli bir sebep degildir.
            return new ScalarValue(UnreadableValue);
        }

        if (plan.Decision.Action == MaskAction.Mask)
        {
            return new ScalarValue(MaskRules.Apply(plan.Decision, AsText(propertyValue)));
        }

        // Ic ice nesneler icin bu ilke yeniden devreye girer: maskeleme derinlemesine calisir.
        return propertyValueFactory.CreatePropertyValue(propertyValue, destructureObjects: true);
    }

    private static PropertyPlan[] BuildPlan(Type type) =>
        [.. type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0)
            .Select(p => new PropertyPlan(p.Name, p, MaskRules.For(p)))];

    private static bool IsApplicationType(Type type) =>
        !type.IsPrimitive
        && !type.IsEnum
        && type != typeof(string)
        && type.Assembly.GetName().Name?.StartsWith(AssemblyPrefix, StringComparison.Ordinal) == true;

    /// <summary>
    /// Maskeleyicilerin bekledigi metin bicimine cevirir.
    /// </summary>
    /// <remarks>
    /// Kultur bagimsiz cevrim zorunludur: Turkce kulturde bicimlenmis bir sayi
    /// maskeleyicinin beklemedigi ayiraclar tasir ve deger gereksiz yere tamamen
    /// maskelenirdi.
    /// </remarks>
    private static string? AsText(object? value) => value switch
    {
        null => null,
        string text => text,
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString(),
    };

    private sealed record PropertyPlan(string Name, PropertyInfo Property, MaskDecision Decision);
}
