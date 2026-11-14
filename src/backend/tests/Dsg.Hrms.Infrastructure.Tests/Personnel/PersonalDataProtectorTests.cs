using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using Dsg.Hrms.Infrastructure.Personnel.Protection;

namespace Dsg.Hrms.Infrastructure.Tests.Personnel;

/// <summary>
/// Ozel nitelikli verinin sifrelenmesi (SYG-PERS-060, 061; ADR-0020 §1–3).
/// </summary>
public sealed class PersonalDataProtectorTests
{
    private static readonly Guid Owner = Guid.CreateVersion7();
    private static readonly string Key1 = NewKey();
    private static readonly string Key2 = NewKey();

    [Fact]
    public void Value_round_trips_and_carries_the_key_id()
    {
        var protector = Protector(("k1", Key1));

        var value = protector.Protect("3", ProtectedField.BloodGroup(Owner));

        var parts = value.Split(':');
        parts.Length.ShouldBe(3);
        parts[0].ShouldBe("k1");
        // Duz metin hicbir parcada ne acik ne base64 olarak durur. Alt dize aramasi ("3:") rastgele
        // nonce'ta tesadufen olusuyordu ve test ~1/64 olasilikla dusuyordu (#404).
        // kisa-alt-dize: dizi elemaninda esitlik aranir, alt dize degil.
        parts.ShouldNotContain("3");
        parts.ShouldNotContain(Convert.ToBase64String("3"u8));
        protector.TryUnprotect(value, ProtectedField.BloodGroup(Owner)).ShouldBe("3");
    }

    [Fact]
    public void Same_value_produces_a_different_ciphertext_each_time()
    {
        // Rastgele nonce: veritabanina erisen biri ayni kan grubunu tasiyanlari gruplayamaz.
        var protector = Protector(("k1", Key1));

        protector.Protect("3", ProtectedField.BloodGroup(Owner))
            .ShouldNotBe(protector.Protect("3", ProtectedField.BloodGroup(Owner)));
    }

    [Fact]
    public void Value_copied_to_another_owner_table_or_column_cannot_be_decrypted()
    {
        // AAD: tablo, alan ve kayit sahibinin public_id'si.
        var protector = Protector(("k1", Key1));
        var value = protector.Protect("3", ProtectedField.BloodGroup(Owner));

        protector.TryUnprotect(value, ProtectedField.BloodGroup(Guid.CreateVersion7())).ShouldBeNull();
        protector.TryUnprotect(value, new ProtectedField("personnel.person_sensitive", "address", Owner)).ShouldBeNull();
        protector.TryUnprotect(value, new ProtectedField("personnel.correction_request", "blood_group", Owner)).ShouldBeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("duz metin")]
    [InlineData("k1:bozuk:deger")]
    [InlineData("k9:AAAAAAAAAAAAAAAA:AAAAAAAAAAAAAAAAAAAAAA==")]
    public void Malformed_or_unknown_value_is_not_decrypted(string value) =>
        Protector(("k1", Key1)).TryUnprotect(value, ProtectedField.BloodGroup(Owner)).ShouldBeNull();

    [Fact]
    public void Tampered_ciphertext_is_rejected()
    {
        var protector = Protector(("k1", Key1));
        var parts = protector.Protect("3", ProtectedField.BloodGroup(Owner)).Split(':');
        var sealedBytes = Convert.FromBase64String(parts[2]);
        sealedBytes[0] ^= 0x01;

        protector.TryUnprotect($"{parts[0]}:{parts[1]}:{Convert.ToBase64String(sealedBytes)}", ProtectedField.BloodGroup(Owner))
            .ShouldBeNull();
    }

    [Fact]
    public void New_key_is_used_for_writes_and_the_old_key_still_decrypts()
    {
        // ADR-0020 §3: dondurmede yeni anahtar yeni kimlikle eklenir; eski degerler cozulmeye devam eder.
        var old = Protector(("k1", Key1)).Protect("5", ProtectedField.BloodGroup(Owner));
        var rotated = Protector(("k1", Key1), ("k2", Key2));

        rotated.CurrentKeyId.ShouldBe("k2");
        rotated.Protect("5", ProtectedField.BloodGroup(Owner)).ShouldStartWith("k2:");
        rotated.TryUnprotect(old, ProtectedField.BloodGroup(Owner)).ShouldBe("5");
        PersonalDataProtector.KeyIdOf(old).ShouldBe("k1");

        // Anahtar kimlikleri metin olarak degil sayi olarak siralanir.
        Protector(("k2", Key1), ("k10", Key2)).CurrentKeyId.ShouldBe("k10");
    }

    [Fact]
    public void Missing_keys_fail_validation()
    {
        var errors = Validate(new PersonalDataProtectionOptions());

        errors.ShouldHaveSingleItem().ShouldContain("PersonalDataProtection:Keys");
    }

    [Theory]
    [InlineData("k1", "kisa-anahtar")]
    [InlineData("k1", "AAAA")]
    [InlineData("anahtar", null)]
    [InlineData("k0", null)]
    public void Invalid_key_fails_validation_without_revealing_its_value(string id, string? value)
    {
        // SYG-PERS-061: hata iletisi anahtarin degerini icermez.
        value ??= Key1;
        var errors = Validate(new PersonalDataProtectionOptions { Keys = new() { [id] = value } });

        errors.ShouldHaveSingleItem().ShouldNotContain(value);
    }

    [Fact]
    public void Empty_key_slots_are_treated_as_undefined()
    {
        // Compose dosyalari k1-k3 yuvalarini her zaman aktarir; bos yuva tanimsizdir (SYG-PERS-062).
        var rotated = new PersonalDataProtectionOptions { Keys = new() { ["k1"] = "", ["k2"] = Key2, ["k3"] = " " } };
        Validate(rotated).ShouldBeEmpty();
        rotated.CurrentKeyId.ShouldBe("k2");
        new PersonalDataProtector(rotated).KeyIds.ShouldBe(["k2"]);

        var allEmpty = new PersonalDataProtectionOptions { Keys = new() { ["k1"] = "", ["k2"] = "" } };
        Validate(allEmpty).ShouldHaveSingleItem().ShouldContain("PersonalDataProtection:Keys tanimli degil");
    }

    [Fact]
    public void Valid_keys_pass_validation() =>
        Validate(new PersonalDataProtectionOptions { Keys = new() { ["k1"] = Key1, ["k2"] = Key2 } }).ShouldBeEmpty();

    private static PersonalDataProtector Protector(params (string Id, string Key)[] keys) =>
        new(new PersonalDataProtectionOptions { Keys = keys.ToDictionary(k => k.Id, k => k.Key) });

    private static List<string> Validate(PersonalDataProtectionOptions options) =>
        [.. options.Validate(new ValidationContext(options)).Select(r => r.ErrorMessage!)];

    private static string NewKey() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
}
