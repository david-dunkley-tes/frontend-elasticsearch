namespace StudentSearch.Api.Models;

public sealed record DevAccessToken(string Sub, string? Name, List<AuthorizationScope> Scopes);

public sealed record AuthorizationScope(
    string Type,
    string? SchoolId = null,
    string? TrustId = null,
    // Per-scope roles. A scope granting "DSL" gives safeguarding access to the schools it covers;
    // when a school is covered by several scopes, the most specific scope's roles win
    // (school > trust > global) and are NOT inherited from broader scopes — so an empty/absent
    // role on a more specific scope overrides (removes) a DSL granted by a broader one.
    // No "DSL" anywhere (empty or absent) means no safeguarding access. A multi-school user is
    // modelled as several "school" scopes.
    List<string>? Role = null);

public sealed record AuthorizedSchoolScope(bool IsGlobal, IReadOnlyCollection<string> SchoolIds)
{
    public static AuthorizedSchoolScope Global { get; } = new(true, []);

    /// <summary>True when the scope grants at least one school — global, or a non-empty school set.</summary>
    public bool GrantsAnySchool => IsGlobal || SchoolIds.Count > 0;
}

public sealed record CurrentUserResponse(string Sub, string? Name, IReadOnlyList<AuthorizationScope> Scopes);
