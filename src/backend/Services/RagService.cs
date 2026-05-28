using System.Text;
using StudentSearch.Api.Configuration;
using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public sealed class RagService(
    RagConfiguration configuration,
    IEmbeddingClient embeddingClient,
    IStudentKnnRetriever knnRetriever,
    IAnthropicClient anthropicClient) : IRagService
{
    private const string SystemPromptTemplate = """
        You are a safeguarding assistant for a UK primary-school MAT. Today's date is {0}; use
        it as the anchor for any judgement about whether an incident is recent. You are given
        a small set of safeguarding records that have already been pre-filtered for the user's
        authorisation scope. Names and schools have been redacted as [student] and [school]
        for privacy — refer to students by their numeric id in square brackets, e.g. [S10042],
        and never invent a real name. Dates are real and you may reason about timing (recent,
        clustered, escalating, etc.). Use only the records provided to answer the question;
        do not invent details. If the records do not answer the question, say so directly.
        Keep answers concise (2-5 sentences) unless asked for more detail.
        """;

    public async Task<RagAnswer> AskAsync(RagRequest request, AuthorizedSchoolScope authorizationScope, CancellationToken cancellationToken = default)
    {
        if (!configuration.IsEnabled)
        {
            throw new InvalidOperationException(configuration.DisabledReason);
        }

        var question = (request.Question ?? string.Empty).Trim();
        if (question.Length == 0)
        {
            throw new ArgumentException("Question is required.", nameof(request));
        }

        var embeddings = await embeddingClient.EmbedAsync([question], inputType: "query", cancellationToken);
        var queryVector = embeddings[0];

        var knn = await knnRetriever.RetrieveAsync(queryVector, configuration.RetrievalTopK, authorizationScope, cancellationToken);

        var sources = knn.Hits
            .Where(hit => hit.Record.SafeguardingLog is not null)
            .Select(hit => new RagSource(
                hit.Record.Student.Id,
                hit.Record.Student.FullName,
                hit.Record.Student.YearGroup,
                hit.Record.School.Id,
                hit.Record.School.Name,
                hit.Record.Trust?.Name,
                hit.Record.SafeguardingLog!.Category,
                hit.Record.SafeguardingLog.Date,
                hit.Record.SafeguardingLog.Narrative,
                hit.Score))
            .ToList();

        var redactedForPrompt = knn.Hits
            .Where(hit => hit.Record.SafeguardingLog is not null)
            .Select(hit => new RedactedRecord(
                hit.Record.Student.Id,
                hit.Record.Student.YearGroup,
                hit.Record.SafeguardingLog!.Category,
                hit.Record.SafeguardingLog.NarrativeRedacted ?? hit.Record.SafeguardingLog.Narrative))
            .ToList();

        var userPrompt = BuildUserPrompt(question, redactedForPrompt);
        var systemPrompt = string.Format(SystemPromptTemplate, DateTime.UtcNow.ToString("d MMMM yyyy"));
        var completion = await anthropicClient.SendMessageAsync(
            new AnthropicMessageRequest(
                configuration.CompletionModel,
                configuration.CompletionMaxTokens,
                systemPrompt,
                userPrompt),
            cancellationToken);

        var debug = request.DebugMode
            ? new RagDebug(
                configuration.EmbeddingModel,
                configuration.CompletionModel,
                sources.Count,
                knn.Query,
                systemPrompt,
                userPrompt,
                completion.Text)
            : null;

        return new RagAnswer(completion.Text, sources, debug);
    }

    private static string BuildUserPrompt(string question, IReadOnlyList<RedactedRecord> records)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Question: {question}");
        builder.AppendLine();

        if (records.Count == 0)
        {
            builder.AppendLine("No safeguarding records matched the question within the caller's authorisation scope.");
            return builder.ToString();
        }

        builder.AppendLine($"Retrieved {records.Count} safeguarding record(s). Names, schools and dates have been redacted; refer to students by their id only:");
        builder.AppendLine();
        foreach (var record in records)
        {
            builder.AppendLine($"[{record.StudentId}] {record.YearGroup}");
            builder.AppendLine($"  Category: {record.Category}");
            builder.AppendLine($"  Narrative: {record.Narrative}");
            builder.AppendLine();
        }
        return builder.ToString();
    }

    private sealed record RedactedRecord(string StudentId, string YearGroup, string Category, string Narrative);
}
