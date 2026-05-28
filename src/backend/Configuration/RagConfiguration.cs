namespace StudentSearch.Api.Configuration;

public sealed class RagConfiguration(IConfiguration configuration)
{
    public string? VoyageApiKey { get; } = configuration["VOYAGE_API_KEY"] ?? configuration["Voyage:ApiKey"];
    public string? AnthropicApiKey { get; } = configuration["ANTHROPIC_API_KEY"] ?? configuration["Anthropic:ApiKey"];

    public string EmbeddingModel { get; } = configuration["Voyage:Model"] ?? "voyage-3";
    public int EmbeddingDimensions { get; } = int.TryParse(configuration["Voyage:Dimensions"], out var dims) ? dims : 1024;

    public string CompletionModel { get; } = configuration["Anthropic:Model"] ?? "claude-haiku-4-5";
    public int CompletionMaxTokens { get; } = int.TryParse(configuration["Anthropic:MaxTokens"], out var max) ? max : 800;

    public int RetrievalTopK { get; } = int.TryParse(configuration["Rag:TopK"], out var k) ? k : 8;

    public bool IsEnabled => !string.IsNullOrWhiteSpace(VoyageApiKey) && !string.IsNullOrWhiteSpace(AnthropicApiKey);

    public string DisabledReason
    {
        get
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(VoyageApiKey))
            {
                missing.Add("VOYAGE_API_KEY");
            }
            if (string.IsNullOrWhiteSpace(AnthropicApiKey))
            {
                missing.Add("ANTHROPIC_API_KEY");
            }
            return missing.Count == 0 ? "" : $"Missing environment variable(s): {string.Join(", ", missing)}";
        }
    }
}
