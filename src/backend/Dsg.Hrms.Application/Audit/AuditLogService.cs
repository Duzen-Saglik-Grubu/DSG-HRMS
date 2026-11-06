using Dsg.Hrms.Application.Common.Abstractions;
using Dsg.Hrms.Application.Common.Exceptions;
using Dsg.Hrms.Application.Common.Paging;

namespace Dsg.Hrms.Application.Audit;

/// <summary>Denetim kaydi turu (SYG-PERS-069; ADR-0022 §2).</summary>
public enum AuditLogKind
{
    /// <summary>Degisiklik kaydi (denetim izi).</summary>
    Change = 1,

    /// <summary>Erisim kaydi; disa aktarma haric.</summary>
    Access = 2,

    /// <summary>Disa aktarma (erisim kaydinda <c>Export</c>).</summary>
    Export = 3,

    /// <summary>Guvenlik olayi (kimlik olaylari, SYG-KMLK-060).</summary>
    Security = 4,
}

/// <summary>Kayitta adiyla gosterilen kisi.</summary>
/// <param name="PersonId">Kisinin dis kimligi.</param>
/// <param name="FullName">Ad soyad.</param>
public sealed record AuditPerson(Guid PersonId, string FullName);

/// <summary>Denetim kayitlari listesinde bir satir.</summary>
/// <param name="Id">Satir kimligi: tur ve kaydin numarasi (ornek <c>change-42</c>).</param>
/// <param name="Kind">Tur.</param>
/// <param name="OccurredAt">Zaman (UTC).</param>
/// <param name="Action">Islem kodu: degisiklikte <c>Insert/Update/Delete</c>, erisimde erisim turu, guvenlik olayinda olay turu.</param>
/// <param name="Actor">Islemi yapan; arka plan isleminde <c>null</c> ("Sistem").</param>
/// <param name="EntityName">Kayit turu (ornek <c>Person</c>, <c>EmergencyContact</c>).</param>
/// <param name="EntityId">Kaydin dis kimligi; yoksa <c>null</c>.</param>
/// <param name="Subject">Verisi goruntulenen veya olayin konusu olan kisi; yoksa <c>null</c>.</param>
/// <param name="RecordCount">Erisilen kayit sayisi (erisim ve disa aktarma); digerlerinde <c>null</c>.</param>
/// <param name="SubjectCount">Listede veya dosyada verisi gosterilen kisi sayisi (SYG-PERS-058).</param>
/// <param name="Details">Kayittaki ayrinti, kayitta nasilsa oyle: maskeli alanlar, maskeli suzgecler veya olay kodu.</param>
/// <param name="TraceId">Uygulama gunlugu ile iliskilendirme kimligi.</param>
/// <param name="IpAddress">Istegin geldigi IP adresi.</param>
public sealed record AuditLogItem(
    string Id,
    AuditLogKind Kind,
    DateTimeOffset OccurredAt,
    string Action,
    AuditPerson? Actor,
    string EntityName,
    Guid? EntityId,
    AuditPerson? Subject,
    int? RecordCount,
    int SubjectCount,
    string? Details,
    string? TraceId,
    string? IpAddress);

/// <summary>Liste istegi.</summary>
/// <param name="From">Ilk gun (Europe/Istanbul), dahil.</param>
/// <param name="To">Son gun (Europe/Istanbul), dahil.</param>
/// <param name="Kind">Tur; <c>null</c> ise dordu.</param>
/// <param name="ActorPersonId">Islemi yapan kisi.</param>
/// <param name="SubjectPersonId">Verisi goruntulenen kisi.</param>
/// <param name="Page">Sayfa (1'den baslar).</param>
/// <param name="PageSize">Sayfa boyutu.</param>
public sealed record AuditLogRequest(
    DateOnly? From,
    DateOnly? To,
    AuditLogKind? Kind,
    Guid? ActorPersonId,
    Guid? SubjectPersonId,
    int Page,
    int PageSize);

/// <summary>Sorgunun aldigi olcutler: gunler UTC anlara cevrilmistir.</summary>
/// <param name="FromInstant">Baslangic ani, dahil.</param>
/// <param name="ToInstant">Bitis ani, haric.</param>
/// <param name="Kind">Tur.</param>
/// <param name="ActorPersonId">Islemi yapan kisi.</param>
/// <param name="SubjectPersonId">Verisi goruntulenen kisi.</param>
/// <param name="Page">Sayfa.</param>
/// <param name="PageSize">Sayfa boyutu.</param>
public sealed record AuditLogCriteria(
    DateTimeOffset? FromInstant,
    DateTimeOffset? ToInstant,
    AuditLogKind? Kind,
    Guid? ActorPersonId,
    Guid? SubjectPersonId,
    int Page,
    int PageSize);

/// <summary>Denetim kayitlarini okur. Kayitlara yalnizca okuma yoluyla erisir (<c>KR-118</c>).</summary>
public interface IAuditLogQuery
{
    /// <summary>Dort kaynagi tek, yeniden eskiye sirali, sayfali liste olarak doner.</summary>
    Task<PagedResult<AuditLogItem>> ListAsync(AuditLogCriteria criteria, CancellationToken cancellationToken);
}

/// <summary>
/// "Denetim kayitlari" ekrani (PG-PERS-28, 30; SYG-PERS-069; ADR-0022 §2). Yalnizca
/// <c>audit.log.view</c> izniyle cagrilir; izin uctadir.
/// </summary>
/// <remarks>
/// <para>
/// Salt okunurdur: kayitlari degistiren bir yol yoktur. Uygulama rolu <c>audit</c> semasinda
/// zaten yalnizca okuma ve ekleme yetkisine sahiptir (<c>KR-118</c>).
/// </para>
/// <para>
/// Maskeli degerler maskeli kalir; ayrinti kayitta nasilsa oyle doner. Ekrani goruntulemek de
/// erisim kaydina yazilir (<c>List</c>, suzgeclerle; AN-25).
/// </para>
/// </remarks>
public sealed class AuditLogService
{
    /// <summary>Erisim kaydindaki veri turu.</summary>
    public const string EntityName = "AuditLog";

    /// <summary>
    /// Sayfa x sayfa boyutunun ust siniri. Her kaynaktan bu kadar satir okunup birlestirilir; daha
    /// eski kayitlar tarih araligi daraltilarak bulunur (AN-25).
    /// </summary>
    public const int MaxDepth = 10_000;

    private readonly IAuditLogQuery _query;
    private readonly IAccessLogger _accessLogger;

    /// <summary>Yeni ornek olusturur.</summary>
    public AuditLogService(IAuditLogQuery query, IAccessLogger accessLogger)
    {
        _query = query;
        _accessLogger = accessLogger;
    }

    /// <summary>Kayitlari listeler ve goruntulemeyi erisim kaydina yazar.</summary>
    public async Task<PagedResult<AuditLogItem>> ListAsync(AuditLogRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Page < 1 || request.PageSize < 1 || request.PageSize > PagedResult<AuditLogItem>.MaxPageSize)
        {
            throw new InvalidRequestException($"Sayfa 1 veya büyük, sayfa boyutu 1–{PagedResult<AuditLogItem>.MaxPageSize} olmalıdır.");
        }

        if (request.From is { } from && request.To is { } to && from > to)
        {
            throw new InvalidRequestException("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
        }

        if ((long)request.Page * request.PageSize > MaxDepth)
        {
            throw new InvalidRequestException("Daha eski kayıtlar için tarih aralığını daraltın.");
        }

        var result = await _query.ListAsync(
            new AuditLogCriteria(
                request.From is { } start ? StartOf(start) : null,
                request.To is { } end ? StartOf(end.AddDays(1)) : null,
                request.Kind,
                request.ActorPersonId,
                request.SubjectPersonId,
                request.Page,
                request.PageSize),
            cancellationToken).ConfigureAwait(false);

        await _accessLogger.LogAsync(
            AccessRecord.List(EntityName, result.Items.Count, new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["from"] = request.From?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                ["to"] = request.To?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                ["type"] = request.Kind?.ToString(),
                ["actor"] = request.ActorPersonId?.ToString(),
                ["subject"] = request.SubjectPersonId?.ToString(),
            }),
            cancellationToken).ConfigureAwait(false);

        return result;
    }

    /// <summary>Gunun basladigi an: Europe/Istanbul gece yarisi (ADR-0021 §2 gun siniri).</summary>
    private static DateTimeOffset StartOf(DateOnly day)
    {
        var local = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, CorporateTime.Zone.GetUtcOffset(local)).ToUniversalTime();
    }
}
