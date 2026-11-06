using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Personnel.Access;
using Dsg.Hrms.Application.Personnel.Cards;

namespace Dsg.Hrms.Application.Personnel.History;

/// <summary>Bir istihdamin tarihce donemleri, yeniden eskiye.</summary>
/// <param name="Assignments">Atama donemleri: firma, sube, birim, unvan (SYG-PERS-018).</param>
/// <param name="Duties">Gorev donemleri; silinmis kayitlar haric (SYG-PERS-042).</param>
public sealed record EmploymentHistory(IReadOnlyList<AssignmentRecord> Assignments, IReadOnlyList<DutyRecord> Duties);

/// <summary>
/// Istihdam tarihcesi ve tarihli sorgu (PG-PERS-12; SYG-PERS-020). Bir tarih verilirse yalnizca
/// o tarihte gecerli donemler doner; T1 oncesi bir tarih icin listeler bostur, deger tahmin
/// edilmez (ADR-0021 §2).
/// </summary>
/// <remarks>
/// <para>
/// Yetki ve kapsam kartla aynidir: kapsam disi veya olmayan kisi icin kart ile ayni <c>404</c>
/// doner (SYG-PERS-051). Kisi kendi tarihcesini izinsiz gorur (SYG-PERS-044); kendi kaydi kartta
/// oldugu gibi gorunur kalma suresinden bagimsiz okunur.
/// </para>
/// <para>
/// Tarihce kisisel veridir: her goruntuleme kisiye ait bir <c>View</c> erisim kaydi uretir
/// (SYG-PERS-057 ile ayni ilke; AN-23).
/// </para>
/// </remarks>
public sealed class EmploymentHistoryService
{
    private readonly ICurrentUserScope _scope;
    private readonly IPersonnelQuery _query;
    private readonly IAccessLogger _accessLogger;

    /// <summary>Yeni ornek olusturur.</summary>
    public EmploymentHistoryService(ICurrentUserScope scope, IPersonnelQuery query, IAccessLogger accessLogger)
    {
        _scope = scope;
        _query = query;
        _accessLogger = accessLogger;
    }

    /// <summary>Istihdamin tarihcesi; <paramref name="date"/> verilirse o tarihte gecerli donemler.</summary>
    public async Task<EmploymentHistory> GetAsync(Guid personId, string registryCode, DateOnly? date, CancellationToken cancellationToken)
    {
        var viewer = await _scope.GetAsync(cancellationToken).ConfigureAwait(false);
        var person = await _query.FindAsync(personId, viewer, cancellationToken).ConfigureAwait(false)
            ?? await OwnAsync(personId, viewer, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(PersonCardService.NotFoundMessage);

        var employment = person.Employments.SingleOrDefault(e => e.RegistryCode == registryCode)
            ?? throw new NotFoundException("İstihdam bulunamadı.");

        await _accessLogger.LogAsync(AccessRecord.View(PersonCardService.EntityName, person.PublicId), cancellationToken).ConfigureAwait(false);

        return await _query.HistoryAsync(employment.Id, date, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Istenen kisi kullanicinin kendisiyse kendi kaydi; degilse <c>null</c>.</summary>
    private async Task<PersonRecord?> OwnAsync(Guid personId, PersonnelViewer viewer, CancellationToken cancellationToken)
    {
        var own = await _query.FindOwnAsync(viewer, cancellationToken).ConfigureAwait(false);
        return own?.PublicId == personId ? own : null;
    }
}
