namespace Cursus.PL.Models;

/// <summary>
/// A page-slice of any <typeparamref name="T"/> sequence.
/// Follows the standard ASP.NET Core MVC pagination pattern.
/// </summary>
public class PaginatedList<T>
{
    public IReadOnlyList<T> Items { get; }
    public int PageIndex { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }
    public int PageSize { get; }

    public bool HasPreviousPage => PageIndex > 1;
    public bool HasNextPage => PageIndex < TotalPages;

    private PaginatedList(IReadOnlyList<T> items, int totalCount, int pageIndex, int pageSize)
    {
        Items = items;
        TotalCount = totalCount;
        PageIndex = pageIndex;
        PageSize = pageSize;
        TotalPages = totalCount == 0
            ? 1
            : (int)Math.Ceiling(totalCount / (double)pageSize);
    }

    /// <summary>
    /// Executes <c>COUNT(*)</c> + <c>SKIP/TAKE</c> against the supplied
    /// <paramref name="source"/> and returns a populated
    /// <see cref="PaginatedList{T}"/>.
    /// </summary>
    public static async Task<PaginatedList<T>> CreateAsync(
        IQueryable<T> source, int pageIndex, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var count = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .CountAsync(source);

        var totalPages = count == 0 ? 1 : (int)Math.Ceiling(count / (double)pageSize);
        var safePage = Math.Clamp(pageIndex, 1, totalPages);

        var items = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(source.Skip((safePage - 1) * pageSize).Take(pageSize));

        return new PaginatedList<T>(items, count, safePage, pageSize);
    }

    /// <summary>Creates from an already-materialised in-memory list.</summary>
    public static PaginatedList<T> Create(
        IEnumerable<T> source, int pageIndex, int pageSize)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var list = source as IList<T> ?? source.ToList();
        var totalCount = list.Count;
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)pageSize);
        var safePage = Math.Clamp(pageIndex, 1, totalPages);
        var items = list.Skip((safePage - 1) * pageSize).Take(pageSize).ToList();
        return new PaginatedList<T>(items, totalCount, safePage, pageSize);
    }
}
