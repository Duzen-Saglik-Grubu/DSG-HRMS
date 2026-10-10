using System.Globalization;
using Dsg.Hrms.Domain.Identity;

namespace Dsg.Hrms.Application.Identity.Verification;

/// <summary>
/// Dogrulama kodu iletilerinin metinleri (SYG-KMLK-030, <c>analiz/02</c> §5).
/// </summary>
/// <remarks>
/// <para>
/// Ileti kod ve gecerlilik suresi disinda KISISEL VERI icermez: ad, sicil veya TCKN
/// yazilmaz. Ileti yanlis kisiye ulassa bile kimin adina gonderildigi anlasilmaz.
/// </para>
/// <para>
/// SMS tek boya sigar (TR kodlamasinda 150 karakter). Uzun ileti iki SMS olarak
/// ucretlendirilir ve bazi cihazlarda parcali gelir.
/// </para>
/// </remarks>
public static class VerificationMessages
{
    /// <summary>Kurum adi.</summary>
    public const string Organization = "Düzen Sağlık Grubu";

    /// <summary>TR kodlamasinda tek SMS boyu.</summary>
    public const int SmsLimit = 150;

    /// <summary>SMS metni.</summary>
    public static string Sms(string code, VerificationPurpose purpose, int lifetimeMinutes) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{Organization} DSG-HRMS {PurposeText(purpose)} kodunuz: {code}. {lifetimeMinutes} dakika geçerlidir. Kodu kimseyle paylaşmayın.");

    /// <summary>E-posta konusu.</summary>
    public static string EmailSubject(VerificationPurpose purpose) =>
        $"DSG-HRMS {PurposeText(purpose)} kodu";

    /// <summary>E-posta govdesi (duz metin).</summary>
    public static string EmailBody(string code, VerificationPurpose purpose, int lifetimeMinutes) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"""
            Merhaba,

            DSG-HRMS {PurposeText(purpose)} kodunuz: {code}

            Kod {lifetimeMinutes} dakika geçerlidir. Kodu kimseyle paylaşmayın; {Organization} çalışanları sizden bu kodu istemez.

            Bu işlemi siz başlatmadıysanız iletiyi dikkate almayın ve Bilgi İşlem birimine bilgi verin.

            {Organization}
            """);

    private static string PurposeText(VerificationPurpose purpose) => purpose switch
    {
        VerificationPurpose.Registration => "üyelik doğrulama",
        VerificationPurpose.PasswordReset => "parola sıfırlama",
        VerificationPurpose.TwoFactor => "giriş doğrulama",
        VerificationPurpose.TwoFactorSetup => "iki adımlı doğrulamayı açma",
        _ => "doğrulama",
    };
}
