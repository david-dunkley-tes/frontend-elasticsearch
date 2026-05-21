using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using StudentSearch.Api.Configuration;
using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Infrastructure.Elasticsearch;

public sealed class ElasticsearchStudentSearchIndex : IStudentSearchIndex
{
    private const string NoTrustValue = "__NO_TRUST__";
    private readonly SearchConfiguration _configuration;
    private readonly IElasticsearchGateway _gateway;

    private static readonly FacetDefinition[] Facets =
    [
        new("yearGroup", "Year group", "student.yearGroup"),
        new("school", "School", "school.name.keyword"),
        new("trust", "Trust", "trust.name.keyword", SupportsMissing: true)
    ];

    public ElasticsearchStudentSearchIndex(IElasticsearchGateway gateway, SearchConfiguration configuration)
    {
        _gateway = gateway;
        _configuration = configuration;
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var query = BuildSearchPayload(request);
        var response = await _gateway.SendAsync(HttpMethod.Post, $"/{_configuration.IndexName}/_search", query);
        stopwatch.Stop();

        return MapResponse(request, query, response!, stopwatch.ElapsedMilliseconds);
    }

    private static JsonObject BuildSearchPayload(SearchRequest request)
    {
        var from = (request.Page - 1) * request.PageSize;
        var payload = new JsonObject
        {
            ["from"] = from,
            ["size"] = request.PageSize,
            ["track_total_hits"] = true,
            ["query"] = BuildMainQuery(request),
            ["highlight"] = BuildHighlight(),
            ["aggs"] = BuildFacetAggregations(request)
        };

        payload["sort"] = string.IsNullOrWhiteSpace(request.Query)
            ? new JsonArray(
                new JsonObject { ["student.surname.keyword"] = new JsonObject { ["order"] = "asc" } },
                new JsonObject { ["student.foreName.keyword"] = new JsonObject { ["order"] = "asc" } })
            : new JsonArray(new JsonObject { ["_score"] = new JsonObject { ["order"] = "desc" } });

        return payload;
    }

    private static JsonObject BuildMainQuery(SearchRequest request)
    {
        var filters = BuildFilterClauses(request.Filters, null);
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return new JsonObject
            {
                ["bool"] = new JsonObject
                {
                    ["must"] = new JsonArray(new JsonObject { ["match_all"] = new JsonObject() }),
                    ["filter"] = filters
                }
            };
        }

        return new JsonObject
        {
            ["bool"] = new JsonObject
            {
                ["must"] = new JsonArray(BuildFreeTextQuery(request.Query)),
                ["filter"] = filters
            }
        };
    }

    private static JsonObject BuildFreeTextQuery(string query)
    {
        var shouldClauses = new JsonArray
        {
            new JsonObject
            {
                ["term"] = new JsonObject
                {
                    ["student.id"] = new JsonObject { ["value"] = query.ToLowerInvariant(), ["boost"] = 20 }
                }
            },
            new JsonObject
            {
                ["multi_match"] = new JsonObject
                {
                    ["query"] = query,
                    ["type"] = "phrase",
                    ["fields"] = new JsonArray("student.fullName^8", "student.surname^10", "school.name^5", "trust.name^5"),
                    ["boost"] = 4
                }
            }
        };

        if (ShouldUsePrefixSearch(query))
        {
            shouldClauses.Add(new JsonObject
            {
                ["multi_match"] = new JsonObject
                {
                    ["query"] = query,
                    ["type"] = "bool_prefix",
                    ["fields"] = new JsonArray("student.fullName^4", "student.foreName^3", "student.surname^5", "school.name^3", "trust.name^3", "school.address^1"),
                    ["boost"] = 1.5
                }
            });
        }

        shouldClauses.Add(new JsonObject
        {
            ["multi_match"] = new JsonObject
            {
                ["query"] = query,
                ["type"] = "best_fields",
                ["fields"] = new JsonArray("student.id^12", "student.fullName^6", "student.foreName^4", "student.surname^8", "student.yearGroup^2", "school.name^4", "trust.name^4", "school.address^1"),
                ["fuzziness"] = "AUTO",
                ["prefix_length"] = 1,
                ["boost"] = 2
            }
        });

        return new JsonObject
        {
            ["bool"] = new JsonObject
            {
                ["should"] = shouldClauses,
                ["minimum_should_match"] = 1
            }
        };
    }

    private static bool ShouldUsePrefixSearch(string query)
    {
        var tokens = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return tokens.Length == 1;
    }

    private static JsonArray BuildFilterClauses(IReadOnlyDictionary<string, List<string>> filters, string? excludeFacetId)
    {
        var clauses = new JsonArray();
        foreach (var facet in Facets)
        {
            if (facet.Id.Equals(excludeFacetId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!filters.TryGetValue(facet.Id, out var values) || values.Count == 0)
            {
                continue;
            }

            clauses.Add(BuildFacetFilter(facet, values));
        }

        return clauses;
    }

    private static JsonObject BuildFacetFilter(FacetDefinition facet, IReadOnlyCollection<string> values)
    {
        if (!facet.SupportsMissing)
        {
            return new JsonObject { ["terms"] = new JsonObject { [facet.Field] = ToJsonArray(values) } };
        }

        var namedValues = values.Where(value => !value.Equals(NoTrustValue, StringComparison.OrdinalIgnoreCase)).ToArray();
        var includeMissing = values.Any(value => value.Equals(NoTrustValue, StringComparison.OrdinalIgnoreCase));
        var should = new JsonArray();

        if (namedValues.Length > 0)
        {
            should.Add(new JsonObject { ["terms"] = new JsonObject { [facet.Field] = ToJsonArray(namedValues) } });
        }

        if (includeMissing)
        {
            should.Add(new JsonObject
            {
                ["bool"] = new JsonObject
                {
                    ["must_not"] = new JsonArray(new JsonObject { ["exists"] = new JsonObject { ["field"] = facet.Field } })
                }
            });
        }

        return new JsonObject
        {
            ["bool"] = new JsonObject { ["should"] = should, ["minimum_should_match"] = 1 }
        };
    }

    private static JsonObject BuildFacetAggregations(SearchRequest request)
    {
        var aggs = new JsonObject();
        foreach (var facet in Facets)
        {
            var facetAggs = new JsonObject
            {
                ["values"] = new JsonObject
                {
                    ["terms"] = new JsonObject
                    {
                        ["field"] = facet.Field,
                        ["size"] = 100,
                        ["order"] = new JsonObject { ["_key"] = "asc" }
                    }
                }
            };

            if (facet.SupportsMissing)
            {
                facetAggs["missing"] = new JsonObject
                {
                    ["missing"] = new JsonObject { ["field"] = facet.Field }
                };
            }

            aggs[facet.Id] = new JsonObject
            {
                ["filter"] = BuildAggregationFilter(request, facet.Id),
                ["aggs"] = facetAggs
            };
        }

        return aggs;
    }

    private static JsonArray ToJsonArray(IEnumerable<string> values)
    {
        var array = new JsonArray();
        foreach (var value in values)
        {
            array.Add(value);
        }

        return array;
    }

    private static JsonObject BuildAggregationFilter(SearchRequest request, string facetId)
    {
        var filters = BuildFilterClauses(request.Filters, facetId);
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return new JsonObject
            {
                ["bool"] = new JsonObject
                {
                    ["must"] = new JsonArray(new JsonObject { ["match_all"] = new JsonObject() }),
                    ["filter"] = filters
                }
            };
        }

        return new JsonObject
        {
            ["bool"] = new JsonObject
            {
                ["must"] = new JsonArray(BuildFreeTextQuery(request.Query)),
                ["filter"] = filters
            }
        };
    }

    private static JsonObject BuildHighlight()
    {
        var fields = new JsonObject();
        foreach (var field in new[] { "student.id", "student.fullName", "student.foreName", "student.surname", "student.yearGroup", "school.name", "school.address", "trust.name" })
        {
            fields[field] = new JsonObject();
        }

        return new JsonObject
        {
            ["pre_tags"] = new JsonArray("<mark>"),
            ["post_tags"] = new JsonArray("</mark>"),
            ["fields"] = fields
        };
    }

    private SearchResponse MapResponse(SearchRequest request, JsonObject query, JsonNode response, long backendTookMs)
    {
        var hitsNode = response["hits"]!;
        var total = hitsNode["total"]?["value"]?.GetValue<int>() ?? 0;
        var tookMs = response["took"]?.GetValue<int>() ?? 0;
        var results = new List<StudentSearchResult>();

        foreach (var hit in hitsNode["hits"]!.AsArray())
        {
            if (hit is null)
            {
                continue;
            }

            var source = hit["_source"]!;
            var record = source.Deserialize<StudentRecord>(JsonOptions())!;
            var highlight = hit["highlight"]?.Deserialize<Dictionary<string, string[]>>(JsonOptions()) ?? new();
            results.Add(new StudentSearchResult(record.Student.Id, record.Student, record.School, record.Trust, highlight, hit["_score"]?.GetValue<double?>()));
        }

        return new SearchResponse(
            total,
            tookMs,
            backendTookMs,
            results,
            MapFacets(request, response["aggregations"]!),
            request.DebugMode ? new SearchDebug(query, request.Filters) : null);
    }

    private static Dictionary<string, FacetResponse> MapFacets(SearchRequest request, JsonNode aggregations)
    {
        var response = new Dictionary<string, FacetResponse>();
        foreach (var facet in Facets)
        {
            var selected = request.Filters.TryGetValue(facet.Id, out var selectedValues) ? selectedValues : [];
            var options = new List<FacetOption>();
            var buckets = aggregations[facet.Id]?["values"]?["buckets"]?.AsArray() ?? [];

            foreach (var bucket in buckets)
            {
                var value = bucket?["key"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                options.Add(new FacetOption(value, ToTitle(value), bucket!["doc_count"]!.GetValue<int>(), selected.Contains(value, StringComparer.OrdinalIgnoreCase)));
            }

            if (facet.SupportsMissing)
            {
                var missingCount = aggregations[facet.Id]?["missing"]?["doc_count"]?.GetValue<int>() ?? 0;
                if (missingCount > 0 || selected.Contains(NoTrustValue, StringComparer.OrdinalIgnoreCase))
                {
                    options.Add(new FacetOption(NoTrustValue, "No trust", missingCount, selected.Contains(NoTrustValue, StringComparer.OrdinalIgnoreCase)));
                }
            }

            var orderedOptions = options
                .OrderByDescending(option => option.Selected)
                .ThenBy(option => option.Label, StringComparer.OrdinalIgnoreCase)
                .ToList();

            response[facet.Id] = new FacetResponse(facet.Label, "multi-select", selected, orderedOptions);
        }

        return response;
    }

    private static string ToTitle(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(part => char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private static JsonSerializerOptions JsonOptions() => new(JsonSerializerDefaults.Web);
}
