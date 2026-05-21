namespace StudentSearch.Api.Models;

public sealed record SearchResponse(int Total, int TookMs, long BackendTookMs, IReadOnlyList<StudentSearchResult> Results, IReadOnlyDictionary<string, FacetResponse> Facets, SearchDebug? Debug);
