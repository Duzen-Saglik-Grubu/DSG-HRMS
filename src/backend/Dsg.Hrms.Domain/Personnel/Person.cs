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
/// <b>Her metin ve tarih alani siniflandirilmistir</b> (<c>[PersonalData]</c>,
/// <c>[SpecialCategoryData]</c>, <c>[Secret]</c> veya gerekceli <c>[AuditVisible]</c>);
/// denetim izi ve gunluk maskelemesi bu oznitelige bakar (ADR-0009 §4). Isaretsiz yeni alan
/// mimari testini dusurur (#244, B-02). Ad tabanli kural ikinci savunma hatti olarak kalir.
/// </para>
/// </remarks>
public sealed class Person : Entity, IAuditable
{
    private readonly List<Employment> _employments = [];

    private Person()
    {
    }

    /// <summary>T.C. Kimlik Numarasi. Tekildir.</summary>
    [PersonalData(PersonalDataKind.NationalId)]
    public string NationalId { get; private set; } = string.Empty;

    /// <summary>Ad.</summary>
    [AuditVisible("Denetim izinin okunabilmesi icin gerekir: hangi kisinin kaydinin degistigi ve ad degisikligi (LOGO duzeltmesi) gorunmelidir")]
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Soyad.</summary>
    [AuditVisible("Denetim izinin okunabilmesi icin gerekir; soyadi degisikligi (evlilik) IK icin anlamli bir kayittir")]
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Dogum tarihi. Uyelikte kimlik dogrulama unsurudur (SYG-KMLK-013).</summary>
    [PersonalData(PersonalDataKind.Unspecified)]
    public DateOnly BirthDate { get; private set; }

    /// <summary>
    /// LOGO'da tanimli kurumsal e-posta (normallestirilmis). Tanimli degilse <c>null</c>.
    /// </summary>
    [PersonalData(PersonalDataKind.Email)]
    public string? Email { get; private set; }

    /// <summary>
    /// Ayni e-posta baska bir kisiye de tanimliysa <c>true</c>. Bu durumda adres ne giris
    /// kimligi ne dogrulama hedefi olarak kullanilabilir (SYG-KMLK-008).
    /// </summary>
    public bool IsEmailShared { get; private set; }

    /// <summary>
    /// Gecerli cep telefonu (<c>5XXXXXXXXX</c>). Tanimli degilse veya gecersizse <c>null</c>.
    /// </summary>
    [PersonalData(PersonalDataKind.Phone)]
    public string? MobilePhone { get; private set; }

    /// <summary>Cinsiyet (SYG-PERS-034). LOGO'da tanimsizsa <c>null</c>.</summary>
    [PersonalData(PersonalDataKind.Unspecified)]
    public Gender? Gender { get; private set; }

    /// <summary>
    /// Onceki soyadi. Yalnizca kadin personelde ve LOGO'da doluysa tutulur (PG-PERS-19):
    /// gosterilmeyecek bir degeri saklamak veri en aza indirme ilkesine aykiridir.
    /// </summary>
    [PersonalData(PersonalDataKind.Unspecified)]
    public string? PreviousSurname { get; private set; }

    /// <summary>Medeni hal (SYG-PERS-034).</summary>
    [PersonalData(PersonalDataKind.Unspecified)]
    public MaritalStatus? MaritalStatus { get; private set; }

    /// <summary>Egitim durumu; LOGO tanim adi (SYG-PERS-034).</summary>
    [AuditVisible("Egitim duzeyi bir tanim adidir (ilkokul, lisans...); kisiyi tek basina tanimlamaz ve IK duzeltmeleri icin degisikligi gorunmelidir")]
    public string? EducationName { get; private set; }

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

    /// <summary>
    /// T1 genel bilgilerini gunceller (SYG-PERS-003). Herhangi bir alan degistiyse <c>true</c>.
    /// </summary>
    /// <remarks>
    /// Onceki soyadi kadin olmayan kiside atilir. Degismeyen alanlar yazilmaz; aksi halde her
    /// senkronizasyon denetim izine gurultu uretirdi.
    /// </remarks>
    public bool ApplyProfile(PersonProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var previousSurname = profile.Gender == Personnel.Gender.Female && !string.IsNullOrWhiteSpace(profile.PreviousSurname)
            ? profile.PreviousSurname.Trim()
            : null;
        var education = string.IsNullOrWhiteSpace(profile.EducationName) ? null : profile.EducationName.Trim();

        var changed =
            Gender != profile.Gender ||
            !string.Equals(PreviousSurname, previousSurname, StringComparison.Ordinal) ||
            MaritalStatus != profile.MaritalStatus ||
            !string.Equals(EducationName, education, StringComparison.Ordinal);

        if (!changed)
        {
            return false;
        }

        Gender = profile.Gender;
        PreviousSurname = previousSurname;
        MaritalStatus = profile.MaritalStatus;
        EducationName = education;
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
