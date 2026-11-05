using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Domain.Tests.Personnel;

/// <summary>Acil durum kisisinin dogrulamasi ve degisikligi (SYG-PERS-043). Veriler SENTETIKTIR.</summary>
public sealed class EmergencyContactTests
{
    [Fact]
    public void Valid_input_is_normalized()
    {
        var (details, errors) = EmergencyContactDetails.Parse("  Eşi ", "Ayşe\n  Nur", " Kaya ", "0532 123 45 67");

        errors.ShouldBeEmpty();
        details.ShouldBe(new EmergencyContactDetails("Eşi", "Ayşe Nur", "Kaya", "5321234567"));
    }

    [Fact]
    public void All_four_fields_are_required_and_each_error_names_its_field()
    {
        var (details, errors) = EmergencyContactDetails.Parse(" ", null, "", "  ");

        details.ShouldBeNull();
        errors.Keys.Order(StringComparer.Ordinal).ShouldBe(["FirstName", "LastName", "MobilePhone", "Relationship"]);
    }

    [Theory]
    [InlineData("212 123 45 67")]
    [InlineData("532 123 45")]
    [InlineData("telefon")]
    public void A_number_that_is_not_a_mobile_phone_is_refused(string phone)
    {
        // SYG-KMLK-009: 5 ile baslayan 10 hane.
        var (_, errors) = EmergencyContactDetails.Parse("Annesi", "Fatma", "Kaya", phone);

        errors.Keys.ShouldBe(["MobilePhone"]);
    }

    [Fact]
    public void Too_long_text_is_refused()
    {
        var (_, errors) = EmergencyContactDetails.Parse(new string('a', EmergencyContact.MaxRelationshipLength + 1), new string('b', EmergencyContact.MaxNameLength + 1), "Kaya", "5321234567");

        errors.Keys.Order(StringComparer.Ordinal).ShouldBe(["FirstName", "Relationship"]);
    }

    [Fact]
    public void Change_reports_whether_anything_changed()
    {
        var details = new EmergencyContactDetails("Eşi", "Ayşe", "Kaya", "5321234567");
        var contact = EmergencyContact.For(7, details);

        contact.Change(details).ShouldBeFalse();
        contact.Change(details with { MobilePhone = "5339876543" }).ShouldBeTrue();
        contact.MobilePhone.ShouldBe("5339876543");
        contact.PersonId.ShouldBe(7);
    }

    [Fact]
    public void Details_never_print_their_values()
    {
        // Kayit bir gunluk iletisine karisirsa kisisel veri dusmemeli.
        new EmergencyContactDetails("Eşi", "Ayşe", "Kaya", "5321234567").ToString().ShouldNotContain("Ayşe");
    }
}
