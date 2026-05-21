using System.Text.Json.Nodes;

namespace StudentSearch.Api.Models;

public sealed record SearchDebug(JsonObject ElasticsearchQuery, IReadOnlyDictionary<string, List<string>> SelectedFilters);
