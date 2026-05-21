namespace StudentSearch.Api.Models;

public sealed record ReindexResponse(string IndexName, int DocumentsIndexed, string Status);
