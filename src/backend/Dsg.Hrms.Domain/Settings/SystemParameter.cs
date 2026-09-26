using Dsg.Hrms.Domain.Common;

namespace Dsg.Hrms.Domain.Settings;

/// <summary>
/// Sistem parametresinin veritabaninda saklanan degeri (SYG-KMLK-075).
/// </summary>
/// <remarks>
/// <para>
/// Parametrenin turu, gecerli araligi ve varsayilani burada DEGIL, uygulama
/// katmanindaki katalogdadir. Bu tablo yalnizca yoneticinin degistirdigi degerleri
/// tutar; satiri olmayan parametre varsayilan degerle calisir.
/// </para>
/// <para>
/// <b>Sir parametreler</b> <see cref="ProtectedValue"/> alaninda sifreli saklanir,
/// <see cref="Value"/> bos kalir. Alan adi bilincli secildi: denetim izinde ad tabanli
/// maskeleme kurali bu alani HIC yazmaz (<c>KR-071</c>). Domain katmani uygulama
/// katmanindaki <c>[Secret]</c> ozniteligini goremedigi icin dayanak bu addir.
/// </para>
/// </remarks>
public sealed class SystemParameter : Entity, IAuditable
{
    private SystemParameter()
    {
    }

    /// <summary>Katalog kimligi (orn. <c>PRM-KML-03</c>). Tekildir.</summary>
    public string Key { get; private set; } = string.Empty;

    /// <summary>Sir olmayan parametrenin kanonik degeri.</summary>
    public string? Value { get; private set; }

    /// <summary>Sir parametrenin sifreli degeri.</summary>
    public string? ProtectedValue { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public long? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }

    /// <summary>Yeni parametre satiri olusturur.</summary>
    public static SystemParameter Create(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return new SystemParameter { Key = key };
    }

    /// <summary>Sir olmayan degeri yazar. Deger degistiyse <c>true</c> doner.</summary>
    public bool SetValue(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (string.Equals(Value, value, StringComparison.Ordinal) && ProtectedValue is null)
        {
            return false;
        }

        Value = value;
        ProtectedValue = null;
        return true;
    }

    /// <summary>Sir parametrenin sifreli degerini yazar.</summary>
    /// <remarks>
    /// Her sifreleme farkli bir cikti uretir (rastgele nonce); ayni parolanin yeniden
    /// girilmesi de degisiklik sayilir. Denetim izinde yalnizca "degisti" gorunur.
    /// </remarks>
    public void SetProtectedValue(string protectedValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(protectedValue);

        ProtectedValue = protectedValue;
        Value = null;
    }
}
