using Dsg.Hrms.Domain.Common;

namespace Dsg.Hrms.Domain.Personnel;

/// <summary>
/// Kisinin acil durumda aranacak kisisi (PG-PERS-44; ADR-0021 §1, §6; SYG-PERS-043, 054). HRMS'te
/// IK veya Sistem Yoneticisi girer; LOGO'da yoktur (<c>KR-112</c>). Bir kisiye birden fazla girilebilir.
/// </summary>
/// <remarks>
/// <para>
/// Korumali alandir: kisi tablosunda degil kendi tablosunda durur ve yalnizca listelenen siniflar
/// bu tipe bagimli olabilir (ADR-0019 §1, §3). Gorunurluk <c>PersonFieldVisibility.CanSeeSensitive</c>
/// ile ayni kuraldir (SYG-PERS-052).
/// </para>
/// <para>
/// Yakinlik referans verideki "Yakinlik" tanimlarindan secilir (<c>KR-135</c>); kayit tanimin ic
/// kimligini tutar. Pasife alinan tanim eski kayitta adiyla gorunmeye devam eder.
/// </para>
/// <para>
/// Kayit silinmez, yumusak silinir (ADR-0004 §5). Butun alanlari kisisel veridir; denetim izinde
/// maskelenir (ADR-0021 §6).
/// </para>
/// </remarks>
public sealed class EmergencyContact : Entity, IAuditable, ISoftDeletable
{
    /// <summary>Ad ve soyadin en buyuk uzunlugu.</summary>
    public const int MaxNameLength = 100;

    private EmergencyContact()
    {
    }

    /// <summary>Kisinin ic kimligi.</summary>
    public long PersonId { get; private set; }

    /// <summary>
    /// Yakinlik taniminin ic kimligi (referans veri, tur yakinlik). Tanim kisisel veri degildir ama
    /// kisiye bagli secim oldugu icin denetim izinde maskelenir.
    /// </summary>
    [PersonalData(PersonalDataKind.Unspecified)]
    public long RelationshipDefinitionId { get; private set; }

    /// <summary>Ad.</summary>
    [PersonalData(PersonalDataKind.Unspecified)]
    public string FirstName { get; private set; } = string.Empty;

    /// <summary>Soyad.</summary>
    [PersonalData(PersonalDataKind.Unspecified)]
    public string LastName { get; private set; } = string.Empty;

    /// <summary>Cep telefonu (<c>5XXXXXXXXX</c>, SYG-KMLK-009).</summary>
    [PersonalData(PersonalDataKind.Phone)]
    public string MobilePhone { get; private set; } = string.Empty;

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public long? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAt { get; set; }

    /// <inheritdoc />
    public long? DeletedBy { get; set; }

    /// <summary>Kisiye yeni acil durum kisisi ekler.</summary>
    public static EmergencyContact For(long personId, EmergencyContactDetails details)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(personId);
        ArgumentNullException.ThrowIfNull(details);

        var contact = new EmergencyContact { PersonId = personId };
        contact.Change(details);
        return contact;
    }

    /// <summary>Bilgileri degistirir; degistiyse <c>true</c>.</summary>
    public bool Change(EmergencyContactDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        if (RelationshipDefinitionId == details.RelationshipDefinitionId && FirstName == details.FirstName
            && LastName == details.LastName && MobilePhone == details.MobilePhone)
        {
            return false;
        }

        RelationshipDefinitionId = details.RelationshipDefinitionId;
        FirstName = details.FirstName;
        LastName = details.LastName;
        MobilePhone = details.MobilePhone;
        return true;
    }
}

/// <summary>Acil durum kisisinin dogrulanmis bilgileri; yakinlik taniminin ic kimligiyle.</summary>
/// <param name="RelationshipDefinitionId">Yakinlik taniminin ic kimligi.</param>
/// <param name="FirstName">Ad.</param>
/// <param name="LastName">Soyad.</param>
/// <param name="MobilePhone">Cep telefonu, <c>5XXXXXXXXX</c>.</param>
public sealed record EmergencyContactDetails(long RelationshipDefinitionId, string FirstName, string LastName, string MobilePhone)
{
    /// <summary>Degerleri yazmaz: kayit bir gunluk iletisine karisirsa kisisel veri dusmez.</summary>
    public override string ToString() => nameof(EmergencyContactDetails);
}

/// <summary>
/// Formdan gelen, bicimi dogrulanmis ve normallestirilmis giris (SYG-PERS-043). Yakinlik tanimin dis
/// kimligidir; tanimin varligi ve aktifligi servis tarafinda denetlenir.
/// </summary>
/// <param name="RelationshipId">Yakinlik taniminin dis kimligi.</param>
/// <param name="FirstName">Ad.</param>
/// <param name="LastName">Soyad.</param>
/// <param name="MobilePhone">Cep telefonu, <c>5XXXXXXXXX</c>.</param>
public sealed record EmergencyContactInput(Guid RelationshipId, string FirstName, string LastName, string MobilePhone)
{
    /// <summary>
    /// Girilen degerleri dogrular ve normallestirir. Metinlerde fazla bosluk ve satir sonu atilir;
    /// telefon SYG-KMLK-009 kuralina gore 10 haneye getirilir. Gecersizse <c>null</c> ve alan adina
    /// gore hatalar doner.
    /// </summary>
    public static (EmergencyContactInput? Input, IReadOnlyDictionary<string, string> Errors) Parse(
        Guid? relationshipId,
        string? firstName,
        string? lastName,
        string? mobilePhone)
    {
        var errors = new Dictionary<string, string>(StringComparer.Ordinal);
        if (relationshipId is not { } relationship || relationship == Guid.Empty)
        {
            errors[nameof(RelationshipId)] = "Yakınlığını seçin.";
        }

        var normalizedFirstName = Text(nameof(FirstName), firstName, "Adı yazın.", errors);
        var normalizedLastName = Text(nameof(LastName), lastName, "Soyadı yazın.", errors);

        string? phone = null;
        if (string.IsNullOrWhiteSpace(mobilePhone))
        {
            errors[nameof(MobilePhone)] = "Cep telefonunu yazın.";
        }
        else if ((phone = ContactNormalizer.NormalizeMobilePhone(mobilePhone)) is null)
        {
            errors[nameof(MobilePhone)] = "Cep telefonu 5 ile başlayan 10 haneli olmalıdır (örneğin 532 123 45 67).";
        }

        return errors.Count > 0
            ? (null, errors)
            : (new EmergencyContactInput(relationshipId!.Value, normalizedFirstName, normalizedLastName, phone!), errors);
    }

    /// <summary>Yakinlik taniminin ic kimligiyle kayda yazilacak bilgiler.</summary>
    public EmergencyContactDetails WithRelationship(long relationshipDefinitionId) =>
        new(relationshipDefinitionId, FirstName, LastName, MobilePhone);

    /// <summary>Degerleri yazmaz: kayit bir gunluk iletisine karisirsa kisisel veri dusmez.</summary>
    public override string ToString() => nameof(EmergencyContactInput);

    private static string Text(string field, string? value, string requiredMessage, Dictionary<string, string> errors)
    {
        var normalized = ContactNormalizer.NormalizeName(value);
        if (normalized.Length == 0)
        {
            errors[field] = requiredMessage;
        }
        else if (normalized.Length > EmergencyContact.MaxNameLength)
        {
            errors[field] = $"En fazla {EmergencyContact.MaxNameLength} karakter yazılabilir.";
        }

        return normalized;
    }
}
