using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public sealed class StudentSearchService : IStudentSearchService
{
    private readonly IStudentSearchIndex _studentSearchIndex;

    public StudentSearchService(IStudentSearchIndex studentSearchIndex)
    {
        _studentSearchIndex = studentSearchIndex;
    }

    public Task<SearchResponse> SearchAsync(SearchRequest request)
    {
        var normalized = NormalizeRequest(request);
        return _studentSearchIndex.SearchAsync(normalized);
    }

    private static SearchRequest NormalizeRequest(SearchRequest request)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize <= 0 ? 25 : request.PageSize, 1, 100);
        var filters = request.Filters.ToDictionary(
            item => item.Key,
            item => item.Value.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            StringComparer.OrdinalIgnoreCase);

        return request with
        {
            Query = request.Query?.Trim() ?? string.Empty,
            Page = page,
            PageSize = pageSize,
            Filters = filters
        };
    }
}
