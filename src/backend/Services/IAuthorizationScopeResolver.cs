using StudentSearch.Api.Models;
using System.Security.Claims;

namespace StudentSearch.Api.Services;

public interface IAuthorizationScopeResolver
{
    Task<AuthorizedSchoolScope> ResolveAsync(ClaimsPrincipal user);
}
