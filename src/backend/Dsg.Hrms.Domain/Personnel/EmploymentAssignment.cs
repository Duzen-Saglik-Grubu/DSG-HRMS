using Dsg.Hrms.Domain.Common;

namespace Dsg.Hrms.Domain.Personnel;

/// <summary>
/// Istihdamin bir donemdeki LOGO kaynakli nitelikleri: firma, sube, birim, unvan, statu, SGK
/// meslek (PG-PERS-12, ADR-0021 §1–§2; SYG-PERS-018).
/// </summary>
/// <remarks>
/// <para>
/// Aralik <see cref="ValidFrom"/> dahil, <see cref="ValidTo"/> haric; acik kayitta
/// <see cref="ValidTo"/> bostur. Ayni istihdamda araliklar cakisamaz; bunu veritabani
/// <c>EXCLUDE USING gist</c> kisitiyla zorlar (SYG-PERS-019).
/// </para>
/// <para>
/// Kaynak LOGO'dur ve LOGO yalnizca bugunku degeri verir. Gecmis, T1'in devreye girdigi gunden
/// baslar; onceki donemler icin deger uydurulmaz. Gecis kurallari
/// <see cref="AssignmentTimeline"/>'dadir.
/// </para>
/// </remarks>
public sealed class EmploymentAssignment : Entity, IAuditable
{
    private EmploymentAssignment()
    {
    }

    /// <summary>Istihdamin ic kimligi.</summary>
    public long EmploymentId { get; private set; }

    /// <summary>Gecerlilik baslangici (dahil).</summary>
    [AuditVisible("Donemin baslangici; gecmisin denetlenebilmesi icin gorunmelidir")]
    public DateOnly ValidFrom { get; private set; }

    /// <summary>Gecerlilik bitisi (haric); acik kayitta <c>null</c>.</summary>
    [AuditVisible("Donemin bitisi; gecmisin denetlenebilmesi icin gorunmelidir")]
    public DateOnly? ValidTo { get; private set; }

    /// <summary>Firmanin kimligi (degisiklik talebi #284).</summary>
    public long CompanyId { get; private set; }

    /// <summary>Subenin kimligi; LOGO'da tanimsizsa <c>null</c>.</summary>
    public long? BranchId { get; private set; }

    /// <summary>Birimin kimligi; LOGO'da tanimsizsa <c>null</c>.</summary>
    public long? DepartmentId { get; private set; }

    /// <summary>Unvan kodu (LOGO <c>TITLE</c>, 1–4); adi referans veriden gelir (SYG-PERS-038).</summary>
    [AuditVisible("Unvan bir tanim kodudur; kisiyi tek basina tanimlamaz, degisikligi izlenebilmelidir")]
    public string? TitleCode { get; private set; }

    /// <summary>Statu (LOGO <c>SPECODE</c>).</summary>
    [AuditVisible("Statu bir tanim kodudur; kisiyi tek basina tanimlamaz, degisikligi izlenebilmelidir")]
    public string? StatusCode { get; private set; }

    /// <summary>SGK meslek kodu.</summary>
    [AuditVisible("SGK meslek kodu resmi bir tanim kodudur; kisiyi tek basina tanimlamaz")]
    public string? OccupationCode { get; private set; }

    /// <summary>SGK meslek adi.</summary>
    [AuditVisible("SGK meslek adi resmi bir tanimdir; kisiyi tek basina tanimlamaz")]
    public string? OccupationName { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; set; }

    /// <inheritdoc />
    public long? CreatedBy { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? UpdatedAt { get; set; }

    /// <inheritdoc />
    public long? UpdatedBy { get; set; }

    /// <summary>Kayit acik mi.</summary>
    public bool IsOpen => ValidTo is null;

    /// <summary>Kaydin degerleri.</summary>
    public AssignmentValues Values =>
        new(CompanyId, BranchId, DepartmentId, TitleCode, StatusCode, OccupationCode, OccupationName);

    /// <summary>Verilen gun kaydin araligina giriyor mu.</summary>
    public bool Covers(DateOnly day) => ValidFrom <= day && (ValidTo is null || day < ValidTo.Value);

    internal static EmploymentAssignment Open(long employmentId, DateOnly from, DateOnly? to, AssignmentValues values)
    {
        var assignment = new EmploymentAssignment { EmploymentId = employmentId, ValidFrom = from, ValidTo = to };
        assignment.Set(values);
        return assignment;
    }

    internal void Set(AssignmentValues values)
    {
        CompanyId = values.CompanyId;
        BranchId = values.BranchId;
        DepartmentId = values.DepartmentId;
        TitleCode = values.TitleCode;
        StatusCode = values.StatusCode;
        OccupationCode = values.OccupationCode;
        OccupationName = values.OccupationName;
    }

    internal void Close(DateOnly to) => ValidTo = to;
}

/// <summary>Bir donemin nitelikleri. Esitlik karsilastirmasi "degisti mi" sorusunu cevaplar.</summary>
/// <param name="CompanyId">Firma.</param>
/// <param name="BranchId">Sube.</param>
/// <param name="DepartmentId">Birim.</param>
/// <param name="TitleCode">Unvan kodu.</param>
/// <param name="StatusCode">Statu.</param>
/// <param name="OccupationCode">SGK meslek kodu.</param>
/// <param name="OccupationName">SGK meslek adi.</param>
public sealed record AssignmentValues(
    long CompanyId,
    long? BranchId,
    long? DepartmentId,
    string? TitleCode,
    string? StatusCode,
    string? OccupationCode,
    string? OccupationName);

/// <summary>
/// Bir istihdamin tarih aralikli kayitlarina LOGO'nun bugunku degerini uygular (ADR-0021 §2;
/// SYG-PERS-018, 021).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><b>Gun siniri:</b> "bugun" Europe/Istanbul takvim gunudur; cagiran verir.</item>
/// <item><b>Ilk gorulme, aktif istihdam:</b> kayit bugunden baslar. T1 oncesi icin kayit yoktur.</item>
/// <item><b>Ilk gorulme, bitmis istihdam:</b> LOGO karti cikistan sonra donar; karttaki deger cikis
/// gunundeki deger sayilir ve tek gunluk kapali bir kayit acilir (cikis gunu, ertesi gun haric).
/// Kartta istihdamin son bilinen nitelikleri bu yuzden gorunur.</item>
/// <item><b>Deger degisti:</b> acik kayit bugunle kapanir, yeni kayit bugunden acilir.</item>
/// <item><b>Ayni gun ikinci degisiklik:</b> bugun acilmis kayit guncellenir; sifir uzunlukta aralik
/// olusmaz, gunun son degeri gecerlidir.</item>
/// <item><b>Istihdam bitti:</b> acik kayit cikis tarihinin ertesi gunu kapanir. Cikis tarihi kaydin
/// baslangicindan onceyse (LOGO'ya geriye donuk girilmis) kayit basladigi gunun ertesi kapanir;
/// aralik bos olamaz.</item>
/// <item><b>Bitmis istihdamda LOGO degisikligi:</b> son kayit yerinde guncellenir; yeni donem
/// acilmaz.</item>
/// <item><b>Yeniden ise giris:</b> istihdam tekrar aktif olunca bugunden yeni kayit acilir.</item>
/// </list>
/// </remarks>
public static class AssignmentTimeline
{
    /// <summary>Degisiklik uygular; eklenecek yeni kaydi (varsa) ve bir sey degisip degismedigini doner.</summary>
    /// <param name="employment">Istihdam (kimligi ve cikis tarihi).</param>
    /// <param name="existing">Istihdamin mevcut kayitlari.</param>
    /// <param name="values">LOGO'nun bugunku degerleri.</param>
    /// <param name="today">Bugun (Europe/Istanbul).</param>
    public static (EmploymentAssignment? Added, bool Changed) Apply(
        Employment employment,
        IReadOnlyList<EmploymentAssignment> existing,
        AssignmentValues values,
        DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(employment);
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(values);

        var open = existing.SingleOrDefault(a => a.IsOpen);
        var last = existing.OrderByDescending(a => a.ValidFrom).FirstOrDefault();

        if (employment.IsActive)
        {
            if (open is null)
            {
                // Ilk gorulme veya yeniden ise giris.
                return (EmploymentAssignment.Open(employment.Id, today, null, values), true);
            }

            if (open.Values == values)
            {
                return (null, false);
            }

            if (open.ValidFrom >= today)
            {
                open.Set(values);
                return (null, true);
            }

            open.Close(today);
            return (EmploymentAssignment.Open(employment.Id, today, null, values), true);
        }

        // Istihdam bitti.
        var end = employment.TerminationDate is { } termination ? termination.AddDays(1) : today;
        if (open is not null)
        {
            // Cikis gunundeki son deger kayda gecer, sonra kayit kapanir.
            open.Set(values);
            open.Close(end > open.ValidFrom ? end : open.ValidFrom.AddDays(1));
            return (null, true);
        }

        if (last is null)
        {
            var day = end.AddDays(-1);
            return (EmploymentAssignment.Open(employment.Id, day, end, values), true);
        }

        if (last.Values == values)
        {
            return (null, false);
        }

        last.Set(values);
        return (null, true);
    }
}
