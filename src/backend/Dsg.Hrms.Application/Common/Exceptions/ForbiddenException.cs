namespace Dsg.Hrms.Application.Common.Exceptions;

/// <summary>
/// Eylem yetkisi yok (ADR-0010 §6).
/// </summary>
/// <remarks>
/// Yalnizca <b>rol kaynakli</b> yetkisizlik icindir: kullanici o eylemi hicbir kayit
/// uzerinde yapamaz. Belirli bir kaydin kapsam disi olmasi durumu farklidir ve
/// <see cref="NotFoundException"/> ile karsilanir (ADR-0007 §4).
/// </remarks>
public sealed class ForbiddenException(
    string message = "Bu işlem için yetkiniz bulunmuyor.")
    : HrmsException(message)
{
    /// <inheritdoc />
    public override int StatusCode => 403;

    /// <inheritdoc />
    public override string ErrorType => "forbidden";

    /// <inheritdoc />
    public override string Title => "Yetkisiz işlem";
}
