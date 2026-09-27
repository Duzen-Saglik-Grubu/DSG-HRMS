using Dsg.Hrms.Domain.Identity;
using Dsg.Hrms.Domain.Personnel;

namespace Dsg.Hrms.Application.Identity.Registration;

/// <summary>Uyelik akisinin veritabani tarafi.</summary>
public interface IRegistrationStore
{
    /// <summary>TCKN ozetiyle verilen andan sonra baslatilan deneme sayisi.</summary>
    Task<int> CountAttemptsSinceAsync(string nationalIdHash, DateTimeOffset since, CancellationToken cancellationToken);

    /// <summary>IP adresinden verilen andan sonra baslatilan deneme sayisi.</summary>
    Task<int> CountAttemptsFromIpSinceAsync(string ipAddress, DateTimeOffset since, CancellationToken cancellationToken);

    /// <summary>TCKN ozetiyle verilen andan sonra yapilan kod istegi sayisi.</summary>
    Task<int> CountCodeRequestsSinceAsync(string nationalIdHash, DateTimeOffset since, CancellationToken cancellationToken);

    /// <summary>TCKN ile kisiyi, aktif istihdami olup olmadigini ve hesabini bulur; kisi yoksa <c>null</c>.</summary>
    Task<RegistrationCandidate?> FindCandidateAsync(string nationalId, CancellationToken cancellationToken);

    /// <summary>Kisiyi kimligiyle bulur (aktiflik ve hesapla).</summary>
    Task<RegistrationCandidate?> FindCandidateAsync(long personId, CancellationToken cancellationToken);

    /// <summary>Denemeyi dis kimligiyle bulur (izlenen).</summary>
    Task<RegistrationAttempt?> FindAttemptAsync(Guid publicId, CancellationToken cancellationToken);

    /// <summary>Yeni denemeyi ekler.</summary>
    void Add(RegistrationAttempt attempt);

    /// <summary>Kod istegini kaydeder.</summary>
    void Add(RegistrationCodeRequest request);

    /// <summary>Yeni hesabi ekler.</summary>
    void Add(UserAccount account);

    /// <summary>Degisiklikleri kaydeder.</summary>
    /// <exception cref="Common.Exceptions.ConflictException">
    /// Kisiye bu arada baska bir istekle hesap olusturulduysa (SYG-KMLK-021).
    /// </exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Uyelik icin aday kisi.</summary>
/// <param name="Person">Kisi.</param>
/// <param name="HasActiveEmployment">En az bir aktif istihdami var mi (SYG-KMLK-013).</param>
/// <param name="Account">Kisinin hesabi; yoksa <c>null</c>.</param>
public sealed record RegistrationCandidate(Person Person, bool HasActiveEmployment, UserAccount? Account);

/// <summary>Kisiye bagli olmayan degerlerin anahtarli ozeti (TCKN, hiz siniri icin).</summary>
public interface IIdentifierHasher
{
    /// <summary>Degerin ozetini uretir. Ayni deger her zaman ayni ozeti verir.</summary>
    string HashIdentifier(string value);
}
