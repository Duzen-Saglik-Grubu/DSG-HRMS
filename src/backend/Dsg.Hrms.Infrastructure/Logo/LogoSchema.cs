namespace Dsg.Hrms.Infrastructure.Logo;

/// <summary>
/// Senkronizasyonun bekledigi LOGO semasi: tablo, kolon ve veri tipleri (SYG-KMLK-012,
/// SYG-PERS-011).
/// </summary>
/// <remarks>
/// <para>
/// Tipler canli veritabaninin <c>INFORMATION_SCHEMA</c> gorunumunden okunmustur (SQL Server
/// 2019; T3 alanlari 26.09.2026, T1 alanlari 23.10.2026). LOGO surum yukseltmesi bunlardan
/// birini degistirirse senkronizasyon baslamaz ve sapma kayda gecer (<c>KR-049</c>).
/// </para>
/// <para>
/// <b>Okunan alanlarin tek listesidir</b> (SYG-PERS-003). Din (<c>RELIGION</c>) ve saglik
/// tablolari (<c>LH_001_HEALTH</c>) listede YOKTUR ve olamaz (SYG-PERS-004); bunu
/// <c>LogoFieldScopeTests</c> denetler.
/// </para>
/// </remarks>
public static class LogoSchema
{
    /// <summary>Personel kartlari. Yedi firmanin personeli bu tek tablodadir (ADR-0003 §3).</summary>
    public const string PersonTable = "LH_001_PERSON";

    /// <summary>Iletisim kayitlari.</summary>
    public const string ContactTable = "LH_001_CONTACT";

    /// <summary>Firmalar.</summary>
    public const string FirmTable = "L_CAPIFIRM";

    /// <summary>Aile ve resim; her kartin yakinlik 0 olan tek satiri kisinin kendisidir.</summary>
    public const string FamilyTable = "LH_001_FAMILY";

    /// <summary>Kimlik ek bilgileri (onceki soyadi; kan grubu sifreleme PR'inda).</summary>
    public const string IdentityInfoTable = "LH_001_PERIDINF";

    /// <summary>Tanim tablosu (egitim TYP = -5; SGK meslek TYP = -44).</summary>
    public const string TypeDefinitionTable = "LH_001_TYPEDEF";

    /// <summary>Subeler (isyerleri).</summary>
    public const string DivisionTable = "L_CAPIDIV";

    /// <summary>Birimler.</summary>
    public const string DepartmentTable = "L_CAPIDEPT";

    /// <summary>Iletisim turu: adres.</summary>
    public const short AddressType = 1;

    /// <summary>Iletisim turu: cep telefonu.</summary>
    public const short MobilePhoneType = 3;

    /// <summary>Iletisim turu: e-posta.</summary>
    public const short EmailType = 6;

    /// <summary>Aile satirinda kisinin kendisi (yakinlik 0).</summary>
    public const short SelfRelation = 0;

    /// <summary>Tanim turu: egitim.</summary>
    public const short EducationDefinition = -5;

    /// <summary>Okunan tablolar; yetki ve sema denetimi bu listeyle yapilir.</summary>
    public static IReadOnlyList<string> Tables { get; } =
        [PersonTable, ContactTable, FirmTable, FamilyTable, IdentityInfoTable, TypeDefinitionTable, DivisionTable, DepartmentTable];

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
        new(PersonTable, "SEX", "smallint"),
        new(PersonTable, "EDUCATION", "smallint"),
        new(ContactTable, "LREF", "int"),
        new(ContactTable, "CARDREF", "int"),
        new(ContactTable, "TYP", "smallint"),
        new(ContactTable, "EXP1", "varchar"),
        new(ContactTable, "EXP2", "varchar"),
        new(FirmTable, "NR", "smallint"),
        new(FirmTable, "NAME", "varchar"),
        new(FamilyTable, "LREF", "int"),
        new(FamilyTable, "PERREF", "int"),
        new(FamilyTable, "IDREF", "int"),
        new(FamilyTable, "RELATION", "smallint"),
        new(FamilyTable, "STATUS", "smallint"),
        new(IdentityInfoTable, "LREF", "int"),
        new(IdentityInfoTable, "EXSURNAME", "varchar"),
        new(TypeDefinitionTable, "LREF", "int"),
        new(TypeDefinitionTable, "TYP", "smallint"),
        new(TypeDefinitionTable, "NR", "smallint"),
        new(TypeDefinitionTable, "EXP", "varchar"),
        new(DivisionTable, "LOGICALREF", "int"),
        new(DivisionTable, "FIRMNR", "smallint"),
        new(DivisionTable, "NR", "smallint"),
        new(DivisionTable, "NAME", "varchar"),
        new(DivisionTable, "CITY", "varchar"),
        new(DepartmentTable, "LOGICALREF", "int"),
        new(DepartmentTable, "FIRMNR", "smallint"),
        new(DepartmentTable, "NR", "smallint"),
        new(DepartmentTable, "NAME", "varchar"),
    ];
}

/// <summary>Beklenen bir kolon.</summary>
/// <param name="Table">Tablo adi.</param>
/// <param name="Column">Kolon adi.</param>
/// <param name="DataType"><c>INFORMATION_SCHEMA</c> veri tipi.</param>
public sealed record ExpectedColumn(string Table, string Column, string DataType);
