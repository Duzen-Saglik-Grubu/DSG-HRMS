using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Common.Security;
using Dsg.Hrms.Application.Identity.Authorization;
using Dsg.Hrms.Application.Personnel.Access;
using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Application.Personnel.Cards;

/// <summary>Kisi karti (SYG-PERS-034, 037, 038, 044).</summary>
/// <param name="PersonId">Dis kimlik.</param>
/// <param name="FirstName">Ad; LOGO'daki gibi buyuk harfle, donusum yapilmaz.</param>
/// <param name="LastName">Soyad.</param>
/// <param name="NationalId">TCKN; tam degeri gorme izni yoksa maskeli (SYG-PERS-053).</param>
/// <param name="IsNationalIdMasked">TCKN maskeli mi.</param>
/// <param name="BirthDate">Dogum tarihi.</param>
/// <param name="Age">Yas (SYG-PERS-035).</param>
/// <param name="Email">Kurumsal e-posta; kayitli degilse bos.</param>
/// <param name="MobilePhone">Cep telefonu; kayitli degilse bos.</param>
/// <param name="IsSelf">Kart kullanicinin kendisine mi ait.</param>
/// <param name="Employments">Istihdamlar; esas sicil onde (SYG-PERS-017, <c>Employment.InPrimaryOrder</c>).</param>
/// <param name="Gender">Cinsiyet.</param>
/// <param name="PreviousSurname">Onceki soyadi; yalnizca kadin personelde ve LOGO'da doluysa.</param>
/// <param name="MaritalStatus">Medeni hal.</param>
/// <param name="EducationName">Egitim.</param>
/// <param name="Sensitive">
/// Korumali alanlar; yalnizca gorme hakki olana (<c>view_sensitive</c> veya kisinin kendisi)
/// doldurulur, digerlerinde <c>null</c> ve yanitta hic yer almaz (SYG-PERS-052).
/// </param>
public sealed record PersonCard(
    Guid PersonId,
    string FirstName,
    string LastName,
    string NationalId,
    bool IsNationalIdMasked,
    DateOnly BirthDate,
    int Age,
    string? Email,
    string? MobilePhone,
    bool IsSelf,
    IReadOnlyList<EmploymentCard> Employments,
    Gender? Gender,
    string? PreviousSurname,
    MaritalStatus? MaritalStatus,
    string? EducationName,
    SensitiveCard? Sensitive)
{
    /// <summary>Kisinin fotografi var mi; yoksa bas harfler gosterilir (SYG-PERS-040).</summary>
    public bool HasPhoto { get; init; }

    /// <summary>
    /// Evlilik bolumu; yalnizca <c>view_sensitive</c> izni olana, kisinin kendisi dahil degil
    /// (ADR-0019 §2). Goremeyende <c>null</c> ve yanitta bolum yoktur.
    /// </summary>
    public MarriageCard? Marriage { get; init; }
}

/// <summary>Kartin evlilik bolumu (SYG-PERS-036).</summary>
/// <param name="Record">Girilmis bilgi; kayitli degilse <c>null</c> ("Kayitli degil").</param>
/// <param name="Editable">Giris izni var ve medeni hal Evli; Bekar'a donmusse bilgi korunur ama duzenlenemez.</param>
public sealed record MarriageCard(MarriageRecord? Record, bool Editable);

/// <summary>Kartin korumali alanlari (ADR-0019 §2). Alan bos ise "Kayitli degil".</summary>
/// <param name="Address">Adres; kayitli degilse <c>null</c>.</param>
/// <param name="BloodGroup">Kan grubu (ornek "A Rh+"); kayitli degilse <c>null</c> (SYG-PERS-034).</param>
/// <param name="EmergencyContacts">Acil durum kisileri (SYG-PERS-043); kayitli degilse bos.</param>
public sealed record SensitiveCard(string? Address, string? BloodGroup, IReadOnlyList<EmergencyContactRecord> EmergencyContacts);

/// <summary>Kartta bir istihdam.</summary>
/// <param name="RegistryCode">Sicil numarasi.</param>
/// <param name="CompanyName">Firma.</param>
/// <param name="HireDate">Ise giris tarihi.</param>
/// <param name="TerminationDate">Isten cikis tarihi.</param>
/// <param name="IsActive">Istihdam suruyor mu.</param>
/// <param name="Seniority">Kidem suresi (SYG-PERS-039).</param>
/// <param name="Current">Guncel tarih aralikli nitelikler (SYG-PERS-038); yoksa <c>null</c>.</param>
/// <param name="Duty">Guncel gorev (SYG-PERS-042); girilmediyse <c>null</c>.</param>
public sealed record EmploymentCard(
    string RegistryCode,
    string CompanyName,
    DateOnly HireDate,
    DateOnly? TerminationDate,
    bool IsActive,
    Seniority Seniority,
    AssignmentRecord? Current,
    DutyRecord? Duty);

/// <summary>
/// Kisi kartini ve "Profilim"i sunar (ADR-0019; SYG-PERS-044, 051, 052, 053, 057).
/// </summary>
/// <remarks>
/// <para>
/// Okuma <see cref="IPersonnelQuery"/>'den gecer. Kapsam disi kisi ile var olmayan kisi AYNI
/// istisnayi ve iletiyi uretir; uc ikisine de ayni <c>404</c> govdesini doner (PG-PERS-27).
/// </para>
/// <para>
/// Her kart acilisi erisim kaydina yazilir, kisinin kendi karti dahil (AN-10). Kayit
/// yazilamazsa kart sunulmaz (fail-closed, ADR-0009 §3).
/// </para>
/// </remarks>
public sealed class PersonCardService
{
    /// <summary>Erisim kaydindaki veri turu.</summary>
    public const string EntityName = "Person";

    /// <summary>Kapsam disi ve bulunmayan kisi icin tek ileti.</summary>
    public const string NotFoundMessage = "Kişi bulunamadı.";

    private readonly ICurrentUserScope _scope;
    private readonly IPersonnelQuery _query;
    private readonly IAccessLogger _accessLogger;
    private readonly IDateTimeProvider _clock;

    /// <summary>Yeni ornek olusturur.</summary>
    public PersonCardService(ICurrentUserScope scope, IPersonnelQuery query, IAccessLogger accessLogger, IDateTimeProvider clock)
    {
        _scope = scope;
        _query = query;
        _accessLogger = accessLogger;
        _clock = clock;
    }

    /// <summary>Kisinin karti; kapsam disindaysa veya yoksa <see cref="NotFoundException"/>.</summary>
    public async Task<PersonCard> GetAsync(Guid personId, CancellationToken cancellationToken)
    {
        var viewer = await _scope.GetAsync(cancellationToken).ConfigureAwait(false);
        var record = await _query.FindAsync(personId, viewer, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(NotFoundMessage);

        return await PresentAsync(record, viewer, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Kullanicinin kendi karti ("Profilim"); izin gerektirmez (PG-PERS-25).</summary>
    public async Task<PersonCard> GetOwnAsync(CancellationToken cancellationToken)
    {
        var viewer = await _scope.GetAsync(cancellationToken).ConfigureAwait(false);
        var record = await _query.FindOwnAsync(viewer, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(NotFoundMessage);

        return await PresentAsync(record, viewer, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Kisinin fotografi (SYG-PERS-041). Kapsam disi, bulunmayan ve fotografi olmayan kisi ayni
    /// <see cref="NotFoundException"/>'i uretir. Ayri erisim kaydi yazilmaz: fotograf, kaydi
    /// yazilan kart veya liste goruntulemesinin parcasidir.
    /// </summary>
    public async Task<PhotoRecord> GetPhotoAsync(Guid personId, CancellationToken cancellationToken)
    {
        var viewer = await _scope.GetAsync(cancellationToken).ConfigureAwait(false);
        return await _query.FindPhotoAsync(personId, viewer, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(NotFoundMessage);
    }

    /// <summary>Kullanicinin kendi fotografi; yoksa <see cref="NotFoundException"/>.</summary>
    public async Task<PhotoRecord> GetOwnPhotoAsync(CancellationToken cancellationToken)
    {
        var viewer = await _scope.GetAsync(cancellationToken).ConfigureAwait(false);
        return await _query.FindOwnPhotoAsync(viewer, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(NotFoundMessage);
    }

    private async Task<PersonCard> PresentAsync(PersonRecord record, PersonnelViewer viewer, CancellationToken cancellationToken)
    {
        await _accessLogger.LogAsync(AccessRecord.View(EntityName, record.PublicId), cancellationToken).ConfigureAwait(false);

        var sensitive = PersonFieldVisibility.CanSeeSensitive(viewer, record.Id)
            ? new SensitiveCard(record.Address, record.BloodGroup, record.EmergencyContacts)
            : null;

        // Yanitta kan grubu yer aldiysa ayrica ozel nitelikli veri goruntulemesi yazilir
        // (SYG-PERS-057, ADR-0020 §5); kayitli degilse gosterilen bir deger yoktur.
        if (sensitive?.BloodGroup is not null)
        {
            await _accessLogger.LogAsync(AccessRecord.SpecialCategoryView(EntityName, record.PublicId), cancellationToken).ConfigureAwait(false);
        }

        var today = _clock.Today;
        var fullNationalId = PersonFieldVisibility.CanSeeFullNationalId(viewer);

        return new PersonCard(
            record.PublicId,
            record.FirstName,
            record.LastName,
            fullNationalId ? record.NationalId : Mask.NationalIdForDisplay(record.NationalId)!,
            !fullNationalId,
            record.BirthDate,
            Age.On(record.BirthDate, today),
            record.Email,
            record.MobilePhone,
            viewer.IsSelf(record.Id),
            [.. Employment.InPrimaryOrder(record.Employments, e => (e.IsActive, e.HireDate, e.TerminationDate, e.RegistryCode))
                .Select(e => new EmploymentCard(
                    e.RegistryCode,
                    e.CompanyName,
                    e.HireDate,
                    e.TerminationDate,
                    e.IsActive,
                    Seniority.Calculate(e.HireDate, e.TerminationDate, today),
                    e.Current,
                    e.CurrentDuty))],
            record.Profile.Gender,
            record.Profile.PreviousSurname,
            record.Profile.MaritalStatus,
            record.Profile.EducationName,
            sensitive)
        {
            HasPhoto = record.HasPhoto,
            Marriage = PersonFieldVisibility.CanSeeMarriage(viewer)
                ? new MarriageCard(record.Marriage, viewer.Has(Permissions.PersonUpdate) && record.Profile.MaritalStatus == MaritalStatus.Married)
                : null,
        };
    }
}
