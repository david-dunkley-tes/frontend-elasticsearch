using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using StudentSearch.Api.Configuration;
using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Infrastructure.Elasticsearch;

public sealed class ElasticsearchStudentIndexSeeder(
    IElasticsearchGateway gateway,
    SearchConfiguration configuration) : IStudentIndexSeeder
{
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
        var bulkPayload = new StringBuilder();

        foreach (var document in documents)
        {
            bulkPayload.AppendLine(JsonSerializer.Serialize(new { index = new { _id = document.Student.Id } }, JsonOptions()));
            bulkPayload.AppendLine(JsonSerializer.Serialize(document, JsonOptions()));
        }

        await gateway.SendRawAsync(HttpMethod.Post, $"/{configuration.IndexName}/_bulk", bulkPayload.ToString());
        await gateway.SendAsync(HttpMethod.Post, $"/{configuration.IndexName}/_refresh");

        return new ReindexResponse(configuration.IndexName, documents.Count, "ok");
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
              }
            }
          }
        }
        """)!;

        return gateway.SendAsync(HttpMethod.Put, $"/{configuration.IndexName}", mapping);
    }

    private static JsonSerializerOptions JsonOptions() => new(JsonSerializerDefaults.Web);
}
