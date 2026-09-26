namespace Dsg.Hrms.Infrastructure.Logo;

/// <summary>
/// Senkronizasyonun bekledigi LOGO semasi: tablo, kolon ve veri tipleri (SYG-KMLK-012).
/// </summary>
/// <remarks>
/// Tipler 26.09.2026'da canli veritabaninin <c>INFORMATION_SCHEMA</c> gorunumunden
/// okunmustur (SQL Server 2019). LOGO surum yukseltmesi bunlardan birini degistirirse
/// senkronizasyon baslamaz ve sapma kayda gecer (<c>KR-049</c>).
/// </remarks>
public static class LogoSchema
{
    /// <summary>Personel kartlari. Yedi firmanin personeli bu tek tablodadir (ADR-0003 §3).</summary>
    public const string PersonTable = "LH_001_PERSON";

    /// <summary>Iletisim kayitlari.</summary>
    public const string ContactTable = "LH_001_CONTACT";

    /// <summary>Firmalar.</summary>
    public const string FirmTable = "L_CAPIFIRM";

    /// <summary>Iletisim turu: cep telefonu.</summary>
    public const short MobilePhoneType = 3;

    /// <summary>Iletisim turu: e-posta.</summary>
    public const short EmailType = 6;

    /// <summary>Senkronizasyonun okudugu tablolar.</summary>
    public static IReadOnlyList<string> Tables { get; } = [PersonTable, ContactTable, FirmTable];

    /// <summary>Beklenen kolonlar ve veri tipleri.</summary>
    public static IReadOnlyList<ExpectedColumn> Columns { get; } =
    [
        new(PersonTable, "LREF", "int"),
        new(PersonTable, "CODE", "varchar"),
        new(PersonTable, "TTFNO", "varchar"),
        new(PersonTable, "NAME", "varchar"),
        new(PersonTable, "SURNAME", "varchar"),
        new(PersonTable, "BIRTHDATE", "datetime"),
        new(PersonTable, "INDATE", "datetime"),
        new(PersonTable, "OUTDATE", "datetime"),
        new(PersonTable, "FIRMNR", "smallint"),
        new(ContactTable, "LREF", "int"),
        new(ContactTable, "CARDREF", "int"),
        new(ContactTable, "TYP", "smallint"),
        new(ContactTable, "EXP1", "varchar"),
        new(FirmTable, "NR", "smallint"),
        new(FirmTable, "NAME", "varchar"),
    ];
}

/// <summary>Beklenen bir kolon.</summary>
/// <param name="Table">Tablo adi.</param>
/// <param name="Column">Kolon adi.</param>
/// <param name="DataType">SQL Server veri tipi (<c>INFORMATION_SCHEMA.COLUMNS.DATA_TYPE</c>).</param>
public sealed record ExpectedColumn(string Table, string Column, string DataType);
