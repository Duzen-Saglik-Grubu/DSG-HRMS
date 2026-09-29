namespace Dsg.Hrms.Application.Common.Paging;

/// <summary>
/// Sayfalanmis liste (ADR-0010 §4). Tum listeleme uclari bu bicimde doner; sayfasiz liste
/// yazilmaz.
/// </summary>
/// <param name="Items">Bu sayfadaki kayitlar.</param>
/// <param name="Page">Sayfa numarasi (1'den baslar).</param>
/// <param name="PageSize">Sayfa boyutu.</param>
/// <param name="TotalCount">Suzgece uyan toplam kayit sayisi.</param>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    /// <summary>Sayfa boyutunun ust siniri (ADR-0010 §4).</summary>
    public const int MaxPageSize = 100;

    /// <summary>Toplam sayfa sayisi; kayit yoksa 0.</summary>
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>Sayfa istegi.</summary>
/// <param name="Page">Sayfa numarasi (1'den baslar).</param>
/// <param name="PageSize">Sayfa boyutu (1–100).</param>
public sealed record PageRequest(int Page, int PageSize)
{
    /// <summary>Atlanacak kayit sayisi.</summary>
    public int Skip => (Math.Max(1, Page) - 1) * PageSize;
}
