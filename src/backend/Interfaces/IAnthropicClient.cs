namespace StudentSearch.Api.Interfaces;

public sealed record AnthropicMessageRequest(
    string Model,
    int MaxTokens,
    string System,
    string UserMessage);

public sealed record AnthropicMessageResponse(
    string Text,
    int? InputTokens,
    int? OutputTokens);

public interface IAnthropicClient
{
    Task<AnthropicMessageResponse> SendMessageAsync(AnthropicMessageRequest request, CancellationToken cancellationToken = default);
}
