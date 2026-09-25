using Dsg.Hrms.Domain.Common;
using Dsg.Hrms.Domain.Organization;

namespace Dsg.Hrms.Domain.Personnel;

/// <summary>
/// Istihdam donemi (ADR-0005 §1). Bir LOGO sicil kaydina karsilik gelir.
/// </summary>
/// <remarks>
/// <para>
/// Bir kisinin birden fazla es zamanli aktif istihdami olabilir; model bunu
/// kisitlamaz (SYG-KMLK-001). 22.09.2026 olcumunde 12 kisinin es zamanli birden fazla
/// aktif sicili vardir.
/// </para>
/// <para>
/// Sube, birim ve gorev gibi zamanla degisen nitelikler bu varlikta TUTULMAZ;
/// T1 kapsaminda tarih aralikli tablolarda tutulacaktir (ADR-0005 §3).
/// </para>
/// </remarks>
public sealed class Employment : Entity, IAuditable
{
    private Employment()
    {
    }

    /// <summary>Kisinin kimligi.</summary>
    public long PersonId { get; private set; }

    /// <summary>Kisi.</summary>
    public Person Person { get; private set; } = null!;

    /// <summary>LOGO sicil kodu. Veritabani genelinde tekildir.</summary>
    public string RegistryCode { get; private set; } = string.Empty;

    /// <summary>Firmanin kimligi.</summary>
    public long CompanyId { get; private set; }

    /// <summary>Firma.</summary>
    public Company Company { get; private set; } = null!;

    /// <summary>Ise giris tarihi.</summary>
    public DateOnly HireDate { get; private set; }

    /// <summary>Isten cikis tarihi (son calisma gunu). Aktif istihdamda <c>null</c>.</summary>
    public DateOnly? TerminationDate { get; private set; }

    /// <summary>
    /// Istihdam aktif mi. <see cref="TerminationDate"/> ve senkronizasyon gunune gore
    /// hesaplanir ve saklanir.
    /// </summary>
    /// <remarks>
    /// Saklanmasinin nedeni sorgu kolayligidir: "aktif personel" en sik sorulan
    /// sorudur ve her sorguda tarih karsilastirmasi yazmak hataya aciktir. Deger her
    /// senkronizasyonda yeniden hesaplandigi icin cikis tarihi gelen istihdam, en
    /// gec bir sonraki calismada pasiflesir.
    /// </remarks>
    public bool IsActive { get; private set; }

    /// <summary>
    /// LOGO kart referansi (<c>LH_001_PERSON.LREF</c>). Yalnizca izleme ve mutabakat
    /// icindir; yabanci anahtar DEGILDIR (ADR-0005 §6).
    /// </summary>
    public int LogoRef { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public long? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }

    /// <summary>
    /// Cikis tarihine gore istihdamin verilen gunde aktif olup olmadigi.
    /// </summary>
    /// <remarks>
    /// Cikis tarihi SON CALISMA GUNUDUR: o gun personel hala calisir. Cikis tarihi
    /// bugunse istihdam aktiftir; ertesi gun pasiflesir.
    /// </remarks>
    public static bool IsActiveOn(DateOnly? terminationDate, DateOnly today) =>
        terminationDate is null || terminationDate.Value >= today;

    /// <summary>Yeni istihdam olusturur.</summary>
    public static Employment Create(
        Person person,
        Company company,
        string registryCode,
        EmploymentDetails details,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(person);
        ArgumentNullException.ThrowIfNull(company);
        ArgumentNullException.ThrowIfNull(details);
        ArgumentException.ThrowIfNullOrWhiteSpace(registryCode);

        var employment = new Employment { RegistryCode = registryCode };
        employment.Apply(person, company, details, today);
        return employment;
    }

    /// <summary>
    /// Istihdam bilgilerini gunceller. Degisiklik olduysa <c>true</c> doner.
    /// </summary>
    /// <remarks>
    /// Kisi degisebilir: LOGO'da bir kartin TCKN'si duzeltildiginde istihdam dogru
    /// kisiye tasinir.
    /// </remarks>
    public bool Apply(Person person, Company company, EmploymentDetails details, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(person);
        ArgumentNullException.ThrowIfNull(company);
        ArgumentNullException.ThrowIfNull(details);

        var isActive = IsActiveOn(details.TerminationDate, today);

        var changed =
            !ReferenceEquals(Person, person) ||
            !ReferenceEquals(Company, company) ||
            HireDate != details.HireDate ||
            TerminationDate != details.TerminationDate ||
            IsActive != isActive ||
            LogoRef != details.LogoRef;

        if (!changed)
        {
            return false;
        }

        Person = person;
        Company = company;
        HireDate = details.HireDate;
        TerminationDate = details.TerminationDate;
        IsActive = isActive;
        LogoRef = details.LogoRef;
        return true;
    }
}

/// <summary>
/// Istihdamin kaynak sistemden gelen, degisebilen bilgileri.
/// </summary>
/// <param name="HireDate">Ise giris tarihi.</param>
/// <param name="TerminationDate">Isten cikis tarihi; aktifse <c>null</c>.</param>
/// <param name="LogoRef">LOGO kart referansi.</param>
public sealed record EmploymentDetails(DateOnly HireDate, DateOnly? TerminationDate, int LogoRef);
