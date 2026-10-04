using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Common.Paging;
using Dsg.Hrms.Application.Identity.Sessions;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;
using Microsoft.Extensions.Logging;

namespace Dsg.Hrms.Application.Identity.Accounts;

/// <summary>Hesap islemleri ekraninda bir kisinin hesap durumu (SYG-KMLK-073).</summary>
public enum AccountState
{
    /// <summary>Kisi henuz uye olmamis.</summary>
    None = 1,

    /// <summary>Hesap aktif.</summary>
    Active = 2,

    /// <summary>Hesap pasif (istihdam bitti veya elle).</summary>
    Passive = 3,

    /// <summary>Hesap aktif ama hatali giris siniri asildigi icin gecici olarak kilitli.</summary>
    Locked = 4,
}

/// <summary>Kisinin bir istihdami (yalnizca sicil ve firma).</summary>
/// <param name="RegistryCode">Sicil numarasi.</param>
/// <param name="CompanyName">Firma.</param>
/// <param name="IsActive">Istihdam suruyor mu.</param>
public sealed record AccountEmployment(string RegistryCode, string CompanyName, bool IsActive);

/// <summary>
/// Listedeki bir kisi. Kisisel veri olarak YALNIZCA ad, soyad, sicil ve firma tasir
/// (SYG-KMLK-073); TCKN, dogum tarihi, e-posta ve telefon bu ekrana hic gelmez.
/// </summary>
/// <param name="PersonId">Kisinin dis kimligi.</param>
/// <param name="FirstName">Ad.</param>
/// <param name="LastName">Soyad.</param>
/// <param name="Employments">Istihdamlar.</param>
/// <param name="State">Hesap durumu.</param>
/// <param name="StatusReason">Durumun nedeni (pasif veya elle aktiflestirilmisse).</param>
/// <param name="IsCurrentUser">Satir, islemi yapan kullanicinin kendisi mi; ekran pasife alma dugmesini gostermez (#138).</param>
public sealed record AccountSummary(
    Guid PersonId,
    string FirstName,
    string LastName,
    IReadOnlyList<AccountEmployment> Employments,
    AccountState State,
    AccountStatusReason? StatusReason,
    bool IsCurrentUser);

/// <summary>Arama siralamasi.</summary>
public enum AccountSort
{
    /// <summary>Soyad, sonra ad.</summary>
    LastName = 1,

    /// <summary>Ad, sonra soyad.</summary>
    FirstName = 2,
}

/// <summary>Hesap islemlerinin veritabani tarafi.</summary>
public interface IAccountAdministrationStore
{
    /// <summary>
    /// Kisileri arar. Terim rakamlardan olusuyorsa sicil numarasinin basiyla, degilse ad veya
    /// soyadin iceriyle eslesir (buyuk/kucuk harf duyarsiz). Terim bossa tum kisiler.
    /// </summary>
    Task<PagedResult<AccountSummary>> SearchAsync(
        string? term,
        AccountSort sort,
        bool descending,
        PageRequest page,
        DateTimeOffset now,
        long? currentAccountId,
        CancellationToken cancellationToken);

    /// <summary>Kisiyi, hesabini (izlenen) ve aktif istihdami olup olmadigini bulur.</summary>
    Task<(Person Person, UserAccount? Account, bool HasActiveEmployment)?> FindAsync(Guid personId, CancellationToken cancellationToken);

    /// <summary>
    /// Hesap Sistem Yoneticisi rolune sahip ve bu role sahip baska aktif hesap yok mu (#138).
    /// </summary>
    Task<bool> IsLastActiveSystemAdministratorAsync(long accountId, CancellationToken cancellationToken);
}

/// <summary>
/// IK hesap islemleri (SYG-KMLK-057, 073): arama ve elle pasife alma / aktiflestirme.
/// </summary>
/// <remarks>
/// <para>
/// Listeleme erisim kaydina yazilir (ADR-0009 §3): bir IK kullanicisinin hangi aramayla kac
/// kisiyi gordugu sonradan izlenebilir. Durum degisiklikleri hesabin denetim izine onceki ve
/// sonraki durumla yazilir (SYG-KMLK-058).
/// </para>
/// <para>
/// Kapsam T4'e kadar tum personeldir (SYG-KMLK-074); satir bazli kapsam geldiginde arama
/// sorgusu kapsam filtresiyle daraltilir.
/// </para>
/// </remarks>
public sealed partial class AccountAdministrationService
{
    private readonly IAccountAdministrationStore _store;
    private readonly ISessionStore _sessions;
    private readonly ICurrentUser _currentUser;
    private readonly IAccessLogger _accessLogger;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<AccountAdministrationService> _logger;

    /// <summary>Yeni ornek olusturur.</summary>
    /// <remarks>Iki depo ayni veritabani baglamini paylasir; durum ve oturumlar birlikte yazilir.</remarks>
    public AccountAdministrationService(
        IAccountAdministrationStore store,
        ISessionStore sessions,
        ICurrentUser currentUser,
        IAccessLogger accessLogger,
        IDateTimeProvider clock,
        ILogger<AccountAdministrationService> logger)
    {
        _store = store;
        _sessions = sessions;
        _currentUser = currentUser;
        _accessLogger = accessLogger;
        _clock = clock;
        _logger = logger;
    }

    /// <summary>Kisileri arar ve listelemeyi erisim kaydina yazar.</summary>
    public async Task<PagedResult<AccountSummary>> SearchAsync(
        string? term,
        AccountSort sort,
        bool descending,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(page);

        var trimmed = string.IsNullOrWhiteSpace(term) ? null : term.Trim();
        var result = await _store.SearchAsync(trimmed, sort, descending, page, _clock.UtcNow, _currentUser.UserId, cancellationToken).ConfigureAwait(false);

        await _accessLogger.LogAsync(
            AccessRecord.List(nameof(Person), result.Items.Count, new Dictionary<string, string?> { ["q"] = trimmed, ["page"] = page.Page.ToString(System.Globalization.CultureInfo.InvariantCulture) }),
            cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <summary>
    /// Hesabi gerekceyle elle pasife alir; acik oturumlar hemen kapanir (SYG-KMLK-054, 057).
    /// </summary>
    /// <exception cref="NotFoundException">Kisi yoksa.</exception>
    /// <exception cref="BusinessRuleException">
    /// Kisinin hesabi yoksa, hesap zaten pasifse, hesap islemi yapanin kendisininse veya
    /// aktif kalan son sistem yoneticisininse.
    /// </exception>
    /// <remarks>
    /// Son iki kural sistemin yonetilemez hale gelmesini onler (#138): pasif hesap giris
    /// yapamaz ve hesabi yeniden aktiflestirecek baska yonetici yoksa yalnizca veritabanina
    /// dogrudan mudahaleyle geri donulebilir.
    /// </remarks>
    public async Task DeactivateAsync(Guid personId, string reason, CancellationToken cancellationToken)
    {
        var (_, account, _) = await FindWithAccountAsync(personId, cancellationToken).ConfigureAwait(false);
        if (account.Status == AccountStatus.Passive)
        {
            throw new BusinessRuleException("Hesap zaten pasif.");
        }

        if (account.Id == _currentUser.UserId)
        {
            throw new BusinessRuleException("Kendi hesabınızı pasife alamazsınız. Gerekirse başka bir yetkili kullanıcıdan isteyin.");
        }

        if (await _store.IsLastActiveSystemAdministratorAsync(account.Id, cancellationToken).ConfigureAwait(false))
        {
            throw new BusinessRuleException("Bu kişi aktif kalan son sistem yöneticisi; hesabı pasife alınırsa sistemi yönetecek kimse kalmaz. Önce başka bir kişiye Sistem Yöneticisi rolü verilmelidir.");
        }

        account.DeactivateManually(reason);

        var now = _clock.UtcNow;
        foreach (var session in await _sessions.GetOpenSessionsAsync(account.Id, cancellationToken).ConfigureAwait(false))
        {
            session.End(SessionEndReason.AccountChanged, now);
        }

        await _sessions.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogDeactivated(_logger, account.Id);
    }

    /// <summary>Hesabi gerekceyle elle yeniden aktiflestirir (SYG-KMLK-057).</summary>
    /// <exception cref="NotFoundException">Kisi yoksa.</exception>
    /// <exception cref="BusinessRuleException">
    /// Kisinin hesabi yoksa, hesap zaten aktifse veya kisinin aktif istihdami yoksa.
    /// </exception>
    public async Task ActivateAsync(Guid personId, string reason, CancellationToken cancellationToken)
    {
        var (_, account, hasActiveEmployment) = await FindWithAccountAsync(personId, cancellationToken).ConfigureAwait(false);
        if (account.Status == AccountStatus.Active)
        {
            throw new BusinessRuleException("Hesap zaten aktif.");
        }

        if (!hasActiveEmployment)
        {
            // Bir sonraki senkronizasyon hesabi yeniden pasife alirdi; arada ayrilmis bir kisi
            // giris yapabilirdi (KR-015).
            throw new BusinessRuleException("Aktif çalışma kaydı olmayan kişinin hesabı aktifleştirilemez.");
        }

        account.ActivateManually(reason, hasActiveEmployment);
        await _sessions.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogActivated(_logger, account.Id);
    }

    private async Task<(Person Person, UserAccount Account, bool HasActiveEmployment)> FindWithAccountAsync(Guid personId, CancellationToken cancellationToken)
    {
        var found = await _store.FindAsync(personId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Kişi bulunamadı.");

        var (person, account, hasActiveEmployment) = found;
        return account is null
            ? throw new BusinessRuleException("Kişinin hesabı yok; henüz üye olmamış.")
            : (person, account, hasActiveEmployment);
    }

    [LoggerMessage(EventId = 3950, Level = LogLevel.Information, Message = "Hesap elle pasife alindi: hesap {AccountId}; acik oturumlar kapatildi (SYG-KMLK-057).")]
    private static partial void LogDeactivated(ILogger logger, long accountId);

    [LoggerMessage(EventId = 3951, Level = LogLevel.Information, Message = "Hesap elle aktiflestirildi: hesap {AccountId} (SYG-KMLK-057).")]
    private static partial void LogActivated(ILogger logger, long accountId);
}
