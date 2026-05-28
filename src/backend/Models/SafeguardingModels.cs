using System.Text.Json.Nodes;

namespace StudentSearch.Api.Models;

public sealed record SafeguardingQuestion(string Question, bool DebugMode);

public sealed record SafeguardingAnswer(
    string Answer,
    IReadOnlyList<SafeguardingSource> Sources,
    RagDebug? Debug);

public sealed record SafeguardingSource(
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

public sealed record SafeguardingAvailability(bool Available, string? Reason);
