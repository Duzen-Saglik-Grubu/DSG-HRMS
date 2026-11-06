using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Personnel.Access;
using Dsg.Hrms.Application.Personnel.Cards;
using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Application.Personnel.Marriage;

/// <summary>Evlilik bilgisi girisinin veritabani tarafi.</summary>
public interface IMarriageStore
{
    /// <summary>Kisinin silinmemis evlilik kaydi (izlenen); yoksa <c>null</c>.</summary>
    Task<PersonMarriage?> FindAsync(long personId, CancellationToken cancellationToken);

    /// <summary>Yeni kaydi ekler.</summary>
    void Add(PersonMarriage marriage);

    /// <summary>Kaydi (yumusak) siler.</summary>
    void Remove(PersonMarriage marriage);

    /// <summary>Kaydeder.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Evlilik tarihi ve evlilik yardimi girisi, degisikligi ve silme (PG-PERS-19; SYG-PERS-036). IK ve
/// Sistem Yoneticisi (<c>personnel.person.update</c>) yapar; degisiklik denetim izine yazilir
/// (SYG-PERS-066).
/// </summary>
/// <remarks>
/// <para>
/// Yalnizca medeni hali Evli olan kiside yapilir. Medeni hal Bekar'a donerse girilmis bilgi korunur
/// ve gorunmeye devam eder ama duzenlenemez ve silinemez (<c>KR-125</c>).
/// </para>
/// <para>
/// Kisi once kapsamli sorgudan okunur: kapsam disi veya olmayan kisi icin kart ile ayni <c>404</c>
/// doner (SYG-PERS-051). Okuma kartin parcasidir (<c>PersonnelQuery</c>).
/// </para>
/// </remarks>
public sealed class MarriageService
{
    /// <summary>Medeni hal Evli degilken iletilen hata.</summary>
    public const string NotMarriedMessage = "Evlilik bilgisi yalnızca medeni hâli Evli olan kişide girilir ve değiştirilir.";

    private readonly ICurrentUserScope _scope;
    private readonly IPersonnelQuery _query;
    private readonly IMarriageStore _store;
    private readonly IDateTimeProvider _clock;

    /// <summary>Yeni ornek olusturur.</summary>
    public MarriageService(ICurrentUserScope scope, IPersonnelQuery query, IMarriageStore store, IDateTimeProvider clock)
    {
        _scope = scope;
        _query = query;
        _store = store;
        _clock = clock;
    }

    /// <summary>
    /// Evlilik bilgisini girer veya degistirir. Tarih bugunden sonra ve dogum tarihinden once
    /// olamaz (AN-24).
    /// </summary>
    public async Task<MarriageRecord> SetAsync(Guid personId, MarriageDetails details, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(details);

        var person = await MarriedPersonAsync(personId, cancellationToken).ConfigureAwait(false);
        if (details.MarriageDate > _clock.Today)
        {
            throw new BusinessRuleException("Evlilik tarihi bugünden sonra olamaz.");
        }

        if (details.MarriageDate < person.BirthDate)
        {
            throw new BusinessRuleException("Evlilik tarihi doğum tarihinden önce olamaz.");
        }

        var marriage = await _store.FindAsync(person.Id, cancellationToken).ConfigureAwait(false);
        if (marriage is null)
        {
            marriage = PersonMarriage.For(person.Id, details);
            _store.Add(marriage);
            await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        else if (marriage.Change(details))
        {
            await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return new MarriageRecord(marriage.MarriageDate, marriage.AllowanceReceived);
    }

    /// <summary>Evlilik bilgisini (yumusak) siler.</summary>
    public async Task DeleteAsync(Guid personId, CancellationToken cancellationToken)
    {
        var person = await MarriedPersonAsync(personId, cancellationToken).ConfigureAwait(false);
        var marriage = await _store.FindAsync(person.Id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Evlilik bilgisi bulunamadı.");

        _store.Remove(marriage);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<PersonRecord> MarriedPersonAsync(Guid personId, CancellationToken cancellationToken)
    {
        var viewer = await _scope.GetAsync(cancellationToken).ConfigureAwait(false);
        var person = await _query.FindAsync(personId, viewer, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(PersonCardService.NotFoundMessage);

        return person.Profile.MaritalStatus == MaritalStatus.Married
            ? person
            : throw new BusinessRuleException(NotMarriedMessage);
    }
}
