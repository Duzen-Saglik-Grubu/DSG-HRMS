using System.Globalization;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Domain.Reference;

namespace Dsg.Hrms.Application.Reference;

/// <summary>Referans tanimlarinin veritabani tarafi.</summary>
public interface IReferenceDefinitionStore
{
    /// <summary>Turun tanimlari, siraya gore.</summary>
    Task<IReadOnlyList<ReferenceDefinition>> ListAsync(ReferenceType type, bool includeInactive, CancellationToken cancellationToken);

    /// <summary>Tanimi dis kimligiyle bulur (izlenen).</summary>
    Task<ReferenceDefinition?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Tur icinde kod kullaniliyor mu (pasifler dahil).</summary>
    Task<bool> CodeExistsAsync(ReferenceType type, string code, CancellationToken cancellationToken);

    /// <summary>Turdeki tanim sayisi (pasifler dahil).</summary>
    Task<int> CountAsync(ReferenceType type, CancellationToken cancellationToken);

    /// <summary>Yeni tanimi ekler.</summary>
    void Add(ReferenceDefinition definition);

    /// <summary>Degisiklikleri kaydeder; denetim izi ayni islemde yazilir.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Referans tanimlari (ADR-0022 §1; SYG-PERS-072): listeleme, ekleme, ad degistirme, pasife alma ve
/// yeniden aktif etme (<c>KR-136</c>).
/// </summary>
/// <remarks>
/// Tanim silinmez. Sistem tanimi (kan grubu, <c>KR-129</c>) degistirilemez, pasife alinamaz ve yeniden aktif edilemez;
/// istek is kurali hatasiyla (<c>422</c>) reddedilir. Degisiklikler denetim izine yazilir
/// (varlik <c>IAuditable</c>).
/// </remarks>
public sealed class ReferenceDefinitionService
{
    private readonly IReferenceDefinitionStore _store;

    /// <summary>Yeni ornek olusturur.</summary>
    public ReferenceDefinitionService(IReferenceDefinitionStore store)
    {
        _store = store;
    }

    /// <summary>Turun tanimlari; varsayilan olarak yalnizca aktifler (yeni secim icin).</summary>
    public Task<IReadOnlyList<ReferenceDefinition>> ListAsync(ReferenceType type, bool includeInactive, CancellationToken cancellationToken) =>
        _store.ListAsync(type, includeInactive, cancellationToken);

    /// <summary>
    /// Yeni tanim ekler. Unvan kodu LOGO kodudur ve zorunludur; gorev ve yakinlik kodu verilmezse
    /// sirayla uretilir (gorev <c>G001</c>…, yakinlik <c>Y001</c>…).
    /// </summary>
    public async Task<ReferenceDefinition> CreateAsync(ReferenceType type, string? code, string name, CancellationToken cancellationToken)
    {
        if (type == ReferenceType.BloodGroup)
        {
            throw new BusinessRuleException("Kan grubu tanımları sistem tanımıdır; eklenemez.");
        }

        var count = await _store.CountAsync(type, cancellationToken).ConfigureAwait(false);
        var effectiveCode = string.IsNullOrWhiteSpace(code)
            ? CodePrefix(type) is { } prefix
                ? await NextCodeAsync(type, prefix, count, cancellationToken).ConfigureAwait(false)
                : throw new BusinessRuleException("Ünvan kodu zorunludur (LOGO'daki kod).")
            : code.Trim();

        if (await _store.CodeExistsAsync(type, effectiveCode, cancellationToken).ConfigureAwait(false))
        {
            throw new BusinessRuleException($"Bu kod zaten kullanılıyor: {effectiveCode}.");
        }

        var definition = ReferenceDefinition.Create(type, effectiveCode, name, count + 1);
        _store.Add(definition);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return definition;
    }

    /// <summary>Adi degistirir; ekran ve kart kod degisikligi olmadan yeni adi gosterir.</summary>
    public async Task RenameAsync(Guid id, string name, CancellationToken cancellationToken)
    {
        var definition = await FindEditableAsync(id, cancellationToken).ConfigureAwait(false);
        definition.Rename(name);
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Pasife alir; gecmis kayitlarda ad gorunmeye devam eder.</summary>
    public async Task DeactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var definition = await FindEditableAsync(id, cancellationToken).ConfigureAwait(false);
        if (!definition.Deactivate())
        {
            throw new BusinessRuleException("Tanım zaten pasif.");
        }

        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Pasif tanimi yeniden aktif eder; yeni giriste yeniden secilebilir (<c>KR-136</c>).</summary>
    public async Task ReactivateAsync(Guid id, CancellationToken cancellationToken)
    {
        var definition = await FindEditableAsync(id, cancellationToken).ConfigureAwait(false);
        if (!definition.Reactivate())
        {
            throw new BusinessRuleException("Tanım zaten aktif.");
        }

        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<ReferenceDefinition> FindEditableAsync(Guid id, CancellationToken cancellationToken)
    {
        var definition = await _store.FindAsync(id, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Tanım bulunamadı.");

        return definition.IsSystem
            ? throw new BusinessRuleException("Sistem tanımı ekrandan değiştirilemez.")
            : definition;
    }

    private static char? CodePrefix(ReferenceType type) => type switch
    {
        ReferenceType.Duty => 'G',
        ReferenceType.Relationship => 'Y',
        _ => null,
    };

    private async Task<string> NextCodeAsync(ReferenceType type, char prefix, int count, CancellationToken cancellationToken)
    {
        // Silinen tanim olmadigi icin sayi + 1 cogunlukla bostur; elle verilmis bir kod
        // cakisirsa bir sonrakine gecilir.
        for (var n = count + 1; ; n++)
        {
            var candidate = string.Create(CultureInfo.InvariantCulture, $"{prefix}{n:000}");
            if (!await _store.CodeExistsAsync(type, candidate, cancellationToken).ConfigureAwait(false))
            {
                return candidate;
            }
        }
    }
}
