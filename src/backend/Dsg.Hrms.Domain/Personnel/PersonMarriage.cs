using Dsg.Hrms.Domain.Common;

namespace Dsg.Hrms.Domain.Personnel;

/// <summary>
/// Kisinin evlilik tarihi ve evlilik yardimi bilgisi (PG-PERS-19; ADR-0021 §1, §6; SYG-PERS-036).
/// LOGO'da yoktur; HRMS'te IK veya Sistem Yoneticisi girer (<c>KR-112</c>). Kisi basina bir kayit.
/// </summary>
/// <remarks>
/// <para>
/// Korumali alandir: kisi tablosunda degil kendi tablosunda durur ve yalnizca listelenen siniflar
/// bu tipe bagimli olabilir (ADR-0019 §1, §3). Gorunurluk <c>PersonFieldVisibility.CanSeeMarriage</c>
/// kuralidir: yalnizca izin; kisinin kendisi dahil degil.
/// </para>
/// <para>
/// Kayit silinmez, yumusak silinir (ADR-0004 §5, ADR-0021 §6). Butun alanlari kisisel veridir;
/// denetim izinde maskelenir.
/// </para>
/// </remarks>
public sealed class PersonMarriage : Entity, IAuditable, ISoftDeletable
{
    private PersonMarriage()
    {
    }

    /// <summary>Kisinin ic kimligi.</summary>
    public long PersonId { get; private set; }

    /// <summary>Evlilik tarihi.</summary>
    [PersonalData(PersonalDataKind.Unspecified)]
    public DateOnly MarriageDate { get; private set; }

    /// <summary>Evlilik yardimi alindi mi.</summary>
    [PersonalData(PersonalDataKind.Unspecified)]
    public bool AllowanceReceived { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public long? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? DeletedAt { get; set; }

    /// <inheritdoc />
    public long? DeletedBy { get; set; }

    /// <summary>Kisiye evlilik bilgisi girer.</summary>
    public static PersonMarriage For(long personId, MarriageDetails details)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(personId);
        ArgumentNullException.ThrowIfNull(details);

        var marriage = new PersonMarriage { PersonId = personId };
        marriage.Change(details);
        return marriage;
    }

    /// <summary>Bilgileri degistirir; degistiyse <c>true</c>.</summary>
    public bool Change(MarriageDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        if (MarriageDate == details.MarriageDate && AllowanceReceived == details.AllowanceReceived)
        {
            return false;
        }

        MarriageDate = details.MarriageDate;
        AllowanceReceived = details.AllowanceReceived;
        return true;
    }
}

/// <summary>Evlilik bilgisi.</summary>
/// <param name="MarriageDate">Evlilik tarihi.</param>
/// <param name="AllowanceReceived">Evlilik yardimi alindi mi.</param>
public sealed record MarriageDetails(DateOnly MarriageDate, bool AllowanceReceived)
{
    /// <summary>Degerleri yazmaz: kayit bir gunluk iletisine karisirsa kisisel veri dusmez.</summary>
    public override string ToString() => nameof(MarriageDetails);
}
