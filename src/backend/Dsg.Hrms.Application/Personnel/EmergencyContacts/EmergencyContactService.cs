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

    /// <summary>Yakinlik tanimi (tur yakinlik), dis kimligiyle; yoksa <c>null</c>.</summary>
    Task<RelationshipDefinition?> FindRelationshipAsync(Guid definitionId, CancellationToken cancellationToken);

    /// <summary>Aktif yakinlik tanimlari, siraya gore (yeni secim icin).</summary>
    Task<IReadOnlyList<RelationshipDefinition>> ListActiveRelationshipsAsync(CancellationToken cancellationToken);

    /// <summary>Yeni kaydi ekler.</summary>
    void Add(EmergencyContact contact);

    /// <summary>Kaydi (yumusak) siler.</summary>
    void Remove(EmergencyContact contact);

    /// <summary>Kaydeder.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Yakinlik tanimi (referans veri, tur yakinlik; <c>KR-135</c>).</summary>
/// <param name="Id">Ic kimlik.</param>
/// <param name="PublicId">Dis kimlik.</param>
/// <param name="Name">Ad.</param>
/// <param name="IsActive">Yeni secimde kullanilabilir mi.</param>
public sealed record RelationshipDefinition(long Id, Guid PublicId, string Name, bool IsActive);

/// <summary>
/// Kisiye acil durum kisisi girisi, degisikligi ve silme (PG-PERS-44; SYG-PERS-043). IK ve Sistem
/// Yoneticisi (<c>personnel.person.update</c>) yapar; degisiklik denetim izine yazilir (SYG-PERS-066).
/// </summary>
/// <remarks>
/// <para>
/// Kisi once kapsamli sorgudan okunur: kapsam disi veya olmayan kisi icin kart ile ayni <c>404</c>
/// doner (SYG-PERS-051). Giris izni olmayan kisi kendi kartinda gorur ama degistiremez; bu, izin
/// olmadan ucun <c>403</c> donmesiyle saglanir. Giris izni olan kullanici kendi kaydini da girer
/// (<c>KR-137</c>).
/// </para>
/// <para>
/// Yakinlik referans verideki aktif bir tanimdan secilir (<c>KR-135</c>). Kaydin mevcut tanimi
/// sonradan pasife alindiysa kayit o tanimla duzenlenebilir; yeni secimde pasif tanim reddedilir.
/// </para>
/// </remarks>
public sealed class EmergencyContactService
{
    /// <summary>Kayit bulunamadiginda ileti.</summary>
    public const string ContactNotFoundMessage = "Acil durum kişisi bulunamadı.";

    /// <summary>Gecersiz veya pasif yakinlikta ileti.</summary>
    public const string RelationshipUnavailableMessage = "Yakınlık tanımı bulunamadı veya pasif.";

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

    /// <summary>Yeni secim icin aktif yakinlik tanimlari.</summary>
    public Task<IReadOnlyList<RelationshipDefinition>> ListRelationshipsAsync(CancellationToken cancellationToken) =>
        _store.ListActiveRelationshipsAsync(cancellationToken);

    /// <summary>Kisiye acil durum kisisi ekler.</summary>
    public async Task<EmergencyContactRecord> AddAsync(Guid personId, EmergencyContactInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var person = await FindPersonAsync(personId, cancellationToken).ConfigureAwait(false);
        var relationship = await ActiveRelationshipAsync(input.RelationshipId, currentId: null, cancellationToken).ConfigureAwait(false);
        var contact = EmergencyContact.For(person, input.WithRelationship(relationship.Id));

        _store.Add(contact);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ToRecord(contact, relationship);
    }

    /// <summary>Acil durum kisisinin bilgilerini degistirir.</summary>
    public async Task<EmergencyContactRecord> ChangeAsync(Guid personId, Guid contactId, EmergencyContactInput input, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);

        var contact = await FindContactAsync(personId, contactId, cancellationToken).ConfigureAwait(false);
        var relationship = await ActiveRelationshipAsync(input.RelationshipId, contact.RelationshipDefinitionId, cancellationToken).ConfigureAwait(false);
        if (contact.Change(input.WithRelationship(relationship.Id)))
        {
            await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return ToRecord(contact, relationship);
    }

    /// <summary>Acil durum kisisini (yumusak) siler.</summary>
    public async Task DeleteAsync(Guid personId, Guid contactId, CancellationToken cancellationToken)
    {
        var contact = await FindContactAsync(personId, contactId, cancellationToken).ConfigureAwait(false);

        _store.Remove(contact);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static EmergencyContactRecord ToRecord(EmergencyContact contact, RelationshipDefinition relationship) =>
        new(contact.PublicId, relationship.PublicId, relationship.Name, contact.FirstName, contact.LastName, contact.MobilePhone);

    /// <summary>
    /// Secilen tanim aktif olmali; kaydin zaten tasidigi tanim pasif olsa da kabul edilir.
    /// </summary>
    private async Task<RelationshipDefinition> ActiveRelationshipAsync(Guid definitionId, long? currentId, CancellationToken cancellationToken)
    {
        var definition = await _store.FindRelationshipAsync(definitionId, cancellationToken).ConfigureAwait(false);
        return definition is not null && (definition.IsActive || definition.Id == currentId)
            ? definition
            : throw new BusinessRuleException(RelationshipUnavailableMessage);
    }

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
