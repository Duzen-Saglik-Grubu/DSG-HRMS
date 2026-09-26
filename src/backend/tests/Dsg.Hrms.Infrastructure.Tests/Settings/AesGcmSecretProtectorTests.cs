using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Dsg.Hrms.Infrastructure.Settings;
using Microsoft.Extensions.Configuration;

namespace Dsg.Hrms.Infrastructure.Tests.Settings;

/// <summary>Sir parametre sifrelemesi (SYG-KMLK-075).</summary>
public sealed class AesGcmSecretProtectorTests
{
    private static string NewKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static AesGcmSecretProtector Create(string? key) => new(new SecretProtectionOptions { Key = key });

    [Fact]
    public void Round_trip_restores_the_value()
    {
        var protector = Create(NewKey());

        var protectedValue = protector.Protect("P@rola ğüşİ 123", "PRM-ENT-06");

        protectedValue.ShouldStartWith("v1:");
        protectedValue.ShouldNotContain("P@rola");
        protector.Unprotect(protectedValue, "PRM-ENT-06").ShouldBe("P@rola ğüşİ 123");
    }

    [Fact]
    public void Same_value_encrypts_differently_each_time()
    {
        // Rastgele nonce: ayni parolayi tasiyan iki satir veritabaninda ayirt edilemez olmali.
        var protector = Create(NewKey());

        protector.Protect("parola", "PRM-ENT-06").ShouldNotBe(protector.Protect("parola", "PRM-ENT-06"));
    }

    [Fact]
    public void Value_cannot_be_moved_to_another_parameter()
    {
        var protector = Create(NewKey());
        var smtp = protector.Protect("parola", "PRM-ENT-06");

        Should.Throw<CryptographicException>(() => protector.Unprotect(smtp, "PRM-ENT-02"));
    }

    [Fact]
    public void Value_cannot_be_decrypted_with_another_key()
    {
        // Veritabani yedegi baska bir kuruluma tasinsa bile sirlar acilmaz.
        var protectedValue = Create(NewKey()).Protect("parola", "PRM-ENT-06");

        Should.Throw<CryptographicException>(() => Create(NewKey()).Unprotect(protectedValue, "PRM-ENT-06"));
    }

    [Fact]
    public void Tampered_value_is_rejected()
    {
        var protector = Create(NewKey());
        var payload = Convert.FromBase64String(protector.Protect("parola", "PRM-ENT-06")[3..]);
        payload[^1] ^= 0x01;

        Should.Throw<CryptographicException>(() => protector.Unprotect("v1:" + Convert.ToBase64String(payload), "PRM-ENT-06"));
    }

    [Theory]
    [InlineData("parola")]
    [InlineData("v2:AAAA")]
    [InlineData("v1:%%%")]
    [InlineData("v1:AAAA")]
    public void Unrecognized_format_is_rejected(string value)
    {
        Should.Throw<CryptographicException>(() => Create(NewKey()).Unprotect(value, "PRM-ENT-06"));
    }

    [Fact]
    public void Without_a_key_nothing_can_be_protected()
    {
        var protector = Create(null);

        protector.IsConfigured.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => protector.Protect("parola", "PRM-ENT-06"))
            .Message.ShouldContain("ParameterProtection:Key");
        Should.Throw<InvalidOperationException>(() => protector.Unprotect("v1:AAAA", "PRM-ENT-06"));
    }

    [Theory]
    [InlineData("kisa")]
    [InlineData("QUJD")] // gecerli Base64, 3 bayt
    public void Invalid_key_fails_validation_without_revealing_it(string key)
    {
        var options = new SecretProtectionOptions { Key = key };
        var results = new List<ValidationResult>();

        Validator.TryValidateObject(options, new ValidationContext(options), results, validateAllProperties: true).ShouldBeFalse();
        results.Single().ErrorMessage!.ShouldNotContain(key);
    }

    [Fact]
    public void Missing_or_valid_key_passes_validation()
    {
        foreach (var options in new[] { new SecretProtectionOptions(), new SecretProtectionOptions { Key = NewKey() } })
        {
            Validator.TryValidateObject(options, new ValidationContext(options), [], validateAllProperties: true).ShouldBeTrue();
        }
    }

    // ------------------------------------------------------------------ yapilandirma dogrulamasi

    [Fact]
    public void Valid_configuration_passes()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PersonnelSync:IntervalMinutes"] = "15",
            ["Parameters:SmtpServer"] = "smtp.duzen.com.tr:587",
            ["Parameters:SmtpPassword"] = "herhangi bir sir", // sirlar bicim denetimine girmez
        }).Build();

        Should.NotThrow(() => SystemParameters.ValidateConfiguration(configuration));
    }

    [Fact]
    public void Invalid_configuration_fails_at_startup_and_names_the_key()
    {
        // ADR-0008 §4: gecersiz ayar ilk kullanimda degil, acilista fark edilir.
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["PersonnelSync:IntervalMinutes"] = "0",
            ["Parameters:TwoFactorEnabled"] = "evet",
        }).Build();

        var ex = Should.Throw<InvalidOperationException>(() => SystemParameters.ValidateConfiguration(configuration));
        ex.Message.ShouldContain("PersonnelSync:IntervalMinutes");
        ex.Message.ShouldContain("PRM-ENT-07");
        ex.Message.ShouldContain("Parameters:TwoFactorEnabled");
    }
}
