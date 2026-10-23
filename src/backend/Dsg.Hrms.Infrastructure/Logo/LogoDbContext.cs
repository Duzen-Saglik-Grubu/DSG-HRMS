using Microsoft.EntityFrameworkCore;

namespace Dsg.Hrms.Infrastructure.Logo;

/// <summary>
/// LOGO Bordro veritabani icin SALT OKUNUR baglam (ADR-0003 §1, §3).
/// </summary>
/// <remarks>
/// <para>
/// LOGO'ya yazma, guncelleme ve silme KESINLIKLE yapilmaz (<c>KR-003</c>). Bu baglam
/// uc kilidin uygulama tarafindaki ikisidir:
/// </para>
/// <list type="number">
///   <item>Veritabani: oturumda <c>INSERT/UPDATE/DELETE/ALTER/EXECUTE</c> icin <c>DENY</c> (<c>KR-004</c>).</item>
///   <item>Bu baglam: kaydetme cagrilarinin TAMAMI istisna firlatir; sorgular varsayilan olarak izlenmez.</item>
///   <item>Senkronizasyon: her calismadan once oturumun yazma yetkisi olmadigi denetlenir (<see cref="LogoPersonnelSource.VerifyReadOnlyAccessAsync"/>).</item>
/// </list>
/// <para>
/// Bu siniftaki eslemeler yalnizca senkronizasyonun ihtiyac duydugu kolonlari
/// kapsar. LOGO'nun tablo ve kolon adlari bu klasorun disina SIZMAZ (ADR-0003 §2).
/// </para>
/// </remarks>
public sealed class LogoDbContext : DbContext
{
    private const string ReadOnlyMessage =
        "LOGO veritabani SALT OKUNURDUR; yazma, guncelleme ve silme yapilmaz (KR-003).";

    /// <summary>Yeni ornek olusturur.</summary>
    public LogoDbContext(DbContextOptions<LogoDbContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        ChangeTracker.AutoDetectChangesEnabled = false;
    }

    /// <summary>Personel kartlari (<c>LH_001_PERSON</c>).</summary>
    public DbSet<LogoPerson> Persons => Set<LogoPerson>();

    /// <summary>Iletisim kayitlari (<c>LH_001_CONTACT</c>).</summary>
    public DbSet<LogoContact> Contacts => Set<LogoContact>();

    /// <summary>Firmalar (<c>L_CAPIFIRM</c>).</summary>
    public DbSet<LogoFirm> Firms => Set<LogoFirm>();

    /// <summary>Aile ve resim satirlari (yalnizca medeni hal ve kimlik ek bilgisi baglantisi).</summary>
    public DbSet<LogoFamily> Families => Set<LogoFamily>();

    /// <summary>Kimlik ek bilgileri.</summary>
    public DbSet<LogoIdentityInfo> IdentityInfos => Set<LogoIdentityInfo>();

    /// <summary>Tanimlar (egitim).</summary>
    public DbSet<LogoTypeDefinition> TypeDefinitions => Set<LogoTypeDefinition>();

    /// <summary>Subeler.</summary>
    public DbSet<LogoDivision> Divisions => Set<LogoDivision>();

    /// <summary>Birimler.</summary>
    public DbSet<LogoDepartment> Departments => Set<LogoDepartment>();

    /// <inheritdoc />
    public override int SaveChanges() => throw new InvalidOperationException(ReadOnlyMessage);

    /// <inheritdoc />
    public override int SaveChanges(bool acceptAllChangesOnSuccess) => throw new InvalidOperationException(ReadOnlyMessage);

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(ReadOnlyMessage);

    /// <inheritdoc />
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException(ReadOnlyMessage);

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<LogoPerson>(person =>
        {
            person.ToTable(LogoSchema.PersonTable);
            person.HasKey(p => p.LogicalRef);
            person.Property(p => p.LogicalRef).HasColumnName("LREF");
            person.Property(p => p.Code).HasColumnName("CODE");
            person.Property(p => p.NationalId).HasColumnName("TTFNO");
            person.Property(p => p.FirstName).HasColumnName("NAME");
            person.Property(p => p.LastName).HasColumnName("SURNAME");
            person.Property(p => p.BirthDate).HasColumnName("BIRTHDATE");
            person.Property(p => p.HireDate).HasColumnName("INDATE");
            person.Property(p => p.TerminationDate).HasColumnName("OUTDATE");
            person.Property(p => p.FirmNumber).HasColumnName("FIRMNR");
            person.Property(p => p.Sex).HasColumnName("SEX");
            person.Property(p => p.EducationNumber).HasColumnName("EDUCATION");
        });

        modelBuilder.Entity<LogoContact>(contact =>
        {
            contact.ToTable(LogoSchema.ContactTable);
            contact.HasKey(c => c.LogicalRef);
            contact.Property(c => c.LogicalRef).HasColumnName("LREF");
            contact.Property(c => c.CardRef).HasColumnName("CARDREF");
            contact.Property(c => c.Type).HasColumnName("TYP");
            contact.Property(c => c.Value).HasColumnName("EXP1");
            contact.Property(c => c.Value2).HasColumnName("EXP2");
        });

        // L_CAPIFIRM.NR kolonu veritabaninda NULL olabilir; birincil anahtar olarak
        // tanimlanmaz. Yalnizca birlestirmede kullanilir.
        modelBuilder.Entity<LogoFirm>(firm =>
        {
            firm.ToTable(LogoSchema.FirmTable);
            firm.HasNoKey();
            firm.Property(f => f.Number).HasColumnName("NR");
            firm.Property(f => f.Name).HasColumnName("NAME");
        });

        modelBuilder.Entity<LogoFamily>(family =>
        {
            family.ToTable(LogoSchema.FamilyTable);
            family.HasKey(f => f.LogicalRef);
            family.Property(f => f.LogicalRef).HasColumnName("LREF");
            family.Property(f => f.PersonRef).HasColumnName("PERREF");
            family.Property(f => f.IdentityRef).HasColumnName("IDREF");
            family.Property(f => f.Relation).HasColumnName("RELATION");
            family.Property(f => f.MaritalStatus).HasColumnName("STATUS");
        });

        modelBuilder.Entity<LogoIdentityInfo>(info =>
        {
            info.ToTable(LogoSchema.IdentityInfoTable);
            info.HasKey(i => i.LogicalRef);
            info.Property(i => i.LogicalRef).HasColumnName("LREF");
            info.Property(i => i.PreviousSurname).HasColumnName("EXSURNAME");
        });

        modelBuilder.Entity<LogoTypeDefinition>(definition =>
        {
            definition.ToTable(LogoSchema.TypeDefinitionTable);
            definition.HasKey(d => d.LogicalRef);
            definition.Property(d => d.LogicalRef).HasColumnName("LREF");
            definition.Property(d => d.Type).HasColumnName("TYP");
            definition.Property(d => d.Number).HasColumnName("NR");
            definition.Property(d => d.Name).HasColumnName("EXP");
        });

        modelBuilder.Entity<LogoDivision>(division =>
        {
            division.ToTable(LogoSchema.DivisionTable);
            division.HasKey(d => d.LogicalRef);
            division.Property(d => d.LogicalRef).HasColumnName("LOGICALREF");
            division.Property(d => d.FirmNumber).HasColumnName("FIRMNR");
            division.Property(d => d.Number).HasColumnName("NR");
            division.Property(d => d.Name).HasColumnName("NAME");
            division.Property(d => d.City).HasColumnName("CITY");
        });

        modelBuilder.Entity<LogoDepartment>(department =>
        {
            department.ToTable(LogoSchema.DepartmentTable);
            department.HasKey(d => d.LogicalRef);
            department.Property(d => d.LogicalRef).HasColumnName("LOGICALREF");
            department.Property(d => d.FirmNumber).HasColumnName("FIRMNR");
            department.Property(d => d.Number).HasColumnName("NR");
            department.Property(d => d.Name).HasColumnName("NAME");
        });
    }
}

/// <summary>LOGO personel karti (yalnizca kullanilan kolonlar).</summary>
public sealed class LogoPerson
{
    /// <summary><c>LREF</c>.</summary>
    public int LogicalRef { get; init; }

    /// <summary><c>CODE</c> — sicil kodu.</summary>
    public string? Code { get; init; }

    /// <summary><c>TTFNO</c> — TCKN.</summary>
    public string? NationalId { get; init; }

    /// <summary><c>NAME</c>.</summary>
    public string? FirstName { get; init; }

    /// <summary><c>SURNAME</c>.</summary>
    public string? LastName { get; init; }

    /// <summary><c>BIRTHDATE</c>.</summary>
    public DateTime? BirthDate { get; init; }

    /// <summary><c>INDATE</c> — ise giris.</summary>
    public DateTime? HireDate { get; init; }

    /// <summary><c>OUTDATE</c> — isten cikis.</summary>
    public DateTime? TerminationDate { get; init; }

    /// <summary><c>FIRMNR</c>.</summary>
    public short? FirmNumber { get; init; }

    /// <summary>Cinsiyet (1 erkek, 2 kadin).</summary>
    public short? Sex { get; init; }

    /// <summary>Egitim tanim numarasi (<c>LH_001_TYPEDEF</c>, TYP = -5).</summary>
    public short? EducationNumber { get; init; }
}

/// <summary>LOGO iletisim kaydi.</summary>
public sealed class LogoContact
{
    /// <summary><c>LREF</c>.</summary>
    public int LogicalRef { get; init; }

    /// <summary><c>CARDREF</c> — personel kartinin <c>LREF</c> degeri.</summary>
    public int? CardRef { get; init; }

    /// <summary><c>TYP</c> — 3: cep telefonu, 6: e-posta.</summary>
    public short? Type { get; init; }

    /// <summary><c>EXP1</c> — deger.</summary>
    public string? Value { get; init; }

    /// <summary>Ikinci satir (adreste devam satiri).</summary>
    public string? Value2 { get; init; }
}

/// <summary>LOGO firmasi.</summary>
public sealed class LogoFirm
{
    /// <summary><c>NR</c> — firma numarasi.</summary>
    public short? Number { get; init; }

    /// <summary><c>NAME</c>.</summary>
    public string? Name { get; init; }
}

/// <summary>LOGO aile ve resim satiri (<c>LH_001_FAMILY</c>).</summary>
public sealed class LogoFamily
{
    /// <summary>Satir kimligi.</summary>
    public int LogicalRef { get; init; }

    /// <summary>Kartin <c>LREF</c>'i.</summary>
    public int? PersonRef { get; init; }

    /// <summary>Kimlik ek bilgisi satiri (<c>LH_001_PERIDINF.LREF</c>).</summary>
    public int? IdentityRef { get; init; }

    /// <summary>Yakinlik; 0 kisinin kendisi.</summary>
    public short? Relation { get; init; }

    /// <summary>Medeni hal (1 evli).</summary>
    public short? MaritalStatus { get; init; }
}

/// <summary>LOGO kimlik ek bilgisi (<c>LH_001_PERIDINF</c>).</summary>
public sealed class LogoIdentityInfo
{
    /// <summary>Satir kimligi.</summary>
    public int LogicalRef { get; init; }

    /// <summary>Onceki soyadi.</summary>
    public string? PreviousSurname { get; init; }
}

/// <summary>LOGO tanimi (<c>LH_001_TYPEDEF</c>).</summary>
public sealed class LogoTypeDefinition
{
    /// <summary>Satir kimligi.</summary>
    public int LogicalRef { get; init; }

    /// <summary>Tanim turu.</summary>
    public short? Type { get; init; }

    /// <summary>Tur icindeki numara.</summary>
    public short? Number { get; init; }

    /// <summary>Tanim adi.</summary>
    public string? Name { get; init; }
}

/// <summary>LOGO subesi (<c>L_CAPIDIV</c>).</summary>
public sealed class LogoDivision
{
    /// <summary>Satir kimligi.</summary>
    public int LogicalRef { get; init; }

    /// <summary>Firma numarasi.</summary>
    public short? FirmNumber { get; init; }

    /// <summary>Firma icinde sube numarasi.</summary>
    public short? Number { get; init; }

    /// <summary>Ad.</summary>
    public string? Name { get; init; }

    /// <summary>Sehir.</summary>
    public string? City { get; init; }
}

/// <summary>LOGO birimi (<c>L_CAPIDEPT</c>).</summary>
public sealed class LogoDepartment
{
    /// <summary>Satir kimligi.</summary>
    public int LogicalRef { get; init; }

    /// <summary>Firma numarasi.</summary>
    public short? FirmNumber { get; init; }

    /// <summary>Firma icinde birim numarasi.</summary>
    public short? Number { get; init; }

    /// <summary>Ad.</summary>
    public string? Name { get; init; }
}
