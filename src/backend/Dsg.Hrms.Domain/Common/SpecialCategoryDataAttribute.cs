namespace Dsg.Hrms.Domain.Common;

/// <summary>
/// Isaretlendigi ozellik KVKK md. 6 anlaminda ozel nitelikli kisisel veridir (ornek: kan grubu).
/// </summary>
/// <remarks>
/// <para>
/// Denetim izinde ve gunlukte <b>deger hicbir bicimde yazilmaz</b>; denetim izi yalnizca alanin
/// degistigini gosterir (ADR-0009 §4: "ozel nitelikli veri icerigi: 'degisti' bilgisi tutulur").
/// Kismi gosterim de yapilmaz: kan grubu gibi az sayida degeri olan bir alanda kismi gosterim
/// degerin kendisidir.
/// </para>
/// <para>
/// Veritabaninda kolon bazli sifreleme ve gorunturlemenin erisim kaydina
/// <c>SpecialCategoryView</c> olarak yazilmasi ayrica gerekir (ADR-0009 §3, §5).
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SpecialCategoryDataAttribute : Attribute;
