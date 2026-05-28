using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using StudentSearch.Api.Configuration;
using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Infrastructure.Elasticsearch;

public sealed class ElasticsearchStudentIndexSeeder(
    IElasticsearchGateway gateway,
    SearchConfiguration configuration,
    RagConfiguration ragConfiguration,
    IEmbeddingClient embeddingClient,
    INarrativeRedactor redactor,
    ILogger<ElasticsearchStudentIndexSeeder> logger) : IStudentIndexSeeder
{
    private const int EmbeddingBatchSize = 64;

    public async Task<ReindexResponse> ReindexAsync()
    {
        if (!File.Exists(configuration.SeedDataPath))
        {
            throw new FileNotFoundException("Seed data file was not found.", configuration.SeedDataPath);
        }

        await gateway.DeleteIfExistsAsync($"/{configuration.IndexName}");
        await CreateIndexAsync();

        var seedText = await File.ReadAllTextAsync(configuration.SeedDataPath);
        var documents = JsonSerializer.Deserialize<List<StudentRecord>>(seedText, JsonOptions()) ?? [];

        var redactedNarratives = ComputeRedactedNarratives(documents);
        var embeddings = await ComputeEmbeddingsAsync(documents, redactedNarratives);

        var bulkPayload = new StringBuilder();
        foreach (var document in documents)
        {
            float[]? embedding = null;
            string? redactedNarrative = null;
            if (document.SafeguardingLog is not null)
            {
                redactedNarratives.TryGetValue(document.Student.Id, out redactedNarrative);
                embeddings.TryGetValue(document.Student.Id, out embedding);
            }

            bulkPayload.AppendLine(JsonSerializer.Serialize(new { index = new { _id = document.Student.Id } }, JsonOptions()));
            bulkPayload.AppendLine(BuildDocumentJson(document, redactedNarrative, embedding));
        }

        await gateway.SendRawAsync(HttpMethod.Post, $"/{configuration.IndexName}/_bulk", bulkPayload.ToString());
        await gateway.SendAsync(HttpMethod.Post, $"/{configuration.IndexName}/_refresh");

        return new ReindexResponse(configuration.IndexName, documents.Count, "ok");
    }

    private Dictionary<string, string> ComputeRedactedNarratives(List<StudentRecord> documents)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var record in documents)
        {
            if (record.SafeguardingLog is null || string.IsNullOrWhiteSpace(record.SafeguardingLog.Narrative))
            {
                continue;
            }
            result[record.Student.Id] = redactor.Redact(record.SafeguardingLog.Narrative, record.Student, record.School);
        }
        return result;
    }

    private async Task<Dictionary<string, float[]>> ComputeEmbeddingsAsync(List<StudentRecord> documents, Dictionary<string, string> redactedNarratives)
    {
        var result = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase);
        if (!ragConfiguration.IsEnabled)
        {
            logger.LogInformation("Skipping safeguarding embeddings: {Reason}", ragConfiguration.DisabledReason);
            return result;
        }

        var pending = documents
            .Where(record => record.SafeguardingLog is not null && redactedNarratives.ContainsKey(record.Student.Id))
            .Select(record => (record.Student.Id, Narrative: redactedNarratives[record.Student.Id]))
            .ToList();

        if (pending.Count == 0)
        {
            return result;
        }

        logger.LogInformation("Embedding {Count} redacted narratives with model {Model}.", pending.Count, ragConfiguration.EmbeddingModel);
        for (var offset = 0; offset < pending.Count; offset += EmbeddingBatchSize)
        {
            var batch = pending.Skip(offset).Take(EmbeddingBatchSize).ToList();
            var vectors = await embeddingClient.EmbedAsync(batch.ConvertAll(item => item.Narrative), inputType: "document");
            for (var i = 0; i < batch.Count; i++)
            {
                result[batch[i].Id] = vectors[i];
            }
        }

        return result;
    }

    private string BuildDocumentJson(StudentRecord document, string? redactedNarrative, float[]? embedding)
    {
        var node = JsonSerializer.SerializeToNode(document, JsonOptions())!.AsObject();

        if (document.SafeguardingLog is null)
        {
            node["safeguardingLog"] = null;
        }
        else
        {
            var log = node["safeguardingLog"]!.AsObject();
            if (redactedNarrative is not null)
            {
                log["narrativeRedacted"] = redactedNarrative;
            }
            if (embedding is not null)
            {
                var vector = new JsonArray();
                foreach (var value in embedding)
                {
                    vector.Add(value);
                }
                log["embedding"] = vector;
            }
        }

        return node.ToJsonString(JsonOptions());
    }

    private Task CreateIndexAsync()
    {
        var mapping = JsonNode.Parse($$"""
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
                  "id": { "type": "keyword", "normalizer": "lowercase_keyword" },
                  "name": { "type": "text", "fields": { "keyword": { "type": "keyword", "normalizer": "lowercase_keyword" } } },
                  "address": { "type": "text" }
                }
              },
              "trust": {
                "properties": {
                  "id": { "type": "keyword", "normalizer": "lowercase_keyword" },
                  "name": { "type": "text", "fields": { "keyword": { "type": "keyword", "normalizer": "lowercase_keyword" } } }
                }
              },
              "safeguardingLog": {
                "properties": {
                  "category": { "type": "keyword", "normalizer": "lowercase_keyword" },
                  "date": { "type": "date" },
                  "narrative": { "type": "text" },
                  "narrativeRedacted": { "type": "text" },
                  "embedding": {
                    "type": "dense_vector",
                    "dims": {{ragConfiguration.EmbeddingDimensions}},
                    "index": true,
                    "similarity": "cosine"
                  }
                }
              }
            }
          }
        }
        """)!;

        return gateway.SendAsync(HttpMethod.Put, $"/{configuration.IndexName}", mapping);
    }

    private static JsonSerializerOptions JsonOptions() => new(JsonSerializerDefaults.Web);
}
