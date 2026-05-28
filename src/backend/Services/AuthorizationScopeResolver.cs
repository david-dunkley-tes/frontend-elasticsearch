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
        var scopesJson = user.FindFirstValue(DevBearerAuthenticationMiddleware.ScopesClaimType);
        if (string.IsNullOrWhiteSpace(scopesJson))
        {
            return new AuthorizedSchoolScope(false, []);
        }

        var scopes = JsonSerializer.Deserialize<List<AuthorizationScope>>(scopesJson, JsonDefaults.Web) ?? [];
        if (scopes.Any(scope => scope.Type.Equals("global", StringComparison.OrdinalIgnoreCase)))
        {
            return AuthorizedSchoolScope.Global;
        }

        var records = await ReadSeedRecordsAsync();
        var allowedSchoolIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var scope in scopes)
        {
            switch (scope.Type.ToLowerInvariant())
            {
                case "school":
                    if (!string.IsNullOrWhiteSpace(scope.SchoolId))
                    {
                        allowedSchoolIds.Add(scope.SchoolId);
                    }
                    break;

                case "trust":
                    if (!string.IsNullOrWhiteSpace(scope.TrustId))
                    {
                        foreach (var schoolId in records
                                     .Where(record => record.Trust?.Id.Equals(scope.TrustId, StringComparison.OrdinalIgnoreCase) == true)
                                     .Select(record => record.School.Id))
                        {
                            allowedSchoolIds.Add(schoolId);
                        }
                    }
                    break;

                case "schoolgroup":
                    foreach (var schoolId in scope.SchoolIds ?? [])
                    {
                        if (!string.IsNullOrWhiteSpace(schoolId))
                        {
                            allowedSchoolIds.Add(schoolId);
                        }
                    }
                    break;
            }
        }

        return new AuthorizedSchoolScope(false, allowedSchoolIds.ToArray());
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
