using System.Text.Json.Nodes;
using StudentSearch.Api.Configuration;
using StudentSearch.Api.Infrastructure.Elasticsearch;
using StudentSearch.Api.Models;

namespace StudentSearch.Api.Tests.Infrastructure;

public sealed class ElasticsearchStudentSearchIndexTests
{
    [Fact]
    public async Task SearchAsync_IncludesPrefixClauseForSingleTokenQueries()
    {
        var gateway = new CapturingElasticsearchGateway();
        var index = new ElasticsearchStudentSearchIndex(gateway, CreateConfiguration());

        await index.SearchAsync(new SearchRequest(Query: "West", Page: 1, PageSize: 10), AuthorizedSchoolScope.Global);

        Assert.NotNull(gateway.CapturedBody);
        Assert.Contains("\"bool_prefix\"", gateway.CapturedBody.ToJsonString());
    }

    [Fact]
    public async Task SearchAsync_DoesNotIncludePrefixClauseForMultiTokenQueries()
    {
        var gateway = new CapturingElasticsearchGateway();
        var index = new ElasticsearchStudentSearchIndex(gateway, CreateConfiguration());

        await index.SearchAsync(new SearchRequest(Query: "West b", Page: 1, PageSize: 10), AuthorizedSchoolScope.Global);

        Assert.NotNull(gateway.CapturedBody);
        Assert.DoesNotContain("\"bool_prefix\"", gateway.CapturedBody.ToJsonString());
    }

    [Fact]
    public async Task SearchAsync_BuildsSelfExcludingFacetAggregations()
    {
        var gateway = new CapturingElasticsearchGateway();
        var index = new ElasticsearchStudentSearchIndex(gateway, CreateConfiguration());
        var request = new SearchRequest(
            Query: "West",
            Filters: new Dictionary<string, List<string>>
            {
                ["school"] = ["westbrook college"],
                ["yearGroup"] = ["year 9"]
            });

        await index.SearchAsync(request, AuthorizedSchoolScope.Global);

        var body = gateway.CapturedBody!;
        var schoolFilter = body["aggs"]!["school"]!["filter"]!.ToJsonString();
        var yearGroupFilter = body["aggs"]!["yearGroup"]!["filter"]!.ToJsonString();

        Assert.DoesNotContain("school.name.keyword", schoolFilter);
        Assert.Contains("student.yearGroup", schoolFilter);
        Assert.Contains("school.name.keyword", yearGroupFilter);
        Assert.DoesNotContain("\"student.yearGroup\"", yearGroupFilter);
    }

    [Fact]
    public async Task SearchAsync_AppliesAuthorizationScopeToHitsAndFacetAggregations()
    {
        var gateway = new CapturingElasticsearchGateway();
        var index = new ElasticsearchStudentSearchIndex(gateway, CreateConfiguration());
        var authorizationScope = new AuthorizedSchoolScope(false, ["SCH-WESTBROOK"]);

        await index.SearchAsync(new SearchRequest(Query: "West"), authorizationScope);

        var body = gateway.CapturedBody!;
        var bodyJson = body.ToJsonString();
        var schoolFilter = body["aggs"]!["school"]!["filter"]!.ToJsonString();

        Assert.Contains("school.id", bodyJson);
        Assert.Contains("sch-westbrook", bodyJson);
        Assert.Contains("school.id", schoolFilter);
    }

    [Fact]
    public async Task SearchAsync_DeniesAllDocumentsWhenAuthorizationScopeIsEmpty()
    {
        var gateway = new CapturingElasticsearchGateway();
        var index = new ElasticsearchStudentSearchIndex(gateway, CreateConfiguration());
        var authorizationScope = new AuthorizedSchoolScope(false, []);

        await index.SearchAsync(new SearchRequest(), authorizationScope);

        Assert.Contains("\"must_not\"", gateway.CapturedBody!.ToJsonString());
    }

    private static SearchConfiguration CreateConfiguration()
    {
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationManager
        {
            ["Elasticsearch:Url"] = "http://localhost:9200",
            ["Elasticsearch:IndexName"] = "students",
            ["SeedData:Path"] = "data/students.seed.json"
        };

        return new SearchConfiguration(configuration, new StubWebHostEnvironment());
    }

    private sealed class CapturingElasticsearchGateway : IElasticsearchGateway
    {
        public JsonNode? CapturedBody { get; private set; }

        public Task<JsonNode?> SendAsync(HttpMethod method, string path, JsonNode? body = null)
        {
            CapturedBody = body;
            return Task.FromResult<JsonNode?>(JsonNode.Parse("""
            {
              "took": 1,
              "hits": {
                "total": { "value": 0 },
                "hits": []
              },
              "aggregations": {
                "yearGroup": { "values": { "buckets": [] } },
                "school": { "values": { "buckets": [] } },
                "trust": { "values": { "buckets": [] }, "missing": { "doc_count": 0 } }
              }
            }
            """));
        }

        public Task<string> SendRawAsync(HttpMethod method, string path, string body)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DeleteIfExistsAsync(string path)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubWebHostEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "StudentSearch.Api.Tests";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = "Development";
        public string WebRootPath { get; set; } = string.Empty;
        public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
