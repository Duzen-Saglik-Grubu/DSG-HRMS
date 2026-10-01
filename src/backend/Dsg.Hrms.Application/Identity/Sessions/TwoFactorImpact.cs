using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Settings;

namespace Dsg.Hrms.Application.Identity.Sessions;

/// <summary>2FA etki sayiminin veritabani tarafi.</summary>
public interface ITwoFactorImpactStore
{
    /// <summary>
    /// Aktif hesabi ve aktif istihdami olan, verilen kanallarin hicbirinde kullanilabilir
    /// iletisim bilgisi bulunmayan kisilerin sayisi.
    /// </summary>
    /// <param name="emailAllowed">E-posta kanali kullanilabiliyor mu.</param>
    /// <param name="smsAllowed">SMS kanali kullanilabiliyor mu.</param>
    /// <param name="cancellationToken">Iptal.</param>
    Task<int> CountWithoutChannelAsync(bool emailAllowed, bool smsAllowed, CancellationToken cancellationToken);
}

/// <summary>2FA'nin acilmasinin etkisi.</summary>
/// <param name="AffectedCount">Hicbir dogrulama kanali olmayan aktif hesap sahibi sayisi.</param>
/// <param name="ConfirmationRequired">Acmadan once onay isteniyor mu (PRM-KML-15).</param>
public sealed record TwoFactorImpactResult(int AffectedCount, bool ConfirmationRequired);

/// <summary>
/// Iki adimli dogrulama acilmadan once etki uyarisi (SYG-KMLK-035, REQ-KMLK-052).
/// </summary>
/// <remarks>
/// <para>
/// 2FA acikken hicbir dogrulama kanali bulunmayan kisi giris yapamaz. Bu kisilerin sayisi,
/// parametre acilmadan ONCE yoneticiye gosterilir ve onay istenir: karar sonucunu gormeden
/// verilmez. "Kanal yok" girisle AYNI kurala gore hesaplanir
/// (<see cref="SessionService"/>): izin verilen kanallar (PRM-KML-02) arasinda kisinin
/// e-postasi veya cep telefonu yoksa. Hicbir kanala izin verilmiyorsa giris e-postayi
/// kullanir; sayim da oyle yapar.
/// </para>
/// <para>
/// Onay kurali SUNUCUDA denetlenir: istemci uyariyi atlasa da 2FA onaysiz acilmaz. Uyari
/// parametresi (PRM-KML-15) kapatilirsa onay istenmez.
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

    /// <summary>2FA acilirsa giris yapamayacak aktif hesap sahiplerinin sayisi.</summary>
    public async Task<TwoFactorImpactResult> EvaluateAsync(CancellationToken cancellationToken)
    {
        var channels = await _parameters.GetListAsync(ParameterCatalog.VerificationChannels, cancellationToken).ConfigureAwait(false);
        var emailAllowed = channels.Contains("email") || !channels.Contains("sms");
        var smsAllowed = channels.Contains("sms");

        var count = await _store.CountWithoutChannelAsync(emailAllowed, smsAllowed, cancellationToken).ConfigureAwait(false);
        var warn = await _parameters.GetBooleanAsync(ParameterCatalog.TwoFactorImpactWarning, cancellationToken).ConfigureAwait(false);
        return new TwoFactorImpactResult(count, warn);
    }

    /// <summary>
    /// Parametre degisikligi 2FA'yi aciyorsa ve uyari aciksa onay ister.
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
                $"İki adımlı doğrulama açılırsa {impact.AffectedCount} aktif hesap sahibi giriş yapamaz (doğrulama kanalı yok). Açmak için etkiyi onaylayın.");
        }
    }
}
