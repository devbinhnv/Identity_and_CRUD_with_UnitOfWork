using Microsoft.EntityFrameworkCore;

namespace IdentityUoW.Api.Common.Models;

public class PageList<T>
{
    public PageList(IReadOnlyList<T> items, long totalItems, int pageIndex, int pageSize)
    {
        Items = items;
        MetaData = new MetaData
        {
            TotalItems = totalItems,
            PageSize = pageSize,
            CurrentPage = pageIndex,
            TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
        };
    }

    public IReadOnlyList<T> Items { get; }

    public MetaData MetaData { get; }

    public static async Task<PageList<T>> ToPagedListAsync(
        IQueryable<T> source,
        int pageIndex,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var count = await source.CountAsync(cancellationToken);
        var items = await source
            .Skip((pageIndex - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PageList<T>(items, count, pageIndex, pageSize);
    }
}
