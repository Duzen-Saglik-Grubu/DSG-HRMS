using Dsg.Hrms.Domain.Common;

namespace Dsg.Hrms.Domain.Personnel;

/// <summary>
/// Kisinin fotografi: LOGO'daki goruntunun kucultulmus JPEG hali (ADR-0021 §5; SYG-PERS-014).
/// Kisi basina bir satir.
/// </summary>
/// <remarks>
/// <para>
/// Kaynak LOGO'dur; yalnizca senkronizasyon yazar. Personelin fotograf yuklemesi icin ekran ve uc
/// yoktur (SYG-PERS-040). Orijinal saklanmaz; LOGO'da silinen fotografin satiri da silinir.
/// </para>
/// <para>
/// <see cref="SourceHash"/> LOGO'daki ORIJINAL goruntunun SHA-256 ozetidir; kucuk halin degil.
/// Senkronizasyon her calismada yalnizca ozeti okur ve goruntuyu ozet degistiyse alir
/// (PG-PERS-41). Ayni ozet tarayici onbellegi icin ETag olarak da kullanilir (SYG-PERS-041).
/// </para>
/// </remarks>
public sealed class PersonPhoto : Entity, IAuditable
{
    /// <summary>Ozetin uzunlugu: SHA-256'nin buyuk harfli onaltilik yazimi.</summary>
    public const int SourceHashLength = 64;

    private PersonPhoto()
    {
    }

    /// <summary>Kisinin ic kimligi; kisi basina tek satir.</summary>
    public long PersonId { get; private set; }

    /// <summary>Kucultulmus goruntu (JPEG, uzun kenar en fazla 400 piksel).</summary>
    [PersonalData(PersonalDataKind.Unspecified)]
    public byte[] Content { get; private set; } = [];

    /// <summary>LOGO'daki orijinal goruntunun SHA-256 ozeti (64 onaltilik karakter, buyuk harf).</summary>
    [AuditVisible("Goruntunun ozetidir; goruntuyu geri vermez ve hangi kaynak surumunun aktarildigini gosterir.")]
    public string SourceHash { get; private set; } = string.Empty;

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public long? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }

    /// <summary>Kisinin fotografini olusturur.</summary>
    public static PersonPhoto For(long personId, string sourceHash, byte[] content)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(personId);

        var photo = new PersonPhoto { PersonId = personId };
        photo.Replace(sourceHash, content);
        return photo;
    }

    /// <summary>Goruntuyu kaynagin yeni surumuyle degistirir.</summary>
    public void Replace(string sourceHash, byte[] content)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceHash);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentOutOfRangeException.ThrowIfNotEqual(sourceHash.Length, SourceHashLength, nameof(sourceHash));
        ArgumentOutOfRangeException.ThrowIfZero(content.Length, nameof(content));

        SourceHash = sourceHash.ToUpperInvariant();
        Content = content;
    }
}
