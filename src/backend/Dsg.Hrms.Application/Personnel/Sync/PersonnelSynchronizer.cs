using System.Globalization;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Organization;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Domain.Personnel.Sync;

namespace Dsg.Hrms.Application.Personnel.Sync;

/// <summary>
/// LOGO kartlarini HRMS'teki kisi, istihdam ve firma kayitlariyla karsilastirir ve
/// farki uygular (ADR-0003 §4, SYG-KMLK-004…009).
/// </summary>
/// <remarks>
/// <para>
/// Saf bir siniftir: veritabanina ve LOGO'ya dokunmaz. Mevcut varliklari yerinde
/// gunceller, yenilerini <see cref="SyncOutcome"/> icinde dondurur. Boylece veri
/// kalitesi kurallarinin tamami hizli birim testleriyle sinanabilir.
/// </para>
/// <para>
/// <b>Birden fazla karti olan kisi:</b> kisinin adi, dogum tarihi ve iletisim bilgisi
/// <b>esas kart</b>tan alinir: en guncel aktif istihdamin karti; aktif istihdam yoksa
/// en guncel kart. Kartlar arasindaki celiski uyari uretir; IK LOGO'da duzeltir.
/// 26.09.2026'daki ilk canli calismada aktif personelde 13 kisinin kartlari arasinda
/// farkli e-posta, 2 kisinin farkli dogum tarihi vardi; IK ayni gun LOGO'da duzeltti ve
/// ikinci olcumde aktif personelde celiski kalmadi.
/// </para>
/// <para>
/// <b>Hesap yasam dongusu (SYG-KMLK-054, 056):</b> istihdamlar uygulandiktan sonra her
/// hesap icin kisinin aktif istihdami olup olmadigina bakilir. Hic yoksa hesap
/// pasiflesir (<c>PRM-HSP-02</c> aciksa); istihdam bitimiyle pasiflesmis bir hesabin
/// kisisinde yeniden aktif istihdam varsa ayni hesap aktiflesir.
/// </para>
/// </remarks>
public sealed class PersonnelSynchronizer
{
    private readonly HashSet<string> _excludedRegistryCodes;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonnelSynchronizer(IEnumerable<string> excludedRegistryCodes)
    {
        ArgumentNullException.ThrowIfNull(excludedRegistryCodes);
        _excludedRegistryCodes = excludedRegistryCodes.ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>Kaynak kartlari mevcut duruma uygular.</summary>
    /// <param name="records">LOGO kartlari.</param>
    /// <param name="snapshot">HRMS'teki mevcut durum.</param>
    /// <param name="today">Senkronizasyon gunu.</param>
    /// <param name="autoDeactivateAccounts">
    /// Istihdami biten kisinin hesabi pasiflesir mi (<c>PRM-HSP-02</c>, varsayilan acik).
    /// </param>
    public SyncOutcome Synchronize(
        IReadOnlyList<LogoPersonnelRecord> records,
        PersonnelSnapshot snapshot,
        DateOnly today,
        bool autoDeactivateAccounts = true)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(snapshot);

        var context = new Context(snapshot, today);

        var inScope = records.Where(r => !_excludedRegistryCodes.Contains(r.RegistryCode)).ToList();
        var skipped = records.Count - inScope.Count;

        SyncCompanies(inScope, context);

        var cards = inScope.Select(r => NormalizeCard(r, context)).Where(c => c is not null).Cast<Card>().ToList();
        var groups = cards.GroupBy(c => c.NationalId, StringComparer.Ordinal)
            .Select(g => BuildPersonGroup(g.ToList(), today, context))
            .ToList();

        MarkSharedEmails(groups, context);

        foreach (var group in groups)
        {
            var person = UpsertPerson(group, context);
            foreach (var card in group.Cards)
            {
                UpsertEmployment(card, person, context);
            }
        }

        ReportMissingFromSource(records, context);
        ApplyAccountLifecycle(snapshot.Accounts, autoDeactivateAccounts, context);

        var counts = new SyncCounts(
            RecordsRead: records.Count,
            RecordsSkipped: skipped,
            PersonsCreated: context.NewPersons.Count,
            PersonsUpdated: context.PersonsUpdated,
            EmploymentsCreated: context.NewEmployments.Count,
            EmploymentsUpdated: context.EmploymentsUpdated,
            EmploymentsDeactivated: context.EmploymentsDeactivated,
            CompaniesChanged: context.NewCompanies.Count + context.CompaniesRenamed,
            AccountsDeactivated: context.AccountsDeactivated,
            AccountsReactivated: context.AccountsReactivated);

        return new SyncOutcome(
            counts,
            context.Warnings,
            context.NewCompanies,
            context.NewPersons,
            context.NewEmployments);
    }

    // ------------------------------------------------------------------ firmalar

    private static void SyncCompanies(List<LogoPersonnelRecord> records, Context context)
    {
        foreach (var firm in records.GroupBy(r => r.FirmNumber))
        {
            var name = ContactNormalizer.NormalizeName(firm.Select(r => r.FirmName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)));

            if (name.Length == 0)
            {
                // Firma listesinde olmayan firma numarasi: kart DUSURULMEZ, firma
                // yer tutucu adla olusturulur ve uyari uretilir (ADR-0003 §5).
                name = string.Create(CultureInfo.InvariantCulture, $"Firma {firm.Key}");
                foreach (var record in firm)
                {
                    context.Warn(SyncWarningCode.UnknownCompany, record.RegistryCode,
                        string.Create(CultureInfo.InvariantCulture, $"Firma numarasi {firm.Key} LOGO firma listesinde yok."));
                }
            }

            if (context.CompaniesByFirm.TryGetValue(firm.Key, out var company))
            {
                if (company.Rename(name))
                {
                    context.CompaniesRenamed++;
                }
            }
            else
            {
                company = Company.FromLogo(firm.Key, name);
                context.CompaniesByFirm[firm.Key] = company;
                context.NewCompanies.Add(company);
            }
        }
    }

    // ------------------------------------------------------------------ kart

    private static Card? NormalizeCard(LogoPersonnelRecord record, Context context)
    {
        var nationalId = record.NationalId?.Trim();

        if (string.IsNullOrEmpty(nationalId))
        {
            context.Warn(SyncWarningCode.MissingNationalId, record.RegistryCode,
                "Kartta TCKN yok; kisi olusturulmadi.");
            return null;
        }

        if (!NationalId.IsValid(nationalId))
        {
            context.Warn(SyncWarningCode.InvalidNationalId, record.RegistryCode,
                "TCKN gecersiz (sagla algoritmasini gecmiyor); kisi olusturulmadi.");
            return null;
        }

        if (record.BirthDate is null)
        {
            context.Warn(SyncWarningCode.MissingBirthDate, record.RegistryCode,
                "Kartta dogum tarihi yok; kisi olusturulmadi.");
            return null;
        }

        return new Card(
            record,
            nationalId,
            ContactNormalizer.NormalizeName(record.FirstName),
            ContactNormalizer.NormalizeName(record.LastName),
            record.BirthDate.Value,
            PickEmail(record, context),
            PickMobilePhone(record, context));
    }

    private static string? PickEmail(LogoPersonnelRecord record, Context context)
    {
        var normalized = record.Emails
            .Where(e => !string.IsNullOrWhiteSpace(e))
            .Select(ContactNormalizer.NormalizeEmail)
            .ToList();
        var valid = normalized.OfType<string>().Distinct(StringComparer.Ordinal).ToList();

        if (normalized.Exists(e => e is null))
        {
            context.Warn(SyncWarningCode.InvalidEmail, record.RegistryCode,
                "Kartta bicimi gecersiz bir e-posta var; kullanilmadi.");
        }

        if (valid.Count > 1)
        {
            context.Warn(SyncWarningCode.MultipleEmailsOnCard, record.RegistryCode,
                string.Create(CultureInfo.InvariantCulture, $"Kartta {valid.Count} farkli e-posta var; ilki kullanildi."));
        }

        return valid.FirstOrDefault();
    }

    private static string? PickMobilePhone(LogoPersonnelRecord record, Context context)
    {
        string? chosen = null;
        foreach (var raw in record.MobilePhones.Where(p => !string.IsNullOrWhiteSpace(p)))
        {
            var normalized = ContactNormalizer.NormalizeMobilePhone(raw);
            if (normalized is null)
            {
                context.Warn(SyncWarningCode.InvalidMobilePhone, record.RegistryCode,
                    "Cep telefonu gecersiz (10 hane ve 5 ile baslamali); kullanilmadi.");
            }
            else
            {
                chosen ??= normalized;
            }
        }

        return chosen;
    }

    // ------------------------------------------------------------------ kisi

    private static PersonGroup BuildPersonGroup(List<Card> cards, DateOnly today, Context context)
    {
        // Esas kart: en guncel aktif istihdamin karti; yoksa en guncel kart.
        // Esitlikte daha buyuk LOGO referansi (daha yeni kart) kazanir; siralama
        // belirlenimcidir, her calismada ayni kart secilir.
        var primary = cards
            .OrderByDescending(c => Employment.IsActiveOn(c.Record.TerminationDate, today))
            .ThenByDescending(c => c.Record.HireDate)
            .ThenByDescending(c => c.Record.LogoRef)
            .First();

        if (cards.Count > 1)
        {
            var others = string.Join(", ", cards.Where(c => c != primary).Select(c => c.Record.RegistryCode).Order(StringComparer.Ordinal));

            if (cards.Select(c => c.BirthDate).Distinct().Count() > 1)
            {
                context.Warn(SyncWarningCode.ConflictingBirthDate, primary.Record.RegistryCode,
                    $"Kisinin kartlari arasinda farkli dogum tarihi var; bu kart esas alindi. Diger sicil(ler): {others}.");
            }

            if (cards.Where(c => c.Email is not null).Select(c => c.Email).Distinct(StringComparer.Ordinal).Count() > 1)
            {
                context.Warn(SyncWarningCode.ConflictingEmail, primary.Record.RegistryCode,
                    $"Kisinin kartlari arasinda farkli e-posta var; bu kart esas alindi. Diger sicil(ler): {others}.");
            }

            // Ad/soyad yalnizca AKTIF kartlar arasinda karsilastirilir (#77). Onceki
            // sicille aktif sicil arasindaki soyad farki dogaldir: evlilikten sonra
            // esin soyadi veya iki soyad birlikte kullanilabilir. Aktif sicildeki ad
            // esas alinir (esas kart kurali). Iki aktif kart arasindaki fark ise
            // gercek bir tutarsizliktir.
            var activeCards = cards.Where(c => Employment.IsActiveOn(c.Record.TerminationDate, today)).ToList();
            if (activeCards.Select(c => $"{c.FirstName} {c.LastName}").Distinct(StringComparer.Ordinal).Count() > 1)
            {
                var otherActive = string.Join(", ", activeCards.Where(c => c != primary).Select(c => c.Record.RegistryCode).Order(StringComparer.Ordinal));
                context.Warn(SyncWarningCode.ConflictingName, primary.Record.RegistryCode,
                    $"Kisinin aktif kartlari arasinda farkli ad veya soyad var; bu kart esas alindi. Diger aktif sicil(ler): {otherActive}.");
            }
        }

        var hasActiveEmployment = cards.Exists(c => Employment.IsActiveOn(c.Record.TerminationDate, today));
        return new PersonGroup(primary.NationalId, primary, cards, hasActiveEmployment);
    }

    private static void MarkSharedEmails(List<PersonGroup> groups, Context context)
    {
        // Paylasilan adres, KISILER arasinda tespit edilir: ayni kisinin iki karti
        // ayni adresi tasiyabilir, bu paylasim degildir.
        //
        // Yalnizca AKTIF istihdami olan kisiler sayilir (#77). Paylasilan adresin
        // riski, kodun baskasinin okuyabildigi bir kutuya gitmesi veya birinin
        // baskasi adina uye olmasidir. Ayrilmis kisi uye olamaz ve giris yapamaz;
        // ayni adresi tasimasi bu riski dogurmaz. Sayilsaydi, adresi yeniden verilen
        // aktif personel e-postasiyla giris yapamazdi (26.09.2026, sicil 0001100).
        var shared = groups
            .Where(g => g.HasActiveEmployment && g.Primary.Email is not null)
            .GroupBy(g => g.Primary.Email!, StringComparer.Ordinal)
            .Where(g => g.Count() > 1)
            .SelectMany(g => g.Select(group => (group, count: g.Count())));

        foreach (var (group, count) in shared)
        {
            group.IsEmailShared = true;
            context.Warn(SyncWarningCode.SharedEmail, group.Primary.Record.RegistryCode,
                string.Create(CultureInfo.InvariantCulture, $"E-posta {count} farkli kisiye tanimli; giris ve dogrulama icin kullanilamaz."));
        }
    }

    private static Person UpsertPerson(PersonGroup group, Context context)
    {
        var details = new PersonDetails(
            group.Primary.FirstName,
            group.Primary.LastName,
            group.Primary.BirthDate,
            group.Primary.Email,
            group.IsEmailShared,
            group.Primary.MobilePhone);

        if (context.PersonsByNationalId.TryGetValue(group.NationalId, out var person))
        {
            if (person.Apply(details))
            {
                context.PersonsUpdated++;
            }

            return person;
        }

        person = Person.Create(group.NationalId, details);
        context.PersonsByNationalId[group.NationalId] = person;
        context.NewPersons.Add(person);
        return person;
    }

    // ------------------------------------------------------------------ istihdam

    private static void UpsertEmployment(Card card, Person person, Context context)
    {
        var record = card.Record;
        var company = context.CompaniesByFirm[record.FirmNumber];
        var details = new EmploymentDetails(record.HireDate, record.TerminationDate, record.LogoRef);

        if (context.EmploymentsByCode.TryGetValue(record.RegistryCode, out var employment))
        {
            var wasActive = employment.IsActive;
            if (employment.Apply(person, company, details, context.Today))
            {
                context.EmploymentsUpdated++;
                if (wasActive && !employment.IsActive)
                {
                    context.EmploymentsDeactivated++;
                }
            }

            return;
        }

        employment = Employment.Create(person, company, record.RegistryCode, details, context.Today);
        context.EmploymentsByCode[record.RegistryCode] = employment;
        context.NewEmployments.Add(employment);
    }

    private void ReportMissingFromSource(IReadOnlyList<LogoPersonnelRecord> records, Context context)
    {
        // Karti LOGO'dan silinmis istihdam DEGISTIRILMEZ: silinmenin anlami (hatali
        // kart mi, cikis mi) kaynaktan anlasilamaz. Uyari uretilir, IK karar verir.
        var sourceCodes = records.Select(r => r.RegistryCode).ToHashSet(StringComparer.Ordinal);

        foreach (var code in context.EmploymentsByCode.Keys
                     .Where(code => !sourceCodes.Contains(code) && !_excludedRegistryCodes.Contains(code))
                     .Order(StringComparer.Ordinal))
        {
            context.Warn(SyncWarningCode.MissingFromSource, code,
                "Bu sicilin karti LOGO'da artik bulunmuyor; istihdam degistirilmedi.");
        }
    }

    // ------------------------------------------------------------------ hesap

    private static void ApplyAccountLifecycle(
        IReadOnlyList<UserAccount> accounts,
        bool autoDeactivate,
        Context context)
    {
        if (accounts.Count == 0)
        {
            return;
        }

        // Aktiflik, TUM istihdamlar uzerinden hesaplanir: bu calismada kaynaktan gelmeyen
        // (karti silinmis) istihdam da degistirilmeden kaldigi haliyle sayilir. Kisi
        // istihdamin gezinme ozelliginden alinir; kimlik (PersonId) degil. TCKN duzeltmesiyle
        // baska kisiye tasinan istihdamin kimligi kayda kadar eski kalir.
        var personsWithActiveEmployment = context.EmploymentsByCode.Values
            .Where(e => e.IsActive)
            .Select(e => e.Person)
            .ToHashSet();

        var personsById = context.PersonsByNationalId.Values
            .Where(p => p.Id != 0)
            .ToDictionary(p => p.Id);

        foreach (var account in accounts)
        {
            if (!personsById.TryGetValue(account.PersonId, out var person))
            {
                // Yabanci anahtar nedeniyle olamaz; olursa hesaba dokunulmaz.
                continue;
            }

            if (personsWithActiveEmployment.Contains(person))
            {
                if (account.ReactivateForNewEmployment())
                {
                    context.AccountsReactivated++;
                }
            }
            else if (autoDeactivate)
            {
                // Sayac yalnizca aktiften pasife gecisi sayar; elle pasif bir hesabin
                // nedeninin "istihdam bitti"ye donmesi pasiflesme degildir.
                var wasActive = account.Status == AccountStatus.Active;
                if (account.DeactivateForEmploymentEnd() && wasActive)
                {
                    context.AccountsDeactivated++;
                }
            }
        }
    }

    // ------------------------------------------------------------------ ic tipler

    private sealed record Card(
        LogoPersonnelRecord Record,
        string NationalId,
        string FirstName,
        string LastName,
        DateOnly BirthDate,
        string? Email,
        string? MobilePhone);

    private sealed class PersonGroup(string nationalId, Card primary, List<Card> cards, bool hasActiveEmployment)
    {
        public string NationalId { get; } = nationalId;

        public Card Primary { get; } = primary;

        public List<Card> Cards { get; } = cards;

        public bool HasActiveEmployment { get; } = hasActiveEmployment;

        public bool IsEmailShared { get; set; }
    }

    private sealed class Context
    {
        public Context(PersonnelSnapshot snapshot, DateOnly today)
        {
            Today = today;
            CompaniesByFirm = snapshot.Companies.ToDictionary(c => c.LogoFirmNumber);
            PersonsByNationalId = snapshot.Persons.ToDictionary(p => p.NationalId, StringComparer.Ordinal);
            EmploymentsByCode = snapshot.Employments.ToDictionary(e => e.RegistryCode, StringComparer.Ordinal);
        }

        public DateOnly Today { get; }

        public Dictionary<short, Company> CompaniesByFirm { get; }

        public Dictionary<string, Person> PersonsByNationalId { get; }

        public Dictionary<string, Employment> EmploymentsByCode { get; }

        public List<Company> NewCompanies { get; } = [];

        public List<Person> NewPersons { get; } = [];

        public List<Employment> NewEmployments { get; } = [];

        public List<PersonnelSyncWarning> Warnings { get; } = [];

        public int CompaniesRenamed { get; set; }

        public int PersonsUpdated { get; set; }

        public int EmploymentsUpdated { get; set; }

        public int EmploymentsDeactivated { get; set; }

        public int AccountsDeactivated { get; set; }

        public int AccountsReactivated { get; set; }

        public void Warn(SyncWarningCode code, string registryCode, string detail) =>
            Warnings.Add(PersonnelSyncWarning.Create(code, registryCode, detail));
    }
}

/// <summary>Senkronizasyonun sonucu.</summary>
/// <param name="Counts">Sayaclar.</param>
/// <param name="Warnings">Veri kalitesi uyarilari.</param>
/// <param name="NewCompanies">Olusturulan firmalar.</param>
/// <param name="NewPersons">Olusturulan kisiler.</param>
/// <param name="NewEmployments">Olusturulan istihdamlar.</param>
public sealed record SyncOutcome(
    SyncCounts Counts,
    IReadOnlyList<PersonnelSyncWarning> Warnings,
    IReadOnlyList<Company> NewCompanies,
    IReadOnlyList<Person> NewPersons,
    IReadOnlyList<Employment> NewEmployments);
