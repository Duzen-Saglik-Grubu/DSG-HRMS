using Dsg.Hrms.Domain.Common;

namespace Dsg.Hrms.Domain.Personnel;

/// <summary>
/// "IK'ya bildir" duzeltme talebi (PG-PERS-37; ADR-0022 §3; SYG-PERS-045…049).
/// </summary>
/// <remarks>
/// <para>
/// Kisi kendi kartinda yanlis gordugu bir alanin dogru degerini bildirir; IK duzeltmeyi LOGO'da
/// yapar ve talebi kapatir. HRMS'te LOGO alani duzenlenmez (PG-PERS-03); talep yalnizca bilgiyi
/// IK'ya kayitli olarak ulastirir.
/// </para>
/// <para>
/// Onerilen deger yalnizca IK'nin LOGO'yu duzeltmesi icin tutulur ve talep kapaninca
/// <b>silinir</b> (<c>KR-126</c>). Kan grubu icin deger sifrelidir (ADR-0020): bu varlik yalnizca
/// saklanan bicimi tasir, sifrelemeyi depo yapar.
/// </para>
/// </remarks>
public sealed class CorrectionRequest : Entity, IAuditable
{
    /// <summary>Adres ve egitimin en buyuk uzunlugu (SYG-PERS-045).</summary>
    public const int MaxTextLength = 500;

    private CorrectionRequest()
    {
    }

    /// <summary>Talebi acan kisinin ic kimligi.</summary>
    public long PersonId { get; private set; }

    /// <summary>Bildirilen alan.</summary>
    public CorrectionField Field { get; private set; }

    /// <summary>
    /// Onerilen deger (saklanan bicim: kan grubunda sifreli). Talep kapaninca <c>null</c>.
    /// Denetim izine degeri yazilmaz.
    /// </summary>
    [PersonalData(PersonalDataKind.Unspecified)]
    public string? ProposedValue { get; private set; }

    /// <summary>Durum.</summary>
    public CorrectionStatus Status { get; private set; }

    /// <summary>Gonderim ani (UTC).</summary>
    public DateTimeOffset SubmittedAt { get; private set; }

    /// <summary>Kapatilma ani (UTC); acik talepte <c>null</c>.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    /// <summary>Kapatan kullanici hesabi; acik talepte <c>null</c>.</summary>
    public long? CompletedBy { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public long? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }

    /// <summary>
    /// Yeni talep. Deger <see cref="CorrectionValue.Normalize"/> ile dogrulanmis kanonik bicimde
    /// olmalidir.
    /// </summary>
    public static CorrectionRequest Submit(long personId, CorrectionField field, string value, DateTimeOffset now)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(personId);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        return new CorrectionRequest
        {
            PersonId = personId,
            Field = field,
            ProposedValue = value,
            Status = CorrectionStatus.New,
            SubmittedAt = now,
        };
    }

    /// <summary>
    /// Saklanan degeri degistirir; depo kan grubunu sifrelemek icin kullanir. Talebin dis kimligi
    /// sifrelemeye baglandigi icin deger ancak talep olustuktan sonra sifrelenebilir.
    /// </summary>
    public void ProtectValue(Func<string, string> protect)
    {
        ArgumentNullException.ThrowIfNull(protect);

        if (ProposedValue is not null)
        {
            ProposedValue = protect(ProposedValue);
        }
    }

    /// <summary>
    /// Talebi "Tamamlandi" olarak kapatir ve onerilen degeri siler (SYG-PERS-048). Kapali talep
    /// yeniden kapatilamaz.
    /// </summary>
    public void Complete(long? completedBy, DateTimeOffset now)
    {
        if (Status == CorrectionStatus.Completed)
        {
            throw new InvalidOperationException("Talep zaten tamamlandı.");
        }

        Status = CorrectionStatus.Completed;
        ProposedValue = null;
        CompletedAt = now;
        CompletedBy = completedBy;
    }
}

/// <summary>"IK'ya bildir" ile bildirilebilen alanlar (PG-PERS-37).</summary>
public enum CorrectionField
{
    /// <summary>Cep telefonu.</summary>
    MobilePhone = 1,

    /// <summary>E-posta.</summary>
    Email = 2,

    /// <summary>Adres.</summary>
    Address = 3,

    /// <summary>Egitim.</summary>
    Education = 4,

    /// <summary>Medeni hal.</summary>
    MaritalStatus = 5,

    /// <summary>Kan grubu (ozel nitelikli; sifreli saklanir).</summary>
    BloodGroup = 6,
}

/// <summary>Duzeltme talebinin durumu.</summary>
public enum CorrectionStatus
{
    /// <summary>IK'nin islemesini bekliyor.</summary>
    New = 1,

    /// <summary>IK duzeltmeyi LOGO'da yapti ve talebi kapatti.</summary>
    Completed = 2,
}

/// <summary>Onerilen degerin alana gore dogrulanmasi (SYG-PERS-045).</summary>
public static class CorrectionValue
{
    /// <summary>Medeni halin kanonik degerleri.</summary>
    public const string Married = "married";

    /// <summary>Bekar.</summary>
    public const string NotMarried = "notMarried";

    /// <summary>Kan grubu kodunun alt ve ust siniri (referans veri, <c>KR-129</c>).</summary>
    public const int MinBloodGroupCode = 1;

    /// <summary>Ust sinir.</summary>
    public const int MaxBloodGroupCode = 8;

    /// <summary>
    /// Degeri dogrular ve kanonik bicime getirir; gecersizse <c>null</c> ve neden doner.
    /// </summary>
    /// <remarks>
    /// Telefon SYG-KMLK-009 kuralina gore 10 haneli <c>5XXXXXXXXX</c>, e-posta kucuk harfli ve
    /// bosluksuz, medeni hal <see cref="Married"/> veya <see cref="NotMarried"/>, kan grubu
    /// <c>1</c>–<c>8</c> kodu olarak saklanir. Adres ve egitim kirpilir; satir sonlari korunur.
    /// </remarks>
    public static (string? Value, string? Error) Normalize(CorrectionField field, string? input)
    {
        var raw = input?.Trim();
        if (string.IsNullOrEmpty(raw))
        {
            return (null, "Doğru değeri yazın.");
        }

        return field switch
        {
            CorrectionField.MobilePhone => ContactNormalizer.NormalizeMobilePhone(raw) is { } phone
                ? (phone, null)
                : (null, "Cep telefonu 5 ile başlayan 10 haneli olmalıdır (örneğin 532 123 45 67)."),
            CorrectionField.Email => IsEmail(ContactNormalizer.NormalizeEmail(raw)) is { } email
                ? (email, null)
                : (null, "Geçerli bir e-posta adresi yazın."),
            CorrectionField.Address or CorrectionField.Education => raw.Length <= CorrectionRequest.MaxTextLength
                ? (raw, null)
                : (null, $"En fazla {CorrectionRequest.MaxTextLength} karakter yazılabilir."),
            CorrectionField.MaritalStatus => raw is Married or NotMarried
                ? (raw, null)
                : (null, "Medeni hâl Evli veya Bekar olmalıdır."),
            CorrectionField.BloodGroup => int.TryParse(raw, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var code)
                && code is >= MinBloodGroupCode and <= MaxBloodGroupCode
                ? (code.ToString(System.Globalization.CultureInfo.InvariantCulture), null)
                : (null, "Kan grubu listeden seçilmelidir."),
            _ => (null, "Bu alan bildirilemez."),
        };
    }

    // ContactNormalizer yalnizca tek "@" arar; alan adinda en az bir nokta ve makul uzunluk da istenir.
    private static string? IsEmail(string? value)
    {
        if (value is null || value.Length > 254)
        {
            return null;
        }

        var domain = value[(value.IndexOf('@', StringComparison.Ordinal) + 1)..];
        return domain.Contains('.', StringComparison.Ordinal) && !domain.StartsWith('.') && !domain.EndsWith('.')
            ? value
            : null;
    }
}
