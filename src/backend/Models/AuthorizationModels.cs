using System.Text.Json.Serialization;

namespace StudentSearch.Api.Models;

public sealed record DevAccessToken(string Sub, string? Name, List<AuthorizationScope> Scopes);

public sealed record AuthorizationScope(
    string Type,
    string? SchoolId = null,
    string? TrustId = null,
    string? SchoolGroupId = null,
    List<string>? SchoolIds = null);

public sealed record AuthorizedSchoolScope(bool IsGlobal, IReadOnlyCollection<string> SchoolIds)
{
    public static AuthorizedSchoolScope Global { get; } = new(true, []);
}

public sealed record CurrentUserResponse(string Sub, string? Name, IReadOnlyList<AuthorizationScope> Scopes);

[JsonSerializable(typeof(DevAccessToken))]
[JsonSerializable(typeof(List<AuthorizationScope>))]
public sealed partial class AuthorizationJsonContext : JsonSerializerContext;
