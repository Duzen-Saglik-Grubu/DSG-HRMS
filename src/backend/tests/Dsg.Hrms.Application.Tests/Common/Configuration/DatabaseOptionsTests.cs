using System.ComponentModel.DataAnnotations;
using Dsg.Hrms.Application.Common.Configuration;

namespace Dsg.Hrms.Application.Tests.Common.Configuration;

/// <summary>
/// Yapilandirma dogrulama kurallarini denetler (ADR-0008 §4).
/// </summary>
/// <remarks>
/// Bu testlerin amaci, eksik veya hatali bir ayarin uygulama ACILIRKEN yakalanmasini
/// garanti etmektir. Yakalanmazsa hata, ayarin ilk kullanildigi anda — belki gunler
/// sonra, bir kullanici isleminin ortasinda — ortaya cikar.
/// </remarks>
public sealed class DatabaseOptionsTests
{
    private static List<ValidationResult> Validate(DatabaseOptions options)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(
            options,
            new ValidationContext(options),
            results,
            validateAllProperties: true);

        return results;
    }

    [Fact]
    public void Validation_fails_when_connection_string_is_empty()
    {
        var options = new DatabaseOptions { Hrms = string.Empty };

        var results = Validate(options);

        results.ShouldNotBeEmpty("Baglanti dizesi zorunludur; eksikse uygulama acilmamalidir.");
        results.ShouldContain(r => r.MemberNames.Contains(nameof(DatabaseOptions.Hrms)));
    }

    [Fact]
    public void Error_message_explains_how_to_provide_the_setting()
    {
        // Hata mesajinin degeri, sorunu YASAYAN kisiye ne yapacagini soylemesindedir.
        // "Alan zorunludur" demek yeterli degil; nereye yazilacagi da yazmalidir.
        var results = Validate(new DatabaseOptions { Hrms = string.Empty });

        var message = results[0].ErrorMessage;

        message.ShouldNotBeNull();
        message.ShouldContain("user-secrets", Case.Insensitive);
        message.ShouldContain("Database__Hrms");
    }

    [Fact]
    public void Validation_succeeds_with_valid_settings()
    {
        var options = new DatabaseOptions
        {
            Hrms = "Host=localhost;Database=dsg_hrms;Username=test;Password=test",
        };

        Validate(options).ShouldBeEmpty();
    }

    [Theory]
    [InlineData(4)]      // alt sinirin altinda
    [InlineData(301)]    // ust sinirin ustunde
    [InlineData(-1)]
    public void Invalid_command_timeout_is_rejected(int seconds)
    {
        var options = new DatabaseOptions
        {
            Hrms = "Host=localhost;Database=d;Username=u;Password=p",
            CommandTimeoutSeconds = seconds,
        };

        Validate(options)
            .ShouldContain(r => r.MemberNames.Contains(nameof(DatabaseOptions.CommandTimeoutSeconds)));
    }

    [Theory]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(300)]
    public void Boundary_command_timeout_is_accepted(int seconds)
    {
        var options = new DatabaseOptions
        {
            Hrms = "Host=localhost;Database=d;Username=u;Password=p",
            CommandTimeoutSeconds = seconds,
        };

        Validate(options).ShouldBeEmpty();
    }

    [Fact]
    public void Retry_count_is_bounded()
    {
        var options = new DatabaseOptions
        {
            Hrms = "Host=localhost;Database=d;Username=u;Password=p",
            RetryCount = 11,
        };

        Validate(options)
            .ShouldContain(r => r.MemberNames.Contains(nameof(DatabaseOptions.RetryCount)));
    }

    [Fact]
    public void Detailed_logging_is_disabled_by_default()
    {
        // Kisisel verinin gunluge dusmesine yol acan bir ayarin varsayilani
        // KAPALI olmalidir; acik olmasi bilincli bir tercih gerektirmelidir.
        new DatabaseOptions().DetailedLoggingEnabled.ShouldBeFalse();
    }
}
