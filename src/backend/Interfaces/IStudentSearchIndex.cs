using StudentSearch.Api.Models;

namespace StudentSearch.Api.Interfaces;

public interface IStudentSearchIndex
{
    Task<SearchResponse> SearchAsync(SearchRequest request, AuthorizedSchoolScope authorizationScope, AuthorizedSchoolScope? safeguardingScope = null);
}
