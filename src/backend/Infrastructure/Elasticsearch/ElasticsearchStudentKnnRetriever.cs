using System.Text.Json;
using System.Text.Json.Nodes;
using StudentSearch.Api.Configuration;
using StudentSearch.Api.Interfaces;
using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Infrastructure.Elasticsearch;

public sealed class ElasticsearchStudentKnnRetriever(
    IElasticsearchGateway gateway,
    SearchConfiguration configuration) : IStudentKnnRetriever
{
    private const string EmbeddingField = "safeguardingLog.embedding";
    private const int NumCandidatesMultiplier = 8;

    public async Task<KnnSearchResult> RetrieveAsync(
        float[] queryVector,
        int topK,
        AuthorizedSchoolScope authorizationScope,
        CancellationToken cancellationToken = default)
    {
        var query = BuildKnnPayload(queryVector, topK, authorizationScope);
        var response = await gateway.SendAsync(HttpMethod.Post, $"/{configuration.IndexName}/_search", query);
        var hits = MapHits(response);
        return new KnnSearchResult(hits, query);
    }

    private static JsonObject BuildKnnPayload(float[] queryVector, int topK, AuthorizedSchoolScope authorizationScope)
    {
        var vector = new JsonArray();
        foreach (var component in queryVector)
        {
            vector.Add(component);
        }

        var knn = new JsonObject
        {
            ["field"] = EmbeddingField,
            ["query_vector"] = vector,
            ["k"] = topK,
            ["num_candidates"] = Math.Max(topK * NumCandidatesMultiplier, 50),
        };

        var prefilter = BuildPrefilter(authorizationScope);
        if (prefilter is not null)
        {
            knn["filter"] = prefilter;
        }

        return new JsonObject
        {
            ["size"] = topK,
            ["knn"] = knn,
            ["_source"] = new JsonArray("student", "school", "trust", "safeguardingLog.category", "safeguardingLog.date", "safeguardingLog.narrative", "safeguardingLog.narrativeRedacted")
        };
    }

    private static JsonArray? BuildPrefilter(AuthorizedSchoolScope authorizationScope)
    {
        var filter = new JsonArray
        {
            new JsonObject { ["exists"] = new JsonObject { ["field"] = "safeguardingLog.narrative" } }
        };

        if (!authorizationScope.IsGlobal)
        {
            if (authorizationScope.SchoolIds.Count == 0)
            {
                filter.Add(new JsonObject
                {
                    ["bool"] = new JsonObject
                    {
                        ["must_not"] = new JsonArray(new JsonObject { ["match_all"] = new JsonObject() })
                    }
                });
            }
            else
            {
                var values = new JsonArray();
                foreach (var schoolId in authorizationScope.SchoolIds)
                {
                    values.Add(schoolId.ToLowerInvariant());
                }
                filter.Add(new JsonObject
                {
                    ["terms"] = new JsonObject { ["school.id"] = values }
                });
            }
        }

        return filter;
    }

    private static IReadOnlyList<KnnHit> MapHits(JsonNode? response)
    {
        var hits = new List<KnnHit>();
        var hitNodes = response?["hits"]?["hits"]?.AsArray();
        if (hitNodes is null)
        {
            return hits;
        }

        foreach (var hitNode in hitNodes)
        {
            if (hitNode is null)
            {
                continue;
            }

            var source = hitNode["_source"];
            if (source is null)
            {
                continue;
            }

            var record = source.Deserialize<StudentRecord>(JsonDefaults.Web);
            if (record is null || record.SafeguardingLog is null)
            {
                continue;
            }

            var score = hitNode["_score"]?.GetValue<double>() ?? 0.0;
            hits.Add(new KnnHit(record, score));
        }

        return hits;
    }

}
