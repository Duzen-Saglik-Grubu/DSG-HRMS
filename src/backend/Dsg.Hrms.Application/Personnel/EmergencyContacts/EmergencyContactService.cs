using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Personnel.Access;
using Dsg.Hrms.Application.Personnel.Cards;
using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Application.Personnel.EmergencyContacts;

/// <summary>Acil durum kisisi girisinin veritabani tarafi.</summary>
public interface IEmergencyContactStore
{
    /// <summary>Kisinin silinmemis acil durum kisisi (izlenen); yoksa <c>null</c>.</summary>
    Task<EmergencyContact?> FindAsync(long personId, Guid contactId, CancellationToken cancellationToken);

    /// <summary>Yeni kaydi ekler.</summary>
    void Add(EmergencyContact contact);

    /// <summary>Kaydi (yumusak) siler.</summary>
    void Remove(EmergencyContact contact);

    /// <summary>Kaydeder.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Kisiye acil durum kisisi girisi, degisikligi ve silme (PG-PERS-44; SYG-PERS-043). IK ve Sistem
/// Yoneticisi (<c>personnel.person.update</c>) yapar; degisiklik denetim izine yazilir (SYG-PERS-066).
/// </summary>
/// <remarks>
/// Kisi once kapsamli sorgudan okunur: kapsam disi veya olmayan kisi icin kart ile ayni <c>404</c>
/// doner (SYG-PERS-051). Kisinin kendisi kartinda gorur ama degistiremez; bu, izin olmadan ucun
/// <c>403</c> donmesiyle saglanir.
/// </remarks>
public sealed class EmergencyContactService
{
    /// <summary>Kayit bulunamadiginda ileti.</summary>
    public const string ContactNotFoundMessage = "Acil durum kişisi bulunamadı.";

    private readonly ICurrentUserScope _scope;
    private readonly IPersonnelQuery _query;
    private readonly IEmergencyContactStore _store;

    /// <summary>Yeni ornek olusturur.</summary>
    public EmergencyContactService(ICurrentUserScope scope, IPersonnelQuery query, IEmergencyContactStore store)
    {
        _scope = scope;
        _query = query;
        _store = store;
    }

    /// <summary>Kisiye acil durum kisisi ekler.</summary>
    public async Task<EmergencyContactRecord> AddAsync(Guid personId, EmergencyContactDetails details, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(details);

        var person = await FindPersonAsync(personId, cancellationToken).ConfigureAwait(false);
        var contact = EmergencyContact.For(person, details);

        _store.Add(contact);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ToRecord(contact);
    }

    /// <summary>Acil durum kisisinin bilgilerini degistirir.</summary>
    public async Task<EmergencyContactRecord> ChangeAsync(Guid personId, Guid contactId, EmergencyContactDetails details, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(details);

        var contact = await FindContactAsync(personId, contactId, cancellationToken).ConfigureAwait(false);
        if (contact.Change(details))
        {
            await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return ToRecord(contact);
    }

    /// <summary>Acil durum kisisini (yumusak) siler.</summary>
    public async Task DeleteAsync(Guid personId, Guid contactId, CancellationToken cancellationToken)
    {
        var contact = await FindContactAsync(personId, contactId, cancellationToken).ConfigureAwait(false);

        _store.Remove(contact);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static EmergencyContactRecord ToRecord(EmergencyContact contact) =>
        new(contact.PublicId, contact.Relationship, contact.FirstName, contact.LastName, contact.MobilePhone);

    private async Task<EmergencyContact> FindContactAsync(Guid personId, Guid contactId, CancellationToken cancellationToken)
    {
        var person = await FindPersonAsync(personId, cancellationToken).ConfigureAwait(false);
        return await _store.FindAsync(person, contactId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(ContactNotFoundMessage);
    }

    private async Task<long> FindPersonAsync(Guid personId, CancellationToken cancellationToken)
    {
        var viewer = await _scope.GetAsync(cancellationToken).ConfigureAwait(false);
        var person = await _query.FindAsync(personId, viewer, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(PersonCardService.NotFoundMessage);

        return person.Id;
    }
}
