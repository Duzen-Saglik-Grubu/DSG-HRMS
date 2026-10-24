namespace Dsg.Hrms.Application.Common.Exceptions;

/// <summary>
/// Istek gecersiz (<c>400</c>), ama gecersizligi yalnizca calisma anindaki bir degere gore
/// anlasilabiliyor: or. sayfa boyutunun parametredeki en buyuk degeri asmasi (SYG-PERS-029) veya
/// gorulemeyen alana gore suzgec (SYG-PERS-026). Bicimsel dogrulama istek dogrulayicilarindadir.
/// </summary>
public sealed class InvalidRequestException(string message) : HrmsException(message)
{
    /// <inheritdoc />
    public override int StatusCode => 400;

    /// <inheritdoc />
    public override string ErrorType => "invalid-request";

    /// <inheritdoc />
    public override string Title => "Gecersiz istek";
}
