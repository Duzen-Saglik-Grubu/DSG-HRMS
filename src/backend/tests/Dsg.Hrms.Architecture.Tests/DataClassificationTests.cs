using System.Reflection;
using Dsg.Hrms.Application.Common.Security;
using Dsg.Hrms.Domain.Common;
using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Architecture.Tests;

/// <summary>
/// Personel varliklarinin her metin ve tarih alani siniflandirilmistir (#244, tasarim gozden
/// gecirmesi B-02; ADR-0009 §4).
/// </summary>
/// <remarks>
/// <para>
/// Denetim izi ve gunluk maskelemesi alanin ozniteligine bakar; ozniteligi olmayan alan icin
/// yalnizca ad listesi kalir ve listede olmayan ad duz metin yazilir. T1 kan grubu, adres ve
/// acil durum kisisi gibi yeni kisisel veri alanlari ekleyecek; bu test bir alanin
/// "unutularak" duz metin yazilmasini engeller: isaretsiz alan testi dusurur ve PR'da bir
/// karar verilmesini zorunlu kilar.
/// </para>
/// <para>
/// Kapsam: <c>Dsg.Hrms.Domain.Personnel</c> ad alanindaki kalici varliklarin <c>string</c> ve
/// <c>DateOnly</c> alanlari. Kimlik ve sayisal alanlar kisisel veri tasimaz.
/// </para>
/// </remarks>
public sealed class DataClassificationTests
{
    private static readonly Type[] Classifications =
    [
        typeof(PersonalDataAttribute),
        typeof(SpecialCategoryDataAttribute),
        typeof(SecretAttribute),
        typeof(AuditVisibleAttribute),
    ];

    [Fact]
    public void Every_text_and_date_field_of_personnel_entities_is_classified()
    {
        var unclassified = Fields()
            .Where(p => p.GetCustomAttributes().Count(a => Classifications.Contains(a.GetType())) != 1)
            .Select(Name)
            .ToList();

        unclassified.ShouldBeEmpty(
            "Her alan tam olarak bir siniflandirma tasimali: [PersonalData], [SpecialCategoryData], [Secret] veya gerekceli [AuditVisible]");
    }

    [Fact]
    public void Audit_visible_fields_state_a_reason()
    {
        var missing = Fields()
            .Where(p => p.GetCustomAttribute<AuditVisibleAttribute>() is { } a && a.Reason.Trim().Length < 20)
            .Select(Name)
            .ToList();

        missing.ShouldBeEmpty("[AuditVisible] gerekcesi en az bir cumle olmali");
    }

    [Fact]
    public void Known_sensitive_names_cannot_be_marked_audit_visible()
    {
        // TCKN, e-posta, telefon, parola gibi adlar ad listesinde hassas sayilir; bir alana bu
        // adla [AuditVisible] verilmesi maskelemeyi bilerek kapatmak olurdu.
        var violations = Fields()
            .Where(p => p.GetCustomAttribute<AuditVisibleAttribute>() is not null
                        && MaskRules.ForName(p.Name).Action != MaskAction.None)
            .Select(Name)
            .ToList();

        violations.ShouldBeEmpty();
    }

    [Fact]
    public void Classified_fields_resolve_to_the_expected_mask_decision()
    {
        MaskRules.For(typeof(Person).GetProperty(nameof(Person.NationalId))!).Kind.ShouldBe(PersonalDataKind.NationalId);
        MaskRules.For(typeof(Person).GetProperty(nameof(Person.MobilePhone))!).Kind.ShouldBe(PersonalDataKind.Phone);
        MaskRules.For(typeof(Person).GetProperty(nameof(Person.LastName))!).ShouldBe(MaskDecision.None);
    }

    [Fact]
    public void Field_discovery_is_not_empty()
    {
        // Kesif bozulursa yukaridaki testler hicbir sey denetlemeden gecerdi.
        var names = Fields().Select(Name).ToList();

        names.Count.ShouldBeGreaterThanOrEqualTo(11);
        names.ShouldContain("Person.NationalId");
        names.ShouldContain("Employment.HireDate");
    }

    /// <summary>
    /// Taranan ad alanlari: personel, T1'de eklenen referans veri ve kurum tanimlari (SYG-PERS-067,
    /// ADR-0021 §6 / B-12).
    /// </summary>
    private static readonly string[] Namespaces =
    [
        typeof(Person).Namespace!,
        typeof(Dsg.Hrms.Domain.Reference.ReferenceDefinition).Namespace!,
        typeof(Dsg.Hrms.Domain.Organization.Company).Namespace!,
    ];

    private static bool InScope(string? ns) =>
        ns is not null && Namespaces.Any(n => ns == n || ns.StartsWith(n + ".", StringComparison.Ordinal));

    private static List<PropertyInfo> Fields() =>
        typeof(Person).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && InScope(t.Namespace) && t.GetProperty("Id") is not null)
            .SelectMany(t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .Where(p => InScope(p.DeclaringType!.Namespace))
            .Where(p => IsTextOrDate(p.PropertyType))
            .ToList();

    private static bool IsTextOrDate(Type type) =>
        type == typeof(string) || (Nullable.GetUnderlyingType(type) ?? type) == typeof(DateOnly);

    private static string Name(PropertyInfo property) => $"{property.DeclaringType!.Name}.{property.Name}";
}
