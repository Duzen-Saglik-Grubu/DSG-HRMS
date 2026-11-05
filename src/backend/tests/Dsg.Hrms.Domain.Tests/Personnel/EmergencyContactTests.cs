using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Domain.Tests.Personnel;

/// <summary>Acil durum kisisinin dogrulamasi ve degisikligi (SYG-PERS-043, KR-135). Veriler SENTETIKTIR.</summary>
public sealed class EmergencyContactTests
{
    private static readonly Guid Spouse = new("0192a3b4-1004-7000-8000-000000000001");

    [Fact]
    public void Valid_input_is_normalized()
    {
        var (input, errors) = EmergencyContactInput.Parse(Spouse, "Ayşe\n  Nur", " Kaya ", "0532 123 45 67");

        errors.ShouldBeEmpty();
        input.ShouldBe(new EmergencyContactInput(Spouse, "Ayşe Nur", "Kaya", "5321234567"));
    }

    [Fact]
    public void All_four_fields_are_required_and_each_error_names_its_field()
    {
        var (input, errors) = EmergencyContactInput.Parse(null, null, "", "  ");

        input.ShouldBeNull();
        errors.Keys.Order(StringComparer.Ordinal).ShouldBe(["FirstName", "LastName", "MobilePhone", "RelationshipId"]);
    }

    [Fact]
    public void An_empty_relationship_id_counts_as_not_chosen()
    {
        // KR-135: yakinlik listeden secilir; bos kimlik secim yapilmadigi anlamina gelir.
        var (_, errors) = EmergencyContactInput.Parse(Guid.Empty, "Fatma", "Kaya", "5321234567");

        errors.Keys.ShouldBe(["RelationshipId"]);
    }

    [Theory]
    [InlineData("212 123 45 67")]
    [InlineData("532 123 45")]
    [InlineData("telefon")]
    public void A_number_that_is_not_a_mobile_phone_is_refused(string phone)
    {
        // SYG-KMLK-009: 5 ile baslayan 10 hane.
        var (_, errors) = EmergencyContactInput.Parse(Spouse, "Fatma", "Kaya", phone);

        errors.Keys.ShouldBe(["MobilePhone"]);
    }

    [Fact]
    public void Too_long_name_is_refused()
    {
        var (_, errors) = EmergencyContactInput.Parse(Spouse, new string('b', EmergencyContact.MaxNameLength + 1), "Kaya", "5321234567");

        errors.Keys.ShouldBe(["FirstName"]);
    }

    [Fact]
    public void Change_reports_whether_anything_changed()
    {
        var details = new EmergencyContactDetails(21, "Ayşe", "Kaya", "5321234567");
        var contact = EmergencyContact.For(7, details);

        contact.Change(details).ShouldBeFalse();
        contact.Change(details with { RelationshipDefinitionId = 22 }).ShouldBeTrue();
        contact.RelationshipDefinitionId.ShouldBe(22);
        contact.Change(details with { RelationshipDefinitionId = 22, MobilePhone = "5339876543" }).ShouldBeTrue();
        contact.MobilePhone.ShouldBe("5339876543");
        contact.PersonId.ShouldBe(7);
    }

    [Fact]
    public void Input_and_details_never_print_their_values()
    {
        // Kayit bir gunluk iletisine karisirsa kisisel veri dusmemeli.
        new EmergencyContactInput(Spouse, "Ayşe", "Kaya", "5321234567").ToString().ShouldNotContain("Ayşe");
        new EmergencyContactDetails(21, "Ayşe", "Kaya", "5321234567").ToString().ShouldNotContain("5321234567");
    }
}
