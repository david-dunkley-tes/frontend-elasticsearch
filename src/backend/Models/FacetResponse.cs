namespace StudentSearch.Api.Models;

public sealed record FacetResponse(string Label, string Type, IReadOnlyList<string> Selected, IReadOnlyList<FacetOption> Options);
