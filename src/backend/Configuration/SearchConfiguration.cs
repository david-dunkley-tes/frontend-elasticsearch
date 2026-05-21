namespace StudentSearch.Api.Configuration;

public sealed class SearchConfiguration(IConfiguration configuration, IWebHostEnvironment environment)
{
    public string ElasticsearchUrl { get; } = configuration["Elasticsearch:Url"] ?? "http://localhost:9200";
    public string IndexName { get; } = configuration["Elasticsearch:IndexName"] ?? "students";
    public string SeedDataPath { get; } = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", configuration["SeedData:Path"] ?? "data/students.seed.json"));
    public string SavedSearchesPath { get; } = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", configuration["SavedSearches:Path"] ?? "data/saved-searches.json"));
}
