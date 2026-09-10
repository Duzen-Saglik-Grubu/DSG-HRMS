namespace Dsg.Hrms.Domain.Common;

/// <summary>
/// Yumusak silme (soft delete) uygulanan varliklari isaretler (ADR-0004 §5).
/// </summary>
/// <remarks>
/// <para>
/// Is kayitlari fiziksel olarak SILINMEZ; isaretlenir. Gerekce: Insan Kaynaklari
/// verisinde gecmise donuk denetim ve raporlama ihtiyaci vardir. Silinen bir izin
/// kaydinin hic var olmamis gibi davranmasi, bakiye gecmisini aciklanamaz kilar.
/// </para>
/// <para>
/// Gercek fiziksel silme yalnizca KVKK imha sureci kapsaminda ve kayit altina
/// alinarak yapilir (<c>KR-023</c>).
/// </para>
/// </remarks>
public interface ISoftDeletable
{
    /// <summary>Silinme ani (UTC). Silinmemisse <c>null</c>.</summary>
    DateTimeOffset? DeletedAt { get; set; }

    /// <summary>Silen kullanici hesabinin kimligi.</summary>
    long? DeletedBy { get; set; }
}
