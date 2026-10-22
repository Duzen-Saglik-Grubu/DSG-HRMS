using Dsg.Hrms.Application.Common.Security;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Personnel.Access;

namespace Dsg.Hrms.Application.Tests.Personnel;

/// <summary>
/// Kapsam ve alan gorunurlugu politikasi (SYG-PERS-050, 052, 053; ADR-0019 §1–§2).
/// </summary>
public sealed class PersonFieldVisibilityTests
{
    private const long Own = 10;
    private const long Other = 20;

    [Fact]
    public void Scope_is_all_with_the_view_permission_and_self_without_it()
    {
        // SYG-PERS-050: kapsam izinden turetilir.
        Viewer().Scope.ShouldBe(PersonnelScope.Self);
        Viewer(Permissions.PersonView).Scope.ShouldBe(PersonnelScope.All);
        Viewer(Permissions.PersonViewSensitive, Permissions.PersonViewNationalId).Scope.ShouldBe(PersonnelScope.Self);
    }

    [Fact]
    public void Full_national_id_needs_the_permission_even_for_the_person_themselves()
    {
        // SYG-PERS-053 (S-01): kisinin kendisi de maskeli gorur.
        PersonFieldVisibility.CanSeeFullNationalId(Viewer()).ShouldBeFalse();
        PersonFieldVisibility.CanSeeFullNationalId(Viewer(Permissions.PersonView)).ShouldBeFalse();
        PersonFieldVisibility.CanSeeFullNationalId(Viewer(Permissions.PersonViewNationalId)).ShouldBeTrue();
        PersonFieldVisibility.CanSearchByNationalId(Viewer(Permissions.PersonView)).ShouldBeFalse();
    }

    [Fact]
    public void Sensitive_fields_are_visible_to_the_permission_or_the_person_themselves_on_a_single_record()
    {
        // SYG-PERS-052: adres, kan grubu, acil durum kisileri.
        PersonFieldVisibility.CanSeeSensitive(Viewer(), Own).ShouldBeTrue();
        PersonFieldVisibility.CanSeeSensitive(Viewer(), Other).ShouldBeFalse();
        PersonFieldVisibility.CanSeeSensitive(Viewer(Permissions.PersonView), Other).ShouldBeFalse();
        PersonFieldVisibility.CanSeeSensitive(Viewer(Permissions.PersonViewSensitive), Other).ShouldBeTrue();
    }

    [Fact]
    public void Marriage_information_is_hidden_from_the_person_themselves()
    {
        // SYG-PERS-052 (PG-PERS-19, IK teyidi 22.10.2026).
        PersonFieldVisibility.CanSeeMarriage(Viewer()).ShouldBeFalse();
        PersonFieldVisibility.CanSeeMarriage(Viewer(Permissions.PersonViewSensitive)).ShouldBeTrue();
    }

    [Fact]
    public void Set_operations_use_the_permission_only()
    {
        // ADR-0019 §2 (B-07, AN-14): kendi kan grubunu goren, kan grubuna gore suzemez.
        PersonFieldVisibility.CanUseSensitiveInSets(Viewer()).ShouldBeFalse();
        PersonFieldVisibility.CanUseSensitiveInSets(Viewer(Permissions.PersonViewSensitive)).ShouldBeTrue();
    }

    [Theory]
    [InlineData("12345678901", "123******01")]
    [InlineData(" 12345678901 ", "123******01")]
    [InlineData("1234567890", Mask.Redacted)]
    [InlineData("1234567890a", Mask.Redacted)]
    public void Display_mask_keeps_the_first_three_and_last_two_digits(string value, string expected)
    {
        // SYG-PERS-053: ekran maskesi; gunluk maskesi (REQ-KMLK-041) ayri ve degismedi.
        Mask.NationalIdForDisplay(value).ShouldBe(expected);
        Mask.NationalId("12345678901").ShouldBe("123*****901");
    }

    private static PersonnelViewer Viewer(params string[] permissions) =>
        PersonnelViewer.From(Own, permissions.ToHashSet(StringComparer.Ordinal));
}
