namespace StudentSearch.Api.Models;

public sealed record SaveSearchRequest(
    string Name,
    string Query = "",
    Dictionary<string, List<string>>? Filters = null,
    string Sort = "relevance",
    int PageSize = 10)
{
    public Dictionary<string, List<string>> Filters { get; init; } = Filters ?? new(StringComparer.OrdinalIgnoreCase);
}
