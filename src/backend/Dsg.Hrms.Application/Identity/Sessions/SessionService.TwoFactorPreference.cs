using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Identity.Verification;
using Dsg.Hrms.Application.Settings;
using Dsg.Hrms.Domain.Audit;
using Dsg.Hrms.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace Dsg.Hrms.Application.Identity.Sessions;

/// <summary>
/// Kullanicinin kendi iki adimli dogrulama tercihi (SYG-KMLK-080).
/// </summary>
/// <remarks>
/// <para>
/// Giriste kod yalnizca sistem parametresi (PRM-KML-08) ve kullanicinin kendi tercihi birlikte
/// acikken istenir. Tercih varsayilan olarak kapalidir.
/// </para>
/// <para>
/// Acma ve kapatma mevcut parolayi ister: acik birakilmis bir oturumu bulan kisi tercihi
/// degistiremez. Yanlis parola, parola degisikligindeki gibi giris sayacina eklenir
/// (SYG-KMLK-033, 048). Acmak icin ayrica secilen kanala gonderilen kod dogrulanir: kisi
/// giriste kullanacagi kanalin kendisine ulastigini boylece gosterir. Kapatmak sistem
/// parametresi kapaliyken de mumkundur.
/// </para>
/// <para>
/// Tercih degisince guvenlik damgasi yenilenmez; acik oturumlar kapanmaz. Degisiklik hesap
/// satirinda oldugu icin denetim izine kendiliginden duser (SYG-KMLK-058).
/// </para>
/// </remarks>
public sealed partial class SessionService
{
    private const string TwoFactorUnavailableMessage =
        "İki adımlı doğrulama sistem yöneticisi tarafından kapatıldı. Şu anda açılamaz.";

    private const string ChannelUnavailableMessage = "Seçilen doğrulama yöntemi kullanılamıyor.";

    /// <summary>Kullanicinin iki adimli dogrulama durumu (SYG-KMLK-080).</summary>
    /// <exception cref="SessionEndedException">Oturum kapaliysa.</exception>
    public async Task<TwoFactorPreferenceStatus> GetTwoFactorPreferenceAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var (_, candidate) = await OpenSessionCandidateAsync(sessionId, cancellationToken).ConfigureAwait(false);
        var available = await _parameters.GetBooleanAsync(ParameterCatalog.TwoFactorEnabled, cancellationToken).ConfigureAwait(false);
        var channels = await ContactChannelsAsync(candidate, cancellationToken).ConfigureAwait(false);
        return new TwoFactorPreferenceStatus(available, candidate.Account.TwoFactorEnabled, channels);
    }

    /// <summary>
    /// Iki adimli dogrulamayi acmayi baslatir: mevcut parola denetlenir ve secilen kanala kod
    /// gonderilir (SYG-KMLK-080). Tercih ancak kod dogrulaninca acilir
    /// (<see cref="CompleteTwoFactorSetupAsync"/>).
    /// </summary>
    /// <param name="sessionId">Oturum.</param>
    /// <param name="currentPassword">Mevcut parola.</param>
    /// <param name="channel">Secilen kanal (<see cref="RegistrationChannels.Email"/> veya <see cref="RegistrationChannels.Sms"/>).</param>
    /// <param name="cancellationToken">Iptal.</param>
    /// <exception cref="SessionEndedException">Oturum kapaliysa.</exception>
    /// <exception cref="BusinessRuleException">Sistemde kapaliysa, zaten aciksa veya kanal kullanilamiyorsa.</exception>
    /// <exception cref="SignInLockedException">Hatali parola siniri asildiysa.</exception>
    /// <exception cref="TooManyRequestsException">Kod gonderim siniri asildiysa.</exception>
    public async Task<TwoFactorSetupResult> StartTwoFactorSetupAsync(
        Guid sessionId,
        string currentPassword,
        RegistrationChannels channel,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentPassword);

        var now = _clock.UtcNow;
        var (_, candidate) = await OpenSessionCandidateAsync(sessionId, cancellationToken).ConfigureAwait(false);

        if (!await _parameters.GetBooleanAsync(ParameterCatalog.TwoFactorEnabled, cancellationToken).ConfigureAwait(false))
        {
            throw new BusinessRuleException(TwoFactorUnavailableMessage);
        }

        if (candidate.Account.TwoFactorEnabled)
        {
            throw new BusinessRuleException("İki adımlı doğrulama hesabınızda zaten açık.");
        }

        var throttle = await VerifyCurrentPasswordAsync(
            sessionId, candidate, currentPassword, PasswordCheck.EnableTwoFactor, now, cancellationToken).ConfigureAwait(false);
        if (throttle is null)
        {
            return TwoFactorSetupResult.InvalidPassword;
        }

        throttle.Reset(now);

        var available = await ContactChannelsAsync(candidate, cancellationToken).ConfigureAwait(false);
        if (channel is not (RegistrationChannels.Email or RegistrationChannels.Sms) || !available.HasFlag(channel))
        {
            await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            throw new BusinessRuleException(ChannelUnavailableMessage);
        }

        var recipient = channel == RegistrationChannels.Email ? candidate.Person.Email! : candidate.Person.MobilePhone!;
        var issued = await _codes.IssueAsync(
            new VerificationRequest(candidate.Person.Id, VerificationPurpose.TwoFactorSetup,
                channel == RegistrationChannels.Email ? VerificationChannel.Email : VerificationChannel.Sms, recipient),
            cancellationToken).ConfigureAwait(false);

        // Sayacin sifirlanmasi kod gonderilemese de kaydedilir: parola dogruydu.
        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        if (issued.IsRateLimited)
        {
            throw new TooManyRequestsException();
        }

        return new TwoFactorSetupResult(false, issued.CodeId, issued.ExpiresAt);
    }

    /// <summary>
    /// Iki adimli dogrulamayi acma kodunu dogrular; dogruysa tercih acilir (SYG-KMLK-080). Kod bu
    /// kisiye ve bu amaca ait degilse <see cref="VerificationResult.NotUsable"/> doner.
    /// </summary>
    /// <exception cref="SessionEndedException">Oturum kapaliysa.</exception>
    /// <exception cref="BusinessRuleException">Iki adimli dogrulama sistemde kapatildiysa.</exception>
    public async Task<VerificationResult> CompleteTwoFactorSetupAsync(
        Guid sessionId,
        Guid codeId,
        string? code,
        CancellationToken cancellationToken)
    {
        var (_, candidate) = await OpenSessionCandidateAsync(sessionId, cancellationToken).ConfigureAwait(false);

        // Kod beklenirken parametre kapatilmis olabilir.
        if (!await _parameters.GetBooleanAsync(ParameterCatalog.TwoFactorEnabled, cancellationToken).ConfigureAwait(false))
        {
            throw new BusinessRuleException(TwoFactorUnavailableMessage);
        }

        var result = await _codes.VerifyAsync(codeId, code, candidate.Person.Id, VerificationPurpose.TwoFactorSetup, cancellationToken)
            .ConfigureAwait(false);
        if (result != VerificationResult.Verified)
        {
            return result;
        }

        if (candidate.Account.EnableTwoFactor())
        {
            _events.Record(SecurityEventType.TwoFactorEnabled, candidate.Account.Id, candidate.Person.Id);
            await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            LogTwoFactorPreferenceChanged(_logger, candidate.Account.Id, true);
        }

        return VerificationResult.Verified;
    }

    /// <summary>
    /// Iki adimli dogrulamayi kapatir; mevcut parola istenir (SYG-KMLK-080). Sistem parametresi
    /// kapaliyken de kullanilabilir. Tercih zaten kapaliysa hicbir sey degismez.
    /// </summary>
    /// <returns>Mevcut parola dogruysa <c>true</c>.</returns>
    /// <exception cref="SessionEndedException">Oturum kapaliysa.</exception>
    /// <exception cref="SignInLockedException">Hatali parola siniri asildiysa.</exception>
    public async Task<bool> DisableTwoFactorAsync(Guid sessionId, string currentPassword, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(currentPassword);

        var now = _clock.UtcNow;
        var (_, candidate) = await OpenSessionCandidateAsync(sessionId, cancellationToken).ConfigureAwait(false);

        var throttle = await VerifyCurrentPasswordAsync(
            sessionId, candidate, currentPassword, PasswordCheck.DisableTwoFactor, now, cancellationToken).ConfigureAwait(false);
        if (throttle is null)
        {
            return false;
        }

        throttle.Reset(now);
        if (candidate.Account.DisableTwoFactor())
        {
            _events.Record(SecurityEventType.TwoFactorDisabled, candidate.Account.Id, candidate.Person.Id);
            LogTwoFactorPreferenceChanged(_logger, candidate.Account.Id, false);
        }

        await _store.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return true;
    }

    [LoggerMessage(EventId = 3807, Level = LogLevel.Information, Message = "Iki adimli dogrulama tercihi degisti: hesap {AccountId}, acik {Enabled} (SYG-KMLK-080).")]
    private static partial void LogTwoFactorPreferenceChanged(ILogger logger, long accountId, bool enabled);
}

/// <summary>Kullanicinin iki adimli dogrulama durumu (SYG-KMLK-080).</summary>
/// <param name="Available">Sistemde kullaniliyor mu (PRM-KML-08).</param>
/// <param name="Enabled">Kullanicinin kendi tercihi acik mi.</param>
/// <param name="Channels">Kisinin kullanabilecegi kanallar; hicbiri yoksa <see cref="RegistrationChannels.None"/>.</param>
public sealed record TwoFactorPreferenceStatus(bool Available, bool Enabled, RegistrationChannels Channels);

/// <summary>Iki adimli dogrulamayi acmanin ilk adiminin sonucu (SYG-KMLK-080).</summary>
/// <param name="CurrentPasswordInvalid">Mevcut parola hataliydi; kod gonderilmedi.</param>
/// <param name="CodeId">Gonderilen kodun dis kimligi.</param>
/// <param name="CodeExpiresAt">Kodun gecerlilik sonu.</param>
public sealed record TwoFactorSetupResult(bool CurrentPasswordInvalid, Guid? CodeId, DateTimeOffset? CodeExpiresAt)
{
    /// <summary>Mevcut parola hatali.</summary>
    public static TwoFactorSetupResult InvalidPassword { get; } = new(true, null, null);
}
