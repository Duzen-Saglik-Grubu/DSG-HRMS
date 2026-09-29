using Dsg.Hrms.Domain.Identity;

namespace Dsg.Hrms.Application.Identity.Authorization;

/// <summary>
/// Sabit izin listesi (ADR-0007 §1, SYG-KMLK-074). Bicim: <c>&lt;modul&gt;.&lt;kaynak&gt;.&lt;eylem&gt;</c>.
/// </summary>
/// <remarks>
/// <para>
/// Izinler KODDA tanimlidir ve migration ile veritabanina yuklenir; kullanici yeni izin
/// uretemez. Boylece <c>[HasPermission(Permissions.SyncView)]</c> yazildiginda karsiligi
/// garanti edilir. Liste <c>docs/mimari/izin-listesi.md</c> ile birlikte guncellenir.
/// </para>
/// <para>
/// T3 yalnizca kendi izinlerini tanimlar; diger moduller kendi izinlerini ekler.
/// </para>
/// </remarks>
public static class Permissions
{
    /// <summary>Hesap durumunu gorme (SYG-KMLK-073).</summary>
    public const string AccountView = "identity.account.view";

    /// <summary>Hesabi elle pasife alma ve yeniden aktiflestirme (SYG-KMLK-057).</summary>
    public const string AccountUpdate = "identity.account.update";

    /// <summary>Parola olusturma baglantisi gonderme (SYG-KMLK-051).</summary>
    public const string InviteCreate = "identity.invite.create";

    /// <summary>Senkronizasyonun son calisma bilgisini okuma (SYG-KMLK-072).</summary>
    public const string SyncView = "identity.sync.view";

    /// <summary>Senkronizasyonu elle tetikleme (SYG-KMLK-072).</summary>
    public const string SyncCreate = "identity.sync.create";

    /// <summary>Sistem parametrelerini gorme (SYG-KMLK-076).</summary>
    public const string ParameterView = "system.parameter.view";

    /// <summary>Sistem parametrelerini degistirme (SYG-KMLK-076).</summary>
    public const string ParameterUpdate = "system.parameter.update";

    /// <summary>Tum izinler ve aciklamalari.</summary>
    public static readonly IReadOnlyDictionary<string, string> All = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [AccountView] = "Hesap durumunu görme",
        [AccountUpdate] = "Hesabı pasife alma ve yeniden aktifleştirme",
        [InviteCreate] = "Parola oluşturma bağlantısı gönderme",
        [SyncView] = "Senkronizasyon durumunu görme",
        [SyncCreate] = "Senkronizasyonu elle başlatma",
        [ParameterView] = "Sistem parametrelerini görme",
        [ParameterUpdate] = "Sistem parametrelerini değiştirme",
    };

    /// <summary>Hazir rollerin izinleri (SYG-KMLK-074).</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ByRole = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
    {
        [Role.SystemAdministratorCode] = [.. All.Keys],

        // Kapsam T4'e kadar tum personeldir; parametre ve elle senkronizasyon sistem
        // yoneticisinindir.
        [Role.HrIdentityOperationsCode] = [AccountView, AccountUpdate, InviteCreate, SyncView],
    };
}
