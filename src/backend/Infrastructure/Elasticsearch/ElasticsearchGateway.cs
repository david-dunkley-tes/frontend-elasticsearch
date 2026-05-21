using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using System.Text.Json;
using System.Text.Json.Nodes;
using ElasticHttpMethod = Elastic.Transport.HttpMethod;
using NetHttpMethod = System.Net.Http.HttpMethod;

namespace StudentSearch.Api.Infrastructure.Elasticsearch;

public sealed class ElasticsearchGateway(ElasticsearchClient client) : IElasticsearchGateway
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    public async Task<JsonNode?> SendAsync(NetHttpMethod method, string path, JsonNode? body = null)
    {
        var text = body is null
            ? await SendWithoutBodyAsync(method, path)
            : await SendRawAsync(method, path, body.ToJsonString(JsonOptions));

        return string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text);
    }

    public async Task<string> SendRawAsync(NetHttpMethod method, string path, string body)
    {
        var response = await client.Transport.RequestAsync<StringResponse>(
            ToElasticMethod(method),
            path,
            PostData.String(body));

        EnsureValid(response, method, path);
        return response.Body ?? string.Empty;
    }

    public async Task<bool> DeleteIfExistsAsync(string path)
    {
        var response = await client.Transport.RequestAsync<StringResponse>(
            ElasticHttpMethod.DELETE,
            path);

        if (response.ApiCallDetails?.HasSuccessfulStatusCode == true)
        {
            return true;
        }

        var statusCode = response.ApiCallDetails?.HttpStatusCode;
        if (statusCode == 404)
        {
            return false;
        }

        throw CreateException(NetHttpMethod.Delete, path, response);
    }

    private async Task<string> SendWithoutBodyAsync(NetHttpMethod method, string path)
    {
        var response = await client.Transport.RequestAsync<StringResponse>(ToElasticMethod(method), path);
        EnsureValid(response, method, path);
        return response.Body ?? string.Empty;
    }

    private static ElasticHttpMethod ToElasticMethod(NetHttpMethod method)
    {
        if (method == NetHttpMethod.Get)
        {
            return ElasticHttpMethod.GET;
        }

        if (method == NetHttpMethod.Post)
        {
            return ElasticHttpMethod.POST;
        }

        if (method == NetHttpMethod.Put)
        {
            return ElasticHttpMethod.PUT;
        }

        if (method == NetHttpMethod.Delete)
        {
            return ElasticHttpMethod.DELETE;
        }

        throw new NotSupportedException($"HTTP method {method} is not supported.");
    }

    private static void EnsureValid(StringResponse response, NetHttpMethod method, string path)
    {
        if (response.ApiCallDetails?.HasSuccessfulStatusCode != true)
        {
            throw CreateException(method, path, response);
        }
    }

    private static InvalidOperationException CreateException(NetHttpMethod method, string path, StringResponse response)
    {
        var statusCode = response.ApiCallDetails?.HttpStatusCode;
        var serverError = response.Body ?? response.ApiCallDetails?.DebugInformation;
        return new InvalidOperationException($"Elasticsearch {method} {path} failed with {statusCode}: {serverError}");
    }
}
