using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using StudentSearch.Api.Configuration;
using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public sealed class ReindexService : IReindexService
{
    private readonly SearchConfiguration _configuration;
    private readonly IElasticsearchGateway _gateway;

    public ReindexService(IElasticsearchGateway gateway, SearchConfiguration configuration)
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

        await _gateway.SendRawAsync(HttpMethod.Post, $"/{_configuration.IndexName}/_bulk", bulkPayload.ToString());
        await _gateway.SendAsync(HttpMethod.Post, $"/{_configuration.IndexName}/_refresh");

        return new ReindexResponse(_configuration.IndexName, documents.Count, "ok");
    }

    private Task DeleteIndexIfExistsAsync()
    {
        return _gateway.DeleteIfExistsAsync($"/{_configuration.IndexName}");
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
