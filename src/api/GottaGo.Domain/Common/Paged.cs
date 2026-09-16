namespace GottaGo.Domain.Common;

/// <summary>One page of results plus the total, so callers can render paging controls.</summary>
public sealed record Paged<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public static Paged<T> Empty(int page, int pageSize) => new([], page, pageSize, 0);
}
