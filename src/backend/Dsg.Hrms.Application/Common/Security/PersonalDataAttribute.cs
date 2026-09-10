namespace Dsg.Hrms.Application.Common.Security;

/// <summary>
/// Isaretlendigi ozelligin gunluk ve denetim kayitlarinda maskelenmesini saglar.
/// </summary>
/// <remarks>
/// <para>
/// Maskeleme kararinin veri modelinin <b>yaninda</b> durmasi bilincli bir tercihtir:
/// yeni bir alan eklendiginde maskeleme kuralinin ayri bir listede guncellenmesi
/// unutulabilirdi. Burada alan ve kurali ayni satirda yasar.
/// </para>
/// <example>
/// <code>
/// public sealed class PersonDto
/// {
///     [PersonalData(PersonalDataKind.NationalId)]
///     public string NationalId { get; init; }
/// }
/// </code>
/// </example>
/// </remarks>
/// <param name="kind">Maskeleme kuralini belirleyen veri turu.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class PersonalDataAttribute(PersonalDataKind kind = PersonalDataKind.Unspecified)
    : Attribute
{
    /// <summary>Maskeleme kuralini belirleyen veri turu.</summary>
    public PersonalDataKind Kind { get; } = kind;
}
