using Dsg.Hrms.Domain.Audit;

namespace Dsg.Hrms.Application.Common.Abstractions;

/// <summary>
/// Kisisel veriye erisimi kaydeder (ADR-0009 §3).
/// </summary>
/// <remarks>
/// <para>
/// Degisiklik kaydinin aksine erisim kaydi <b>otomatik uretilemez</b>: bir sorgunun
/// kisisel veri doneip donmedigini ve kimin adina calistigini yalnizca kullanim
/// senaryosu bilir. Bu nedenle cagri acikca yapilir.
/// </para>
/// <para>
/// Cagrinin unutulmasi riski, kod inceleme kontrol listesi ve uc nokta bazli
/// denetimle karsilanir; ayrintili kural <c>CONTRIBUTING.md</c> §3.4'tedir.
/// </para>
/// </remarks>
public interface IAccessLogger
{
    /// <summary>
    /// Erisimi kaydeder.
    /// </summary>
    /// <remarks>
    /// Kayit yazilamazsa cagri <b>hata firlatir</b> ve veri sunulmaz; gerekce
    /// <see cref="AccessRecord"/> belgesindedir.
    /// </remarks>
    Task LogAsync(AccessRecord record, CancellationToken cancellationToken = default);
}

/// <summary>
/// Kaydedilecek erisim olayi.
/// </summary>
/// <remarks>
/// <para>
/// <b>Kayit yazilamazsa veri sunulmaz (fail-closed).</b> Bu bilincli bir tercihtir:
/// kaydedilemeyen bir erisim, KVKK acisindan gerceklesmemis sayilmaz — yalnizca
/// izlenemez hâle gelir. Maliyeti dusuktur cunku erisim kaydi is verisiyle ayni
/// veritabanindadir: o veritabani erisilemez durumdaysa sorgunun kendisi de zaten
/// calismaz.
/// </para>
/// <para>
/// Nesne dogrudan kurulmaz; asagidaki uretici metotlar kullanilir. Boylece her
/// erisim turu icin anlamli alanlarin doldurulmasi garanti altina alinir.
/// </para>
/// </remarks>
/// <param name="AccessType">Erisim turu.</param>
/// <param name="EntityName">Erisilen veri turu.</param>
/// <param name="EntityId">Tek kayda erisimde kaydin dis kimligi.</param>
/// <param name="RecordCount">Erisilen kayit sayisi.</param>
/// <param name="Filters">
/// Uygulanan suzgecler. Degerler kaydedilirken maskelenir (KR-059).
/// </param>
public sealed record AccessRecord(
    AccessType AccessType,
    string EntityName,
    Guid? EntityId,
    int RecordCount,
    IReadOnlyDictionary<string, string?>? Filters)
{
    /// <summary>Tek bir kaydin goruntulenmesi.</summary>
    public static AccessRecord View(string entityName, Guid entityId) =>
        new(AccessType.View, entityName, entityId, 1, null);

    /// <summary>
    /// Ozel nitelikli kisisel verinin goruntulenmesi (KVKK md. 6).
    /// </summary>
    public static AccessRecord SpecialCategoryView(string entityName, Guid entityId) =>
        new(AccessType.SpecialCategoryView, entityName, entityId, 1, null);

    /// <summary>Birden fazla kaydin listelenmesi.</summary>
    public static AccessRecord List(
        string entityName,
        int recordCount,
        IReadOnlyDictionary<string, string?>? filters = null) =>
        new(AccessType.List, entityName, null, recordCount, filters);

    /// <summary>
    /// Verinin disariya aktarilmasi (Excel, PDF).
    /// </summary>
    /// <remarks>
    /// Kayit sayisi zorunludur: bir sizinti incelemesinin ilk sorusu "kac kisinin
    /// verisi disari cikti" sorusudur.
    /// </remarks>
    public static AccessRecord Export(
        string entityName,
        int recordCount,
        IReadOnlyDictionary<string, string?>? filters = null) =>
        new(AccessType.Export, entityName, null, recordCount, filters);

    /// <summary>Toplu rapor uretimi.</summary>
    public static AccessRecord Report(
        string reportName,
        int recordCount,
        IReadOnlyDictionary<string, string?>? filters = null) =>
        new(AccessType.Report, reportName, null, recordCount, filters);

    /// <summary>Ozluk dosyasi ekinin indirilmesi.</summary>
    public static AccessRecord FileDownload(string entityName, Guid entityId) =>
        new(AccessType.FileDownload, entityName, entityId, 1, null);
}
