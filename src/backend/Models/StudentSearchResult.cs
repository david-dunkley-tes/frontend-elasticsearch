namespace StudentSearch.Api.Models;

public sealed record StudentSearchResult(string Id, Student Student, School School, Trust? Trust, IReadOnlyDictionary<string, string[]> Highlights, double? Score);
