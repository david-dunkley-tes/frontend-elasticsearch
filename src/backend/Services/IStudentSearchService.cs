using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public interface IStudentSearchService
{
    Task<SearchResponse> SearchAsync(SearchRequest request, AuthorizedSchoolScope authorizationScope);
}
