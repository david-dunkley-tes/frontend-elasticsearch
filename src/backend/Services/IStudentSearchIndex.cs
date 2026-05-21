using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public interface IStudentSearchIndex
{
    Task<SearchResponse> SearchAsync(SearchRequest request, AuthorizedSchoolScope authorizationScope);
}
