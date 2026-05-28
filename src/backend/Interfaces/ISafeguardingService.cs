using StudentSearch.Api.Models;

namespace StudentSearch.Api.Interfaces;

public interface ISafeguardingService
{
    Task<SafeguardingAnswer> AskAsync(SafeguardingQuestion request, AuthorizedSchoolScope authorizationScope, CancellationToken cancellationToken = default);
}
