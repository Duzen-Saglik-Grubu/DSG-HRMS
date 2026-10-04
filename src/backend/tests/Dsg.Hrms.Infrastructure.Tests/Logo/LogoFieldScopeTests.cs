using Dsg.Hrms.Infrastructure.Logo;
using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Tests.Logo;

/// <summary>
/// LOGO'dan yalnizca T3 kapsamindaki alanlar okunur (SYG-KMLK-006).
/// </summary>
/// <remarks>
/// Gereksinimin dogrulama yontemi incelemedir; bu test incelemenin sonucunu sabitler. LOGO
/// veri modeline yeni bir sutun eklenirse test duser: alan T1 kapsamindadir (sube, birim,
/// gorev, fotograf…) ve SYG-KMLK-006 yeniden incelenmeden okunamaz.
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
    public void The_schema_check_expects_exactly_the_mapped_fields()
    {
        // Sema denetimi (SYG-KMLK-004) ile veri modeli ayni alan kumesini gosterir.
        LogoSchema.Columns.Select(c => $"{c.Table}.{c.Column}")
            .Order(StringComparer.Ordinal)
            .ShouldBe(Allowed.Order(StringComparer.Ordinal));
    }
}
