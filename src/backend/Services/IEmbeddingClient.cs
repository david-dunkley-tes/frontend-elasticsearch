namespace StudentSearch.Api.Services;

public interface IEmbeddingClient
{
    Task<float[][]> EmbedAsync(IReadOnlyList<string> texts, string inputType, CancellationToken cancellationToken = default);
}
