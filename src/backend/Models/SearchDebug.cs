namespace StudentSearch.Api.Models;

public sealed record SearchDebug(string ElasticsearchQuery, IReadOnlyDictionary<string, List<string>> SelectedFilters);
