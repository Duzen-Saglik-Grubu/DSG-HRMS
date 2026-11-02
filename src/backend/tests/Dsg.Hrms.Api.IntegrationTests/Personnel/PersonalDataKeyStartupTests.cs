using System.Security.Cryptography;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Dsg.Hrms.Api.IntegrationTests.Personnel;

/// <summary>
/// Ozel nitelikli veri anahtari zorunludur (SYG-PERS-061; ADR-0020 §2): tanimsiz veya gecersizse
/// uygulama acilmaz ve hata iletisi anahtarin degerini icermez.
/// </summary>
[Collection(ApiHostGroup.Name)]
public sealed class PersonalDataKeyStartupTests
{
    [Fact]
    public void Application_does_not_start_without_the_personal_data_key()
    {
        var ex = Should.Throw<OptionsValidationException>(() => Start(key: null));

        ex.Message.ShouldContain("PersonalDataProtection:Keys");
    }

    [Fact]
    public void Application_does_not_start_with_a_malformed_key_and_does_not_reveal_it()
    {
        // 16 bayt: Base64 gecerli ama AES-256 icin kisa.
        var shortKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

        var ex = Should.Throw<OptionsValidationException>(() => Start(shortKey));

        ex.Message.ShouldContain("PersonalDataProtection:Keys:k1");
        ex.Message.ShouldNotContain(shortKey);
    }

    private static void Start(string? key)
    {
        // WithWebHostBuilder yeni bir fabrika dondurur; ilk nesne ayrica atilir (CA2000).
        using var root = new WebApplicationFactory<Program>();
        using var factory = root.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Production);
            // Acilis, veritabanina baglanmadan once yapilandirmayi dogrular; baglanti kurulmaz.
            builder.UseSetting("Database:Hrms", "Host=anahtar-denetimi;Database=dsg_hrms;Username=denetim;Password=denetim");
            builder.UseSetting("Identity:JwtSigningKey", Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
            builder.UseSetting("ApplicationLogging:FilePath", "logs/test-.json");
            if (key is not null)
            {
                builder.UseSetting("PersonalDataProtection:Keys:k1", key);
            }
        });

        using var client = factory.CreateClient();
    }
}
