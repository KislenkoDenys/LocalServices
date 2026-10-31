namespace LocalServices.Models;

public class PagedResult<T>
{
    public int TotalCount { get; set; }
    public int Skip { get; set; }
    public int Limit { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
    public string? NextLink { get; set; }
    public string? PreviousLink { get; set; }
    public IEnumerable<T> Items { get; set; } = [];
}

