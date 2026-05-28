using System.Text.Json.Nodes;

namespace StudentSearch.Api.Models;

public sealed record RagRequest(string Question, bool DebugMode);

public sealed record RagAnswer(
    string Answer,
    IReadOnlyList<RagSource> Sources,
    RagDebug? Debug);

public sealed record RagSource(
    string StudentId,
    string FullName,
    string YearGroup,
    string SchoolId,
    string SchoolName,
    string? TrustName,
    string Category,
    string Date,
    string Narrative,
    double? Score);

public sealed record RagDebug(
    string EmbeddingModel,
    string CompletionModel,
    int RetrievedCount,
    JsonNode? KnnQuery,
    string SystemPrompt,
    string UserPrompt,
    string RawCompletion);

public sealed record RagHealth(bool Enabled, string? Reason);
