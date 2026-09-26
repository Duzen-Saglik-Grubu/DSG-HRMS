using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Domain.Tests.Personnel;

public sealed class NationalIdTests
{
    [Theory]
    [InlineData("123456789")]
    [InlineData("987654321")]
    [InlineData("100000001")]
    public void Checksum_computed_values_are_valid(string firstNine)
    {
        NationalId.IsValid(TestNationalIds.From(firstNine)).ShouldBeTrue();
    }

    [Fact]
    public void Wrong_tenth_digit_is_rejected()
    {
        var valid = TestNationalIds.From("123456789");
        var tampered = valid[..9] + (char)('0' + ((valid[9] - '0' + 1) % 10)) + valid[10];

        NationalId.IsValid(tampered).ShouldBeFalse();
    }

    [Fact]
    public void Wrong_eleventh_digit_is_rejected()
    {
        var valid = TestNationalIds.From("123456789");
        var tampered = valid[..10] + (char)('0' + ((valid[10] - '0' + 1) % 10));

        NationalId.IsValid(tampered).ShouldBeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("1234567890")]      // 10 hane
    [InlineData("123456789012")]    // 12 hane
    [InlineData("1234567890a")]     // harf
    [InlineData("１２３４５６７８９０１")] // tam genislikli rakamlar
    public void Malformed_values_are_rejected(string? value)
    {
        NationalId.IsValid(value).ShouldBeFalse();
    }

    [Fact]
    public void Leading_zero_is_rejected_even_when_checksum_matches()
    {
        // Ilk hane 0 olan numara, sagla toplamlari tutsa bile gecersizdir.
        NationalId.IsValid(TestNationalIds.From("012345678")).ShouldBeFalse();
    }
}
