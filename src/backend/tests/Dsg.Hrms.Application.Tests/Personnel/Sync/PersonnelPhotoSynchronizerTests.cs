using Dsg.Hrms.Application.Personnel.Sync;
using Dsg.Hrms.Domain.Common;
using Dsg.Hrms.Domain.Organization;
using Dsg.Hrms.Domain.Personnel;
using NSubstitute;
using static Dsg.Hrms.Application.Tests.Personnel.Sync.TestData;

namespace Dsg.Hrms.Application.Tests.Personnel.Sync;

/// <summary>
/// Senkronizasyonun fotograf adimi (SYG-PERS-014; ADR-0021 §5). Veriler SENTETIKTIR.
/// </summary>
public sealed class PersonnelPhotoSynchronizerTests
{
    private static readonly string HashA = new('A', PersonPhoto.SourceHashLength);
    private static readonly string HashB = new('B', PersonPhoto.SourceHashLength);
    private static readonly byte[] Thumbnail = [0xFF, 0xD8, 0x01];
    private const int Version = 2;

    private readonly Company _company = WithId(Company.FromLogo(1, "Duzen Laboratuvarlar"), 1);
    private readonly IPhotoThumbnailer _thumbnailer = Substitute.For<IPhotoThumbnailer>();

    public PersonnelPhotoSynchronizerTests()
    {
        _thumbnailer.CreateThumbnail(Arg.Any<byte[]>()).Returns(Thumbnail);
        _thumbnailer.Version.Returns(Version);
    }

    [Fact]
    public void Unchanged_digest_is_not_fetched_again()
    {
        // PG-PERS-41: ozeti degismeyen fotograf ikinci calismada aktarilmaz.
        var person = Person(1);
        var snapshot = Snapshot([person], [Employ(person, "00001", logoRef: 10)], [PersonPhoto.For(1, HashA, Thumbnail, Version)]);

        var plan = PersonnelPhotoSynchronizer.Plan([new LogoPhotoDigest(10, HashA.ToLowerInvariant())], snapshot, Version);

        plan.ToFetch.ShouldBeEmpty();
        plan.ToRemove.ShouldBeEmpty();
    }

    [Fact]
    public void New_and_changed_digests_are_fetched_and_applied()
    {
        var withPhoto = Person(1);
        var withoutPhoto = Person(2);
        var existing = PersonPhoto.For(1, HashA, [0x01], Version);
        var snapshot = Snapshot(
            [withPhoto, withoutPhoto],
            [Employ(withPhoto, "00001", logoRef: 10), Employ(withoutPhoto, "00002", logoRef: 20)],
            [existing]);

        var plan = PersonnelPhotoSynchronizer.Plan([new LogoPhotoDigest(10, HashB), new LogoPhotoDigest(20, HashA)], snapshot, Version);
        plan.ToFetch.ShouldBe(new Dictionary<int, long> { [10] = 1, [20] = 2 });

        var outcome = PersonnelPhotoSynchronizer.Apply(
            [new LogoPhoto(10, HashB, [0x10]), new LogoPhoto(20, HashA, [0x20])], plan, snapshot, _thumbnailer);

        existing.SourceHash.ShouldBe(HashB);
        existing.Content.ShouldBe(Thumbnail);
        var added = outcome.Added.ShouldHaveSingleItem();
        added.PersonId.ShouldBe(2);
        added.SourceHash.ShouldBe(HashA);
        outcome.Changed.ShouldBe(2);
    }

    [Fact]
    public void Photo_thumbnailed_by_an_older_rule_is_fetched_again_although_unchanged_in_logo()
    {
        // #315: kucultme kurali degisince (EXIF yonu artik uygulanmaz) saklanan fotograflar LOGO'da
        // degismese de yeniden kucultulur; ayni kuralla saklanan fotograf okunmaz.
        var older = Person(1);
        var current = Person(2);
        var stale = PersonPhoto.For(1, HashA, [0x01], Version - 1);
        var snapshot = Snapshot(
            [older, current],
            [Employ(older, "00001", logoRef: 10), Employ(current, "00002", logoRef: 20)],
            [stale, PersonPhoto.For(2, HashB, Thumbnail, Version)]);

        var plan = PersonnelPhotoSynchronizer.Plan([new LogoPhotoDigest(10, HashA), new LogoPhotoDigest(20, HashB)], snapshot, Version);
        plan.ToFetch.ShouldBe(new Dictionary<int, long> { [10] = 1 });

        var outcome = PersonnelPhotoSynchronizer.Apply([new LogoPhoto(10, HashA, [0x10])], plan, snapshot, _thumbnailer);

        outcome.Changed.ShouldBe(1);
        stale.Content.ShouldBe(Thumbnail);
        stale.ThumbnailVersion.ShouldBe(Version);
    }

    [Fact]
    public void Photo_deleted_in_logo_is_removed()
    {
        // SYG-PERS-014: LOGO'da silinen fotograf HRMS'ten de kaldirilir.
        var person = Person(1);
        var existing = PersonPhoto.For(1, HashA, Thumbnail, Version);

        var plan = PersonnelPhotoSynchronizer.Plan([], Snapshot([person], [Employ(person, "00001", logoRef: 10)], [existing]), Version);

        plan.ToRemove.ShouldBe([existing]);
    }

    [Fact]
    public void Photo_comes_only_from_the_primary_employment_card()
    {
        // KR-126: liste kartinda gorunen sicil ile fotografin kaynagi aynidir; esas kartta fotograf
        // yoksa baska sicilin fotografina dusulmez.
        var person = Person(1);
        var older = Employ(person, "00001", logoRef: 10, hire: new DateOnly(2015, 1, 1));
        var newer = Employ(person, "00002", logoRef: 20, hire: new DateOnly(2022, 1, 1));

        PersonnelPhotoSynchronizer.Plan([new LogoPhotoDigest(10, HashA)], Snapshot([person], [older, newer], []), Version)
            .ToFetch.ShouldBeEmpty();
        PersonnelPhotoSynchronizer.Plan([new LogoPhotoDigest(10, HashA), new LogoPhotoDigest(20, HashB)], Snapshot([person], [older, newer], []), Version)
            .ToFetch.ShouldBe(new Dictionary<int, long> { [20] = 1 });
    }

    [Fact]
    public void Undecodable_image_is_skipped_and_the_old_photo_is_kept()
    {
        var person = Person(1);
        var existing = PersonPhoto.For(1, HashA, Thumbnail, Version);
        var snapshot = Snapshot([person], [Employ(person, "00001", logoRef: 10)], [existing]);
        _thumbnailer.CreateThumbnail(Arg.Any<byte[]>()).Returns((byte[]?)null);

        var plan = PersonnelPhotoSynchronizer.Plan([new LogoPhotoDigest(10, HashB)], snapshot, Version);
        var outcome = PersonnelPhotoSynchronizer.Apply([new LogoPhoto(10, HashB, [0x00])], plan, snapshot, _thumbnailer);

        outcome.Undecodable.ShouldBe(1);
        outcome.Changed.ShouldBe(0);
        existing.SourceHash.ShouldBe(HashA);
    }

    [Fact]
    public void Stored_digest_is_the_digest_of_the_image_actually_read()
    {
        // Ozet sorgusuyla goruntu sorgusu arasinda LOGO'da fotograf degisirse okunan goruntunun
        // ozeti saklanir; bir sonraki calisma ozetler esit oldugu icin yeniden okumaz.
        var person = Person(1);
        var snapshot = Snapshot([person], [Employ(person, "00001", logoRef: 10)], []);

        var plan = PersonnelPhotoSynchronizer.Plan([new LogoPhotoDigest(10, HashA)], snapshot, Version);
        var outcome = PersonnelPhotoSynchronizer.Apply([new LogoPhoto(10, HashB, [0x01])], plan, snapshot, _thumbnailer);

        outcome.Added.ShouldHaveSingleItem().SourceHash.ShouldBe(HashB);
    }

    [Fact]
    public void Photo_rejects_a_malformed_digest_and_empty_content()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => PersonPhoto.For(1, "ABC", Thumbnail, Version));
        Should.Throw<ArgumentOutOfRangeException>(() => PersonPhoto.For(1, HashA, [], Version));
        Should.Throw<ArgumentOutOfRangeException>(() => PersonPhoto.For(1, HashA, Thumbnail, 0));
    }

    private static PersonnelPhotoSnapshot Snapshot(Person[] persons, Employment[] employments, PersonPhoto[] photos) =>
        new([.. persons.Select(p => p.Id)], employments, photos);

    private static Person Person(int index) =>
        WithId(Dsg.Hrms.Domain.Personnel.Person.Create(NationalId(index), new PersonDetails("Ayse", "Yilmaz", new DateOnly(1990, 1, 1), null, false, null)), index);

    private Employment Employ(Person person, string code, int logoRef, DateOnly? hire = null)
    {
        var employment = Employment.Create(person, _company, code, new EmploymentDetails(hire ?? new DateOnly(2020, 1, 1), null, logoRef), Today);
        typeof(Employment).GetProperty(nameof(Employment.PersonId))!.SetValue(employment, person.Id);
        return employment;
    }

    private static T WithId<T>(T entity, long id)
        where T : Entity
    {
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entity, id);
        return entity;
    }
}
