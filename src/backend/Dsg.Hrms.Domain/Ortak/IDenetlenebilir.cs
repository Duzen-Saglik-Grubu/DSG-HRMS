namespace Dsg.Hrms.Domain.Ortak;

/// <summary>
/// Olusturma ve guncelleme bilgisi tutulan varliklari isaretler (ADR-0004 §4).
/// </summary>
/// <remarks>
/// Bu alanlar <c>SaveChanges</c> ara katmaninda OTOMATIK doldurulur; serviste
/// elle yazilmaz. Elle yazim, unutulmaya ve tutarsizliga acik olurdu.
/// </remarks>
public interface IDenetlenebilir
{
    /// <summary>Olusturma ani (UTC).</summary>
    DateTimeOffset OlusturmaAni { get; set; }

    /// <summary>Olusturan kullanici hesabinin kimligi. Sistem islemlerinde <c>null</c>.</summary>
    long? OlusturanKullaniciId { get; set; }

    /// <summary>Son guncelleme ani (UTC). Hic guncellenmediyse <c>null</c>.</summary>
    DateTimeOffset? GuncellemeAni { get; set; }

    /// <summary>Son guncelleyen kullanici hesabinin kimligi.</summary>
    long? GuncelleyenKullaniciId { get; set; }
}
