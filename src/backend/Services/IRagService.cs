using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public interface IRagService
{
    Task<RagAnswer> AskAsync(RagRequest request, AuthorizedSchoolScope authorizationScope, CancellationToken cancellationToken = default);
}
