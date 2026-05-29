using System.Net.Http.Json;
using System.Text.Json.Serialization;
using StudentSearch.Api.Configuration;
using StudentSearch.Api.Interfaces;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Infrastructure.Anthropic;

public sealed class AnthropicMessagesClient(HttpClient httpClient, RagConfiguration configuration) : IAnthropicClient
{
    private const string MessagesPath = "v1/messages";
    private const string ApiVersion = "2023-06-01";

    public async Task<AnthropicMessageResponse> SendMessageAsync(AnthropicMessageRequest request, CancellationToken cancellationToken = default)
    {
        var body = new AnthropicRequestBody(
            request.Model,
            request.MaxTokens,
            request.System,
            [new AnthropicMessage("user", request.UserMessage)],
            request.Temperature);

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, MessagesPath)
        {
            Content = JsonContent.Create(body, options: JsonDefaults.WebIgnoreNullsOnWrite),
        };
        httpRequest.Headers.Add("x-api-key", configuration.AnthropicApiKey);
        httpRequest.Headers.Add("anthropic-version", ApiVersion);

        using var response = await httpClient.SendAsync(httpRequest, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var raw = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Anthropic messages failed with {(int)response.StatusCode}: {raw}");
        }

        var parsed = await response.Content.ReadFromJsonAsync<AnthropicResponseBody>(JsonDefaults.WebIgnoreNullsOnWrite, cancellationToken)
            ?? throw new InvalidOperationException("Anthropic returned an empty messages body.");

        var text = string.Concat(parsed.Content.Where(c => c.Type == "text").Select(c => c.Text));
        return new AnthropicMessageResponse(text, parsed.Usage?.InputTokens, parsed.Usage?.OutputTokens);
    }

    private sealed record AnthropicRequestBody(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("system")] string System,
        [property: JsonPropertyName("messages")] IReadOnlyList<AnthropicMessage> Messages,
        [property: JsonPropertyName("temperature")] double? Temperature);

    private sealed record AnthropicMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record AnthropicResponseBody(
        [property: JsonPropertyName("content")] List<AnthropicContentBlock> Content,
        [property: JsonPropertyName("usage")] AnthropicUsage? Usage);

    private sealed record AnthropicContentBlock(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string Text);

    private sealed record AnthropicUsage(
        [property: JsonPropertyName("input_tokens")] int? InputTokens,
        [property: JsonPropertyName("output_tokens")] int? OutputTokens);
}
