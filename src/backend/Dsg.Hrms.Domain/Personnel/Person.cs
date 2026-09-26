using Dsg.Hrms.Domain.Common;

namespace Dsg.Hrms.Domain.Personnel;

/// <summary>
/// Kisi (ADR-0005 §1). TCKN ile tekildir; omur boyu tek kayit.
/// </summary>
/// <remarks>
/// <para>
/// Bir kisinin birden fazla istihdami olabilir (<see cref="Employments"/>); kullanici
/// hesabi kisiye baglanir, istihdama degil (<c>KR-014</c>).
/// </para>
/// <para>
/// <b>Ozellik adlari bilincli secildi:</b> <see cref="NationalId"/>, <see cref="Email"/>,
/// <see cref="MobilePhone"/> ve <see cref="BirthDate"/>, denetim izinde ad tabanli
/// maskeleme kuralina takilir (ADR-0009 §4). Domain katmani uygulama katmanindaki
/// <c>[PersonalData]</c> ozniteligini goremedigi icin maskelemenin tek dayanagi bu
/// adlardir; yeniden adlandirilirlarsa kisisel veri denetim izine duz metin duser.
/// </para>
/// </remarks>
public sealed class Person : Entity, IAuditable
{
    private readonly List<Employment> _employments = [];

    private Person()
    {
    }

    /// <summary>T.C. Kimlik Numarasi. Tekildir.</summary>
    public string NationalId { get; private set; } = string.Empty;

    /// <summary>Ad.</summary>
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Soyad.</summary>
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Dogum tarihi. Uyelikte kimlik dogrulama unsurudur (SYG-KMLK-013).</summary>
    public DateOnly BirthDate { get; private set; }

    /// <summary>
    /// LOGO'da tanimli kurumsal e-posta (normallestirilmis). Tanimli degilse <c>null</c>.
    /// </summary>
    public string? Email { get; private set; }

    /// <summary>
    /// Ayni e-posta baska bir kisiye de tanimliysa <c>true</c>. Bu durumda adres ne giris
    /// kimligi ne dogrulama hedefi olarak kullanilabilir (SYG-KMLK-008).
    /// </summary>
    public bool IsEmailShared { get; private set; }

    /// <summary>
    /// Gecerli cep telefonu (<c>5XXXXXXXXX</c>). Tanimli degilse veya gecersizse <c>null</c>.
    /// </summary>
    public string? MobilePhone { get; private set; }

    /// <summary>Kisinin istihdamlari.</summary>
    public IReadOnlyCollection<Employment> Employments => _employments;

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public long? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }

    /// <summary>Yeni kisi olusturur.</summary>
    /// <exception cref="ArgumentException">TCKN gecersizse.</exception>
    public static Person Create(string nationalId, PersonDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        if (!Personnel.NationalId.IsValid(nationalId))
        {
            throw new ArgumentException("Gecersiz T.C. Kimlik Numarasi.", nameof(nationalId));
        }

        var person = new Person { NationalId = nationalId };
        person.Apply(details);
        return person;
    }

    /// <summary>
    /// Kisi bilgilerini gunceller. Herhangi bir alan degistiyse <c>true</c> doner.
    /// </summary>
    /// <remarks>
    /// Degismeyen alanlar YAZILMAZ. Aksi halde her senkronizasyon (15 dakikada bir)
    /// her kisi icin bir guncelleme ve bir denetim izi kaydi uretirdi; gercek
    /// degisiklikler bu gurultu icinde kaybolurdu.
    /// </remarks>
    public bool Apply(PersonDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        var changed =
            !string.Equals(FirstName, details.FirstName, StringComparison.Ordinal) ||
            !string.Equals(LastName, details.LastName, StringComparison.Ordinal) ||
            BirthDate != details.BirthDate ||
            !string.Equals(Email, details.Email, StringComparison.Ordinal) ||
            IsEmailShared != details.IsEmailShared ||
            !string.Equals(MobilePhone, details.MobilePhone, StringComparison.Ordinal);

        if (!changed)
        {
            return false;
        }

        FirstName = details.FirstName;
        LastName = details.LastName;
        BirthDate = details.BirthDate;
        Email = details.Email;
        IsEmailShared = details.IsEmailShared;
        MobilePhone = details.MobilePhone;
        return true;
    }
}

/// <summary>
/// Kisinin kaynak sistemden gelen, degisebilen bilgileri.
/// </summary>
/// <param name="FirstName">Ad.</param>
/// <param name="LastName">Soyad.</param>
/// <param name="BirthDate">Dogum tarihi.</param>
/// <param name="Email">Normallestirilmis kurumsal e-posta; yoksa <c>null</c>.</param>
/// <param name="IsEmailShared">Adres baska bir kisiye de tanimli mi.</param>
/// <param name="MobilePhone">Normallestirilmis cep telefonu; yoksa <c>null</c>.</param>
public sealed record PersonDetails(
    string FirstName,
    string LastName,
    DateOnly BirthDate,
    string? Email,
    bool IsEmailShared,
    string? MobilePhone);
