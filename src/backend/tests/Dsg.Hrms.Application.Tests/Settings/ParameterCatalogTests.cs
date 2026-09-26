using System.Text.RegularExpressions;
using Dsg.Hrms.Application.Settings;

namespace Dsg.Hrms.Application.Tests.Settings;

/// <summary>Parametre katalogu ve deger dogrulamasi (SYG-KMLK-075, 076).</summary>
public sealed partial class ParameterCatalogTests
{
    public static TheoryData<string> AllKeys()
    {
        var data = new TheoryData<string>();
        foreach (var parameter in ParameterCatalog.All)
        {
            data.Add(parameter.Key);
        }

        return data;
    }

    // ------------------------------------------------------------------ katalog butunlugu

    [Fact]
    public void Keys_and_configuration_keys_are_unique()
    {
        ParameterCatalog.All.Select(p => p.Key).ShouldBeUnique();
        ParameterCatalog.All.Select(p => p.ConfigurationKey).ShouldBeUnique();
    }

    [Theory]
    [MemberData(nameof(AllKeys))]
    public void Every_default_value_passes_its_own_validation(string key)
    {
        // Varsayilan gecersiz olsaydi, parametre hic ayarlanmamis bir kurulumda ilk
        // okumada hata verirdi.
        var parameter = ParameterCatalog.Find(key)!;

        if (parameter.DefaultValue is null)
        {
            return;
        }

        var result = parameter.Validate(parameter.DefaultValue);
        result.IsValid.ShouldBeTrue(result.Error);
        result.CanonicalValue.ShouldBe(parameter.DefaultValue);
    }

    [Theory]
    [MemberData(nameof(AllKeys))]
    public void Keys_follow_the_catalog_format_and_configuration_keys_are_english(string key)
    {
        var parameter = ParameterCatalog.Find(key)!;

        KeyFormat().IsMatch(parameter.Key).ShouldBeTrue();

        // KR-058: yapilandirma anahtarlari Ingilizce ve ASCII'dir.
        ConfigurationKeyFormat().IsMatch(parameter.ConfigurationKey).ShouldBeTrue(parameter.ConfigurationKey);
    }

    [Fact]
    public void Secrets_have_no_default_value()
    {
        // Varsayilan parola kaynak koda yazilmis bir sir olurdu (ADR-0008).
        ParameterCatalog.All.Where(p => p.IsSecret).ShouldAllBe(p => p.DefaultValue == null);
        ParameterCatalog.All.Where(p => p.IsSecret).Select(p => p.Key).ShouldBe(["PRM-ENT-02", "PRM-ENT-06"]);
    }

    [Fact]
    public void Catalog_values_match_the_approved_requirements()
    {
        // Onayli paydas gereksinimlerindeki varsayilanlar (PG-KMLK). Degisiklik icin
        // tur:degisiklik-talebi issue'su gerekir.
        ParameterCatalog.MaxFailedLogins.DefaultValue.ShouldBe("5");        // REQ-KMLK-024
        ParameterCatalog.LockoutMinutes.DefaultValue.ShouldBe("15");        // REQ-KMLK-024
        ParameterCatalog.MinPasswordLength.DefaultValue.ShouldBe("6");      // REQ-KMLK-028
        ParameterCatalog.RequireComplexPassword.DefaultValue.ShouldBe("false");
        ParameterCatalog.TwoFactorEnabled.DefaultValue.ShouldBe("false");   // REQ-KMLK-025
        ParameterCatalog.VerificationCodeLength.DefaultValue.ShouldBe("6"); // REQ-KMLK-015
        ParameterCatalog.VerificationCodeLifetimeMinutes.DefaultValue.ShouldBe("5"); // REQ-KMLK-016
        ParameterCatalog.MaxVerificationAttempts.DefaultValue.ShouldBe("3"); // REQ-KMLK-017
        ParameterCatalog.IdleTimeoutMinutes.DefaultValue.ShouldBe("30");    // REQ-KMLK-026
        ParameterCatalog.InviteLinkLifetimeHours.DefaultValue.ShouldBe("3"); // REQ-KMLK-011
        ParameterCatalog.AutoDeactivateOnEmploymentEnd.DefaultValue.ShouldBe("true"); // REQ-KMLK-034
        ParameterCatalog.SyncIntervalMinutes.DefaultValue.ShouldBe("15");   // KR-007
        ParameterCatalog.SmsSenderTitle.DefaultValue.ShouldBe("DUZEN");     // REQ-KMLK-021
        ParameterCatalog.AcceptedEmailDomains.DefaultValue.ShouldBe("duzen.com.tr,zeytinim.com,labpt.com.tr");
    }

    [Fact]
    public void Find_returns_null_for_unknown_key()
    {
        ParameterCatalog.Find("PRM-XXX-99").ShouldBeNull();
        ParameterCatalog.Find("PRM-KML-03").ShouldBe(ParameterCatalog.MaxFailedLogins);
    }

    // ------------------------------------------------------------------ dogrulama

    [Theory]
    [InlineData(" 7 ", true, "7")]
    [InlineData("3", true, "3")]
    [InlineData("10", true, "10")]
    [InlineData("2", false, null)]
    [InlineData("11", false, null)]
    [InlineData("-5", false, null)]
    [InlineData("5.0", false, null)]
    [InlineData("bes", false, null)]
    [InlineData("", false, null)]
    [InlineData(null, false, null)]
    public void Integer_values_are_checked_against_the_range(string? input, bool valid, string? canonical)
    {
        var result = ParameterCatalog.MaxFailedLogins.Validate(input);

        result.IsValid.ShouldBe(valid);
        result.CanonicalValue.ShouldBe(canonical);
        if (!valid)
        {
            result.Error.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Theory]
    [InlineData("true", true, "true")]
    [InlineData("FALSE", true, "false")]
    [InlineData("True", true, "true")]
    [InlineData("evet", false, null)]
    [InlineData("1", false, null)]
    public void Boolean_values_are_canonical(string input, bool valid, string? canonical)
    {
        var result = ParameterCatalog.TwoFactorEnabled.Validate(input);

        result.IsValid.ShouldBe(valid);
        result.CanonicalValue.ShouldBe(canonical);
    }

    [Theory]
    [InlineData("Duzen.com.tr, labpt.com.tr ,duzen.com.tr", true, "duzen.com.tr,labpt.com.tr")]
    [InlineData("yeni-sirket.com.tr", true, "yeni-sirket.com.tr")]
    [InlineData("@duzen.com.tr", false, null)]
    [InlineData("duzen", false, null)]
    [InlineData(" , ", false, null)]
    public void Domain_list_is_normalized_and_validated(string input, bool valid, string? canonical)
    {
        var result = ParameterCatalog.AcceptedEmailDomains.Validate(input);

        result.IsValid.ShouldBe(valid);
        result.CanonicalValue.ShouldBe(canonical);
    }

    [Theory]
    [InlineData("sms", true, "sms")]
    [InlineData("SMS,email", true, "sms,email")]
    [InlineData("whatsapp", false, null)]
    public void Channel_list_accepts_only_known_channels(string input, bool valid, string? canonical)
    {
        var result = ParameterCatalog.VerificationChannels.Validate(input);

        result.IsValid.ShouldBe(valid);
        result.CanonicalValue.ShouldBe(canonical);
    }

    [Theory]
    [InlineData("smtp.duzen.com.tr:587", true)]
    [InlineData("SMTP.duzen.com.tr:25", true)]
    [InlineData("smtp.duzen.com.tr", false)]
    [InlineData("smtp duzen:587", false)]
    public void Smtp_server_requires_host_and_port(string input, bool valid)
    {
        ParameterCatalog.SmtpServer.Validate(input).IsValid.ShouldBe(valid);
    }

    [Fact]
    public void Free_text_is_trimmed()
    {
        ParameterCatalog.SupportContact.Validate("  Bilgi Islem - dahili 1234 ").CanonicalValue
            .ShouldBe("Bilgi Islem - dahili 1234");
    }

    [GeneratedRegex("^PRM-[A-Z]{3}-[0-9]{2}$")]
    private static partial Regex KeyFormat();

    [GeneratedRegex("^[A-Za-z]+:[A-Za-z]+$")]
    private static partial Regex ConfigurationKeyFormat();
}
