using StudentSearch.Api.Models;
using System.Security.Claims;

namespace StudentSearch.Api.Interfaces;

public interface IAuthorizationScopeResolver
{
    /// <summary>The schools whose student records the caller may view (roles ignored).</summary>
    Task<AuthorizedSchoolScope> ResolveAsync(ClaimsPrincipal user);

    /// <summary>
    /// The schools for which the caller holds <paramref name="role"/>. For each school the most specific
    /// covering scope wins (school &gt; schoolGroup &gt; trust &gt; global); the school is included when that
    /// scope's role list contains <paramref name="role"/>.
    /// </summary>
    Task<AuthorizedSchoolScope> ResolveRoleScopeAsync(ClaimsPrincipal user, string role);
}
