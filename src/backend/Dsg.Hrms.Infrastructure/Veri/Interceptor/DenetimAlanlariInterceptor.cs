using Dsg.Hrms.Application.Ortak.Soyutlamalar;
using Dsg.Hrms.Domain.Ortak;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Dsg.Hrms.Infrastructure.Veri.Interceptor;

/// <summary>
/// Denetim ve yumusak silme alanlarini <b>otomatik</b> doldurur (ADR-0004 §4, §5).
/// </summary>
/// <remarks>
/// <para>
/// Bu alanlarin serviste elle doldurulmasi, tek bir yerde unutulunca sessizce
/// bozulan bir denetim izi uretirdi. Merkezi ara katman bu riski ortadan kaldirir.
/// </para>
/// <para>
/// Silme istegi burada <b>guncellemeye donusturulur</b>: EF Core'a "sil" denilse
/// bile satir fiziksel olarak silinmez, <c>SilinmeAni</c> isaretlenir.
/// </para>
/// </remarks>
public sealed class DenetimAlanlariInterceptor(
    IMevcutKullanici mevcutKullanici,
    IZamanSaglayici zamanSaglayici) : SaveChangesInterceptor
{
    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        AlanlariDoldur(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        AlanlariDoldur(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AlanlariDoldur(DbContext? baglam)
    {
        if (baglam is null)
        {
            return;
        }

        var suAn = zamanSaglayici.SuAn;
        var kullaniciId = mevcutKullanici.KullaniciId;

        foreach (var giris in baglam.ChangeTracker.Entries())
        {
            SilmeyiIsaretlemeyeDonustur(giris, suAn, kullaniciId);
            DenetimAlanlariniDoldur(giris, suAn, kullaniciId);
        }
    }

    /// <summary>
    /// Fiziksel silme istegini yumusak silmeye cevirir (ADR-0004 §5).
    /// </summary>
    private static void SilmeyiIsaretlemeyeDonustur(
        EntityEntry giris,
        DateTimeOffset suAn,
        long? kullaniciId)
    {
        if (giris is { State: EntityState.Deleted, Entity: ISilinebilir silinebilir })
        {
            giris.State = EntityState.Modified;
            silinebilir.SilinmeAni = suAn;
            silinebilir.SilenKullaniciId = kullaniciId;
        }
    }

    private static void DenetimAlanlariniDoldur(
        EntityEntry giris,
        DateTimeOffset suAn,
        long? kullaniciId)
    {
        if (giris.Entity is not IDenetlenebilir denetlenebilir)
        {
            return;
        }

        switch (giris.State)
        {
            case EntityState.Added:
                denetlenebilir.OlusturmaAni = suAn;
                denetlenebilir.OlusturanKullaniciId = kullaniciId;
                break;

            case EntityState.Modified:
                denetlenebilir.GuncellemeAni = suAn;
                denetlenebilir.GuncelleyenKullaniciId = kullaniciId;

                // Olusturma bilgisi degistirilemez: denetim izinin guvenilirligi
                // bunun degismezligine dayanir.
                giris.Property(nameof(IDenetlenebilir.OlusturmaAni)).IsModified = false;
                giris.Property(nameof(IDenetlenebilir.OlusturanKullaniciId)).IsModified = false;
                break;

            default:
                break;
        }
    }
}
