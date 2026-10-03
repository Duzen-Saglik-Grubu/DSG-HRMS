using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Domain.Tests.Personnel;

/// <summary>
/// E-posta ve telefonun senkronizasyonda normallestirilmesi (SYG-KMLK-009).
/// </summary>
public sealed class ContactNormalizerTests
{
    [Theory]
    [InlineData("ahmet.yilmaz@duzen.com.tr", "ahmet.yilmaz@duzen.com.tr")]
    [InlineData("  Ahmet.Yilmaz@Duzen.com.tr  ", "ahmet.yilmaz@duzen.com.tr")]
    [InlineData("ahmet.yilmaz@duzen.com.tr\r\n", "ahmet.yilmaz@duzen.com.tr")]
    [InlineData("ahmet .yilmaz@duzen.com.tr", "ahmet.yilmaz@duzen.com.tr")]
    public void Email_is_trimmed_and_lowercased(string raw, string expected)
    {
        ContactNormalizer.NormalizeEmail(raw).ShouldBe(expected);
    }

    [Fact]
    public void Email_lowercasing_is_culture_independent()
    {
        // Turkce kulturde "I".ToLower() == "ı" olurdu ve adres bozulurdu.
        var previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");
            ContactNormalizer.NormalizeEmail("INFO@DUZEN.COM.TR").ShouldBe("info@duzen.com.tr");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("duzen.com.tr")]
    [InlineData("@duzen.com.tr")]
    [InlineData("ahmet@")]
    [InlineData("a@b@duzen.com.tr")]
    public void Malformed_email_yields_null(string? raw)
    {
        ContactNormalizer.NormalizeEmail(raw).ShouldBeNull();
    }

    [Theory]
    [InlineData("5321234567", "5321234567")]
    [InlineData("05321234567", "5321234567")]
    [InlineData("905321234567", "5321234567")]
    [InlineData("+90 532 123 45 67", "5321234567")]
    [InlineData("0 (532) 123-45-67", "5321234567")]
    [InlineData("0532 123 45 67\r\n", "5321234567")]
    public void Mobile_phone_is_normalized_to_ten_digits(string raw, string expected)
    {
        ContactNormalizer.NormalizeMobilePhone(raw).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("03121234567")]   // sabit hat
    [InlineData("532123456")]     // 9 hane
    [InlineData("53212345678")]   // 11 hane, 0 ile baslamiyor
    [InlineData("4321234567")]    // 5 ile baslamiyor
    public void Invalid_mobile_phone_yields_null(string? raw)
    {
        ContactNormalizer.NormalizeMobilePhone(raw).ShouldBeNull();
    }

    [Theory]
    [InlineData("  Ahmet  ", "Ahmet")]
    [InlineData("Ayse\r\nNur", "Ayse Nur")]
    [InlineData("Ayse\t Nur", "Ayse Nur")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void Name_whitespace_is_collapsed(string? raw, string expected)
    {
        ContactNormalizer.NormalizeName(raw).ShouldBe(expected);
    }
}
