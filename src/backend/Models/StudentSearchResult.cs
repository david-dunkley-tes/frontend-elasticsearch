namespace StudentSearch.Api.Models;

public sealed record StudentSearchResult(
    string Id,
    Student Student,
    School School,
    Trust? Trust,
    ClassGroup? ClassGroup,
    SafeguardingLog? SafeguardingLog,
    IReadOnlyDictionary<string, string[]> Highlights,
    double? Score);
