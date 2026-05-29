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
    public async Task SearchAsync_AppliesStudentIdConstraintToHitsAndFacetAggregations()
    {
        var gateway = new CapturingElasticsearchGateway();
        var index = new ElasticsearchStudentSearchIndex(gateway, CreateConfiguration());
        var request = new SearchRequest(StudentIds: ["S11209", "S11761"]);

        await index.SearchAsync(request, AuthorizedSchoolScope.Global);

        var body = gateway.CapturedBody!;
        var queryFilter = body["query"]!["bool"]!["filter"]!.ToJsonString();
        var schoolAggFilter = body["aggs"]!["school"]!["filter"]!.ToJsonString();

        // student.id is a lowercase-normalized keyword, so the terms values must be lowercased.
        Assert.Contains("\"student.id\"", queryFilter);
        Assert.Contains("s11209", queryFilter);
        Assert.Contains("s11761", queryFilter);
        Assert.DoesNotContain("S11209", queryFilter);

        // The constraint must also narrow facet counts.
        Assert.Contains("\"student.id\"", schoolAggFilter);
        Assert.Contains("s11209", schoolAggFilter);
    }

    [Fact]
    public async Task SearchAsync_OmitsStudentIdConstraintWhenNoneProvided()
    {
        var gateway = new CapturingElasticsearchGateway();
        var index = new ElasticsearchStudentSearchIndex(gateway, CreateConfiguration());

        await index.SearchAsync(new SearchRequest(), AuthorizedSchoolScope.Global);

        // No constraint => no student.id terms clause in the query or aggregation filters.
        // (student.id legitimately appears elsewhere, e.g. the highlight fields.)
        var body = gateway.CapturedBody!;
        Assert.DoesNotContain("\"student.id\"", body["query"]!["bool"]!["filter"]!.ToJsonString());
        Assert.DoesNotContain("\"student.id\"", body["aggs"]!["school"]!["filter"]!.ToJsonString());
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

    [Fact]
    public async Task SearchAsync_HidesSafeguardingLogForSchoolsOutsideSafeguardingScope()
    {
        var gateway = new SingleHitGateway("SCH-OAKWOOD");
        var index = new ElasticsearchStudentSearchIndex(gateway, CreateConfiguration());

        var outside = await index.SearchAsync(new SearchRequest(), AuthorizedSchoolScope.Global, new AuthorizedSchoolScope(false, ["SCH-KINGFISHER"]));
        Assert.Null(outside.Results[0].SafeguardingLog);

        var inside = await index.SearchAsync(new SearchRequest(), AuthorizedSchoolScope.Global, new AuthorizedSchoolScope(false, ["SCH-OAKWOOD"]));
        Assert.NotNull(inside.Results[0].SafeguardingLog);

        var global = await index.SearchAsync(new SearchRequest(), AuthorizedSchoolScope.Global, AuthorizedSchoolScope.Global);
        Assert.NotNull(global.Results[0].SafeguardingLog);
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

    private sealed class SingleHitGateway(string schoolId) : IElasticsearchGateway
    {
        public Task<JsonNode?> SendAsync(HttpMethod method, string path, JsonNode? body = null)
        {
            var json = $$"""
            {
              "took": 1,
              "hits": {
                "total": { "value": 1 },
                "hits": [
                  {
                    "_score": 1.0,
                    "_source": {
                      "student": { "id": "S1", "foreName": "Ava", "surname": "Stone", "fullName": "Ava Stone", "yearGroup": "Year 4" },
                      "school": { "id": "{{schoolId}}", "name": "Test School", "address": "1 Test Road" },
                      "trust": null,
                      "classGroup": { "name": "Acorn", "teacher": "Ms Test" },
                      "safeguardingLog": { "category": "Bullying", "date": "2026-05-01", "narrative": "Test narrative." }
                    }
                  }
                ]
              },
              "aggregations": {
                "yearGroup": { "values": { "buckets": [] } },
                "school": { "values": { "buckets": [] } },
                "trust": { "values": { "buckets": [] }, "missing": { "doc_count": 0 } },
                "classTeacher": { "values": { "buckets": [] } }
              }
            }
            """;
            return Task.FromResult<JsonNode?>(JsonNode.Parse(json));
        }

        public Task<string> SendRawAsync(HttpMethod method, string path, string body) => throw new NotSupportedException();
        public Task<bool> DeleteIfExistsAsync(string path) => throw new NotSupportedException();
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
