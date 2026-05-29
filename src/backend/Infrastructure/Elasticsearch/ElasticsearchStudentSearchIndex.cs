using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using StudentSearch.Api.Configuration;
using StudentSearch.Api.Interfaces;
using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Infrastructure.Elasticsearch;

public sealed class ElasticsearchStudentSearchIndex(
    IElasticsearchGateway gateway,
    SearchConfiguration configuration) : IStudentSearchIndex
{
    private const string NoTrustValue = "__NO_TRUST__";

    private static readonly FacetDefinition[] Facets =
    [
        new("yearGroup", "Year group", "student.yearGroup"),
        new("school", "School", "school.name.keyword"),
        new("trust", "Trust", "trust.name.keyword", SupportsMissing: true),
        // Case-preserving combined "Class — Teacher" label; the frontend only shows it once the
        // result set is small (≤5 options), so it isn't title-cased and keeps the original casing.
        new("classTeacher", "Class", "classGroup.label", RawLabel: true)
    ];

    public async Task<SearchResponse> SearchAsync(SearchRequest request, AuthorizedSchoolScope authorizationScope, AuthorizedSchoolScope? safeguardingScope = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var query = BuildSearchPayload(request, authorizationScope);
        var response = await gateway.SendAsync(HttpMethod.Post, $"/{configuration.IndexName}/_search", query);
        stopwatch.Stop();

        // Null means "not restricted separately" — fall back to the viewing scope.
        return MapResponse(request, query, response!, stopwatch.ElapsedMilliseconds, safeguardingScope ?? authorizationScope);
    }

    private static JsonObject BuildSearchPayload(SearchRequest request, AuthorizedSchoolScope authorizationScope)
    {
        var from = (request.Page - 1) * request.PageSize;
        var payload = new JsonObject
        {
            ["from"] = from,
            ["size"] = request.PageSize,
            ["track_total_hits"] = true,
            ["query"] = BuildMainQuery(request, authorizationScope),
            ["highlight"] = BuildHighlight(),
            ["aggs"] = BuildFacetAggregations(request, authorizationScope)
        };

        payload["sort"] = string.IsNullOrWhiteSpace(request.Query)
            ? new JsonArray(
                new JsonObject { ["student.surname.keyword"] = new JsonObject { ["order"] = "asc" } },
                new JsonObject { ["student.foreName.keyword"] = new JsonObject { ["order"] = "asc" } })
            : new JsonArray(new JsonObject { ["_score"] = new JsonObject { ["order"] = "desc" } });

        return payload;
    }

    private static JsonObject BuildMainQuery(SearchRequest request, AuthorizedSchoolScope authorizationScope)
    {
        var filters = BuildFilterClauses(request.Filters, request.StudentIds, null, authorizationScope);
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
                    ["fields"] = new JsonArray("student.fullName^8", "student.surname^10", "school.name^5", "trust.name^5", "classGroup.name^5", "classGroup.teacher^5"),
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
                    ["fields"] = new JsonArray("student.fullName^4", "student.foreName^3", "student.surname^5", "school.name^3", "trust.name^3", "classGroup.name^3", "classGroup.teacher^3", "school.address^1"),
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
                ["fields"] = new JsonArray("student.id^12", "student.fullName^6", "student.foreName^4", "student.surname^8", "student.yearGroup^2", "school.name^4", "trust.name^4", "classGroup.name^4", "classGroup.teacher^4", "school.address^1"),
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

    private static JsonArray BuildFilterClauses(IReadOnlyDictionary<string, List<string>> filters, IReadOnlyList<string> studentIds, string? excludeFacetId, AuthorizedSchoolScope authorizationScope)
    {
        var clauses = new JsonArray();
        AddAuthorizationFilter(clauses, authorizationScope);
        AddStudentIdFilter(clauses, studentIds);

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

    private static void AddAuthorizationFilter(JsonArray clauses, AuthorizedSchoolScope authorizationScope)
    {
        if (authorizationScope.IsGlobal)
        {
            return;
        }

        if (authorizationScope.SchoolIds.Count == 0)
        {
            clauses.Add(new JsonObject
            {
                ["bool"] = new JsonObject
                {
                    ["must_not"] = new JsonArray(new JsonObject { ["match_all"] = new JsonObject() })
                }
            });
            return;
        }

        clauses.Add(new JsonObject
        {
            ["terms"] = new JsonObject { ["school.id"] = ToJsonArray(authorizationScope.SchoolIds.Select(id => id.ToLowerInvariant())) }
        });
    }

    private static void AddStudentIdFilter(JsonArray clauses, IReadOnlyList<string> studentIds)
    {
        if (studentIds.Count == 0)
        {
            return;
        }

        clauses.Add(new JsonObject
        {
            ["terms"] = new JsonObject { ["student.id"] = ToJsonArray(studentIds.Select(id => id.ToLowerInvariant())) }
        });
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

    private static JsonObject BuildFacetAggregations(SearchRequest request, AuthorizedSchoolScope authorizationScope)
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
                ["filter"] = BuildAggregationFilter(request, facet.Id, authorizationScope),
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

    private static JsonObject BuildAggregationFilter(SearchRequest request, string facetId, AuthorizedSchoolScope authorizationScope)
    {
        var filters = BuildFilterClauses(request.Filters, request.StudentIds, facetId, authorizationScope);
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
        foreach (var field in new[] { "student.id", "student.fullName", "student.foreName", "student.surname", "student.yearGroup", "school.name", "school.address", "trust.name", "classGroup.name", "classGroup.teacher" })
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

    private SearchResponse MapResponse(SearchRequest request, JsonObject query, JsonNode response, long backendTookMs, AuthorizedSchoolScope safeguardingScope)
    {
        var hitsNode = response["hits"]!;
        var total = hitsNode["total"]?["value"]?.GetValue<int>() ?? 0;
        var tookMs = response["took"]?.GetValue<int>() ?? 0;
        var results = new List<StudentSearchResult>();

        var safeguardingSchools = safeguardingScope.IsGlobal
            ? null
            : new HashSet<string>(safeguardingScope.SchoolIds, StringComparer.OrdinalIgnoreCase);

        foreach (var hit in hitsNode["hits"]!.AsArray())
        {
            if (hit is null)
            {
                continue;
            }

            var source = hit["_source"]!;
            var record = source.Deserialize<StudentRecord>(JsonDefaults.Web)!;
            var highlight = hit["highlight"]?.Deserialize<Dictionary<string, string[]>>(JsonDefaults.Web) ?? new();

            // Hide safeguarding from anyone without the DSL role for that student's school.
            var canSeeSafeguarding = safeguardingSchools is null || safeguardingSchools.Contains(record.School.Id);
            var safeguardingLog = canSeeSafeguarding ? record.SafeguardingLog : null;

            results.Add(new StudentSearchResult(record.Student.Id, record.Student, record.School, record.Trust, record.ClassGroup, safeguardingLog, highlight, hit["_score"]?.GetValue<double?>()));
        }

        return new SearchResponse(
            total,
            tookMs,
            backendTookMs,
            results,
            MapFacets(request, response["aggregations"]!),
            request.DebugMode ? new SearchDebug(query.ToJsonString(JsonDefaults.WebIndented), request.Filters) : null);
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

                var label = facet.RawLabel ? value : ToTitle(value);
                options.Add(new FacetOption(value, label, bucket!["doc_count"]!.GetValue<int>(), selected.Contains(value, StringComparer.OrdinalIgnoreCase)));
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
}
