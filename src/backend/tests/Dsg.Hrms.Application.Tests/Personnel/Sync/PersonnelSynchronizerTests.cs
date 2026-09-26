using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Domain.Personnel.Sync;
using static Dsg.Hrms.Application.Tests.Personnel.Sync.TestData;

namespace Dsg.Hrms.Application.Tests.Personnel.Sync;

/// <summary>
/// Senkronizasyon motorunun veri kalitesi kurallari. Senaryolarin cogu, 26.09.2026'da
/// canli LOGO verisinde olculen gercek durumlardir (issue #74).
/// </summary>
public sealed class PersonnelSynchronizerTests
{
    private static readonly PersonnelSnapshot Empty = new([], [], []);

    private static SyncOutcome Run(IReadOnlyList<LogoPersonnelRecord> records, PersonnelSnapshot? snapshot = null) =>
        new PersonnelSynchronizer(["0001000"]).Synchronize(records, snapshot ?? Empty, Today);

    /// <summary>Onceki calismanin urettigi varliklarla bir sonraki calismayi simule eder.</summary>
    private static PersonnelSnapshot After(SyncOutcome outcome, PersonnelSnapshot? previous = null) =>
        new(
            [.. previous?.Companies ?? [], .. outcome.NewCompanies],
            [.. previous?.Persons ?? [], .. outcome.NewPersons],
            [.. previous?.Employments ?? [], .. outcome.NewEmployments]);

    // ------------------------------------------------------------------ temel akis

    [Fact]
    public void New_cards_create_persons_employments_and_companies()
    {
        var outcome = Run([Card("00001", NationalId(1)), Card("00002", NationalId(2), firm: 2, firmName: "Zeytinim")]);

        outcome.NewPersons.Count.ShouldBe(2);
        outcome.NewEmployments.Count.ShouldBe(2);
        outcome.NewCompanies.Select(c => c.Name).ShouldBe(["Duzen Laboratuvarlar", "Zeytinim"], ignoreOrder: true);
        outcome.Counts.PersonsCreated.ShouldBe(2);
        outcome.Counts.EmploymentsCreated.ShouldBe(2);
        outcome.Counts.CompaniesChanged.ShouldBe(2);
        outcome.Warnings.ShouldBeEmpty();
    }

    [Fact]
    public void Contact_data_is_normalized()
    {
        var outcome = Run([Card("00001", NationalId(1), emails: ["  A.Yilmaz@Duzen.com.tr\r\n"], phones: ["0532 123 45 67"])]);

        var person = outcome.NewPersons.Single();
        person.Email.ShouldBe("a.yilmaz@duzen.com.tr");
        person.MobilePhone.ShouldBe("5321234567");
    }

    [Fact]
    public void Second_run_with_the_same_source_changes_nothing()
    {
        // 15 dakikada bir calisan senkronizasyon, degisiklik yoksa HICBIR kaydi
        // guncellememelidir; aksi halde denetim izi gurultuyle dolar.
        var cards = new[] { Card("00001", NationalId(1)), Card("00002", NationalId(2)) };
        var first = Run(cards);

        var second = Run(cards, After(first));

        second.NewPersons.ShouldBeEmpty();
        second.NewEmployments.ShouldBeEmpty();
        second.NewCompanies.ShouldBeEmpty();
        second.Counts.PersonsUpdated.ShouldBe(0);
        second.Counts.EmploymentsUpdated.ShouldBe(0);
        second.Counts.CompaniesChanged.ShouldBe(0);
    }

    [Fact]
    public void Changed_card_updates_the_existing_person()
    {
        var first = Run([Card("00001", NationalId(1), lastName: "Yilmaz")]);

        var second = Run([Card("00001", NationalId(1), lastName: "Demir")], After(first));

        second.Counts.PersonsUpdated.ShouldBe(1);
        first.NewPersons.Single().LastName.ShouldBe("Demir");
    }

    [Fact]
    public void Termination_in_logo_deactivates_the_employment()
    {
        var first = Run([Card("00001", NationalId(1))]);

        var second = Run([Card("00001", NationalId(1), terminationDate: Today.AddDays(-1))], After(first));

        second.Counts.EmploymentsUpdated.ShouldBe(1);
        second.Counts.EmploymentsDeactivated.ShouldBe(1);
        first.NewEmployments.Single().IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Excluded_registry_code_is_skipped_without_warning()
    {
        // KR-034: 0001000 personel kaydi olmayan, TCKN'siz sistem kartidir.
        var outcome = Run([Card("0001000", nationalId: null), Card("00001", NationalId(1))]);

        outcome.Counts.RecordsRead.ShouldBe(2);
        outcome.Counts.RecordsSkipped.ShouldBe(1);
        outcome.NewEmployments.Count.ShouldBe(1);
        outcome.Warnings.ShouldBeEmpty();
    }

    // ------------------------------------------------------------------ kimlik kurallari

    [Fact]
    public void Card_without_national_id_creates_no_person_and_warns()
    {
        // KR-043, SYG-KMLK-007.
        var outcome = Run([Card("00001", nationalId: "  ")]);

        outcome.NewPersons.ShouldBeEmpty();
        outcome.NewEmployments.ShouldBeEmpty();
        outcome.Warnings.Single().Code.ShouldBe(SyncWarningCode.MissingNationalId);
        outcome.Warnings.Single().RegistryCode.ShouldBe("00001");
    }

    [Fact]
    public void Card_with_invalid_national_id_creates_no_person_and_warns()
    {
        var outcome = Run([Card("00001", nationalId: "12345678901")]);

        outcome.NewPersons.ShouldBeEmpty();
        outcome.Warnings.Single().Code.ShouldBe(SyncWarningCode.InvalidNationalId);
    }

    [Fact]
    public void Card_without_birth_date_creates_no_person_and_warns()
    {
        var card = Card("00001", NationalId(1)) with { BirthDate = null };

        var outcome = Run([card]);

        outcome.NewPersons.ShouldBeEmpty();
        outcome.Warnings.Single().Code.ShouldBe(SyncWarningCode.MissingBirthDate);
    }

    [Fact]
    public void Warnings_never_contain_personal_data()
    {
        var nationalId = NationalId(1);
        var outcome = Run(
        [
            Card("00001", nationalId, emails: ["a@duzen.com.tr", "b@duzen.com.tr"], phones: ["03121234567"]),
            Card("00002", nationalId, birthDate: new DateOnly(1990, 1, 1), emails: ["c@duzen.com.tr"]),
        ]);

        outcome.Warnings.ShouldNotBeEmpty();
        foreach (var warning in outcome.Warnings)
        {
            warning.Detail.ShouldNotContain(nationalId);
            warning.Detail.ShouldNotContain("@");
            warning.Detail.ShouldNotContain("312");
            warning.Detail.ShouldNotContain("1985");
            warning.Detail.ShouldNotContain("1990");
        }
    }

    // ------------------------------------------------------------------ coklu sicil

    [Fact]
    public void Several_cards_with_the_same_national_id_form_one_person_with_several_employments()
    {
        // SYG-KMLK-001; 22.09.2026: 12 kisinin es zamanli birden fazla aktif sicili var.
        var outcome = Run(
        [
            Card("00001", NationalId(1), logoRef: 1),
            Card("00002", NationalId(1), logoRef: 2, firm: 2, firmName: "Zeytinim"),
        ]);

        outcome.NewPersons.Count.ShouldBe(1);
        outcome.NewEmployments.Count.ShouldBe(2);
        outcome.NewEmployments.ShouldAllBe(e => e.Person == outcome.NewPersons[0]);
    }

    [Fact]
    public void Person_data_comes_from_the_most_recent_active_card()
    {
        var outcome = Run(
        [
            Card("00001", NationalId(1), logoRef: 1, lastName: "Eski", hireDate: new DateOnly(2010, 1, 1), terminationDate: new DateOnly(2015, 1, 1)),
            Card("00002", NationalId(1), logoRef: 2, lastName: "Aktif", hireDate: new DateOnly(2018, 1, 1)),
            Card("00003", NationalId(1), logoRef: 3, lastName: "YeniAmaCikmis", hireDate: new DateOnly(2024, 1, 1), terminationDate: new DateOnly(2025, 1, 1)),
        ]);

        outcome.NewPersons.Single().LastName.ShouldBe("Aktif");
    }

    [Fact]
    public void Without_an_active_card_the_most_recent_card_wins()
    {
        var outcome = Run(
        [
            Card("00001", NationalId(1), logoRef: 1, lastName: "Eski", hireDate: new DateOnly(2010, 1, 1), terminationDate: new DateOnly(2015, 1, 1)),
            Card("00002", NationalId(1), logoRef: 2, lastName: "Yeni", hireDate: new DateOnly(2020, 1, 1), terminationDate: new DateOnly(2022, 1, 1)),
        ]);

        outcome.NewPersons.Single().LastName.ShouldBe("Yeni");
    }

    [Fact]
    public void Conflicting_birth_dates_between_cards_warn_on_the_primary_card()
    {
        // 26.09.2026: 3 kisinin kartlari arasinda farkli dogum tarihi var. Uyelik
        // TCKN + dogum tarihiyle eslestigi icin bu uyari kritik.
        var outcome = Run(
        [
            Card("00001", NationalId(1), logoRef: 1, birthDate: new DateOnly(1985, 4, 12), hireDate: new DateOnly(2010, 1, 1)),
            Card("00002", NationalId(1), logoRef: 2, birthDate: new DateOnly(1985, 12, 4), hireDate: new DateOnly(2020, 1, 1)),
        ]);

        var warning = outcome.Warnings.Single(w => w.Code == SyncWarningCode.ConflictingBirthDate);
        warning.RegistryCode.ShouldBe("00002");
        warning.Detail.ShouldContain("00001");
        outcome.NewPersons.Single().BirthDate.ShouldBe(new DateOnly(1985, 12, 4));
    }

    [Fact]
    public void Conflicting_emails_and_names_between_cards_warn()
    {
        var outcome = Run(
        [
            Card("00001", NationalId(1), logoRef: 1, lastName: "Yilmaz", emails: ["eski@duzen.com.tr"]),
            Card("00002", NationalId(1), logoRef: 2, lastName: "Demir", emails: ["yeni@duzen.com.tr"]),
        ]);

        outcome.Warnings.Select(w => w.Code).ShouldBe(
            [SyncWarningCode.ConflictingEmail, SyncWarningCode.ConflictingName], ignoreOrder: true);
    }

    [Fact]
    public void Surname_change_after_marriage_is_not_a_conflict_and_the_active_card_wins()
    {
        // #77: Onceki sicil ile aktif sicil arasindaki soyad farki dogaldir (evlilik
        // sonrasi esin soyadi ya da iki soyad). Aktif sicildeki ad esas alinir.
        var outcome = Run(
        [
            Card("00001", NationalId(1), logoRef: 1, firstName: "Ayse", lastName: "Yilmaz",
                hireDate: new DateOnly(2012, 1, 1), terminationDate: new DateOnly(2016, 1, 1), emails: ["a@duzen.com.tr"]),
            Card("00002", NationalId(1), logoRef: 2, firstName: "Ayse", lastName: "Yilmaz Demir",
                hireDate: new DateOnly(2019, 1, 1), emails: ["a@duzen.com.tr"]),
        ]);

        outcome.Warnings.ShouldNotContain(w => w.Code == SyncWarningCode.ConflictingName);
        outcome.NewPersons.Single().LastName.ShouldBe("Yilmaz Demir");
    }

    [Fact]
    public void Different_names_on_two_active_cards_still_warn()
    {
        var outcome = Run(
        [
            Card("00001", NationalId(1), logoRef: 1, lastName: "Yilmaz", emails: ["a@duzen.com.tr"]),
            Card("00002", NationalId(1), logoRef: 2, lastName: "Demir", emails: ["a@duzen.com.tr"]),
        ]);

        var warning = outcome.Warnings.Single(w => w.Code == SyncWarningCode.ConflictingName);
        warning.Detail.ShouldContain("aktif");
    }

    [Fact]
    public void Email_left_on_a_former_employees_card_does_not_block_the_active_person()
    {
        // #77, 26.09.2026: adres yeniden verilmis, ayrilanlarin kartinda eski haliyle
        // kalmisti. Ayrilmis kisi uye olamaz ve giris yapamaz; paylasim riski yoktur.
        var outcome = Run(
        [
            Card("00001", NationalId(1), emails: ["birim@duzen.com.tr"]),
            Card("00002", NationalId(2), emails: ["birim@duzen.com.tr"], terminationDate: new DateOnly(2023, 1, 1)),
            Card("00003", NationalId(3), emails: ["birim@duzen.com.tr"], terminationDate: new DateOnly(2021, 1, 1)),
        ]);

        outcome.NewPersons.ShouldAllBe(p => !p.IsEmailShared);
        outcome.Warnings.ShouldNotContain(w => w.Code == SyncWarningCode.SharedEmail);
    }

    [Fact]
    public void Email_shared_by_two_active_persons_is_still_flagged_even_if_a_former_one_also_has_it()
    {
        var outcome = Run(
        [
            Card("00001", NationalId(1), emails: ["ortak@duzen.com.tr"]),
            Card("00002", NationalId(2), emails: ["ortak@duzen.com.tr"]),
            Card("00003", NationalId(3), emails: ["ortak@duzen.com.tr"], terminationDate: new DateOnly(2021, 1, 1)),
        ]);

        outcome.NewPersons.Where(p => p.IsEmailShared).Select(p => p.NationalId)
            .ShouldBe([NationalId(1), NationalId(2)], ignoreOrder: true);
        outcome.Warnings.Where(w => w.Code == SyncWarningCode.SharedEmail).Select(w => w.Detail)
            .ShouldAllBe(d => d.Contains("2 farkli kisiye"));
    }

    [Fact]
    public void Same_email_on_two_cards_of_the_same_person_is_not_shared()
    {
        var outcome = Run(
        [
            Card("00001", NationalId(1), logoRef: 1, emails: ["a@duzen.com.tr"]),
            Card("00002", NationalId(1), logoRef: 2, emails: ["A@Duzen.com.tr"]),
        ]);

        outcome.NewPersons.Single().IsEmailShared.ShouldBeFalse();
        outcome.Warnings.ShouldBeEmpty();
    }

    // ------------------------------------------------------------------ iletisim

    [Fact]
    public void Email_shared_by_different_persons_is_flagged_for_both()
    {
        // SYG-KMLK-008: paylasilan adres ne giris kimligi ne dogrulama hedefi olabilir.
        var outcome = Run(
        [
            Card("00001", NationalId(1), emails: ["ortak@duzen.com.tr"]),
            Card("00002", NationalId(2), emails: ["ORTAK@duzen.com.tr "]),
            Card("00003", NationalId(3), emails: ["tekil@duzen.com.tr"]),
        ]);

        outcome.NewPersons.Where(p => p.IsEmailShared).Select(p => p.NationalId)
            .ShouldBe([NationalId(1), NationalId(2)], ignoreOrder: true);
        outcome.Warnings.Where(w => w.Code == SyncWarningCode.SharedEmail).Select(w => w.RegistryCode)
            .ShouldBe(["00001", "00002"], ignoreOrder: true);
    }

    [Fact]
    public void Shared_flag_is_cleared_when_the_address_becomes_unique()
    {
        var first = Run(
        [
            Card("00001", NationalId(1), emails: ["ortak@duzen.com.tr"]),
            Card("00002", NationalId(2), emails: ["ortak@duzen.com.tr"]),
        ]);

        Run(
        [
            Card("00001", NationalId(1), emails: ["ortak@duzen.com.tr"]),
            Card("00002", NationalId(2), emails: ["yeni@duzen.com.tr"]),
        ], After(first));

        first.NewPersons.ShouldAllBe(p => !p.IsEmailShared);
    }

    [Fact]
    public void Several_emails_on_one_card_take_the_first_and_warn()
    {
        // 26.09.2026: 4 kartta birden fazla e-posta var.
        var outcome = Run([Card("00001", NationalId(1), emails: ["ilk@duzen.com.tr", "ikinci@duzen.com.tr"])]);

        outcome.NewPersons.Single().Email.ShouldBe("ilk@duzen.com.tr");
        outcome.Warnings.Single().Code.ShouldBe(SyncWarningCode.MultipleEmailsOnCard);
    }

    [Fact]
    public void Malformed_email_is_ignored_and_warned()
    {
        var outcome = Run([Card("00001", NationalId(1), emails: ["gecersiz-adres"])]);

        outcome.NewPersons.Single().Email.ShouldBeNull();
        outcome.Warnings.Single().Code.ShouldBe(SyncWarningCode.InvalidEmail);
    }

    [Fact]
    public void Invalid_mobile_phone_is_ignored_and_warned()
    {
        var outcome = Run([Card("00001", NationalId(1), phones: ["03121234567"])]);

        outcome.NewPersons.Single().MobilePhone.ShouldBeNull();
        outcome.Warnings.Single().Code.ShouldBe(SyncWarningCode.InvalidMobilePhone);
    }

    [Fact]
    public void Missing_contact_data_is_not_a_warning_by_itself()
    {
        // Eksik iletisim bilgisi uyelik kanal kurallarinda ele alinir (SYG-KMLK-017);
        // her calismada uyari uretmek gurultu olurdu.
        var outcome = Run([Card("00001", NationalId(1), emails: [], phones: [])]);

        outcome.NewPersons.Single().Email.ShouldBeNull();
        outcome.NewPersons.Single().MobilePhone.ShouldBeNull();
        outcome.Warnings.ShouldBeEmpty();
    }

    // ------------------------------------------------------------------ firma ve kaynak

    [Fact]
    public void Card_of_unknown_company_is_kept_with_a_placeholder_company_and_warned()
    {
        var outcome = Run([Card("00001", NationalId(1), firm: 9, firmName: null)]);

        outcome.NewEmployments.Count.ShouldBe(1);
        outcome.NewCompanies.Single().Name.ShouldBe("Firma 9");
        outcome.Warnings.Single().Code.ShouldBe(SyncWarningCode.UnknownCompany);
    }

    [Fact]
    public void Renamed_company_is_updated()
    {
        var first = Run([Card("00001", NationalId(1), firmName: "Eski Ad")]);

        var second = Run([Card("00001", NationalId(1), firmName: "Yeni Ad")], After(first));

        second.Counts.CompaniesChanged.ShouldBe(1);
        first.NewCompanies.Single().Name.ShouldBe("Yeni Ad");
    }

    [Fact]
    public void Employment_whose_card_disappeared_is_kept_and_warned()
    {
        var first = Run([Card("00001", NationalId(1)), Card("00002", NationalId(2))]);

        var second = Run([Card("00001", NationalId(1))], After(first));

        var warning = second.Warnings.Single();
        warning.Code.ShouldBe(SyncWarningCode.MissingFromSource);
        warning.RegistryCode.ShouldBe("00002");
        first.NewEmployments.Single(e => e.RegistryCode == "00002").IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Corrected_national_id_moves_the_employment_to_the_right_person()
    {
        var first = Run([Card("00001", NationalId(1))]);

        var second = Run([Card("00001", NationalId(2))], After(first));

        second.Counts.PersonsCreated.ShouldBe(1);
        second.Counts.EmploymentsUpdated.ShouldBe(1);
        first.NewEmployments.Single().Person.ShouldBe(second.NewPersons.Single());
    }
}
