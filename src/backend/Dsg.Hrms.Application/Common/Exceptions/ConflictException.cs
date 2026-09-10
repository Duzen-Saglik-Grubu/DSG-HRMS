namespace Dsg.Hrms.Application.Common.Exceptions;

/// <summary>
/// Kaydin mevcut durumuyla cakisan istek (ADR-0010 §6).
/// </summary>
/// <remarks>
/// Ornegin onaylanmis bir izin talebinin yeniden onaylanmaya calisilmasi. Istek
/// bicimsel olarak gecerlidir; cakisan sey kaydin <b>o anki durumudur</b>.
/// </remarks>
public sealed class ConflictException(string message) : HrmsException(message)
{
    /// <inheritdoc />
    public override int StatusCode => 409;

    /// <inheritdoc />
    public override string ErrorType => "conflict";

    /// <inheritdoc />
    public override string Title => "Islem cakismasi";
}
