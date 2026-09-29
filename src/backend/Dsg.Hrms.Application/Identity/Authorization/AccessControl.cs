using System.ComponentModel.DataAnnotations;
using Dsg.Hrms.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace Dsg.Hrms.Application.Identity.Authorization;

/// <summary>Eylem yetkisinin veritabani tarafi.</summary>
public interface IAccessControlStore
{
    /// <summary>Hesabin tum rollerinden gelen izinler (birlesim).</summary>
    Task<IReadOnlyList<string>> GetPermissionsAsync(long userAccountId, CancellationToken cancellationToken);

    /// <summary>Rolu koduyla bulur.</summary>
    Task<Role?> FindRoleAsync(string code, CancellationToken cancellationToken);

    /// <summary>Hesabin bu rolu var mi.</summary>
    Task<bool> HasRoleAsync(long userAccountId, long roleId, CancellationToken cancellationToken);

    /// <summary>Yeni kaydi ekler.</summary>
    void Add(object entity);

    /// <summary>Degisiklikleri kaydeder.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>Eylem yetkisi yapilandirmasi.</summary>
public sealed class AccessControlOptions : IValidatableObject
{
    /// <summary>Yapilandirma bolumunun adi.</summary>
    public const string SectionName = "AccessControl";

    /// <summary>
    /// Ilk sistem yoneticilerinin kurumsal e-posta adresleri, virgulle (SYG-KMLK-074). Bu
    /// adreslerle giris yapan hesaplara Sistem Yoneticisi rolu bir kez atanir. Tek metin olarak
    /// alinir: ortam degiskeninde (<c>AccessControl__BootstrapAdministrators</c>) dizi yazmak
    /// hataya aciktir.
    /// </summary>
    public string? BootstrapAdministrators { get; init; }

    /// <summary>Kucuk harfe cevrilmis adresler.</summary>
    public IReadOnlyList<string> BootstrapAdministratorEmails =>
        [.. (BootstrapAdministrators ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(email => email.ToLowerInvariant())];

    /// <inheritdoc />
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var email = new EmailAddressAttribute();
        if (BootstrapAdministratorEmails.Any(address => !email.IsValid(address)))
        {
            yield return new ValidationResult(
                "AccessControl:BootstrapAdministrators virgulle ayrilmis e-posta adreslerinden olusmalidir.",
                [nameof(BootstrapAdministrators)]);
        }
    }
}

/// <summary>
/// Eylem yetkisi (ADR-0007 §1, SYG-KMLK-074): hesabin izinleri ve ilk sistem yoneticisinin
/// atanmasi.
/// </summary>
/// <remarks>
/// <para>
/// Izinler her istekte veritabanindan okunur; erisim jetonunda TASINMAZ. Rolu kaldirilan
/// kullanici jetonunun suresini beklemeden yetkisini kaybeder.
/// </para>
/// <para>
/// <b>Ilk sistem yoneticisi:</b> Rol yonetim ekrani T4'tedir; o zamana kadar sistem
/// yoneticisi kurulum yapilandirmasiyla belirlenir. Listede e-postasi olan kisi giris
/// yaptiginda rol bir kez atanir ve atama denetim izine yazilir. Listeden cikarmak atamayi
/// GERI ALMAZ: rolun kaldirilmasi bilincli bir islemdir (T4).
/// </para>
/// </remarks>
public sealed partial class AccessControlService
{
    private readonly IAccessControlStore _store;
    private readonly AccessControlOptions _options;
    private readonly ILogger<AccessControlService> _logger;

    /// <summary>Yeni ornek olusturur.</summary>
    public AccessControlService(IAccessControlStore store, AccessControlOptions options, ILogger<AccessControlService> logger)
    {
        _store = store;
        _options = options;
        _logger = logger;
    }

    /// <summary>Hesabin izinleri.</summary>
    public async Task<IReadOnlySet<string>> GetPermissionsAsync(long userAccountId, CancellationToken cancellationToken) =>
        (await _store.GetPermissionsAsync(userAccountId, cancellationToken).ConfigureAwait(false)).ToHashSet(StringComparer.Ordinal);

    /// <summary>
    /// E-posta ilk sistem yoneticileri listesindeyse ve hesabin rolu yoksa Sistem Yoneticisi
    /// rolunu atar. Degisiklik cagiranin kaydiyla birlikte yazilir.
    /// </summary>
    /// <returns>Rol bu cagrida atandiysa <c>true</c>.</returns>
    public async Task<bool> EnsureBootstrapAdministratorAsync(long userAccountId, string? email, CancellationToken cancellationToken)
    {
        if (email is null || !_options.BootstrapAdministratorEmails.Contains(email.Trim().ToLowerInvariant()))
        {
            return false;
        }

        var role = await _store.FindRoleAsync(Role.SystemAdministratorCode, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("Sistem Yoneticisi rolu veritabaninda yok; migration uygulanmamis olabilir.");

        if (await _store.HasRoleAsync(userAccountId, role.Id, cancellationToken).ConfigureAwait(false))
        {
            return false;
        }

        _store.Add(UserRole.Assign(userAccountId, role.Id));
        LogBootstrapped(_logger, userAccountId);
        return true;
    }

    [LoggerMessage(EventId = 3900, Level = LogLevel.Warning, Message = "Ilk sistem yoneticisi atandi: hesap {AccountId} (AccessControl:BootstrapAdministrators).")]
    private static partial void LogBootstrapped(ILogger logger, long accountId);
}
