using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Identity.Accounts;
using Dsg.Hrms.Application.Identity.Passwords;
using Dsg.Hrms.Application.Identity.Registration;
using Dsg.Hrms.Application.Identity.Sessions;
using Dsg.Hrms.Application.Identity.Verification;
using Dsg.Hrms.Application.Notifications;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Notifications;
using Microsoft.Extensions.Logging;

namespace Dsg.Hrms.Application.Identity.Invitations;

/// <summary>Davetlerin veritabani tarafi.</summary>
public interface IInvitationStore
{
    /// <summary>Kisinin hala kullanilabilir davetleri (izlenen).</summary>
    Task<IReadOnlyList<AccountInvitation>> GetUsableAsync(long personId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Jeton ozetiyle daveti bulur (izlenen).</summary>
    Task<AccountInvitation?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>Yeni kaydi ekler.</summary>
    void Add(object entity);

    /// <summary>Degisiklikleri kaydeder.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Gonderilen davet.</summary>
/// <param name="ExpiresAt">Baglantinin gecerlilik sonu.</param>
public sealed record InvitationSent(DateTimeOffset ExpiresAt);

/// <summary>Baglantiyla acilan sayfanin bilgisi.</summary>
/// <param name="FirstName">Kisinin adi (karsilama icin).</param>
/// <param name="AccountExists">Kisinin hesabi var mi; varsa parola yenilenir, yoksa hesap olusur.</param>
/// <param name="ExpiresAt">Gecerlilik sonu.</param>
public sealed record InvitationInfo(string FirstName, bool AccountExists, DateTimeOffset ExpiresAt);

/// <summary>
/// IK'nin tek kullanimlik parola olusturma baglantisi (ADR-0006 §4, SYG-KMLK-051…053).
/// </summary>
/// <remarks>
/// <para>
/// Baglanti YALNIZCA kisinin LOGO'dan senkronize edilmis, kabul edilen alan adindaki ve kisiye
/// tekil e-posta adresine gider; IK adres giremez. Boyle bir adres yoksa islem reddedilir.
/// </para>
/// <para>
/// Jeton 256 bit rastgeledir ve baglantinin <b>#</b> kisminda tasinir: tarayici bu kismi
/// sunucuya gondermez; jeton sunucu gunluklerine ve yonlendiren (Referer) bilgisine dusmez.
/// Sunucuda yalnizca SHA-256 ozeti saklanir.
/// </para>
/// <para>
/// Baglanti kullanilinca: hesap yoksa olusturulur; varsa parolasi yenilenir, acik oturumlari
/// kapanir ve giris kilidi kalkar (parola sifirlamayla ayni sonuc, SYG-KMLK-047).
/// </para>
/// </remarks>
public sealed partial class InvitationService
{
    private const string InvalidLinkMessage = "Bağlantı geçersiz veya süresi dolmuş. Yeni bir bağlantı için İnsan Kaynakları birimine başvurun.";

    private readonly IInvitationStore _store;
    private readonly IAccountAdministrationStore _accounts;
    private readonly IRegistrationStore _registrations;
    private readonly ISessionStore _sessions;
    private readonly IIdentifierHasher _identifierHasher;
    private readonly PasswordPolicy _passwordPolicy;
    private readonly IPasswordHasher _passwordHasher;
    private readonly INotificationDispatch _dispatch;
    private readonly ISystemParameters _parameters;
    private readonly IDateTimeProvider _clock;
    private readonly ILogger<InvitationService> _logger;

    /// <summary>Yeni ornek olusturur.</summary>
    /// <remarks>Depolar ayni veritabani baglamini paylasir; davet, hesap ve oturumlar birlikte yazilir.</remarks>
    public InvitationService(
        IInvitationStore store,
        IAccountAdministrationStore accounts,
        IRegistrationStore registrations,
        ISessionStore sessions,
        IIdentifierHasher identifierHasher,
        PasswordPolicy passwordPolicy,
        IPasswordHasher passwordHasher,
        INotificationDispatch dispatch,
        ISystemParameters parameters,
        IDateTimeProvider clock,
        ILogger<InvitationService> logger)
    {
        _store = store;
        _accounts = accounts;
        _registrations = registrations;
        _sessions = sessions;
        _identifierHasher = identifierHasher;
        _passwordPolicy = passwordPolicy;
        _passwordHasher = passwordHasher;
        _dispatch = dispatch;
        _parameters = parameters;
        _clock = clock;
        _logger = logger;
    }

    // ------------------------------------------------------------------ gonder

    /// <summary>
    /// Kisiye parola olusturma baglantisi gonderir; kisinin onceki baglantilari gecersizlesir.
    /// </summary>
    /// <param name="personId">Kisinin dis kimligi.</param>
    /// <param name="reason">Gerekce (zorunlu; denetim izine yazilir).</param>
    /// <param name="linkBase">Baglantinin sayfa adresi (ornegin <c>https://…/invite</c>).</param>
    /// <param name="cancellationToken">Iptal.</param>
    /// <exception cref="NotFoundException">Kisi yoksa.</exception>
    /// <exception cref="BusinessRuleException">Davet kapaliysa veya kisiye gonderilemiyorsa.</exception>
    public async Task<InvitationSent> SendAsync(Guid personId, string reason, Uri linkBase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(linkBase);

        if (!await _parameters.GetBooleanAsync(ParameterCatalog.HrInviteEnabled, cancellationToken).ConfigureAwait(false))
        {
            throw new BusinessRuleException("Parola oluşturma bağlantısı gönderimi sistem ayarlarında kapalı.");
        }

        var (person, account, hasActiveEmployment) = await _accounts.FindAsync(personId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException("Kişi bulunamadı.");

        if (!hasActiveEmployment)
        {
            throw new BusinessRuleException("Aktif çalışma kaydı olmayan kişiye bağlantı gönderilemez.");
        }

        if (account is { Status: AccountStatus.Passive })
        {
            throw new BusinessRuleException("Hesap pasif; bağlantı göndermeden önce hesabı aktifleştirin.");
        }

        // Yalnizca LOGO'daki, kabul edilen alan adinda ve kisiye tekil adres (SYG-KMLK-051).
        var domains = await _parameters.GetListAsync(ParameterCatalog.AcceptedEmailDomains, cancellationToken).ConfigureAwait(false);
        var email = person.Email;
        if (email is null
            || person.IsEmailShared
            || !domains.Contains(email[(email.LastIndexOf('@') + 1)..], StringComparer.OrdinalIgnoreCase))
        {
            throw new BusinessRuleException(
                "Kişinin LOGO'da tanımlı, kurumsal alan adında ve yalnızca kendisine ait bir e-posta adresi yok; bağlantı gönderilemez. Adresin LOGO'da düzeltilmesi gerekir.");
        }

        var now = _clock.UtcNow;
        foreach (var previous in await _store.GetUsableAsync(person.Id, now, cancellationToken).ConfigureAwait(false))
        {
            previous.Revoke(now);
        }

        var hours = await _parameters.GetIntegerAsync(ParameterCatalog.InviteLinkLifetimeHours, cancellationToken).ConfigureAwait(false);
        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        var invitation = AccountInvitation.Issue(person.Id, HashToken(token), reason, now, TimeSpan.FromHours(hours));
        _store.Add(invitation);

        var link = $"{linkBase.AbsoluteUri.TrimEnd('/')}#token={token}";
        var queued = _dispatch.TryEnqueue(new OutboundMessage(
            NotificationChannel.Email,
            NotificationPurpose.Invitation,
            person.Id,
            email,
            "DSG-HRMS parola oluşturma bağlantısı",
            InvitationMessage(link, hours),
            now));

        if (!queued)
        {
            throw new TooManyRequestsException("İleti kuyruğu dolu. Lütfen biraz sonra tekrar deneyin.");
        }

        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogSent(_logger, person.Id, invitation.PublicId);
        return new InvitationSent(invitation.ExpiresAt);
    }

    // ------------------------------------------------------------------ bagliyla acilan sayfa

    /// <summary>Baglantinin gecerli olup olmadigini ve kimin icin oldugunu okur.</summary>
    /// <exception cref="NotFoundException">Baglanti gecersiz, kullanilmis veya suresi dolmussa.</exception>
    public async Task<InvitationInfo> LookupAsync(string? token, CancellationToken cancellationToken)
    {
        var (invitation, candidate) = await FindUsableAsync(token, cancellationToken).ConfigureAwait(false);
        return new InvitationInfo(candidate.Person.FirstName, candidate.Account is not null, invitation.ExpiresAt);
    }

    /// <summary>
    /// Parolayi belirler: hesap yoksa olusturur, varsa parolayi yeniler ve acik oturumlari
    /// kapatir. Ihlal varsa hicbir sey kaydedilmez ve ihlaller doner.
    /// </summary>
    /// <exception cref="NotFoundException">Baglanti gecersiz, kullanilmis veya suresi dolmussa.</exception>
    /// <exception cref="AccountDisabledException">Hesap kullanima kapaliysa.</exception>
    public async Task<IReadOnlyList<PasswordViolation>> AcceptAsync(string? token, string password, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(password);

        var (invitation, candidate) = await FindUsableAsync(token, cancellationToken).ConfigureAwait(false);
        var person = candidate.Person;

        if (!candidate.HasActiveEmployment || candidate.Account is { Status: AccountStatus.Passive })
        {
            throw new AccountDisabledException();
        }

        var violations = await _passwordPolicy.ValidateAsync(password, RegistrationService.PersonalWords(person.FirstName, person.LastName, person.Email), cancellationToken)
            .ConfigureAwait(false);
        if (violations.Count > 0)
        {
            return violations;
        }

        var now = _clock.UtcNow;
        var hash = _passwordHasher.Hash(PasswordPolicy.Normalize(password));

        if (candidate.Account is null)
        {
            _store.Add(UserAccount.Register(person.Id, hash, now));
        }
        else
        {
            // Aday kaydi izlenmeden okunur; degisiklik icin hesap izlenen olarak yeniden alinir.
            var account = (await _sessions.FindByAccountIdAsync(candidate.Account.Id, cancellationToken).ConfigureAwait(false)
                ?? throw new NotFoundException(InvalidLinkMessage)).Account;
            account.SetPassword(hash, now);
            account.ClearLockout();

            foreach (var session in await _sessions.GetOpenSessionsAsync(account.Id, cancellationToken).ConfigureAwait(false))
            {
                session.End(SessionEndReason.PasswordChanged, now);
            }

            if (person.Email is not null)
            {
                var throttle = await _sessions.FindThrottleAsync(_identifierHasher.HashIdentifier(SessionService.LoginIdentifier(person.Email)), cancellationToken)
                    .ConfigureAwait(false);
                throttle?.Reset(now);
            }
        }

        invitation.Use(now);

        // Es zamanli iki kabulde ikincisi hesabin tekillik kisitina takilir (SYG-KMLK-021).
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        LogAccepted(_logger, person.Id, candidate.Account is null);
        return [];
    }

    /// <summary>Jetonun ozeti (SHA-256, Base64; 44 karakter).</summary>
    public static string HashToken(string token) =>
        Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    /// <summary>
    /// Davet iletisi (SYG-KMLK-030): baglanti ve gecerlilik suresi disinda kisisel veri icermez.
    /// </summary>
    public static string InvitationMessage(string link, int lifetimeHours) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"""
            Merhaba,

            İnsan Kaynakları birimi, DSG-HRMS için parolanızı belirlemeniz amacıyla size bu bağlantıyı gönderdi:

            {link}

            Bağlantı {lifetimeHours} saat geçerlidir ve yalnızca bir kez kullanılabilir. Bağlantıyı kimseyle paylaşmayın.

            Bu iletiyi beklemiyorsanız dikkate almayın ve Bilgi İşlem birimine bilgi verin.

            {VerificationMessages.Organization}
            """);

    private async Task<(AccountInvitation Invitation, RegistrationCandidate Candidate)> FindUsableAsync(string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 100)
        {
            throw new NotFoundException(InvalidLinkMessage);
        }

        var invitation = await _store.FindByTokenHashAsync(HashToken(token.Trim()), cancellationToken).ConfigureAwait(false);
        if (invitation is null || !invitation.IsUsable(_clock.UtcNow))
        {
            throw new NotFoundException(InvalidLinkMessage);
        }

        var candidate = await _registrations.FindCandidateAsync(invitation.PersonId, cancellationToken).ConfigureAwait(false)
            ?? throw new NotFoundException(InvalidLinkMessage);

        return (invitation, candidate);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    [LoggerMessage(EventId = 3960, Level = LogLevel.Information, Message = "Parola olusturma baglantisi gonderildi: kisi {PersonId}, davet {InvitationId} (SYG-KMLK-051).")]
    private static partial void LogSent(ILogger logger, long personId, Guid invitationId);

    [LoggerMessage(EventId = 3961, Level = LogLevel.Information, Message = "Parola olusturma baglantisi kullanildi: kisi {PersonId}; yeni hesap {NewAccount}.")]
    private static partial void LogAccepted(ILogger logger, long personId, bool newAccount);
}
