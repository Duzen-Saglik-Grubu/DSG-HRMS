using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Application.Identity.Registration;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Application.Tests.Settings;

namespace Dsg.Hrms.Application.Tests.Identity;

/// <summary>Parola kurallari (SYG-KMLK-044, 045).</summary>
public sealed class PasswordPolicyTests
{
    private readonly FakeSystemParameters _parameters = new();
    private readonly StubList _list = new("123456", "password", "qwerty123");

    private Task<IReadOnlyList<PasswordViolation>> ValidateAsync(string password) =>
        new PasswordPolicy(_parameters, _list).ValidateAsync(
            password,
            RegistrationService.PersonalWords("Cemal Alptuğ", "Erdoğan", "cemal.erdogan@duzen.com.tr"),
            CancellationToken.None);

    [Theory]
    [InlineData("Kediler uyur")]
    [InlineData("mavi-balon-7")]
    [InlineData("ağaç kök")]
    public async Task Reasonable_passwords_are_accepted(string password)
    {
        (await ValidateAsync(password)).ShouldBeEmpty();
    }

    [Fact]
    public async Task Minimum_length_follows_the_parameter()
    {
        (await ValidateAsync("a1b2c")).ShouldBe([PasswordViolation.TooShort]);

        _parameters.With(ParameterCatalog.MinPasswordLength, "10");
        (await ValidateAsync("Kediler uy")).ShouldBeEmpty();
        (await ValidateAsync("Kediler u")).ShouldContain(PasswordViolation.TooShort);
    }

    [Fact]
    public async Task Length_is_counted_in_characters_not_utf16_units()
    {
        // "ğ" bir karakterdir; 6 Turkce harf 6 karakter sayilir.
        (await ValidateAsync("ğüşıöç")).ShouldBeEmpty();
    }

    [Fact]
    public async Task Maximum_length_is_128()
    {
        (await ValidateAsync(new string('k', 128) + "")).ShouldBeEmpty();
        (await ValidateAsync(new string('k', 129))).ShouldContain(PasswordViolation.TooLong);
    }

    [Fact]
    public async Task Complexity_is_off_by_default_and_can_be_enabled()
    {
        (await ValidateAsync("sadece harfler")).ShouldBeEmpty();

        _parameters.With(ParameterCatalog.RequireComplexPassword, "true");

        (await ValidateAsync("sadece harfler")).ShouldBe([PasswordViolation.NotComplex]);
        (await ValidateAsync("Kedi-uyur-7")).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("PASSWORD")]
    [InlineData("QwErTy123")]
    public async Task Common_passwords_are_rejected_case_insensitively(string password)
    {
        (await ValidateAsync(password)).ShouldContain(PasswordViolation.Common);
    }

    [Theory]
    [InlineData("erdogan")]
    [InlineData("Erdoğan1985")]
    [InlineData("alptug!!")]
    [InlineData("cemalerdogan")]
    [InlineData("CemalAlptuğ")]
    [InlineData("duzen2026")]
    [InlineData("Zeytinim!")]
    [InlineData("123labpt")]
    public async Task Passwords_derived_from_personal_or_organization_words_are_rejected(string password)
    {
        (await ValidateAsync(password)).ShouldContain(PasswordViolation.ContainsPersonalOrOrganizationWord);
    }

    [Fact]
    public async Task Longer_phrases_containing_a_word_are_not_rejected()
    {
        // Sozcugun parolanin icinde gecmesi yetmez; yalnizca sozcuk + ek reddedilir.
        (await ValidateAsync("bayram duzenleme gunu")).ShouldBeEmpty();
    }

    [Fact]
    public void Normalization_makes_equivalent_forms_equal()
    {
        // Bilesik "ü" (U+00FC) ve ayrik "u" + birlestirici iki nokta (U+0308).
        PasswordPolicy.Normalize("üzüm").ShouldBe(PasswordPolicy.Normalize("üzüm"));
    }

    private sealed class StubList(params string[] items) : ICommonPasswordList
    {
        public int Count => items.Length;

        public bool Contains(string loweredPassword) => items.Contains(loweredPassword);
    }
}
