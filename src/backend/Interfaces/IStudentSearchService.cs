using StudentSearch.Api.Models;

namespace StudentSearch.Api.Interfaces;

public interface IStudentSearchService
{
    Task<SearchResponse> SearchAsync(SearchRequest request, AuthorizedSchoolScope authorizationScope);
}
