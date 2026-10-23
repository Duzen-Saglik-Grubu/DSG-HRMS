using Dsg.Hrms.Infrastructure.Logo;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Tests.Logo;

/// <summary>
/// LOGO'dan yalnizca onayli gereksinimlerin alanlari okunur (SYG-KMLK-006, SYG-PERS-003, 004).
/// </summary>
/// <remarks>
/// Bu test okunan alanlarin incelenmis listesini sabitler. LOGO veri modeline yeni bir kolon
/// eklenirse test duser; alan ancak bir SYG maddesine dayanarak listeye girer. Din ve saglik
/// alanlari hicbir kosulda okunmaz (PG-PERS-36).
/// </remarks>
public sealed class LogoFieldScopeTests
{
    /// <summary>SYG-KMLK-006'nin saydigi alanlarin LOGO karsiliklari; LREF ve CARDREF yalnizca baglantidir.</summary>
    private static readonly string[] Allowed =
    [
        "LH_001_PERSON.LREF",       // baglanti
        "LH_001_PERSON.CODE",       // sicil numarasi
        "LH_001_PERSON.TTFNO",      // TCKN
        "LH_001_PERSON.NAME",       // ad
        "LH_001_PERSON.SURNAME",    // soyad
        "LH_001_PERSON.BIRTHDATE",  // dogum tarihi
        "LH_001_PERSON.INDATE",     // ise giris tarihi
        "LH_001_PERSON.OUTDATE",    // isten cikis tarihi (aktiflik buradan)
        "LH_001_PERSON.FIRMNR",     // firma
        "LH_001_CONTACT.LREF",      // baglanti
        "LH_001_CONTACT.CARDREF",   // baglanti (kisiye)
        "LH_001_CONTACT.TYP",       // iletisim turu (e-posta / cep telefonu)
        "LH_001_CONTACT.EXP1",      // kurumsal e-posta / cep telefonu
        "L_CAPIFIRM.NR",            // firma
        "L_CAPIFIRM.NAME",          // firma adi

        // T1 (SYG-PERS-003)
        "LH_001_PERSON.SEX",        // cinsiyet
        "LH_001_PERSON.EDUCATION",  // egitim tanim numarasi
        "LH_001_PERSON.TITLE",      // unvan kodu (SYG-PERS-018)
        "LH_001_PERSON.SPECODE",    // statu
        "LH_001_PERSON.JOBREF",     // SGK meslek tanim numarasi
        "LH_001_PERSON.LOCNR",      // sube numarasi
        "LH_001_PERSON.DEPTNR",     // birim numarasi
        "LH_001_CONTACT.EXP2",      // adresin ikinci satiri
        "LH_001_FAMILY.LREF",       // baglanti
        "LH_001_FAMILY.PERREF",     // baglanti (karta)
        "LH_001_FAMILY.IDREF",      // baglanti (kimlik ek bilgisine)
        "LH_001_FAMILY.RELATION",   // yakinlik (0 = kisinin kendisi)
        "LH_001_FAMILY.STATUS",     // medeni hal
        "LH_001_PERIDINF.LREF",     // baglanti
        "LH_001_PERIDINF.EXSURNAME", // onceki soyadi
        "LH_001_TYPEDEF.LREF",      // baglanti
        "LH_001_TYPEDEF.TYP",       // tanim turu
        "LH_001_TYPEDEF.NR",        // tanim numarasi
        "LH_001_TYPEDEF.EXP",       // egitim adi; SGK meslek kodu
        "LH_001_TYPEDEF.DESC_",     // SGK meslek adi
        "L_CAPIDIV.LOGICALREF",     // baglanti
        "L_CAPIDIV.FIRMNR",         // firma
        "L_CAPIDIV.NR",             // sube numarasi
        "L_CAPIDIV.NAME",           // sube adi
        "L_CAPIDIV.CITY",           // sehir
        "L_CAPIDEPT.LOGICALREF",    // baglanti
        "L_CAPIDEPT.FIRMNR",        // firma
        "L_CAPIDEPT.NR",            // birim numarasi
        "L_CAPIDEPT.NAME",          // birim adi
    ];

    [Fact]
    public void Only_the_fields_in_scope_are_mapped()
    {
        using var context = new LogoDbContext(new DbContextOptionsBuilder<LogoDbContext>()
            .UseSqlServer("Server=model-only;Database=model-only")
            .Options);

        var mapped = context.Model.GetEntityTypes()
            .SelectMany(entity => entity.GetProperties().Select(p => $"{entity.GetTableName()}.{p.GetColumnName()}"))
            .Order(StringComparer.Ordinal)
            .ToList();

        mapped.ShouldBe(Allowed.Order(StringComparer.Ordinal).ToList());
    }

    [Fact]
    public void Religion_and_health_are_never_read()
    {
        // SYG-PERS-004 (PG-PERS-36): din ve saglik bilgisi LOGO'dan senkronize edilmez.
        using var context = new LogoDbContext(new DbContextOptionsBuilder<LogoDbContext>()
            .UseSqlServer("Server=model-only;Database=model-only")
            .Options);

        var tables = context.Model.GetEntityTypes().Select(e => e.GetTableName()).ToList();
        var columns = context.Model.GetEntityTypes().SelectMany(e => e.GetProperties().Select(p => p.GetColumnName())).ToList();

        tables.ShouldNotContain(t => t != null && t.Contains("HEALTH", StringComparison.OrdinalIgnoreCase));
        columns.ShouldNotContain(c => c.Contains("RELIG", StringComparison.OrdinalIgnoreCase) || c.Contains("HEALTH", StringComparison.OrdinalIgnoreCase));
        LogoSchema.Tables.ShouldNotContain(t => t.Contains("HEALTH", StringComparison.OrdinalIgnoreCase));
        LogoSchema.Columns.ShouldNotContain(c => c.Column.Contains("RELIG", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void The_schema_check_expects_exactly_the_mapped_fields()
    {
        // Sema denetimi (SYG-KMLK-004; T1 tablolari SYG-PERS-011) ile veri modeli ayni alan kumesini gosterir.
        LogoSchema.Columns.Select(c => $"{c.Table}.{c.Column}")
            .Order(StringComparer.Ordinal)
            .ShouldBe(Allowed.Order(StringComparer.Ordinal));
    }
}
