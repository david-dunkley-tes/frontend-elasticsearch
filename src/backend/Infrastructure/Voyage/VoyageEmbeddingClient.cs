using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using StudentSearch.Api.Configuration;
using StudentSearch.Api.Interfaces;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Infrastructure.Voyage;

public sealed class VoyageEmbeddingClient(HttpClient httpClient, RagConfiguration configuration) : IEmbeddingClient
{
    private const string EmbeddingsPath = "v1/embeddings";

    public async Task<float[][]> EmbedAsync(IReadOnlyList<string> texts, string inputType, CancellationToken cancellationToken = default)
    {
        if (texts.Count == 0)
        {
            return [];
        }

        var request = new VoyageEmbedRequest(configuration.EmbeddingModel, texts, inputType);
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, EmbeddingsPath)
        {
            Content = JsonContent.Create(request, options: JsonOptions),
        };
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", configuration.VoyageApiKey);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Voyage embeddings failed with {(int)response.StatusCode}: {body}");
        }

        var parsed = await response.Content.ReadFromJsonAsync<VoyageEmbedResponse>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Voyage returned an empty embeddings body.");

        return parsed.Data.OrderBy(item => item.Index).Select(item => item.Embedding).ToArray();
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private sealed record VoyageEmbedRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("input")] IReadOnlyList<string> Input,
        [property: JsonPropertyName("input_type")] string InputType);

    private sealed record VoyageEmbedResponse(
        [property: JsonPropertyName("data")] List<VoyageEmbedDatum> Data);

    private sealed record VoyageEmbedDatum(
        [property: JsonPropertyName("embedding")] float[] Embedding,
        [property: JsonPropertyName("index")] int Index);
}
