using StudentSearch.Api.Models;

namespace StudentSearch.Api.Interfaces;

public sealed record KnnHit(StudentRecord Record, double Score);

public sealed record KnnSearchResult(IReadOnlyList<KnnHit> Hits, string Query);

public interface IStudentKnnRetriever
{
    Task<KnnSearchResult> RetrieveAsync(
        float[] queryVector,
        int topK,
        AuthorizedSchoolScope authorizationScope,
        CancellationToken cancellationToken = default);
}
