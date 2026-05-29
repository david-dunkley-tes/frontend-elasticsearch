using StudentSearch.Api.Models;

namespace StudentSearch.Api.Interfaces;

public interface IStudentSearchService
{
    // safeguardingScope: schools whose safeguardingLog the caller may see (DSL role). Null => same as
    // the viewing scope (default-open, used by tests); the controller always passes an explicit scope.
    Task<SearchResponse> SearchAsync(SearchRequest request, AuthorizedSchoolScope authorizationScope, AuthorizedSchoolScope? safeguardingScope = null);
}
