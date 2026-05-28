namespace StudentSearch.Api.Models;

public sealed record StudentSearchResult(
    string Id,
    Student Student,
    School School,
    Trust? Trust,
    SafeguardingLog? SafeguardingLog,
    IReadOnlyDictionary<string, string[]> Highlights,
    double? Score);
