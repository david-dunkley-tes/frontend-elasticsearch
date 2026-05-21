using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using StudentSearch.Api.Configuration;

namespace StudentSearch.Api.Services;

public sealed class ElasticsearchGateway : IElasticsearchGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };
    private readonly HttpClient _httpClient;

    public ElasticsearchGateway(HttpClient httpClient, SearchConfiguration configuration)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri(configuration.ElasticsearchUrl);
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
