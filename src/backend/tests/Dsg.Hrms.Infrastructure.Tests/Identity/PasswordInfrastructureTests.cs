using System.Diagnostics;
using Dsg.Hrms.Infrastructure.Identity.Passwords;

namespace Dsg.Hrms.Infrastructure.Tests.Identity;

/// <summary>Parola ozeti (SYG-KMLK-049) ve gomulu yaygin parola listesi (SYG-KMLK-045).</summary>
public sealed class PasswordInfrastructureTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_verifies_only_the_same_password()
    {
        var hash = _hasher.Hash("Kediler uyur");

        hash.ShouldStartWith("pbkdf2-sha512$210000$");
        hash.ShouldNotContain("Kediler");
        _hasher.Verify(hash, "Kediler uyur").ShouldBeTrue();
        _hasher.Verify(hash, "kediler uyur").ShouldBeFalse();
    }

    [Fact]
    public void Same_password_gets_a_different_salt_each_time()
    {
        _hasher.Hash("Kediler uyur").ShouldNotBe(_hasher.Hash("Kediler uyur"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("duz-metin")]
    [InlineData("pbkdf2-sha512$abc$AAAA$AAAA")]
    [InlineData("pbkdf2-sha512$1000$AAAAAAAAAAAAAAAAAAAAAA==$AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=")]
    [InlineData("pbkdf2-sha512$210000$%%%$%%%")]
    [InlineData("md5$1$2$3")]
    public void Malformed_or_weak_hashes_never_verify(string hash)
    {
        // 100.000'den az yinelemeli ozet (SYG-KMLK-049 alt siniri) kabul edilmez.
        _hasher.Verify(hash, "herhangi").ShouldBeFalse();
    }

    [Fact]
    public void Hashing_takes_measurable_work()
    {
        // SYG-KMLK-049: 100–500 ms hedefi UAT donaniminda olculur. Burada yalnizca ozetin
        // "hizli ozet" olmadigi (en az 20 ms) sinanir; CI makinelerinin hizi degisir.
        var stopwatch = Stopwatch.StartNew();
        _hasher.Hash("Kediler uyur");
        stopwatch.ElapsedMilliseconds.ShouldBeGreaterThan(20);
    }

    [Fact]
    public void Embedded_list_has_at_least_100000_entries()
    {
        new EmbeddedCommonPasswordList().Count.ShouldBeGreaterThanOrEqualTo(100_000);
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("password")]
    [InlineData("qwerty123")]
    [InlineData("iloveyou")]
    public void Embedded_list_contains_well_known_passwords(string password)
    {
        new EmbeddedCommonPasswordList().Contains(password).ShouldBeTrue();
    }

    [Fact]
    public void Embedded_list_does_not_contain_a_random_phrase()
    {
        new EmbeddedCommonPasswordList().Contains("mor fil pazartesi 42").ShouldBeFalse();
    }
}
