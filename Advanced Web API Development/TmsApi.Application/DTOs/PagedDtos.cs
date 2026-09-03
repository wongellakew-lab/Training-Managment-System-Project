namespace TmsApi.Application.DTOs;

public class PagedRequest
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;
    private int _page = 1;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageNumber
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 1 : value);
    }

    public string? SearchTerm { get; set; }
    public string? Search
    {
        get => SearchTerm;
        set => SearchTerm = value;
    }

    public string? Department { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
    public string? SkillLevel { get; set; }
    public string? SubjectMatter { get; set; }
    public string? Availability { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = false;
    public string? Fields { get; set; }
}

public class PagedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int Page { get; set; }
    public int PageNumber => Page;
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / (PageSize > 0 ? PageSize : 1));
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public PagedResponse() { }

    public PagedResponse(IEnumerable<T> items, int page, int pageSize, int totalCount)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalCount = totalCount;
    }
}
