using StudentSearch.Api.Interfaces;
using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public sealed class StudentSearchService(IStudentSearchIndex studentSearchIndex) : IStudentSearchService
{
    public Task<SearchResponse> SearchAsync(SearchRequest request, AuthorizedSchoolScope authorizationScope)
    {
        var normalized = NormalizeRequest(request);
        return studentSearchIndex.SearchAsync(normalized, authorizationScope);
    }

    private static SearchRequest NormalizeRequest(SearchRequest request)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize <= 0 ? 25 : request.PageSize, 1, 100);
        var filters = request.Filters.ToDictionary(
            item => item.Key,
            item => item.Value.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            StringComparer.OrdinalIgnoreCase);

        var studentIds = request.StudentIds
            .Select(id => id?.Trim() ?? string.Empty)
            .Where(id => id.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return request with
        {
            Query = request.Query?.Trim() ?? string.Empty,
            Page = page,
            PageSize = pageSize,
            Filters = filters,
            StudentIds = studentIds
        };
    }
}
