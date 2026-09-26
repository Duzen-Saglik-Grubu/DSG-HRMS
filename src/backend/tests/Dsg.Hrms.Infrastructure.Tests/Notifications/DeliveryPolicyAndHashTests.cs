using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Dsg.Hrms.Infrastructure.Identity;
using Dsg.Hrms.Infrastructure.Notifications;

namespace Dsg.Hrms.Infrastructure.Tests.Notifications;

/// <summary>Gonderim kipleri (#85) ve kod ozeti (SYG-KMLK-025).</summary>
public sealed class DeliveryPolicyAndHashTests
{
    // ------------------------------------------------------------------ gonderim kipleri

    [Fact]
    public void Default_mode_sends_nothing()
    {
        // Yanlis yapilandirilmis bir ortam gercek personele ileti gondermemeli.
        var options = new NotificationOptions();

        options.DeliveryMode.ShouldBe(DeliveryMode.LogOnly);
        options.Permits("ahmet.yilmaz@duzen.com.tr").ShouldBeFalse();
        options.Permits("5321234567").ShouldBeFalse();
    }

    [Fact]
    public void Allow_list_permits_only_listed_recipients()
    {
        var options = new NotificationOptions
        {
            DeliveryMode = DeliveryMode.AllowList,
            AllowedRecipients = "bilgi.islem@duzen.com.tr, 5321234567",
        };

        options.Permits("Bilgi.Islem@duzen.com.tr").ShouldBeTrue();
        options.Permits("5321234567").ShouldBeTrue();
        options.Permits("ahmet.yilmaz@duzen.com.tr").ShouldBeFalse();
        options.Permits("5329999999").ShouldBeFalse();
    }

    [Fact]
    public void Send_mode_permits_everyone()
    {
        new NotificationOptions { DeliveryMode = DeliveryMode.Send }.Permits("ahmet.yilmaz@duzen.com.tr").ShouldBeTrue();
    }

    [Fact]
    public void Empty_allow_list_fails_validation()
    {
        var options = new NotificationOptions { DeliveryMode = DeliveryMode.AllowList };

        Validator.TryValidateObject(options, new ValidationContext(options), [], validateAllProperties: true).ShouldBeFalse();
    }

    // ------------------------------------------------------------------ kod ozeti

    private static HmacVerificationCodeHasher Hasher(string? key = null) =>
        new(new CodeHashOptions { CodeHashKey = key ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)) });

    [Fact]
    public void Hash_matches_only_the_same_code_for_the_same_record()
    {
        var hasher = Hasher();
        var id = Guid.CreateVersion7();

        var hash = hasher.Hash(id, "482915");

        hash.Length.ShouldBe(44); // veritabani kisiti ck_verification_code_hash_length
        hash.ShouldNotContain("482915");
        hasher.Matches(id, "482915", hash).ShouldBeTrue();
        hasher.Matches(id, "482916", hash).ShouldBeFalse();
        hasher.Matches(Guid.CreateVersion7(), "482915", hash).ShouldBeFalse();
    }

    [Fact]
    public void Same_code_hashes_differently_for_different_records_and_keys()
    {
        var hasher = Hasher();

        hasher.Hash(Guid.CreateVersion7(), "482915").ShouldNotBe(hasher.Hash(Guid.CreateVersion7(), "482915"));

        var id = Guid.CreateVersion7();
        Hasher().Hash(id, "482915").ShouldNotBe(Hasher().Hash(id, "482915"));
    }

    [Fact]
    public void Corrupt_stored_hash_does_not_match()
    {
        Hasher().Matches(Guid.CreateVersion7(), "482915", "%%bozuk%%").ShouldBeFalse();
    }

    [Fact]
    public void Missing_key_is_reported_and_hashing_fails()
    {
        var hasher = new HmacVerificationCodeHasher(new CodeHashOptions());

        hasher.IsConfigured.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => hasher.Hash(Guid.CreateVersion7(), "1")).Message.ShouldContain("Identity:CodeHashKey");
    }

    [Theory]
    [InlineData("kisa")]
    [InlineData("QUJD")]
    public void Invalid_key_fails_validation_without_revealing_it(string key)
    {
        var options = new CodeHashOptions { CodeHashKey = key };
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true).ShouldBeFalse();
        results.Single().ErrorMessage!.ShouldNotContain(key);
    }
}
