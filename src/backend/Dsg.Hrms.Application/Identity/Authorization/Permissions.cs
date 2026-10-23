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

    /// <summary>
    /// Senkronizasyonun durumunu gorme (SYG-KMLK-072, SYG-PERS-008). T3'te
    /// <c>identity.sync.view</c> idi; senkronizasyon personel modulunundur (SYG-PERS-009).
    /// </summary>
    public const string SyncView = "personnel.sync.view";

    /// <summary>Senkronizasyonu elle baslatma (SYG-KMLK-072, SYG-PERS-009); T3'te <c>identity.sync.create</c>.</summary>
    public const string SyncCreate = "personnel.sync.create";

    /// <summary>Tum personeli gorme; kapsam <c>All</c> (ADR-0019 §1, SYG-PERS-050).</summary>
    public const string PersonView = "personnel.person.view";

    /// <summary>Tam TCKN'yi gorme ve TCKN ile arama (SYG-PERS-030, 053).</summary>
    public const string PersonViewNationalId = "personnel.person.view_national_id";

    /// <summary>Adres, kan grubu, acil durum kisileri ve evlilik bilgisini gorme (SYG-PERS-052).</summary>
    public const string PersonViewSensitive = "personnel.person.view_sensitive";

    /// <summary>HRMS'te tutulan alanlari girme: gorev, evlilik bilgisi, acil durum kisisi (KR-112).</summary>
    public const string PersonUpdate = "personnel.person.update";

    /// <summary>Personel listesini disa aktarma (SYG-PERS-032).</summary>
    public const string PersonExport = "personnel.person.export";

    /// <summary>"IK'ya bildir" taleplerini gorme (SYG-PERS-048).</summary>
    public const string CorrectionView = "personnel.correction.view";

    /// <summary>"IK'ya bildir" taleplerini kapatma (SYG-PERS-048).</summary>
    public const string CorrectionUpdate = "personnel.correction.update";

    /// <summary>Veri kalitesi raporunu gorme (SYG-PERS-070).</summary>
    public const string QualityView = "personnel.quality.view";

    /// <summary>Veri kalitesi raporunu disa aktarma (SYG-PERS-070).</summary>
    public const string QualityExport = "personnel.quality.export";

    /// <summary>Referans tanimlarini (unvan, gorev) gorme (SYG-PERS-072).</summary>
    public const string ReferenceView = "reference.definition.view";

    /// <summary>Referans tanimlarini ekleme, adini degistirme, pasife alma (SYG-PERS-072).</summary>
    public const string ReferenceUpdate = "reference.definition.update";

    /// <summary>Denetim kayitlarini gorme (SYG-PERS-069; KR-119, KR-128).</summary>
    public const string AuditLogView = "audit.log.view";

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
        [PersonView] = "Tüm personeli görme",
        [PersonViewNationalId] = "Tam T.C. kimlik numarasını görme",
        [PersonViewSensitive] = "Adres, kan grubu, acil durum kişileri ve evlilik bilgisini görme",
        [PersonUpdate] = "Görev, evlilik bilgisi ve acil durum kişisini girme",
        [PersonExport] = "Personel listesini dışa aktarma",
        [CorrectionView] = "Düzeltme taleplerini görme",
        [CorrectionUpdate] = "Düzeltme taleplerini kapatma",
        [QualityView] = "Veri kalitesi raporunu görme",
        [QualityExport] = "Veri kalitesi raporunu dışa aktarma",
        [ReferenceView] = "Ünvan ve görev tanımlarını görme",
        [ReferenceUpdate] = "Ünvan ve görev tanımlarını değiştirme",
        [AuditLogView] = "Denetim kayıtlarını görme",
    };

    /// <summary>Hazir rollerin izinleri (SYG-KMLK-074).</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ByRole = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
    {
        [Role.SystemAdministratorCode] = [.. All.Keys],

        // Kapsam T4'e kadar tum personeldir. Elle senkronizasyon (KR-126), denetim kayitlari ve
        // parametreler (KR-128) IK'da da vardir (SYG-PERS-055). Liste bilincli olarak acik
        // yazilir: yeni bir izin IK'ya kendiliginden gecmez. Rol ve izin atama ekrani T4'tedir.
        [Role.HrIdentityOperationsCode] =
        [
            AccountView, AccountUpdate, InviteCreate, SyncView, SyncCreate, ParameterView, ParameterUpdate,
            PersonView, PersonViewNationalId, PersonViewSensitive, PersonUpdate, PersonExport,
            CorrectionView, CorrectionUpdate, QualityView, QualityExport, ReferenceView, ReferenceUpdate,
            AuditLogView,
        ],
    };
}
