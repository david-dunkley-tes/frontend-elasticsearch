namespace StudentSearch.Api.Models;

public sealed record FacetDefinition(string Id, string Label, string Field, bool SupportsMissing = false);
