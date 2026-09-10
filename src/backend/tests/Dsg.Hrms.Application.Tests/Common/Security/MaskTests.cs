using Dsg.Hrms.Application.Common.Security;

namespace Dsg.Hrms.Application.Tests.Common.Security;

/// <summary>
/// Maskeleme kurallarini ve <b>fail-closed</b> davranisini dogrular (ADR-0009 §4).
/// </summary>
/// <remarks>
/// Bu testlerin cogu "beklenmeyen girdi" senaryosudur. Maskelemenin asil degeri
/// duzgun veride degil, bozuk veride ortaya cikar: gunluge duz metin dusen bir
/// T.C. kimlik numarasi KVKK ihlalidir ve geri alinamaz.
/// </remarks>
public sealed class MaskTests
{
    // ------------------------------------------------------------------
    // T.C. Kimlik Numarasi
    // ------------------------------------------------------------------

    [Fact]
    public void National_id_keeps_only_the_first_and_last_three_digits()
    {
        Mask.NationalId("12345678901").ShouldBe("123*****901");
    }

    [Theory]
    [InlineData("1234567890")]      // 10 hane - eksik
    [InlineData("123456789012")]    // 12 hane - fazla
    [InlineData("1234567890A")]     // rakam disi karakter
    [InlineData("")]
    [InlineData("   ")]
    public void National_id_is_fully_masked_when_the_format_is_unexpected(string value)
    {
        // Taninmayan bir deger, yanlis alana yazilmis gercek bir kimlik numarasi
        // olabilir. Oldugu gibi yazmak kabul edilemez.
        Mask.NationalId(value).ShouldBe(Mask.Redacted);
    }

    [Fact]
    public void National_id_tolerates_surrounding_whitespace()
    {
        Mask.NationalId("  12345678901  ").ShouldBe("123*****901");
    }

    [Fact]
    public void Null_stays_null_so_that_missing_data_is_distinguishable()
    {
        // "Veri yok" ile "veri var ama gizlendi" ayrimi tani icin gereklidir.
        Mask.NationalId(null).ShouldBeNull();
        Mask.Phone(null).ShouldBeNull();
        Mask.Email(null).ShouldBeNull();
        Mask.Iban(null).ShouldBeNull();
    }

    // ------------------------------------------------------------------
    // Telefon
    // ------------------------------------------------------------------

    [Theory]
    [InlineData("5321234567")]
    [InlineData("05321234567")]
    [InlineData("905321234567")]
    [InlineData("+90 532 123 45 67")]
    [InlineData("(0532) 123 45 67")]
    public void Phone_is_normalised_before_masking(string value)
    {
        // LOGO'da ayni numara farkli bicimlerde saklanabiliyor (bkz. veri envanteri).
        // Bicim farki maskelemeyi bozmamalidir.
        Mask.Phone(value).ShouldBe("532*****67");
    }

    [Theory]
    [InlineData("3121234567")]      // sabit hat - cep degil
    [InlineData("532123456")]       // eksik hane
    [InlineData("abc")]
    [InlineData("")]
    public void Phone_is_fully_masked_when_the_format_is_unexpected(string value)
    {
        Mask.Phone(value).ShouldBe(Mask.Redacted);
    }

    // ------------------------------------------------------------------
    // E-posta
    // ------------------------------------------------------------------

    [Fact]
    public void Email_keeps_the_domain_so_that_corporate_addresses_stay_distinguishable()
    {
        // Dogrulama kodunun kurumsal adrese mi gittigi gunlukten anlasilabilmelidir.
        Mask.Email("ahmet.yilmaz@duzen.com.tr").ShouldBe("ah***@duzen.com.tr");
    }

    [Fact]
    public void Email_hides_a_very_short_local_part_completely()
    {
        // Tek harflik yerel bolum gorunur birakilirsa, kucuk bir kurumda kisiyi
        // tanimlamaya yeterli olabilir.
        Mask.Email("a@duzen.com.tr").ShouldBe("***@duzen.com.tr");
    }

    [Theory]
    [InlineData("duzen.com.tr")]        // @ yok
    [InlineData("@duzen.com.tr")]       // yerel bolum yok
    [InlineData("ahmet@")]              // alan adi yok
    [InlineData("")]
    public void Email_is_fully_masked_when_the_format_is_unexpected(string value)
    {
        Mask.Email(value).ShouldBe(Mask.Redacted);
    }

    // ------------------------------------------------------------------
    // IBAN
    // ------------------------------------------------------------------

    [Fact]
    public void Iban_keeps_the_country_code_and_the_last_four_digits()
    {
        Mask.Iban("TR330006100519786457841326").ShouldBe("TR***1326");
    }

    [Fact]
    public void Iban_tolerates_the_spaced_form_used_on_bank_documents()
    {
        Mask.Iban("TR33 0006 1005 1978 6457 8413 26").ShouldBe("TR***1326");
    }

    [Theory]
    [InlineData("TR33")]
    [InlineData("")]
    public void Iban_is_fully_masked_when_the_format_is_unexpected(string value)
    {
        Mask.Iban(value).ShouldBe(Mask.Redacted);
    }

    // ------------------------------------------------------------------
    // Tur bazli secim
    // ------------------------------------------------------------------

    [Fact]
    public void Unspecified_kind_is_masked_completely()
    {
        // Tur belirtilmemis bir alan, en kisitlayici sekilde islenir.
        Mask.ByKind(PersonalDataKind.Unspecified, "herhangi bir deger")
            .ShouldBe(Mask.Redacted);
    }

    [Fact]
    public void Unknown_kind_value_is_masked_completely()
    {
        // Numaralandirmaya ileride yeni bir tur eklenir ve maskeleyicisi
        // yazilmayi unutulursa, sonuc sizinti degil fazla maskeleme olmalidir.
        Mask.ByKind((PersonalDataKind)999, "12345678901").ShouldBe(Mask.Redacted);
    }
}
