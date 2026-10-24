using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Common.Security;
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
/// <param name="Employments">Istihdamlar; once aktifler, ise giris tarihi yeniden eskiye (SYG-PERS-017).</param>
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
    SensitiveCard? Sensitive);

/// <summary>Kartin korumali alanlari (ADR-0019 §2). Alan bos ise "Kayitli degil".</summary>
/// <param name="Address">Adres; kayitli degilse <c>null</c>.</param>
public sealed record SensitiveCard(string? Address);

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

    private async Task<PersonCard> PresentAsync(PersonRecord record, PersonnelViewer viewer, CancellationToken cancellationToken)
    {
        await _accessLogger.LogAsync(AccessRecord.View(EntityName, record.PublicId), cancellationToken).ConfigureAwait(false);

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
            [.. record.Employments
                .OrderByDescending(e => e.IsActive)
                .ThenByDescending(e => e.HireDate)
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
            PersonFieldVisibility.CanSeeSensitive(viewer, record.Id) ? new SensitiveCard(record.Address) : null);
    }
}
