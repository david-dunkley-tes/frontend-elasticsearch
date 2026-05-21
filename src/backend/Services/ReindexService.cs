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
