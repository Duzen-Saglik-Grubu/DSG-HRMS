namespace Dsg.Hrms.Application.Common.Exceptions;

/// <summary>
/// Sozdizimi dogru, ancak is kurali ihlal edildi (ADR-0010 §6).
/// </summary>
/// <remarks>
/// <para>
/// <c>400</c> ile ayrimi bilincldir: <c>400</c>, gonderilen verinin <b>bicimsel</b>
/// olarak gecersiz oldugunu soyler (bos zorunlu alan, hatali tarih bicimi).
/// <c>422</c> ise verinin bicimsel olarak dogru ama <b>is kurallarina</b> aykiri
/// oldugunu soyler: "yillik izin bakiyeniz yetersiz" gibi.
/// </para>
/// <para>
/// Bu ayrim frontend icin anlamlidir: ilkinde alan duzeltilir, ikincisinde kullaniciya
/// aciklama gosterilir.
/// </para>
/// </remarks>
public sealed class BusinessRuleException(string message) : HrmsException(message)
{
    /// <inheritdoc />
    public override int StatusCode => 422;

    /// <inheritdoc />
    public override string ErrorType => "business-rule";

    /// <inheritdoc />
    public override string Title => "Is kurali ihlali";
}
