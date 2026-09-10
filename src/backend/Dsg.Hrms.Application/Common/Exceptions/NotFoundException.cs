namespace Dsg.Hrms.Application.Common.Exceptions;

/// <summary>
/// Kayit bulunamadi <b>veya kapsam disi</b> (ADR-0007 §4, ADR-0010 §6).
/// </summary>
/// <remarks>
/// <para>
/// Yetki kapsami disindaki bir kayit icin de bu istisna kullanilir; <c>403</c>
/// degil <c>404</c> doner. Aksi hâlde yanit, kaydin <b>var oldugunu</b> sizdirirdi:
/// bir kullanici sicil numaralarini deneyerek hangi kayitlarin mevcut oldugunu
/// cikarabilirdi.
/// </para>
/// <para>
/// Bu nedenle mesajda kaydin kimligi de <b>yer almaz</b>.
/// </para>
/// </remarks>
public sealed class NotFoundException(string message = "Kayit bulunamadi.")
    : HrmsException(message)
{
    /// <inheritdoc />
    public override int StatusCode => 404;

    /// <inheritdoc />
    public override string ErrorType => "not-found";

    /// <inheritdoc />
    public override string Title => "Kayit bulunamadi";
}
