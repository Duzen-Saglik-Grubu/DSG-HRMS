namespace Dsg.Hrms.Application.Common.Exceptions;

/// <summary>
/// Kullaniciya gosterilebilir is hatalarinin ortak tabani (ADR-0010 §5).
/// </summary>
/// <remarks>
/// <para>
/// Bu tabandan turemek, hatanin <b>beklenen</b> bir durum oldugunu ve mesajinin
/// kullaniciya gosterilmeye uygun oldugunu belirtir. Diger tum istisnalar
/// beklenmeyen kabul edilir: kullaniciya genel bir mesaj doner, ayrinti yalnizca
/// sunucu gunluguna yazilir.
/// </para>
/// <para>
/// Ayrim bilinclidir: "hangi hata mesaji disariya cikabilir" karari tek tek cagri
/// yerlerine birakilirsa, er ya da gec bir veritabani mesaji kullanicinin ekranina
/// duser ve veri modelini tarif eder.
/// </para>
/// </remarks>
/// <param name="message">Kullaniciya gosterilecek Turkce mesaj.</param>
public abstract class HrmsException(string message) : Exception(message)
{
    /// <summary>Yanitta donecek HTTP durum kodu (ADR-0010 §6).</summary>
    public abstract int StatusCode { get; }

    /// <summary>Hata turu tanimlayicisi; <c>type</c> alaninda kullanilir.</summary>
    public abstract string ErrorType { get; }

    /// <summary>Yanitta donecek kisa baslik.</summary>
    public abstract string Title { get; }
}
