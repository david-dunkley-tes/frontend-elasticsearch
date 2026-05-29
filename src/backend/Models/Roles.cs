namespace StudentSearch.Api.Models;

/// <summary>
/// Well-known scope roles. Roles are looked up by name against the per-scope <see cref="AuthorizationScope.Role"/>
/// list, so adding a capability later is a matter of defining a new role here and resolving it via
/// <c>IAuthorizationScopeResolver.ResolveRoleScopeAsync</c> — no change to the resolution mechanics.
/// </summary>
public static class Roles
{
    /// <summary>Designated Safeguarding Lead — grants access to safeguarding narratives and the Ask feature.</summary>
    public const string Dsl = "DSL";
}
