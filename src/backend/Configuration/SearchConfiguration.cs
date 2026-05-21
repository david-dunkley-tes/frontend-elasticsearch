namespace StudentSearch.Api.Configuration;

public sealed class SearchConfiguration
{
    public string ElasticsearchUrl { get; }
    public string IndexName { get; }
    public string SeedDataPath { get; }

    public SearchConfiguration(IConfiguration configuration, IWebHostEnvironment environment)
    {
        ElasticsearchUrl = configuration["Elasticsearch:Url"] ?? "http://localhost:9200";
        IndexName = configuration["Elasticsearch:IndexName"] ?? "students";
        var configuredPath = configuration["SeedData:Path"] ?? "data/students.seed.json";
        SeedDataPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", "..", configuredPath));
    }
}
