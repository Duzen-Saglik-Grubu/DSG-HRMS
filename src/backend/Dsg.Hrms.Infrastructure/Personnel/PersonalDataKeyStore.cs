using System.Globalization;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Personnel.Protection;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Personnel;
using Dsg.Hrms.Infrastructure.Data;
using Dsg.Hrms.Infrastructure.Personnel.Protection;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Personnel;

/// <summary>
/// <see cref="IPersonalDataKeyStore"/> uygulamasi (SYG-PERS-062; ADR-0020 §3).
/// </summary>
/// <remarks>
/// <para>
/// Degerler degisiklik izleyicisi DISINDA, dogrudan <c>UPDATE</c> ile yazilir: deger ayni kaldigi
/// icin denetim izine "degisti" kaydi dusmemelidir. Her guncelleme eski sifreli metni kosul olarak
/// tasir; is calisirken senkronizasyon ayni degeri yazdiysa o satir atlanir, ezilmez.
/// </para>
/// <para>
/// Silinmis kayitlar da kapsanir (genel suzgec kapali): sifreli metin silinmis satirda da durur
/// ve eski anahtar cikarilmadan once onun da yeniden yazilmasi gerekir.
/// </para>
/// </remarks>
public sealed class PersonalDataKeyStore(HrmsDbContext context, PersonalDataProtector protector, ISecurityEventLog events) : IPersonalDataKeyStore
{
    /// <inheritdoc />
    public async Task<PersonalDataKeyStatus> StatusAsync(CancellationToken cancellationToken)
    {
        var values = await ValuesAsync(cancellationToken).ConfigureAwait(false);
        return Status(values.Select(v => v.Cipher));
    }

    /// <inheritdoc />
    public async Task<ReEncryptionResult> ReEncryptAsync(CancellationToken cancellationToken)
    {
        var target = protector.CurrentKeyId;
        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            var values = await ValuesAsync(cancellationToken).ConfigureAwait(false);
            var sources = new SortedSet<string>(StringComparer.Ordinal);
            var reEncrypted = 0;
            var undecryptable = 0;

            foreach (var value in values.Where(v => PersonalDataProtector.KeyIdOf(v.Cipher) != target))
            {
                if (protector.TryUnprotect(value.Cipher, value.Field) is not { } plain)
                {
                    undecryptable++;
                    continue;
                }

                var replacement = protector.Protect(plain, value.Field);
                var updated = value.Kind == ValueKind.BloodGroup
                    ? await context.Set<PersonSensitive>().IgnoreQueryFilters()
                        .Where(s => s.Id == value.RowId && s.BloodGroup == value.Cipher)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.BloodGroup, replacement), cancellationToken)
                        .ConfigureAwait(false)
                    : await context.Set<CorrectionRequest>().IgnoreQueryFilters()
                        .Where(r => r.Id == value.RowId && r.ProposedValue == value.Cipher)
                        .ExecuteUpdateAsync(r => r.SetProperty(x => x.ProposedValue, replacement), cancellationToken)
                        .ConfigureAwait(false);

                if (updated == 1)
                {
                    reEncrypted++;
                    sources.Add(PersonalDataProtector.KeyIdOf(value.Cipher)!);
                }
            }

            var remaining = (await ValuesAsync(cancellationToken).ConfigureAwait(false))
                .Count(v => PersonalDataProtector.KeyIdOf(v.Cipher) != target);
            var result = new ReEncryptionResult(target, [.. sources], reEncrypted, undecryptable, remaining);

            // Isin kendisi tek guvenlik olayidir; baslatan istek baglamindan gelir (SYG-PERS-062).
            events.Record(SecurityEventType.PersonalDataReEncrypted, userAccountId: null, personId: null, Detail(result));
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return result;
        }).ConfigureAwait(false);
    }

    /// <summary>Olay ayrintisi: anahtar kimlikleri ve sayilar; deger ve kisi yok (en fazla 100 karakter).</summary>
    private static string Detail(ReEncryptionResult result)
    {
        var sources = result.SourceKeyIds.Count == 0 ? "-" : string.Join(',', result.SourceKeyIds);
        var detail = string.Create(
            CultureInfo.InvariantCulture,
            $"{sources}>{result.TargetKeyId} n={result.ReEncrypted} kalan={result.RemainingOnOldKeys} cozulemeyen={result.Undecryptable}");
        return detail.Length <= 100 ? detail : detail[..100];
    }

    private PersonalDataKeyStatus Status(IEnumerable<string> ciphers)
    {
        var counts = ciphers
            .GroupBy(c => PersonalDataProtector.KeyIdOf(c) ?? "?", StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);
        var keys = protector.KeyIds.Union(counts.Keys, StringComparer.Ordinal)
            .OrderBy(KeyNumber)
            .ThenBy(k => k, StringComparer.Ordinal)
            .Select(k => new KeyUsage(k, counts.GetValueOrDefault(k), k == protector.CurrentKeyId, protector.KeyIds.Contains(k)))
            .ToList();
        return new PersonalDataKeyStatus(protector.CurrentKeyId, keys);
    }

    // k10 > k2: kimlikler sayi olarak siralanir (ADR-0020 §6 notu).
    private static int KeyNumber(string keyId) =>
        keyId.Length > 1 && int.TryParse(keyId.AsSpan(1), NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : int.MaxValue;

    private async Task<List<ProtectedValue>> ValuesAsync(CancellationToken cancellationToken)
    {
        var bloodGroups = await context.Set<PersonSensitive>().IgnoreQueryFilters().AsNoTracking()
            .Where(s => s.BloodGroup != null)
            .Join(context.Set<Person>().IgnoreQueryFilters(), s => s.PersonId, p => p.Id, (s, p) => new { s.Id, p.PublicId, Cipher = s.BloodGroup! })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var proposed = await context.Set<CorrectionRequest>().IgnoreQueryFilters().AsNoTracking()
            .Where(r => r.Field == CorrectionField.BloodGroup && r.ProposedValue != null)
            .Select(r => new { r.Id, r.PublicId, Cipher = r.ProposedValue! })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return
        [
            .. bloodGroups.Select(v => new ProtectedValue(ValueKind.BloodGroup, v.Id, v.Cipher, ProtectedField.BloodGroup(v.PublicId))),
            .. proposed.Select(v => new ProtectedValue(ValueKind.ProposedBloodGroup, v.Id, v.Cipher, ProtectedField.CorrectionValue(v.PublicId))),
        ];
    }

    private enum ValueKind
    {
        BloodGroup,
        ProposedBloodGroup,
    }

    private sealed record ProtectedValue(ValueKind Kind, long RowId, string Cipher, ProtectedField Field);
}
