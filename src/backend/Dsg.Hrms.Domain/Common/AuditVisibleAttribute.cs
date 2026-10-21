namespace Dsg.Hrms.Domain.Common;

/// <summary>
/// Isaretlendigi ozellik denetim izinde ve gunlukte <b>bilerek acik</b> yazilir; gerekce zorunludur.
/// </summary>
/// <remarks>
/// <para>
/// Varlik alanlarinin siniflandirmasi zorunludur (#244, tasarim gozden gecirmesi B-02): her metin
/// ve tarih alani ya <see cref="PersonalDataAttribute"/>, <see cref="SpecialCategoryDataAttribute"/>,
/// <see cref="SecretAttribute"/> ya da bu oznitelikle isaretlenir. Isaretsiz alan mimari testini
/// dusurur; boylece yeni bir kisisel veri alani "unutuldugu icin" duz metin yazilmaz, acik yazilan
/// her alan da bir karar ve gerekceyle acik yazilir.
/// </para>
/// <para>
/// Ad ve soyad gibi kisisel veri tasiyan ama denetimde okunur olmasi gereken alanlar da bu
/// oznitelikle isaretlenir; gerekce kararin kendisidir. Ad tabanli hassas alanlara (TCKN, e-posta,
/// telefon) bu oznitelik verilemez; mimari testi bunu da denetler.
/// </para>
/// </remarks>
/// <param name="reason">Alanin neden acik yazildigi.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class AuditVisibleAttribute(string reason) : Attribute
{
    /// <summary>Alanin neden acik yazildigi.</summary>
    public string Reason { get; } = reason;
}
