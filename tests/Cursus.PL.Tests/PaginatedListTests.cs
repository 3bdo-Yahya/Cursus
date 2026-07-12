using Cursus.PL.Models;

namespace Cursus.PL.Tests;

public sealed class PaginatedListTests
{
    [Fact]
    public void Create_ClampsPageZero_ToFirstPage()
    {
        var source = Enumerable.Range(1, 25);
        var page = PaginatedList<int>.Create(source, pageIndex: 0, pageSize: 10);

        Assert.Equal(1, page.PageIndex);
        Assert.Equal(10, page.Items.Count);
        Assert.Equal(1, page.Items[0]);
        Assert.Equal(25, page.TotalCount);
    }

    [Fact]
    public void Create_ClampsNegativePage_ToFirstPage()
    {
        var page = PaginatedList<int>.Create(Enumerable.Range(1, 5), pageIndex: -3, pageSize: 10);

        Assert.Equal(1, page.PageIndex);
        Assert.Equal(5, page.Items.Count);
    }

    [Fact]
    public void Create_ClampsOutOfRangePage_ToLastPage()
    {
        var page = PaginatedList<int>.Create(Enumerable.Range(1, 25), pageIndex: 99, pageSize: 10);

        Assert.Equal(3, page.PageIndex);
        Assert.Equal(5, page.Items.Count);
        Assert.Equal(21, page.Items[0]);
    }

    [Fact]
    public void Create_EmptySource_ReturnsEmptyFirstPage()
    {
        var page = PaginatedList<int>.Create(Array.Empty<int>(), pageIndex: 5, pageSize: 10);

        Assert.Equal(1, page.PageIndex);
        Assert.Equal(0, page.TotalCount);
        Assert.Empty(page.Items);
    }
}
