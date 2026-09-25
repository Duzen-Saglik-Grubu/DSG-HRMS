using Dsg.Hrms.Application.Common.Security;

namespace Dsg.Hrms.Application.Tests.Common.Security;

/// <summary>
/// Maskeleme kararinin iki katmanini dogrular: oznitelik ve ad benzerligi.
/// </summary>
public sealed class MaskRulesTests
{
    private sealed class MarkedSample
    {
        [PersonalData(PersonalDataKind.NationalId)]
        public string? NationalIdentity { get; init; }

        [PersonalData]
        public string? Address { get; init; }

        [Secret]
        public string? OneTimeCode { get; init; }

        public string? Title { get; init; }
    }

    private sealed class UnmarkedSample
    {
        // Oznitelik BILEREK yazilmadi: ikinci savunma hattini dogrular.
        public string? NationalId { get; init; }

        public string? Email { get; init; }

        public string? Password { get; init; }

        public string? DepartmentName { get; init; }
    }

    private static MaskDecision DecisionFor<T>(string propertyName) =>
        MaskRules.For(typeof(T).GetProperty(propertyName)!);

    [Fact]
    public void Attribute_determines_the_masking_rule()
    {
        var decision = DecisionFor<MarkedSample>(nameof(MarkedSample.NationalIdentity));

        decision.Action.ShouldBe(MaskAction.Mask);
        decision.Kind.ShouldBe(PersonalDataKind.NationalId);
        MaskRules.Apply(decision, "12345678901").ShouldBe("123*****901");
    }

    [Fact]
    public void Attribute_without_a_kind_masks_the_value_completely()
    {
        var decision = DecisionFor<MarkedSample>(nameof(MarkedSample.Address));

        MaskRules.Apply(decision, "Ankara, Cankaya").ShouldBe(Mask.Redacted);
    }

    [Fact]
    public void Secret_attribute_excludes_the_value_entirely()
    {
        var decision = DecisionFor<MarkedSample>(nameof(MarkedSample.OneTimeCode));

        decision.Action.ShouldBe(MaskAction.Exclude);
        MaskRules.Apply(decision, "483920").ShouldBe(Mask.SecretPlaceholder);
    }

    [Fact]
    public void Ordinary_fields_are_left_untouched()
    {
        // Asiri maskeleme, gunlugu ise yaramaz hâle getirir. Hassas olmayan
        // alanlar okunur kalmalidir.
        var decision = DecisionFor<MarkedSample>(nameof(MarkedSample.Title));

        decision.Action.ShouldBe(MaskAction.None);
        MaskRules.Apply(decision, "Laboratuvar Sorumlusu").ShouldBe("Laboratuvar Sorumlusu");
    }

    [Theory]
    [InlineData(nameof(UnmarkedSample.NationalId), MaskAction.Mask)]
    [InlineData(nameof(UnmarkedSample.Email), MaskAction.Mask)]
    [InlineData(nameof(UnmarkedSample.Password), MaskAction.Exclude)]
    public void Known_sensitive_names_are_protected_even_without_an_attribute(
        string propertyName,
        MaskAction expected)
    {
        // Ikinci savunma hatti: bir gelistiricinin oznitelik yazmayi unutmasi,
        // tek basina KVKK ihlaline donusmemelidir.
        DecisionFor<UnmarkedSample>(propertyName).Action.ShouldBe(expected);
    }

    [Fact]
    public void Birth_date_is_masked_completely_because_it_is_an_authentication_factor()
    {
        // Uyelikte TCKN + dogum tarihi birlikte kimlik dogrular (SYG-KMLK-013).
        // Denetim izinde ikisinin birden acik gorunmesi, baskasi adina uyelik
        // baslatmaya yeterli bilgiyi verirdi.
        var decision = MaskRules.ForName("BirthDate");

        decision.Action.ShouldBe(MaskAction.Mask);
        var masked = MaskRules.Apply(decision, "1985-04-12");
        masked.ShouldNotBeNull();
        masked.ShouldNotContain("1985");
        masked.ShouldNotContain("04-12");
    }

    [Fact]
    public void Ordinary_names_are_not_caught_by_the_name_based_rule()
    {
        DecisionFor<UnmarkedSample>(nameof(UnmarkedSample.DepartmentName))
            .Action.ShouldBe(MaskAction.None);
    }

    [Theory]
    [InlineData("iban")]
    [InlineData("IBAN")]
    [InlineData("Iban")]
    public void Name_matching_is_case_insensitive_and_culture_independent(string name)
    {
        // Turkce kulturde "I" harfinin kucugu "i" degildir. Kultur duyarli bir
        // karsilastirma "IBAN" adini kacirabilirdi.
        MaskRules.ForName(name).Kind.ShouldBe(PersonalDataKind.Iban);
    }

    [Fact]
    public void Unknown_name_yields_no_decision()
    {
        MaskRules.ForName("Aciklama").ShouldBe(MaskDecision.None);
        MaskRules.ForName(null).ShouldBe(MaskDecision.None);
    }
}
