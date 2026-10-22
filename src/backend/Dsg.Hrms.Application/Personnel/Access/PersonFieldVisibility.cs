using Dsg.Hrms.Application.Identity.Authorization;

namespace Dsg.Hrms.Application.Personnel.Access;

/// <summary>
/// Korumali kisi alanlarini kimin gorebilecegi: tek politika (ADR-0019 §2, SYG-PERS-052).
/// </summary>
/// <remarks>
/// <para>
/// Politika dort yerde kullanilir ve baska yerde yeniden yazilmaz: API yaniti, suzgec
/// listesi, siralama ve disa aktarma alanlari. Yeniden yazilan kural er gec bir yerde eksik
/// kalir.
/// </para>
/// <para>
/// <b>Kayit duzeyi ve kume duzeyi</b> (B-07, AN-14): "kisinin kendisi" kurali yalnizca tek
/// kaydin gosterildigi yerde (kart, Profilim) gecerlidir. Kume islemlerinde (suzgec,
/// siralama, disa aktarma) yalnizca izin esas alinir; aksi halde kendi kan grubunu goren
/// personel, kan grubuna gore suzerek baskalarinin degerini cikarabilirdi.
/// </para>
/// </remarks>
public static class PersonFieldVisibility
{
    /// <summary>Tam TCKN; kisinin kendisi dahil digerleri maskeli gorur (S-01, SYG-PERS-053).</summary>
    public static bool CanSeeFullNationalId(PersonnelViewer viewer)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        return viewer.Has(Permissions.PersonViewNationalId);
    }

    /// <summary>Adres, kan grubu ve acil durum kisileri: izin veya kisinin kendisi (tek kayit).</summary>
    public static bool CanSeeSensitive(PersonnelViewer viewer, long personId)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        return viewer.Has(Permissions.PersonViewSensitive) || viewer.IsSelf(personId);
    }

    /// <summary>Evlilik tarihi ve evlilik yardimi: yalnizca izin; kisinin kendisi dahil degil (PG-PERS-19).</summary>
    public static bool CanSeeMarriage(PersonnelViewer viewer)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        return viewer.Has(Permissions.PersonViewSensitive);
    }

    /// <summary>Korumali alanlarla kume islemi (suzgec, siralama, disa aktarma): yalnizca izin.</summary>
    public static bool CanUseSensitiveInSets(PersonnelViewer viewer)
    {
        ArgumentNullException.ThrowIfNull(viewer);
        return viewer.Has(Permissions.PersonViewSensitive);
    }

    /// <summary>TCKN ile arama ve suzme: tam TCKN'yi gorebilen (SYG-PERS-030, AN-02).</summary>
    public static bool CanSearchByNationalId(PersonnelViewer viewer) => CanSeeFullNationalId(viewer);
}
