using Dsg.Hrms.Application.Common.Reports;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Personnel.Access;
using Dsg.Hrms.Application.Personnel.Export;

namespace Dsg.Hrms.Application.Tests.Personnel;

/// <summary>
/// Disa aktarma alanlari ve gorunurluk politikasi (SYG-PERS-032, 052; ADR-0019 §2, dorduncu
/// kullanim yeri).
/// </summary>
public sealed class PersonExportFieldsTests
{
    private const long Own = 10;

    [Fact]
    public void Sensitive_fields_are_offered_only_with_the_permission_never_for_the_person_themselves()
    {
        // Kume islemi: kendi adresini goren personel baskalarinin adresini disari aktaramaz (B-07).
        var self = new PersonnelViewer(Own, PersonnelScope.All, new HashSet<string> { Permissions.PersonView, Permissions.PersonExport });
        var fields = PersonExportFields.Available(self).Select(d => d.Field).ToList();

        fields.ShouldNotContain(PersonExportField.Address);
        fields.ShouldNotContain(PersonExportField.BloodGroup);
        fields.ShouldContain(PersonExportField.NationalId); // maskeli aktarilir

        var hr = new PersonnelViewer(Own, PersonnelScope.All, new HashSet<string> { Permissions.PersonView, Permissions.PersonExport, Permissions.PersonViewSensitive });
        PersonExportFields.Available(hr).Select(d => d.Field).ShouldContain(PersonExportField.Address);
        PersonExportFields.Available(hr).Select(d => d.Field).ShouldContain(PersonExportField.BloodGroup);
    }

    [Fact]
    public void Catalog_covers_every_field_once_and_only_the_photo_is_pdf_only()
    {
        PersonExportFields.All.Select(d => d.Field).ShouldBe(Enum.GetValues<PersonExportField>(), ignoreOrder: true);
        PersonExportFields.All.Where(d => d.PdfOnly).Select(d => d.Field).ShouldBe([PersonExportField.Photo]);
        PersonExportFields.All.Single(d => d.Field == PersonExportField.Photo).Kind.ShouldBe(ReportColumnKind.Photo);
        PersonExportFields.All.Where(d => d.Sensitive).Select(d => d.Field).ShouldBe([PersonExportField.BloodGroup, PersonExportField.Address], ignoreOrder: true);
    }
}
