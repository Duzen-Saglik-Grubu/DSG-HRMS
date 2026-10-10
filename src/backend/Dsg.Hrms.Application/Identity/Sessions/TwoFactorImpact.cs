using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Settings;

namespace Dsg.Hrms.Application.Identity.Sessions;

/// <summary>2FA etki sayiminin veritabani tarafi.</summary>
public interface ITwoFactorImpactStore
{
    /// <summary>
    /// Aktif hesabi ve aktif istihdami olan, kendi hesabinda iki adimli dogrulamayi acmis
    /// kisilerin sayisi (SYG-KMLK-080).
    /// </summary>
    /// <param name="cancellationToken">Iptal.</param>
    Task<int> CountWithTwoFactorPreferenceAsync(CancellationToken cancellationToken);
}

/// <summary>2FA'nin acilmasinin etkisi.</summary>
/// <param name="AffectedCount">Kendi hesabinda iki adimli dogrulamayi acmis, giriste kod girmeye baslayacak aktif hesap sahibi sayisi.</param>
/// <param name="ConfirmationRequired">Acmadan once onay isteniyor mu: sayi sifirdan buyuk ve uyari acik (PRM-KML-15).</param>
public sealed record TwoFactorImpactResult(int AffectedCount, bool ConfirmationRequired);

/// <summary>
/// Iki adimli dogrulama acilmadan once etki uyarisi (SYG-KMLK-035, 080; REQ-KMLK-052).
/// </summary>
/// <remarks>
/// <para>
/// Iki adimli dogrulama kisinin kendi tercihidir (SYG-KMLK-080): sistem parametresi
/// (PRM-KML-08) acilinca yalnizca kendi hesabinda tercihi ACIK olanlardan giriste kod istenir.
/// Dogrulama kanali olmayan kisi tercihi acamadigi icin parametre kimsenin girisini
/// engellemez. Bu nedenle etki, tercihi acik olan aktif hesap sahiplerinin sayisidir; parametre
/// acilmadan ONCE yoneticiye gosterilir.
/// </para>
/// <para>
/// Onay kurali SUNUCUDA denetlenir: istemci uyariyi atlasa da 2FA onaysiz acilmaz. Etkilenen
/// kimse yoksa veya uyari parametresi (PRM-KML-15) kapaliysa onay istenmez.
/// </para>
/// </remarks>
public sealed class TwoFactorImpactService
{
    private readonly ITwoFactorImpactStore _store;
    private readonly ISystemParameters _parameters;

    /// <summary>Yeni ornek olusturur.</summary>
    public TwoFactorImpactService(ITwoFactorImpactStore store, ISystemParameters parameters)
    {
        _store = store;
        _parameters = parameters;
    }

    /// <summary>2FA acilirsa giriste kod girmeye baslayacak aktif hesap sahiplerinin sayisi.</summary>
    public async Task<TwoFactorImpactResult> EvaluateAsync(CancellationToken cancellationToken)
    {
        var count = await _store.CountWithTwoFactorPreferenceAsync(cancellationToken).ConfigureAwait(false);
        var warn = await _parameters.GetBooleanAsync(ParameterCatalog.TwoFactorImpactWarning, cancellationToken).ConfigureAwait(false);
        return new TwoFactorImpactResult(count, warn && count > 0);
    }

    /// <summary>
    /// Parametre degisikligi 2FA'yi aciyorsa ve onay gerekiyorsa onay ister.
    /// </summary>
    /// <exception cref="BusinessRuleException">Onay gerekiyor ve verilmediyse.</exception>
    public async Task EnsureConfirmedAsync(string key, string? value, bool confirmed, CancellationToken cancellationToken)
    {
        if (!string.Equals(key, ParameterCatalog.TwoFactorEnabled.Key, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(value?.Trim(), "true", StringComparison.OrdinalIgnoreCase)
            || confirmed)
        {
            return;
        }

        if (await _parameters.GetBooleanAsync(ParameterCatalog.TwoFactorEnabled, cancellationToken).ConfigureAwait(false))
        {
            return; // Zaten acik; degisiklik yok.
        }

        var impact = await EvaluateAsync(cancellationToken).ConfigureAwait(false);
        if (impact.ConfirmationRequired)
        {
            throw new BusinessRuleException(
                $"İki adımlı doğrulama açılırsa, kendi hesabında iki adımlı doğrulamayı açmış {impact.AffectedCount} kişi bundan sonra girişte kod girecek. Açmak için onaylayın.");
        }
    }
}
