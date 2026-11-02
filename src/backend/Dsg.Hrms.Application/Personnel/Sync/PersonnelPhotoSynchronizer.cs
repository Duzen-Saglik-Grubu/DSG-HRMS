using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Application.Personnel.Sync;

/// <summary>
/// Senkronizasyonun fotograf adimi (SYG-PERS-013, 014; ADR-0021 §5). Saf mantiktir; veritabanina
/// ve LOGO'ya dokunmaz.
/// </summary>
/// <remarks>
/// <para>
/// Kisinin fotografi, genel bilgileri gibi <b>esas istihdaminin</b> kartindan alinir
/// (<see cref="Employment.Primary"/>, <c>KR-126</c>). Esas kartta fotograf yoksa kisinin
/// fotografi yoktur; baska sicilin fotografina dusulmez. Boylece liste kartinda gorunen sicil ile
/// fotografin kaynagi hep aynidir.
/// </para>
/// <para>
/// Iki asama vardir: <see cref="Plan"/> ozetleri karsilastirir ve yalnizca degisen kartlari
/// okunacak olarak isaretler; <see cref="Apply"/> okunan goruntuleri kucultup uygular. Arada
/// LOGO'dan yalnizca degisen goruntuler okunur.
/// </para>
/// </remarks>
public static class PersonnelPhotoSynchronizer
{
    /// <summary>Ozetleri mevcut fotograflarla karsilastirir.</summary>
    /// <param name="digests">LOGO'daki fotograflarin ozetleri.</param>
    /// <param name="snapshot">Mevcut durum.</param>
    /// <param name="thumbnailVersion">
    /// Gecerli kucultme kuralinin surumu (<see cref="IPhotoThumbnailer.Version"/>); eski surumle
    /// saklanan fotograf yeniden okunur.
    /// </param>
    public static PhotoPlan Plan(IReadOnlyList<LogoPhotoDigest> digests, PersonnelPhotoSnapshot snapshot, int thumbnailVersion)
    {
        ArgumentNullException.ThrowIfNull(digests);
        ArgumentNullException.ThrowIfNull(snapshot);

        var hashByCard = digests.GroupBy(d => d.LogoRef).ToDictionary(g => g.Key, g => g.First().Hash);
        var photoByPerson = snapshot.Photos.ToDictionary(p => p.PersonId);
        var employmentsByPerson = snapshot.Employments.ToLookup(e => e.PersonId);

        var toFetch = new Dictionary<int, long>();
        var toRemove = new List<PersonPhoto>();

        foreach (var personId in snapshot.PersonIds)
        {
            var primary = Employment.Primary(employmentsByPerson[personId]);
            var hash = primary is null ? null : hashByCard.GetValueOrDefault(primary.LogoRef);
            var existing = photoByPerson.GetValueOrDefault(personId);

            if (hash is null)
            {
                // LOGO'da silinen fotograf HRMS'ten de kaldirilir (SYG-PERS-014).
                if (existing is not null)
                {
                    toRemove.Add(existing);
                }

                continue;
            }

            // Ozeti degismeyen fotograf ikinci calismada aktarilmaz (PG-PERS-41); kucultme kurali
            // degistiyse LOGO'da degismese de yeniden kucultulur (#315).
            if (!string.Equals(existing?.SourceHash, hash, StringComparison.OrdinalIgnoreCase)
                || existing!.ThumbnailVersion != thumbnailVersion)
            {
                toFetch[primary!.LogoRef] = personId;
            }
        }

        return new PhotoPlan(toFetch, toRemove);
    }

    /// <summary>
    /// Okunan goruntuleri kucultur ve uygular. Cozulemeyen goruntu atlanir; kisinin varsa eski
    /// fotografi korunur ve bir sonraki calismada yeniden denenir.
    /// </summary>
    /// <returns>Eklenecek yeni fotograflar, degisen ve cozulemeyen fotograf sayisi.</returns>
    public static PhotoBatchOutcome Apply(
        IReadOnlyList<LogoPhoto> photos,
        PhotoPlan plan,
        PersonnelPhotoSnapshot snapshot,
        IPhotoThumbnailer thumbnailer)
    {
        ArgumentNullException.ThrowIfNull(photos);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(snapshot);
        ArgumentNullException.ThrowIfNull(thumbnailer);

        var photoByPerson = snapshot.Photos.ToDictionary(p => p.PersonId);
        var added = new List<PersonPhoto>();
        var changed = 0;
        var undecodable = 0;

        foreach (var photo in photos)
        {
            if (!plan.ToFetch.TryGetValue(photo.LogoRef, out var personId))
            {
                continue;
            }

            var thumbnail = thumbnailer.CreateThumbnail(photo.Data);
            if (thumbnail is null)
            {
                undecodable++;
                continue;
            }

            if (photoByPerson.TryGetValue(personId, out var existing))
            {
                existing.Replace(photo.Hash, thumbnail, thumbnailer.Version);
            }
            else
            {
                added.Add(PersonPhoto.For(personId, photo.Hash, thumbnail, thumbnailer.Version));
            }

            changed++;
        }

        return new PhotoBatchOutcome(added, changed, undecodable);
    }
}

/// <summary>Fotograf adiminin yukledigi mevcut durum.</summary>
/// <param name="PersonIds">Kisilerin ic kimlikleri.</param>
/// <param name="Employments">Istihdamlar (esas kartin secimi icin).</param>
/// <param name="Photos">Mevcut fotograflar (izlenen).</param>
public sealed record PersonnelPhotoSnapshot(
    IReadOnlyList<long> PersonIds,
    IReadOnlyList<Employment> Employments,
    IReadOnlyList<PersonPhoto> Photos);

/// <summary>Ozet karsilastirmasinin sonucu.</summary>
/// <param name="ToFetch">Goruntusu okunacak kartlar: kart <c>LREF</c>'i ve kisinin ic kimligi.</param>
/// <param name="ToRemove">LOGO'da artik olmayan fotograflar.</param>
public sealed record PhotoPlan(IReadOnlyDictionary<int, long> ToFetch, IReadOnlyList<PersonPhoto> ToRemove);

/// <summary>Bir grup goruntunun uygulanma sonucu.</summary>
/// <param name="Added">Eklenecek yeni fotograflar.</param>
/// <param name="Changed">Eklenen veya degisen fotograf sayisi.</param>
/// <param name="Undecodable">Cozulemeyen goruntu sayisi.</param>
public sealed record PhotoBatchOutcome(IReadOnlyList<PersonPhoto> Added, int Changed, int Undecodable);
