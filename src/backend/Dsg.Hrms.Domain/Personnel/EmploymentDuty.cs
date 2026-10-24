using Dsg.Hrms.Domain.Common;

namespace Dsg.Hrms.Domain.Personnel;

/// <summary>
/// Istihdamin bir donemdeki gorevi (PG-PERS-43; ADR-0021 §1–§2; SYG-PERS-042). HRMS'te IK veya
/// Sistem Yoneticisi girer; LOGO'da yoktur (<c>KR-112</c>).
/// </summary>
/// <remarks>
/// Aralik <see cref="ValidFrom"/> dahil, <see cref="ValidTo"/> haric. Ayni istihdamda silinmemis
/// araliklar cakisamaz; veritabani <c>EXCLUDE ... WHERE deleted_at IS NULL</c> ile zorlar
/// (SYG-PERS-019). Kayit silinmez, yumusak silinir (ADR-0004 §5).
/// </remarks>
public sealed class EmploymentDuty : Entity, IAuditable, ISoftDeletable
{
    private EmploymentDuty()
    {
    }

    /// <summary>Istihdamin ic kimligi.</summary>
    public long EmploymentId { get; private set; }

    /// <summary>Gorev tanimi (referans veri, tur gorev).</summary>
    public long DutyDefinitionId { get; private set; }

    /// <summary>Gecerlilik baslangici (dahil).</summary>
    [AuditVisible("Gorev doneminin baslangici; gecmisin denetlenebilmesi icin gorunmelidir")]
    public DateOnly ValidFrom { get; private set; }

    /// <summary>Gecerlilik bitisi (haric); acik kayitta <c>null</c>.</summary>
    [AuditVisible("Gorev doneminin bitisi; gecmisin denetlenebilmesi icin gorunmelidir")]
    public DateOnly? ValidTo { get; private set; }

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

    /// <summary>Kayit acik mi.</summary>
    public bool IsOpen => ValidTo is null;

    internal static EmploymentDuty Open(long employmentId, long definitionId, DateOnly from, DateOnly? to) =>
        new() { EmploymentId = employmentId, DutyDefinitionId = definitionId, ValidFrom = from, ValidTo = to };

    internal void Change(long definitionId) => DutyDefinitionId = definitionId;

    internal void Close(DateOnly to) => ValidTo = to > ValidFrom ? to : ValidFrom.AddDays(1);
}

/// <summary>Gorev girisinin sonucu.</summary>
public enum DutyChange
{
    /// <summary>Yeni kayit acildi; onceki acik kayit (varsa) yeni baslangicla kapandi.</summary>
    Opened = 1,

    /// <summary>Ayni gun acilmis kaydin gorevi degisti.</summary>
    Replaced = 2,

    /// <summary>Ayni gorev zaten ayni gun acik; degisiklik yok.</summary>
    Unchanged = 3,
}

/// <summary>Gorev girisi kurala aykiri (istihdam araligi disi veya acik kayittan once).</summary>
public sealed class DutyRuleException(string message, bool isConflict) : Exception(message)
{
    /// <summary>Mevcut kayitlarla cakisma mi (<c>409</c>); degilse is kurali (<c>422</c>).</summary>
    public bool IsConflict { get; } = isConflict;
}

/// <summary>
/// Gorev zaman cizelgesi kurallari (ADR-0021 §2; SYG-PERS-019, 021, 042).
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Baslangic ise giris tarihinden once veya cikis tarihinden sonra olamaz (AN-16).</item>
/// <item>Acik gorev, yeni gorevin baslangic tarihiyle kapanir.</item>
/// <item>Ayni gun acilmis gorevin yerine yeni gorev girilirse kayit guncellenir; sifir uzunlukta aralik olusmaz.</item>
/// <item>Acik gorevden ONCE baslayan giris reddedilir: once o kayit duzeltilir (ADR-0021 §2).</item>
/// <item>Kapali kayitlarla cakisma veritabani kisitinda yakalanir ve <c>409</c> doner.</item>
/// <item>Istihdam bitmisse yeni gorev cikisin ertesi gunu kapali acilir.</item>
/// </list>
/// </remarks>
public static class DutyTimeline
{
    /// <summary>Gorevi atar.</summary>
    /// <param name="employment">Istihdam.</param>
    /// <param name="existing">Istihdamin silinmemis gorev kayitlari.</param>
    /// <param name="definitionId">Gorev taniminin ic kimligi (aktif oldugu cagiranca denetlenir).</param>
    /// <param name="start">Baslangic.</param>
    /// <returns>Eklenecek yeni kayit (varsa) ve sonuc.</returns>
    /// <exception cref="DutyRuleException">Kurala aykiri giriste.</exception>
    public static (EmploymentDuty? Added, DutyChange Change) Assign(
        Employment employment,
        IReadOnlyList<EmploymentDuty> existing,
        long definitionId,
        DateOnly start)
    {
        ArgumentNullException.ThrowIfNull(employment);
        ArgumentNullException.ThrowIfNull(existing);

        if (start < employment.HireDate)
        {
            throw new DutyRuleException("Görev başlangıcı işe giriş tarihinden önce olamaz.", isConflict: false);
        }

        if (employment.TerminationDate is { } last && start > last)
        {
            throw new DutyRuleException("Görev başlangıcı işten çıkış tarihinden sonra olamaz.", isConflict: false);
        }

        var end = employment.TerminationDate?.AddDays(1);
        var open = existing.SingleOrDefault(d => d.IsOpen);

        if (open is not null)
        {
            if (start < open.ValidFrom)
            {
                throw new DutyRuleException(
                    "Görev, açık görevden önce başlayamaz; önce mevcut görev kaydı düzeltilmeli.", isConflict: true);
            }

            if (start == open.ValidFrom)
            {
                if (open.DutyDefinitionId == definitionId)
                {
                    return (null, DutyChange.Unchanged);
                }

                open.Change(definitionId);
                return (null, DutyChange.Replaced);
            }

            open.Close(start);
        }

        return (EmploymentDuty.Open(employment.Id, definitionId, start, end), DutyChange.Opened);
    }

    /// <summary>
    /// Istihdam bittiyse acik gorevi cikisin ertesi gunu kapatir (SYG-PERS-021). Kapattiysa <c>true</c>.
    /// </summary>
    public static bool CloseOnTermination(Employment employment, IReadOnlyList<EmploymentDuty> existing)
    {
        ArgumentNullException.ThrowIfNull(employment);
        ArgumentNullException.ThrowIfNull(existing);

        if (employment.IsActive || employment.TerminationDate is not { } termination)
        {
            return false;
        }

        var open = existing.SingleOrDefault(d => d.IsOpen);
        if (open is null)
        {
            return false;
        }

        open.Close(termination.AddDays(1));
        return true;
    }
}
