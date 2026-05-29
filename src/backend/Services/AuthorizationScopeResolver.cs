using System.Security.Claims;
using System.Text.Json;
using StudentSearch.Api.Configuration;
using StudentSearch.Api.Infrastructure;
using StudentSearch.Api.Interfaces;
using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public sealed class AuthorizationScopeResolver(SearchConfiguration configuration) : IAuthorizationScopeResolver
{
    public async Task<AuthorizedSchoolScope> ResolveAsync(ClaimsPrincipal user)
    {
        var scopes = ReadScopes(user);
        if (scopes.Count == 0)
        {
            return new AuthorizedSchoolScope(false, []);
        }

        if (scopes.Any(scope => scope.Type.Equals("global", StringComparison.OrdinalIgnoreCase)))
        {
            return AuthorizedSchoolScope.Global;
        }

        var records = await ReadSeedRecordsAsync();
        var allowedSchoolIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var scope in scopes)
        {
            foreach (var schoolId in ExpandScope(scope, records, allSchoolIds: null))
            {
                allowedSchoolIds.Add(schoolId);
            }
        }

        return new AuthorizedSchoolScope(false, allowedSchoolIds.ToArray());
    }

    public async Task<AuthorizedSchoolScope> ResolveRoleScopeAsync(ClaimsPrincipal user, string role)
    {
        var scopes = ReadScopes(user);
        if (scopes.Count == 0)
        {
            return new AuthorizedSchoolScope(false, []);
        }

        var records = await ReadSeedRecordsAsync();
        var allSchoolIds = records.Select(record => record.School.Id).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        // For each school keep the role list of the most specific covering scope (school > trust > global);
        // scopes that tie on specificity merge their roles.
        var bestSpecificity = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var rolesAtBest = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var scope in scopes)
        {
            var specificity = Specificity(scope.Type);
            if (specificity == 0)
            {
                continue;
            }

            foreach (var schoolId in ExpandScope(scope, records, allSchoolIds))
            {
                if (!bestSpecificity.TryGetValue(schoolId, out var current) || specificity > current)
                {
                    bestSpecificity[schoolId] = specificity;
                    rolesAtBest[schoolId] = new HashSet<string>(scope.Role ?? [], StringComparer.OrdinalIgnoreCase);
                }
                else if (specificity == current)
                {
                    foreach (var scopeRole in scope.Role ?? [])
                    {
                        rolesAtBest[schoolId].Add(scopeRole);
                    }
                }
            }
        }

        var granted = rolesAtBest
            .Where(entry => entry.Value.Contains(role))
            .Select(entry => entry.Key)
            .ToArray();

        if (granted.Length > 0 && granted.Length == allSchoolIds.Length)
        {
            return AuthorizedSchoolScope.Global;
        }

        return new AuthorizedSchoolScope(false, granted);
    }

    private static int Specificity(string type) => type.ToLowerInvariant() switch
    {
        "school" => 3,
        "trust" => 2,
        "global" => 1,
        _ => 0
    };

    private static IEnumerable<string> ExpandScope(AuthorizationScope scope, List<StudentRecord> records, string[]? allSchoolIds)
    {
        switch (scope.Type.ToLowerInvariant())
        {
            case "global":
                return allSchoolIds ?? records.Select(record => record.School.Id).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

            case "trust":
                if (string.IsNullOrWhiteSpace(scope.TrustId))
                {
                    return [];
                }
                return records
                    .Where(record => record.Trust?.Id.Equals(scope.TrustId, StringComparison.OrdinalIgnoreCase) == true)
                    .Select(record => record.School.Id)
                    .Distinct(StringComparer.OrdinalIgnoreCase);

            case "school":
                return string.IsNullOrWhiteSpace(scope.SchoolId) ? [] : [scope.SchoolId];

            default:
                return [];
        }
    }

    private static List<AuthorizationScope> ReadScopes(ClaimsPrincipal user)
    {
        var scopesJson = user.FindFirstValue(DevBearerAuthenticationMiddleware.ScopesClaimType);
        if (string.IsNullOrWhiteSpace(scopesJson))
        {
            return [];
        }

        return JsonSerializer.Deserialize<List<AuthorizationScope>>(scopesJson, JsonDefaults.Web) ?? [];
    }

    private async Task<List<StudentRecord>> ReadSeedRecordsAsync()
    {
        if (!File.Exists(configuration.SeedDataPath))
        {
            return [];
        }

        var json = await File.ReadAllTextAsync(configuration.SeedDataPath);
        return JsonSerializer.Deserialize<List<StudentRecord>>(json, JsonDefaults.Web) ?? [];
    }
}
