namespace Dsg.Hrms.Application.Personnel.Sync;

/// <summary>
/// LOGO personel verisinin tek erisim noktasi (ADR-0003 §2, SYG-KMLK-002).
/// </summary>
/// <remarks>
/// <para>
/// Yolsuzluk onleyici katmandir: LOGO'nun tablo adlari, kolon kodlari ve alan
/// anlamlari bu arayuzun arkasinda kalir. Uygulamanin geri kalani yalnizca
/// <see cref="LogoPersonnelRecord"/> gorur.
/// </para>
/// <para>
/// Arayuzde <b>yazma metodu yoktur</b> ve olmayacaktir (<c>KR-003</c>).
/// </para>
/// </remarks>
public interface ILogoPersonnelSource
{
    /// <summary>
    /// LOGO oturumunun gercekten salt okunur oldugunu, HICBIR SEY YAZMADAN denetler.
    /// </summary>
    /// <remarks>
    /// Senkronizasyon her calismadan once bunu cagirir ve oturumda yazma yetkisi
    /// gorurse calismayi reddeder. <c>DENY</c> yetkisi bir gun yanlislikla kaldirilirsa
    /// uygulama bunu fark eder; sessizce yazma yetkili bir oturumla calismaz.
    /// </remarks>
    Task<LogoAccessCheckResult> VerifyReadOnlyAccessAsync(CancellationToken cancellationToken);

    /// <summary>Beklenen tablo, kolon ve veri tiplerinin varligini denetler (SYG-KMLK-012).</summary>
    Task<LogoSchemaCheckResult> VerifySchemaAsync(CancellationToken cancellationToken);

    /// <summary>Tum personel kartlarini okur (aktif ve ayrilmis).</summary>
    /// <exception cref="LogoUnavailableException">LOGO'ya erisilemezse.</exception>
    Task<IReadOnlyList<LogoPersonnelRecord>> GetAllAsync(CancellationToken cancellationToken);
}

/// <summary>
/// LOGO'daki bir personel karti. Degerler kaynaktaki gibidir; normallestirme ve
/// dogrulama senkronizasyonda yapilir.
/// </summary>
/// <param name="LogoRef">Kart referansi.</param>
/// <param name="RegistryCode">Sicil kodu (bosluklari atilmis).</param>
/// <param name="NationalId">TCKN; yoksa <c>null</c>.</param>
/// <param name="FirstName">Ad.</param>
/// <param name="LastName">Soyad.</param>
/// <param name="BirthDate">Dogum tarihi; yoksa <c>null</c>.</param>
/// <param name="HireDate">Ise giris tarihi.</param>
/// <param name="TerminationDate">Isten cikis tarihi; aktifse <c>null</c>.</param>
/// <param name="FirmNumber">Firma numarasi.</param>
/// <param name="FirmName">Firma adi; firma listesinde yoksa <c>null</c>.</param>
/// <param name="Emails">Karttaki e-posta kayitlari, kaynak sirasiyla.</param>
/// <param name="MobilePhones">Karttaki cep telefonu kayitlari, kaynak sirasiyla.</param>
public sealed record LogoPersonnelRecord(
    int LogoRef,
    string RegistryCode,
    string? NationalId,
    string? FirstName,
    string? LastName,
    DateOnly? BirthDate,
    DateOnly HireDate,
    DateOnly? TerminationDate,
    short FirmNumber,
    string? FirmName,
    IReadOnlyList<string> Emails,
    IReadOnlyList<string> MobilePhones);

/// <summary>Salt okunur erisim denetiminin sonucu.</summary>
/// <param name="IsReadOnly">Oturumda hicbir yazma yetkisi yoksa <c>true</c>.</param>
/// <param name="GrantedWritePermissions">Tespit edilen yazma yetkileri (ornek: <c>LH_001_PERSON:UPDATE</c>).</param>
public sealed record LogoAccessCheckResult(bool IsReadOnly, IReadOnlyList<string> GrantedWritePermissions);

/// <summary>Sema denetiminin sonucu.</summary>
/// <param name="IsCompatible">Beklenen tum tablo ve kolonlar uygun tipte mevcutsa <c>true</c>.</param>
/// <param name="Problems">Tespit edilen sapmalar (ornek: <c>LH_001_PERSON.TTFNO: bulunamadi</c>).</param>
public sealed record LogoSchemaCheckResult(bool IsCompatible, IReadOnlyList<string> Problems);

/// <summary>LOGO veritabanina erisilemediginde firlatilir.</summary>
public sealed class LogoUnavailableException : Exception
{
    /// <summary>Yeni ornek olusturur.</summary>
    public LogoUnavailableException()
        : base("LOGO veritabanina erisilemedi.")
    {
    }

    /// <summary>Yeni ornek olusturur.</summary>
    public LogoUnavailableException(string message)
        : base(message)
    {
    }

    /// <summary>Yeni ornek olusturur.</summary>
    public LogoUnavailableException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
