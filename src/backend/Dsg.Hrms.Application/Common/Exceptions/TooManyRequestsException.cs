namespace Dsg.Hrms.Application.Common.Exceptions;

/// <summary>
/// Hiz siniri asildiginda firlatilir (SYG-KMLK-059). HTTP karsiligi <c>429</c>.
/// </summary>
/// <remarks>
/// Ileti hangi sinirin asildigini SOYLEMEZ (TCKN mi, IP mi): hangisinin oldugunu bilmek
/// saldirganin denemelerini ayarlamasina yardim ederdi.
/// </remarks>
public sealed class TooManyRequestsException(string message = "Çok fazla deneme yapıldı. Lütfen bir süre sonra tekrar deneyin.")
    : HrmsException(message)
{
    /// <inheritdoc />
    public override int StatusCode => 429;

    /// <inheritdoc />
    public override string ErrorType => "too-many-requests";

    /// <inheritdoc />
    public override string Title => "Çok fazla deneme";
}
