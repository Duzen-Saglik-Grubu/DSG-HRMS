using Dsg.Hrms.Domain.Identity;

namespace Dsg.Hrms.Application.Identity.Verification;

/// <summary>Dogrulama kodlarinin veritabani tarafi.</summary>
public interface IVerificationCodeStore
{
    /// <summary>Kisiye verilen tarihten sonra uretilen kod sayisi (amactan bagimsiz).</summary>
    Task<int> CountIssuedSinceAsync(long personId, DateTimeOffset since, CancellationToken cancellationToken);

    /// <summary>Kisinin verilen amactaki, hala <see cref="VerificationCodeStatus.Issued"/> durumundaki kodlari (izlenen).</summary>
    Task<IReadOnlyList<VerificationCode>> GetOpenAsync(long personId, VerificationPurpose purpose, CancellationToken cancellationToken);

    /// <summary>Kodu dis kimligiyle bulur (izlenen); yoksa <c>null</c>.</summary>
    Task<VerificationCode?> FindAsync(Guid publicId, CancellationToken cancellationToken);

    /// <summary>Yeni kodu ekler.</summary>
    void Add(VerificationCode code);

    /// <summary>Degisiklikleri kaydeder.</summary>
    /// <exception cref="VerificationConflictException">
    /// Kod bu arada baska bir istekle degistirildiyse (eszamanli dogrulama, SYG-KMLK-027).
    /// </exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Kod kaydi eszamanli bir istekle degistirildiginde firlatilir.</summary>
public sealed class VerificationConflictException : Exception
{
    /// <summary>Yeni ornek olusturur.</summary>
    public VerificationConflictException()
        : base("Dogrulama kodu eszamanli bir istekle degistirildi.")
    {
    }

    /// <summary>Yeni ornek olusturur.</summary>
    public VerificationConflictException(string message)
        : base(message)
    {
    }

    /// <summary>Yeni ornek olusturur.</summary>
    public VerificationConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Kodun anahtarli ozetini hesaplar (SYG-KMLK-025).</summary>
public interface IVerificationCodeHasher
{
    /// <summary>Anahtar tanimli mi.</summary>
    bool IsConfigured { get; }

    /// <summary>Kodun ozetini hesaplar. Ozet kodun dis kimligine baglanir.</summary>
    string Hash(Guid codeId, string code);

    /// <summary>Girilen kodun ozeti saklanan ozetle eslesiyor mu (sabit surede).</summary>
    bool Matches(Guid codeId, string code, string storedHash);
}
