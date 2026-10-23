using Dsg.Hrms.Domain.Common;

namespace Dsg.Hrms.Domain.Personnel;

/// <summary>
/// Kisinin korumali alanlari: kisi basina bir satir (ADR-0021 §1, ADR-0019 §1; SYG-PERS-054).
/// </summary>
/// <remarks>
/// <para>
/// Korumali alanlar <see cref="Person"/>'da tutulmaz. Orada dursalardi, kisiyi okuyan her kod
/// (Kimlik modulunun hesap listesi ve oturum deposu dahil) bunlari da yuklerdi. Bu tabloya
/// yalnizca kapsamli sorgu bileseni ve senkronizasyon erisir; mimari testi bunu tip
/// duzeyinde denetler.
/// </para>
/// <para>
/// Kaynak LOGO'dur; senkronizasyon yazar. Kan grubu (sifreli, ADR-0020) ayri bir PR'da bu
/// tabloya eklenir.
/// </para>
/// </remarks>
public sealed class PersonSensitive : Entity, IAuditable
{
    private PersonSensitive()
    {
    }

    /// <summary>Kisinin ic kimligi; kisi basina tek satir.</summary>
    public long PersonId { get; private set; }

    /// <summary>Adres (LOGO iletisim turu 1). Kayitli degilse <c>null</c>.</summary>
    [PersonalData(PersonalDataKind.Unspecified)]
    public string? Address { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public long? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }

    /// <summary>Kisi icin bos satir olusturur.</summary>
    public static PersonSensitive For(long personId)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(personId);
        return new PersonSensitive { PersonId = personId };
    }

    /// <summary>Adresi gunceller; degistiyse <c>true</c>. Bos metin "kayitli degil" sayilir.</summary>
    public bool ApplyAddress(string? address)
    {
        var value = string.IsNullOrWhiteSpace(address) ? null : address.Trim();
        if (string.Equals(Address, value, StringComparison.Ordinal))
        {
            return false;
        }

        Address = value;
        return true;
    }
}
