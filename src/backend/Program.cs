using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("ViteDev", policy =>
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.AddHttpClient<ElasticsearchGateway>();
builder.Services.AddSingleton<SearchConfiguration>();
builder.Services.AddScoped<StudentSearchService>();
builder.Services.AddScoped<ReindexService>();

var app = builder.Build();

app.UseCors("ViteDev");

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/search", async (SearchRequest request, StudentSearchService searchService) =>
{
    var response = await searchService.SearchAsync(request);
    return Results.Ok(response);
});

if (app.Environment.IsDevelopment())
{
    app.MapPost("/api/admin/reindex", async (ReindexService reindexService) =>
    {
        var response = await reindexService.ReindexAsync();
        return Results.Ok(response);
    });
}

app.Run();

public sealed class SearchConfiguration
{
    public string ElasticsearchUrl { get; }
    public string IndexName { get; }
    public string SeedDataPath { get; }

    public SearchConfiguration(IConfiguration configuration, IWebHostEnvironment environment)
    {
        ElasticsearchUrl = configuration["Elasticsearch:Url"] ?? "http://localhost:9200";
        IndexName = configuration["Elasticsearch:IndexName"] ?? "students";
        var configuredPath = configuration["SeedData:Path"] ?? "data/students.seed.json";
        SeedDataPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", configuredPath));
    }
}

public sealed class ElasticsearchGateway
{
    private readonly HttpClient _httpClient;
    private readonly SearchConfiguration _configuration;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    public ElasticsearchGateway(HttpClient httpClient, SearchConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _httpClient.BaseAddress = new Uri(_configuration.ElasticsearchUrl);
    }

    public async Task<JsonNode?> SendAsync(HttpMethod method, string path, JsonNode? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null)
        {
            request.Content = new StringContent(body.ToJsonString(JsonOptions), Encoding.UTF8, "application/json");
        }

        using var response = await _httpClient.SendAsync(request);
        var text = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Elasticsearch {method} {path} failed with {(int)response.StatusCode}: {text}");
        }

        return string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
    }
}

public sealed class ReindexService
{
    private readonly ElasticsearchGateway _gateway;
    private readonly SearchConfiguration _configuration;

    public ReindexService(ElasticsearchGateway gateway, SearchConfiguration configuration)
    {
        _gateway = gateway;
        _configuration = configuration;
    }

    public async Task<ReindexResponse> ReindexAsync()
    {
        if (!File.Exists(_configuration.SeedDataPath))
        {
            throw new FileNotFoundException("Seed data file was not found.", _configuration.SeedDataPath);
        }

        await DeleteIndexIfExistsAsync();
        await CreateIndexAsync();

        var seedText = await File.ReadAllTextAsync(_configuration.SeedDataPath);
        var documents = JsonSerializer.Deserialize<List<StudentRecord>>(seedText, JsonOptions()) ?? [];
        var bulkPayload = new StringBuilder();

        foreach (var document in documents)
        {
            bulkPayload.AppendLine(JsonSerializer.Serialize(new { index = new { _id = document.Student.Id } }, JsonOptions()));
            bulkPayload.AppendLine(JsonSerializer.Serialize(document, JsonOptions()));
        }

        using var client = new HttpClient { BaseAddress = new Uri(_configuration.ElasticsearchUrl) };
        using var content = new StringContent(bulkPayload.ToString(), Encoding.UTF8, "application/x-ndjson");
        using var response = await client.PostAsync($"/{_configuration.IndexName}/_bulk?refresh=true", content);
        var responseText = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Bulk indexing failed with {(int)response.StatusCode}: {responseText}");
        }

        return new ReindexResponse(_configuration.IndexName, documents.Count, "ok");
    }

    private async Task DeleteIndexIfExistsAsync()
    {
        using var client = new HttpClient { BaseAddress = new Uri(_configuration.ElasticsearchUrl) };
        using var response = await client.DeleteAsync($"/{_configuration.IndexName}");
        if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            var text = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Deleting index failed with {(int)response.StatusCode}: {text}");
        }
    }

    private Task CreateIndexAsync()
    {
        JsonNode mapping = JsonNode.Parse("""
        {
          "settings": {
            "analysis": {
              "normalizer": {
                "lowercase_keyword": {
                  "type": "custom",
                  "filter": ["lowercase", "asciifolding"]
                }
              }
            }
          },
          "mappings": {
            "properties": {
              "student": {
                "properties": {
                  "id": { "type": "keyword", "normalizer": "lowercase_keyword" },
                  "foreName": { "type": "text", "fields": { "keyword": { "type": "keyword", "normalizer": "lowercase_keyword" } } },
                  "surname": { "type": "text", "fields": { "keyword": { "type": "keyword", "normalizer": "lowercase_keyword" } } },
                  "fullName": { "type": "text" },
                  "yearGroup": { "type": "keyword", "normalizer": "lowercase_keyword" }
                }
              },
              "school": {
                "properties": {
                  "name": { "type": "text", "fields": { "keyword": { "type": "keyword", "normalizer": "lowercase_keyword" } } },
                  "address": { "type": "text" }
                }
              },
              "trust": {
                "properties": {
                  "name": { "type": "text", "fields": { "keyword": { "type": "keyword", "normalizer": "lowercase_keyword" } } }
                }
              }
            }
          }
        }
        """)!;

        return _gateway.SendAsync(HttpMethod.Put, $"/{_configuration.IndexName}", mapping);
    }

    private static JsonSerializerOptions JsonOptions() => new(JsonSerializerDefaults.Web);
}

public sealed class StudentSearchService
{
    private const string NoTrustValue = "__NO_TRUST__";
    private readonly ElasticsearchGateway _gateway;
    private readonly SearchConfiguration _configuration;

    private static readonly FacetDefinition[] Facets =
    [
        new("yearGroup", "Year group", "student.yearGroup"),
        new("school", "School", "school.name.keyword"),
        new("trust", "Trust", "trust.name.keyword", SupportsMissing: true)
    ];

    public StudentSearchService(ElasticsearchGateway gateway, SearchConfiguration configuration)
    {
        _gateway = gateway;
        _configuration = configuration;
    }

    public async Task<SearchResponse> SearchAsync(SearchRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        var normalized = NormalizeRequest(request);
        var query = BuildSearchPayload(normalized);
        var response = await _gateway.SendAsync(HttpMethod.Post, $"/{_configuration.IndexName}/_search", query);
        stopwatch.Stop();

        return MapResponse(normalized, query, response!, stopwatch.ElapsedMilliseconds);
    }

    private static SearchRequest NormalizeRequest(SearchRequest request)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize <= 0 ? 25 : request.PageSize, 1, 100);
        var filters = request.Filters.ToDictionary(
            item => item.Key,
            item => item.Value.Where(value => !string.IsNullOrWhiteSpace(value)).Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            StringComparer.OrdinalIgnoreCase);

        return request with
        {
            Query = request.Query?.Trim() ?? string.Empty,
            Page = page,
            PageSize = pageSize,
            Filters = filters
        };
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
        return new JsonObject
        {
            ["bool"] = new JsonObject
            {
                ["should"] = new JsonArray
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
                    },
                    new JsonObject
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
                    }
                },
                ["minimum_should_match"] = 1
            }
        };
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

public sealed record FacetDefinition(string Id, string Label, string Field, bool SupportsMissing = false);
public sealed record SearchRequest(string Query = "", Dictionary<string, List<string>>? Filters = null, string Sort = "relevance", int Page = 1, int PageSize = 25, bool DebugMode = false)
{
    public Dictionary<string, List<string>> Filters { get; init; } = Filters ?? new(StringComparer.OrdinalIgnoreCase);
}

public sealed record SearchResponse(int Total, int TookMs, long BackendTookMs, IReadOnlyList<StudentSearchResult> Results, IReadOnlyDictionary<string, FacetResponse> Facets, SearchDebug? Debug);
public sealed record StudentSearchResult(string Id, Student Student, School School, Trust? Trust, IReadOnlyDictionary<string, string[]> Highlights, double? Score);
public sealed record FacetResponse(string Label, string Type, IReadOnlyList<string> Selected, IReadOnlyList<FacetOption> Options);
public sealed record FacetOption(string Value, string Label, int Count, bool Selected);
public sealed record SearchDebug(JsonObject ElasticsearchQuery, IReadOnlyDictionary<string, List<string>> SelectedFilters);
public sealed record ReindexResponse(string IndexName, int DocumentsIndexed, string Status);
public sealed record StudentRecord(Student Student, School School, Trust? Trust);
public sealed record Student(string Id, string ForeName, string Surname, string FullName, string YearGroup);
public sealed record School(string Name, string Address);
public sealed record Trust(string Name);
