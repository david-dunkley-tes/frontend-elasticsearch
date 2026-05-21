namespace StudentSearch.Api.Models;

public sealed record SavedSearch(
    string Id,
    string Name,
    string Query,
    Dictionary<string, List<string>> Filters,
    string Sort,
    int PageSize,
    DateTimeOffset CreatedAt);
