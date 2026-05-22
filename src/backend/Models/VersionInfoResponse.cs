namespace StudentSearch.Api.Models;

public sealed record VersionInfoResponse(
    string Service,
    string Version,
    string Commit,
    string BuildTime,
    string Environment);
