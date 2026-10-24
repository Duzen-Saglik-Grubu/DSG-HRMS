using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Domain.Tests.Personnel;

/// <summary>Turkce karaktersiz arama anahtari (ADR-0021 §7; SYG-PERS-030).</summary>
public sealed class SearchTextTests
{
    [Theory]
    [InlineData("IŞIK", "isik")]
    [InlineData("ışık", "isik")]
    [InlineData("İSMAİL", "ismail")]
    [InlineData("ÇAĞLAR ÖZGÜR", "caglar ozgur")]
    [InlineData("  Şükrü  ", "sukru")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void Turkish_letters_fold_to_ascii_lowercase(string? value, string expected)
    {
        SearchText.Fold(value).ShouldBe(expected);
    }

    [Fact]
    public void Folding_is_culture_independent()
    {
        // Turkce kulturde "I".ToLower() "ı" olurdu; anahtar kulturden bagimsiz olmali.
        var previous = Thread.CurrentThread.CurrentCulture;
        try
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("tr-TR");
            SearchText.Fold("ISMAIL").ShouldBe("ismail");
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Fact]
    public void Person_key_holds_names_and_the_previous_surname_only_when_kept()
    {
        var woman = Person.Create(TestNationalIds.Sequential(1), new PersonDetails("AYŞE", "YILMAZ", new DateOnly(1990, 1, 1), null, false, null));
        woman.SearchKey.ShouldBe("ayse yilmaz");

        woman.ApplyProfile(new PersonProfile(Gender.Female, "DEMİR", MaritalStatus.Married, null));
        woman.SearchKey.ShouldBe("ayse yilmaz demir");

        // B-17: onceki soyadi gosterilmeyen kiside anahtara girmez (erkekte tutulmaz).
        var man = Person.Create(TestNationalIds.Sequential(2), new PersonDetails("ALİ", "KAYA", new DateOnly(1990, 1, 1), null, false, null));
        man.ApplyProfile(new PersonProfile(Gender.Male, "ESKİ", MaritalStatus.Married, null));
        man.SearchKey.ShouldBe("ali kaya");
    }
}
