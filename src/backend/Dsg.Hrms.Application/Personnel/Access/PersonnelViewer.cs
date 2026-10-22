namespace Dsg.Hrms.Application.Personnel.Access;

/// <summary>
/// Kullanicinin hangi kisileri gorebildigi (ADR-0019 §1, ADR-0007 §2).
/// </summary>
/// <remarks>
/// T2 dogrudan ast kapsamini (<c>DirectReports</c>), T4 digerlerini ekler (SYG-PERS-050).
/// </remarks>
public enum PersonnelScope
{
    /// <summary>Yalnizca kendi kaydi. Izin gerektirmez (ADR-0007 §5, PG-PERS-25).</summary>
    Self = 1,

    /// <summary>Tum personel (<c>personnel.person.view</c>).</summary>
    All = 2,
}

/// <summary>
/// Istegi yapan kullanicinin personel verisine bakisi: kendi kisi kaydi, kapsami ve izinleri.
/// </summary>
/// <param name="PersonId">Kullanicinin kisi kaydinin ic kimligi; arka plan islerinde <c>null</c>.</param>
/// <param name="Scope">Satir duzeyi kapsam.</param>
/// <param name="Permissions">Kullanicinin izinleri.</param>
public sealed record PersonnelViewer(long? PersonId, PersonnelScope Scope, IReadOnlySet<string> Permissions)
{
    /// <summary>Izinlerden kapsami turetir: <c>personnel.person.view</c> varsa <c>All</c>, yoksa <c>Self</c>.</summary>
    public static PersonnelViewer From(long? personId, IReadOnlySet<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);

        return new PersonnelViewer(
            personId,
            permissions.Contains(Identity.Authorization.Permissions.PersonView) ? PersonnelScope.All : PersonnelScope.Self,
            permissions);
    }

    /// <summary>Kullanicinin bu izni var mi.</summary>
    public bool Has(string permission) => Permissions.Contains(permission);

    /// <summary>Kayit kullanicinin kendisine mi ait.</summary>
    public bool IsSelf(long personId) => PersonId == personId;
}

/// <summary>
/// Istegi yapan kullanicinin personel bakisini verir (ADR-0019 §1).
/// </summary>
/// <remarks>
/// Kisi okumalari bu bakisla <see cref="IPersonnelQuery"/>'den gecer. Izinler, eylem
/// yetkisiyle ayni kaynaktan (her istekte veritabanindan) okunur; rolu kaldirilan kullanici
/// kapsamini da hemen kaybeder.
/// </remarks>
public interface ICurrentUserScope
{
    /// <summary>Kullanicinin bakisi. Oturum yoksa kisi ve izin bos, kapsam <c>Self</c>.</summary>
    Task<PersonnelViewer> GetAsync(CancellationToken cancellationToken);
}
