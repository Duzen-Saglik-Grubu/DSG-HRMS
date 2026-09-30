using Dsg.Hrms.Application.Common.Paging;

namespace Dsg.Hrms.Application.Tests.Common;

/// <summary>Sayfalama sozlesmesi (ADR-0010 §4).</summary>
public sealed class PagedResultTests
{
    [Theory]
    [InlineData(0, 25, 0)]
    [InlineData(1, 25, 1)]
    [InlineData(25, 25, 1)]
    [InlineData(26, 25, 2)]
    [InlineData(584, 25, 24)]
    public void Total_pages_rounds_up(int totalCount, int pageSize, int expected)
    {
        new PagedResult<int>([], 1, pageSize, totalCount).TotalPages.ShouldBe(expected);
    }

    [Theory]
    [InlineData(1, 25, 0)]
    [InlineData(3, 25, 50)]
    [InlineData(0, 25, 0)]
    public void Skip_starts_from_page_one(int page, int pageSize, int expected)
    {
        new PageRequest(page, pageSize).Skip.ShouldBe(expected);
    }
}
