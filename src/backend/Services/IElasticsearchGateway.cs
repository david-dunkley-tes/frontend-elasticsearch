using System.Text.Json.Nodes;

namespace StudentSearch.Api.Services;

public interface IElasticsearchGateway
{
    Task<JsonNode?> SendAsync(HttpMethod method, string path, JsonNode? body = null);
    Task<string> SendRawAsync(HttpMethod method, string path, string body);
    Task<bool> DeleteIfExistsAsync(string path);
}
