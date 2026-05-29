namespace StudentSearch.Api.Models;

public sealed record SearchRequest(string Query = "", Dictionary<string, List<string>>? Filters = null, IReadOnlyList<string>? StudentIds = null, string Sort = "relevance", int Page = 1, int PageSize = 25, bool DebugMode = false)
{
    public Dictionary<string, List<string>> Filters { get; init; } = Filters ?? new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<string> StudentIds { get; init; } = StudentIds ?? [];
}
