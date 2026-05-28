using StudentSearch.Api.Models;
using System.Security.Claims;

namespace StudentSearch.Api.Interfaces;

public interface IAuthorizationScopeResolver
{
    Task<AuthorizedSchoolScope> ResolveAsync(ClaimsPrincipal user);
}
