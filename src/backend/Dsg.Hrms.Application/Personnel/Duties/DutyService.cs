using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Personnel.Access;
using Dsg.Hrms.Application.Personnel.Cards;
using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Application.Personnel.Duties;

/// <summary>Gorev girisinin veritabani tarafi.</summary>
public interface IDutyStore
{
    /// <summary>Kisinin sicil numarasiyla istihdami (izlenen); yoksa <c>null</c>.</summary>
    Task<Employment?> FindEmploymentAsync(long personId, string registryCode, CancellationToken cancellationToken);

    /// <summary>Istihdamin silinmemis gorev kayitlari (izlenen).</summary>
    Task<IReadOnlyList<EmploymentDuty>> ListAsync(long employmentId, CancellationToken cancellationToken);

    /// <summary>Aktif gorev taniminin ic kimligi ve adi; tanim yoksa, pasifse veya gorev turunde degilse <c>null</c>.</summary>
    Task<(long Id, string Name)?> FindActiveDutyDefinitionAsync(Guid definitionId, CancellationToken cancellationToken);

    /// <summary>Yeni kaydi ekler.</summary>
    void Add(EmploymentDuty duty);

    /// <summary>Kaydi (yumusak) siler.</summary>
    void Remove(EmploymentDuty duty);

    /// <summary>Kaydeder. Veritabaninin cakisma kisiti <see cref="ConflictException"/> olarak doner (SYG-PERS-019).</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Girilen gorev.</summary>
/// <param name="Id">Gorev kaydinin dis kimligi.</param>
/// <param name="Name">Gorev.</param>
/// <param name="ValidFrom">Baslangic.</param>
/// <param name="ValidTo">Bitis (haric); acikse <c>null</c>.</param>
public sealed record DutyEntry(Guid Id, string Name, DateOnly ValidFrom, DateOnly? ValidTo);

/// <summary>
/// Kisiye gorev girisi ve silme (PG-PERS-43; SYG-PERS-042). IK ve Sistem Yoneticisi
/// (<c>personnel.person.update</c>) yapar; degisiklik denetim izine yazilir.
/// </summary>
/// <remarks>
/// Kisi once kapsamli sorgudan okunur: kapsam disi veya olmayan kisi icin kart ile ayni
/// <c>404</c> doner (SYG-PERS-051).
/// </remarks>
public sealed class DutyService
{
    private readonly ICurrentUserScope _scope;
    private readonly IPersonnelQuery _query;
    private readonly IDutyStore _store;
    private readonly IDateTimeProvider _clock;

    /// <summary>Yeni ornek olusturur.</summary>
    public DutyService(ICurrentUserScope scope, IPersonnelQuery query, IDutyStore store, IDateTimeProvider clock)
    {
        _scope = scope;
        _query = query;
        _store = store;
        _clock = clock;
    }

    /// <summary>
    /// Istihdama gorev atar. Baslangic verilmezse bugundur. Onceki acik gorev yeni gorevin
    /// baslangiciyla kapanir.
    /// </summary>
    public async Task<DutyEntry> AssignAsync(Guid personId, string registryCode, Guid definitionId, DateOnly? validFrom, CancellationToken cancellationToken)
    {
        var employment = await FindEmploymentAsync(personId, registryCode, cancellationToken).ConfigureAwait(false);
        var definition = await _store.FindActiveDutyDefinitionAsync(definitionId, cancellationToken).ConfigureAwait(false)
            ?? throw new BusinessRuleException("Görev tanımı bulunamadı veya pasif.");
        var existing = await _store.ListAsync(employment.Id, cancellationToken).ConfigureAwait(false);

        EmploymentDuty? added;
        try
        {
            (added, _) = DutyTimeline.Assign(employment, existing, definition.Id, validFrom ?? _clock.Today);
        }
        catch (DutyRuleException ex)
        {
            throw ex.IsConflict ? new ConflictException(ex.Message) : new BusinessRuleException(ex.Message);
        }

        if (added is not null)
        {
            _store.Add(added);
        }

        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        var current = added ?? existing.Single(d => d.IsOpen);
        return new DutyEntry(current.PublicId, definition.Name, current.ValidFrom, current.ValidTo);
    }

    /// <summary>Gorev kaydini (yumusak) siler; yanlis girisi duzeltmek icindir.</summary>
    public async Task DeleteAsync(Guid personId, string registryCode, Guid dutyId, CancellationToken cancellationToken)
    {
        var employment = await FindEmploymentAsync(personId, registryCode, cancellationToken).ConfigureAwait(false);
        var duty = (await _store.ListAsync(employment.Id, cancellationToken).ConfigureAwait(false)).SingleOrDefault(d => d.PublicId == dutyId)
            ?? throw new NotFoundException("Görev kaydı bulunamadı.");

        _store.Remove(duty);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<Employment> FindEmploymentAsync(Guid personId, string registryCode, CancellationToken cancellationToken)
    {
        var viewer = await _scope.GetAsync(cancellationToken).ConfigureAwait(false);
        var person = await _query.FindAsync(personId, viewer, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(PersonCardService.NotFoundMessage);

        return await _store.FindEmploymentAsync(person.Id, registryCode, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("İstihdam bulunamadı.");
    }
}
