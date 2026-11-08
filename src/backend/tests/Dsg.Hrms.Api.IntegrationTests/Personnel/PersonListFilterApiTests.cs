using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Organization;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Domain.Reference;
using Dsg.Hrms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Dsg.Hrms.Api.IntegrationTests.Identity.RegistrationApiFixture;

namespace Dsg.Hrms.Api.IntegrationTests.Personnel;

/// <summary>
/// Personel listesi suzgecleri uctan uca (SYG-PERS-024, 025, 026, 059). Veriler SENTETIKTIR.
/// </summary>
/// <remarks>
/// Kisi 1 oturum acan kullanicidir. Kisi 2: Merkez (Ankara) / Bilgi Islem, unvan 1, statu A, kadin,
/// evli, lisans, adres "Kızılay Mah.". Kisi 3: Bornova (Izmir) / Laboratuvar, unvan 2, statu B, erkek,
/// bekar, lise, adres "Bornova", gorev Biyolog. Kisi 3'un ayrica 2015'te ayrildigi 00007 sicili var:
/// Merkez / Bilgi Islem, unvan 9, statu C. Kisi 5: Merkez / Laboratuvar, unvan 3.
/// </remarks>
[Collection(ApiHostGroup.Name)]
public sealed class PersonListFilterApiTests : IClassFixture<Identity.RegistrationApiFixture>, IAsyncLifetime
{
    private const string Persons = "/api/v1/personnel/persons";
    private const string Password = "Kediler uyur 7";
    private const string UserEmail = "ahmet.yilmaz@duzen.com.tr";

    private readonly Identity.RegistrationApiFixture _fixture;
    private Seed _seed = null!;

    public PersonListFilterApiTests(Identity.RegistrationApiFixture fixture)
    {
        _fixture = fixture;
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetAsync();
        await CreateAccountAsync(1);
        _seed = await _fixture.WithDbAsync(SeedAsync);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Multi_select_matches_any_value_and_different_filters_apply_together()
    {
        // SYG-PERS-025 (PG-PERS-14, 15): secilenlerden herhangi biri; farkli suzgecler VE.
        var (client, jwt) = await HrAsync();
        using (client)
        {
            (await CodesAsync(client, jwt, $"branchId={_seed.Merkez}&branchId={_seed.Bornova}")).ShouldBe(["00002", "00003", "00005"], ignoreOrder: true);
            (await CodesAsync(client, jwt, $"branchId={_seed.Merkez}&departmentId={_seed.Laboratuvar}")).ShouldBe(["00005"]);
        }
    }

    [Fact]
    public async Task Active_status_matches_employment_filters_only_on_active_registries()
    {
        // SYG-PERS-022 (ADR-0019 §6, #338): "aktif" suzgeci yalnizca aktif sicillere bakar. Kisi 3
        // bugun Bornova'da; Merkez'deki eski sicili onu Merkez suzgecine sokmamali.
        var (client, jwt) = await HrAsync();
        using (client)
        {
            (await CodesAsync(client, jwt, $"branchId={_seed.Merkez}")).ShouldBe(["00002", "00005"], ignoreOrder: true);
            (await CodesAsync(client, jwt, "city=Ankara")).ShouldBe(["00002", "00005"], ignoreOrder: true);
            (await CodesAsync(client, jwt, "titleCode=9")).ShouldBeEmpty();
            (await CodesAsync(client, jwt, "statusCode=C")).ShouldBeEmpty();
            (await CodesAsync(client, jwt, "registryCode=00007")).ShouldBeEmpty();
            (await CodesAsync(client, jwt, "hireDateTo=2010-12-31")).ShouldBeEmpty();
            (await CodesAsync(client, jwt, "seniorityMin=5&seniorityMax=5")).ShouldBeEmpty();

            // "Tumu" seciliyken kisinin herhangi bir sicili eslesir (KR-126); kartta esas sicil gorunur.
            (await CodesAsync(client, jwt, $"status=all&branchId={_seed.Merkez}")).ShouldBe(["00002", "00003", "00005"], ignoreOrder: true);
            (await CodesAsync(client, jwt, "status=all&titleCode=9")).ShouldBe(["00003"]);
        }
    }

    [Fact]
    public async Task Each_card_field_has_a_filter()
    {
        // SYG-PERS-024: kartta gosterilen her alan icin suzgec (bugun var olan alanlar).
        var (client, jwt) = await HrAsync();
        using (client)
        {
            (await CodesAsync(client, jwt, "city=" + Uri.EscapeDataString("İzmir"))).ShouldBe(["00003"]);
            (await CodesAsync(client, jwt, "titleCode=1&titleCode=2")).ShouldBe(["00002", "00003"], ignoreOrder: true);
            (await CodesAsync(client, jwt, "statusCode=B")).ShouldBe(["00003"]);
            (await CodesAsync(client, jwt, "occupationCode=2222.01")).ShouldBe(["00003"]);
            (await CodesAsync(client, jwt, $"dutyId={_seed.Biologist}")).ShouldBe(["00003"]);
            (await CodesAsync(client, jwt, $"companyId={_seed.Company}")).ShouldBe(["00002", "00003", "00005"], ignoreOrder: true);
            (await CodesAsync(client, jwt, "gender=female")).ShouldBe(["00002"]);
            (await CodesAsync(client, jwt, "maritalStatus=notMarried")).ShouldBe(["00003"]);
            (await CodesAsync(client, jwt, "education=Lise")).ShouldBe(["00003"]);
            (await CodesAsync(client, jwt, "lastName=" + Uri.EscapeDataString("çağ"))).ShouldBe(["00003"]);
            (await CodesAsync(client, jwt, "registryCode=0005")).ShouldBe(["00005"]);
            (await CodesAsync(client, jwt, "email=ortak@")).ShouldBe(["00003"]);
            (await CodesAsync(client, jwt, "phone=0532%20123%2045%2068")).ShouldBe(["00003"]);
            (await CodesAsync(client, jwt, "address=kizilay")).ShouldBe(["00002"]);
            (await CodesAsync(client, jwt, $"nationalId={NationalId(5)[..6]}")).ShouldContain("00005");
            (await CodesAsync(client, jwt, "hireDateFrom=2020-01-01&hireDateTo=2020-01-01")).Count.ShouldBe(4);
            (await CodesAsync(client, jwt, "status=terminated&terminationDateFrom=2000-01-01")).ShouldBe(["00004"]);
            (await CodesAsync(client, jwt, "birthDateFrom=1985-04-12&birthDateTo=1985-04-12")).Count.ShouldBe(4);
            (await CodesAsync(client, jwt, "ageMin=41&ageMax=41")).Count.ShouldBe(4);
            (await CodesAsync(client, jwt, "ageMin=42")).ShouldBeEmpty();
            (await CodesAsync(client, jwt, "seniorityMin=6&seniorityMax=6")).Count.ShouldBe(4);
            (await CodesAsync(client, jwt, "seniorityMin=7")).ShouldBeEmpty();
        }
    }

    [Fact]
    public async Task Filters_on_fields_the_user_cannot_see_are_refused_and_not_offered()
    {
        // SYG-PERS-026 (KR-116 madde 3): goremedigi alana gore suzen kullanici gizli degeri cikarirdi.
        await GrantRoleAsync(await CreateRoleAsync(Permissions.PersonView));
        using var client = _fixture.CreateClient();
        var jwt = await SignInAsync(client);

        (await SendAsync(client, $"{Persons}?address=kizilay", jwt)).Status.ShouldBe(HttpStatusCode.BadRequest);
        (await SendAsync(client, $"{Persons}?nationalId=123", jwt)).Status.ShouldBe(HttpStatusCode.BadRequest);

        var options = (await SendAsync(client, $"{Persons}/filter-options", jwt)).Body;
        options.GetProperty("canFilterBySensitive").GetBoolean().ShouldBeFalse();
        options.GetProperty("canFilterByNationalId").GetBoolean().ShouldBeFalse();
        // Ayni adli sube farkli firmalarda olabilir: ad firmayla birlikte (#306).
        var company = await _fixture.WithDbAsync(context => context.Set<Company>().Select(c => c.Name).FirstAsync());
        options.GetProperty("branches").EnumerateArray().Select(b => b.GetProperty("label").GetString())
            .ShouldBe([$"Bornova ({company})", $"Merkez ({company})"], ignoreOrder: true);
        options.GetProperty("departments").EnumerateArray().Select(b => b.GetProperty("label").GetString())
            .ShouldContain($"Laboratuvar ({company})");
    }

    [Fact]
    public async Task Hr_is_offered_the_protected_filters()
    {
        var (client, jwt) = await HrAsync();
        using (client)
        {
            var options = (await SendAsync(client, $"{Persons}/filter-options", jwt)).Body;

            options.GetProperty("canFilterBySensitive").GetBoolean().ShouldBeTrue();
            options.GetProperty("defaultPageSize").GetInt32().ShouldBe(24);
            options.GetProperty("maxPageSize").GetInt32().ShouldBe(100);
            options.GetProperty("canFilterByNationalId").GetBoolean().ShouldBeTrue();
            options.GetProperty("cities").EnumerateArray().Select(c => c.GetProperty("value").GetString()).ShouldBe(["Ankara", "İzmir"], ignoreOrder: true);
            options.GetProperty("duties").EnumerateArray().ShouldContain(d => d.GetProperty("label").GetString() == "Biyolog");
        }
    }

    [Fact]
    public async Task Turkish_dotted_and_dotless_i_match_in_name_filters_and_search()
    {
        // #306: ad sutunlari Turkce siralamali (tr-TR-x-icu); SQL lower('I') orada "ı" verir. Suzgecin
        // veritabani tarafi ve arama terimi ayni sonucu uretmezse "ismail" İSMAİL'i bulamaz.
        var (client, jwt) = await HrAsync();
        using (client)
        {
            (await CodesAsync(client, jwt, "firstName=ismail")).ShouldBe(["00005"]);
            (await CodesAsync(client, jwt, "firstName=" + Uri.EscapeDataString("İSMAİL"))).ShouldBe(["00005"]);
            (await CodesAsync(client, jwt, "lastName=" + Uri.EscapeDataString("ışık"))).ShouldBe(["00005"]);
            (await CodesAsync(client, jwt, "lastName=isik")).ShouldBe(["00005"]);
            (await CodesAsync(client, jwt, "q=" + Uri.EscapeDataString("İsmail"))).ShouldBe(["00005"]);
        }

        // Veritabanindaki katlama (suzgec ve migration'daki anahtar doldurma) uygulamayla ayni.
        var folded = await _fixture.WithDbAsync(context => context.Set<Person>()
            .Where(p => p.NationalId == NationalId(5))
            .Select(p => HrmsDbFunctions.Fold(p.FirstName + " " + p.LastName))
            .SingleAsync());
        folded.ShouldBe(SearchText.KeyFor("İSMAİL", "IŞIK", null));
    }

    [Fact]
    public async Task Protected_filter_value_is_not_written_to_the_access_log()
    {
        // SYG-PERS-059 (B-01): korumali suzgecin degeri yazilmaz, yalnizca kullanildigi.
        var (client, jwt) = await HrAsync();
        using (client)
        {
            var before = await _fixture.WithDbAsync(context => context.AccessLog.MaxAsync(e => (long?)e.Id)) ?? 0;

            await CodesAsync(client, jwt, "address=kizilay&statusCode=A");

            var entry = await _fixture.WithDbAsync(context => context.AccessLog.Where(e => e.Id > before).SingleAsync());
            entry.Filters.ShouldNotContain("kizilay");
            entry.Filters.ShouldContain("(kullanıldı)");
            entry.Filters.ShouldContain("\"statusCode\": \"A\"");
        }
    }

    // ------------------------------------------------------------------ karta sonradan eklenen alanlar (#371)

    [Fact]
    public async Task Blood_group_marriage_photo_and_emergency_contact_have_filters()
    {
        await SeedCardExtrasAsync();
        var (client, jwt) = await HrAsync();
        using (client)
        {
            // Kan grubu: kisi 2 "1", kisi 3 "3"; kisi 5'te yok.
            (await CodesAsync(client, jwt, "bloodGroup=1")).ShouldBe(["00002"]);
            (await CodesAsync(client, jwt, "bloodGroup=1&bloodGroup=3")).ShouldBe(["00002", "00003"], ignoreOrder: true);
            (await CodesAsync(client, jwt, "bloodGroup=8")).ShouldBeEmpty();

            // Evlilik: kisi 2 (2015, aldi), kisi 5 (2020, almadi).
            (await CodesAsync(client, jwt, "marriageAllowance=received")).ShouldBe(["00002"]);
            (await CodesAsync(client, jwt, "marriageAllowance=received&marriageAllowance=notReceived")).ShouldBe(["00002", "00005"], ignoreOrder: true);
            (await CodesAsync(client, jwt, "marriageDateFrom=2016-01-01")).ShouldBe(["00005"]);
            (await CodesAsync(client, jwt, "marriageDateFrom=2015-06-20&marriageDateTo=2015-06-20")).ShouldBe(["00002"]);

            // Fotograf yalnizca kisi 3'te; acil durum kisisi yalnizca kisi 5'te.
            (await CodesAsync(client, jwt, "hasPhoto=true")).ShouldBe(["00003"]);
            (await CodesAsync(client, jwt, "hasPhoto=false")).ShouldNotContain("00003");
            (await CodesAsync(client, jwt, "hasEmergencyContact=true")).ShouldBe(["00005"]);
            (await CodesAsync(client, jwt, "hasEmergencyContact=false")).ShouldNotContain("00005");

            // Farkli suzgecler birlikte (VE).
            (await CodesAsync(client, jwt, "bloodGroup=1&bloodGroup=3&city=Ankara")).ShouldBe(["00002"]);

            // Secenek listesi referans veriden.
            var options = (await SendAsync(client, $"{Persons}/filter-options", jwt)).Body;
            options.GetProperty("bloodGroups").EnumerateArray().Select(o => o.GetProperty("label").GetString()).ShouldContain("A Rh+");
        }
    }

    [Fact]
    public async Task Blood_group_filter_counts_and_pages_like_the_equivalent_database_filter()
    {
        // SYG-PERS-063: bellekte suzulen kume; toplam ve sayfa, ayni kumenin veritabaninda suzulmus
        // halinden farkli olmamali. Esdeger: kan grubu bilinen kisilerin sicilleri (kisi 2 ve 3).
        await SeedCardExtrasAsync();
        var (client, jwt) = await HrAsync();
        using (client)
        {
            var expected = (await CodesAsync(client, jwt, "registryCode=0000&sort=lastName&pageSize=100"))
                .Where(code => code is "00002" or "00003")
                .ToList();
            expected.Count.ShouldBe(2);

            for (var page = 1; page <= 2; page++)
            {
                var (status, body) = await SendAsync(client, $"{Persons}?bloodGroup=1&bloodGroup=3&sort=lastName&pageSize=1&page={page}", jwt);
                status.ShouldBe(HttpStatusCode.OK);
                body.GetProperty("totalCount").GetInt32().ShouldBe(2);
                body.GetProperty("items").EnumerateArray().Single().GetProperty("registryCode").GetString().ShouldBe(expected[page - 1]);
            }
        }
    }

    [Fact]
    public async Task Protected_card_extra_filters_are_refused_without_the_sensitive_permission()
    {
        // SYG-PERS-026: kan grubu, evlilik ve acil durum view_sensitive ister; fotograf istemez.
        await GrantRoleAsync(await CreateRoleAsync(Permissions.PersonView));
        using var client = _fixture.CreateClient();
        var jwt = await SignInAsync(client);

        foreach (var query in new[] { "bloodGroup=1", "marriageAllowance=received", "marriageDateFrom=2015-01-01", "hasEmergencyContact=true" })
        {
            (await SendAsync(client, $"{Persons}?{query}", jwt)).Status.ShouldBe(HttpStatusCode.BadRequest, query);
        }

        (await SendAsync(client, $"{Persons}?hasPhoto=true", jwt)).Status.ShouldBe(HttpStatusCode.OK);
        (await SendAsync(client, $"{Persons}?marriageAllowance=maybe", jwt)).Status.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Blood_group_filter_value_is_hidden_and_each_listed_or_exported_person_gets_a_special_category_record()
    {
        // SYG-PERS-059: deger yazilmaz; kan grubu suzgeciyle listelenen her kisi icin SpecialCategoryView.
        await SeedCardExtrasAsync();
        var (client, jwt) = await HrAsync();
        using (client)
        {
            var person2 = await _fixture.WithDbAsync(context => context.Set<Person>().Where(p => p.NationalId == NationalId(2)).Select(p => p.PublicId).SingleAsync());
            var before = await _fixture.WithDbAsync(context => context.AccessLog.MaxAsync(e => (long?)e.Id)) ?? 0;

            await CodesAsync(client, jwt, "bloodGroup=1&marriageAllowance=received&hasEmergencyContact=false&hasPhoto=false");

            var entries = await _fixture.WithDbAsync(context => context.AccessLog.Where(e => e.Id > before).ToListAsync());
            var list = entries.Single(e => e.AccessType == Domain.Audit.AccessType.List);
            list.Filters.ShouldContain("\"bloodGroup\": \"(kullanıldı)\"");
            list.Filters.ShouldContain("\"marriageAllowance\": \"(kullanıldı)\"");
            list.Filters.ShouldContain("\"hasEmergencyContact\": \"(kullanıldı)\"");
            list.Filters.ShouldContain("\"hasPhoto\": \"false\"");
            entries.Where(e => e.AccessType == Domain.Audit.AccessType.SpecialCategoryView).Select(e => e.EntityId).ShouldBe([person2]);

            // Disa aktarma: kan grubu sutunu secilmese de suzgecle dosyaya giren kisi kaydedilir.
            before = entries.Max(e => e.Id);
            using (var request = new HttpRequestMessage(HttpMethod.Get, new Uri($"{Persons}/export?format=excel&field=registryCode&bloodGroup=1&bloodGroup=3", UriKind.Relative)))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
                using var response = await client.SendAsync(request);
                response.StatusCode.ShouldBe(HttpStatusCode.OK);
            }

            var exported = await _fixture.WithDbAsync(context => context.AccessLog.Where(e => e.Id > before).ToListAsync());
            var export = exported.Single(e => e.AccessType == Domain.Audit.AccessType.Export);
            export.Filters.ShouldContain("\"bloodGroup\": \"(kullanıldı)\"");
            exported.Count(e => e.AccessType == Domain.Audit.AccessType.SpecialCategoryView).ShouldBe(2);
        }
    }

    /// <summary>
    /// Kan grubu (sifreli), evlilik bilgisi, fotograf ve acil durum kisisi. Kisi 2: kan grubu "1",
    /// evlilik 20.06.2015 aldi. Kisi 3: kan grubu "3", fotograf. Kisi 5: evlilik 2020 almadi, acil durum kisisi.
    /// </summary>
    private Task<int> SeedCardExtrasAsync() =>
        _fixture.WithServicesAsync(async services =>
        {
            var protector = services.GetRequiredService<Infrastructure.Personnel.Protection.PersonalDataProtector>();
            var context = services.GetRequiredService<HrmsDbContext>();
            Task<Person> PersonAsync(int index) => context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));

            async Task BloodGroup(int index, string code)
            {
                var person = await PersonAsync(index);
                var row = await context.Set<PersonSensitive>().SingleOrDefaultAsync(r => r.PersonId == person.Id);
                if (row is null)
                {
                    row = PersonSensitive.For(person.Id);
                    context.Add(row);
                }

                row.SetProtectedBloodGroup(protector.Protect(code, Infrastructure.Personnel.Protection.ProtectedField.BloodGroup(person.PublicId)));
            }

            async Task Marriage(int index, DateOnly date, bool allowance)
            {
                var person = await PersonAsync(index);
                if (!await context.Set<PersonMarriage>().AnyAsync(m => m.PersonId == person.Id))
                {
                    context.Add(PersonMarriage.For(person.Id, new MarriageDetails(date, allowance)));
                }
            }

            await BloodGroup(2, "1");
            await BloodGroup(3, "3");
            await Marriage(2, new DateOnly(2015, 6, 20), allowance: true);
            await Marriage(5, new DateOnly(2020, 1, 1), allowance: false);

            var person3 = await PersonAsync(3);
            if (!await context.Set<PersonPhoto>().AnyAsync(p => p.PersonId == person3.Id))
            {
                context.Add(PersonPhoto.For(person3.Id, new string('a', 64), [0xFF, 0xD8, 0xFF], 1));
            }

            var person5 = await PersonAsync(5);
            if (!await context.Set<EmergencyContact>().AnyAsync(c => c.PersonId == person5.Id))
            {
                var relationship = await context.Set<ReferenceDefinition>().Where(d => d.Type == ReferenceType.Relationship).Select(d => d.Id).FirstAsync();
                context.Add(EmergencyContact.For(person5.Id, new EmergencyContactDetails(relationship, "Ayşe", "Deniz", "5321234567")));
            }

            return await context.SaveChangesAsync();
        });

    private async Task<Seed> SeedAsync(HrmsDbContext context)
    {
        var company = await context.Set<Company>().FirstAsync();
        var merkez = await Unit(context, () => Branch.FromLogo(company.Id, 1, "Merkez", "Ankara"), b => b.LogoNumber == 1 && b.CompanyId == company.Id);
        var bornova = await Unit(context, () => Branch.FromLogo(company.Id, 2, "Bornova", "İzmir"), b => b.LogoNumber == 2 && b.CompanyId == company.Id);
        var bilgiIslem = await Unit(context, () => Department.FromLogo(company.Id, 1, "Bilgi Islem"), d => d.LogoNumber == 1 && d.CompanyId == company.Id);
        var laboratuvar = await Unit(context, () => Department.FromLogo(company.Id, 2, "Laboratuvar"), d => d.LogoNumber == 2 && d.CompanyId == company.Id);
        var biologist = await context.Set<ReferenceDefinition>().SingleOrDefaultAsync(d => d.Type == ReferenceType.Duty && d.Name == "Biyolog")
            ?? ReferenceDefinition.Create(ReferenceType.Duty, "FBIYO", "Biyolog", 50);
        if (biologist.Id == 0)
        {
            context.Add(biologist);
        }

        await context.SaveChangesAsync();

        if (!await context.Set<EmploymentAssignment>().AnyAsync())
        {
            async Task Assign(string code, Branch branch, Department department, string title, string status, string occupation)
            {
                var employment = await context.Set<Employment>().SingleAsync(e => e.RegistryCode == code);
                var (assignment, _) = AssignmentTimeline.Apply(employment, [], new AssignmentValues(company.Id, branch.Id, department.Id, title, status, occupation, "Meslek " + occupation), Today);
                context.Add(assignment!);
            }

            await Assign("00002", merkez, bilgiIslem, "1", "A", "1111.01");
            await Assign("00003", bornova, laboratuvar, "2", "B", "2222.01");
            await Assign("00005", merkez, laboratuvar, "3", "A", "3333.01");

            // Kisi 3'un ayrilmis eski sicili (#338): bugunku suzgeclere "Aktif" seciliyken girmemeli.
            var person3Old = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(3));
            var old = Employment.Create(person3Old, company, "00007", new EmploymentDetails(new DateOnly(2010, 1, 1), new DateOnly(2015, 1, 1), 7), Today);
            context.Add(old);
            await context.SaveChangesAsync();
            var (oldAssignment, _) = AssignmentTimeline.Apply(old, [], new AssignmentValues(company.Id, merkez.Id, bilgiIslem.Id, "9", "C", "9999.01", "Meslek 9999.01"), Today);
            context.Add(oldAssignment!);

            var third = await context.Set<Employment>().SingleAsync(e => e.RegistryCode == "00003");
            var (duty, _) = DutyTimeline.Assign(third, [], biologist.Id, third.HireDate);
            context.Add(duty!);

            await Profile(context, 2, new PersonProfile(Gender.Female, null, MaritalStatus.Married, "Lisans"), "Kızılay Mah. 1");
            await Profile(context, 3, new PersonProfile(Gender.Male, null, MaritalStatus.NotMarried, "Lise"), "Bornova Cad. 2");
            var person3 = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(3));
            person3.Apply(new PersonDetails("Ahmet", "ÇAĞLAR", person3.BirthDate, person3.Email, person3.IsEmailShared, person3.MobilePhone));
            var person5 = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(5));
            person5.Apply(new PersonDetails("İSMAİL", "IŞIK", person5.BirthDate, person5.Email, person5.IsEmailShared, person5.MobilePhone));
            await context.SaveChangesAsync();
        }

        return new Seed(company.PublicId, merkez.PublicId, bornova.PublicId, laboratuvar.PublicId, biologist.PublicId);
    }

    private static async Task<T> Unit<T>(HrmsDbContext context, Func<T> create, System.Linq.Expressions.Expression<Func<T, bool>> find)
        where T : class
    {
        var existing = await context.Set<T>().SingleOrDefaultAsync(find);
        if (existing is not null)
        {
            return existing;
        }

        var unit = create();
        context.Add(unit);
        return unit;
    }

    private static async Task Profile(HrmsDbContext context, int index, PersonProfile profile, string address)
    {
        var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
        person.ApplyProfile(profile);
        var row = PersonSensitive.For(person.Id);
        row.ApplyAddress(address);
        context.Add(row);
    }

    private static async Task<List<string?>> CodesAsync(HttpClient client, string jwt, string query)
    {
        var (status, body) = await SendAsync(client, $"{Persons}?{query}", jwt);
        status.ShouldBe(HttpStatusCode.OK, query);
        return [.. body.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("registryCode").GetString())];
    }

    private async Task<(HttpClient Client, string Jwt)> HrAsync()
    {
        await GrantRoleAsync(Role.HrIdentityOperationsCode);
        var client = _fixture.CreateClient();
        return (client, await SignInAsync(client));
    }

    private Task<string> CreateRoleAsync(params string[] permissions) =>
        _fixture.WithDbAsync(async context =>
        {
            var code = $"test-{Guid.NewGuid():N}";
            await context.Database.ExecuteSqlAsync(
                $"INSERT INTO identity.role (public_id, code, name, is_system, created_at) VALUES ({Guid.CreateVersion7()}, {code}, {"Test rolu"}, false, now())");
            foreach (var permission in permissions)
            {
                await context.Database.ExecuteSqlAsync(
                    $"INSERT INTO identity.role_permission (role_id, permission) SELECT id, {permission} FROM identity.role WHERE code = {code}");
            }

            return code;
        });

    private Task<int> GrantRoleAsync(string roleCode) =>
        _fixture.WithDbAsync(async context =>
        {
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(1));
            var account = await context.Set<UserAccount>().SingleAsync(a => a.PersonId == person.Id);
            var role = await context.Set<Role>().SingleAsync(r => r.Code == roleCode);
            if (!await context.Set<UserRole>().AnyAsync(r => r.UserAccountId == account.Id && r.RoleId == role.Id))
            {
                context.Add(UserRole.Assign(account.Id, role.Id));
            }

            return await context.SaveChangesAsync();
        });

    private async Task CreateAccountAsync(int index) =>
        await _fixture.WithServicesAsync(async services =>
        {
            var context = services.GetRequiredService<HrmsDbContext>();
            var hasher = services.GetRequiredService<IPasswordHasher>();
            var person = await context.Set<Person>().SingleAsync(p => p.NationalId == NationalId(index));
            context.Add(UserAccount.Register(person.Id, hasher.Hash(PasswordPolicy.Normalize(Password)), _fixture.Clock.UtcNow));
            return await context.SaveChangesAsync();
        });

    private static async Task<string> SignInAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(new Uri("/api/v1/identity/sessions", UriKind.Relative), new { email = UserEmail, password = Password });
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        return body.GetProperty("session").GetProperty("accessToken").GetString()!;
    }

    private static async Task<(HttpStatusCode Status, JsonElement Body)> SendAsync(HttpClient client, string path, string jwt)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(path, UriKind.Relative));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", jwt);
        using var response = await client.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();
        return (response.StatusCode, string.IsNullOrEmpty(text) ? default : JsonDocument.Parse(text).RootElement.Clone());
    }

    private sealed record Seed(Guid Company, Guid Merkez, Guid Bornova, Guid Laboratuvar, Guid Biologist);
}
