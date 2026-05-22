using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;
using StudentSearch.Api.Configuration;
using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Tests.Services;

public sealed class SavedSearchServiceTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveAsync_NormalizesAndPersistsSavedSearch()
    {
        var service = CreateService();
        var request = new SaveSearchRequest(
            Name: "  Westbrook year 9  ",
            Query: "  West  ",
            Filters: new Dictionary<string, List<string>>
            {
                ["school"] = ["westbrook college", "Westbrook College", ""],
                ["empty"] = []
            },
            Sort: "relevance",
            PageSize: 250);

        var saved = await service.SaveAsync("user-1", request);
        var savedSearches = await service.ListAsync("user-1");

        Assert.Equal("Westbrook year 9", saved.Name);
        Assert.Equal("West", saved.Query);
        Assert.Equal(100, saved.PageSize);
        Assert.Single(saved.Filters["school"]);
        Assert.Single(savedSearches);
        Assert.Equal(saved.Id, savedSearches[0].Id);
    }

    [Fact]
    public async Task ListAsync_ReturnsNewestSearchFirst()
    {
        var service = CreateService();

        var first = await service.SaveAsync("user-1", new SaveSearchRequest(Name: "First"));
        await Task.Delay(5);
        var second = await service.SaveAsync("user-1", new SaveSearchRequest(Name: "Second"));

        var savedSearches = await service.ListAsync("user-1");

        Assert.Equal([second.Id, first.Id], savedSearches.Select(search => search.Id).ToArray());
    }

    [Fact]
    public async Task DeleteAsync_RemovesExistingSavedSearch()
    {
        var service = CreateService();
        var saved = await service.SaveAsync("user-1", new SaveSearchRequest(Name: "Remove me"));

        var removed = await service.DeleteAsync("user-1", saved.Id);
        var savedSearches = await service.ListAsync("user-1");

        Assert.True(removed);
        Assert.Empty(savedSearches);
    }

    [Fact]
    public async Task SaveAsync_RejectsBlankName()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<ArgumentException>(() => service.SaveAsync("user-1", new SaveSearchRequest(Name: " ")));
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlySavedSearchesForOwner()
    {
        var service = CreateService();
        var ownSearch = await service.SaveAsync("user-1", new SaveSearchRequest(Name: "Mine"));
        await service.SaveAsync("user-2", new SaveSearchRequest(Name: "Theirs"));

        var savedSearches = await service.ListAsync("user-1");

        var savedSearch = Assert.Single(savedSearches);
        Assert.Equal(ownSearch.Id, savedSearch.Id);
    }

    [Fact]
    public async Task DeleteAsync_DoesNotRemoveAnotherOwnersSavedSearch()
    {
        var service = CreateService();
        var saved = await service.SaveAsync("user-1", new SaveSearchRequest(Name: "Mine"));

        var removed = await service.DeleteAsync("user-2", saved.Id);
        var savedSearches = await service.ListAsync("user-1");

        Assert.False(removed);
        Assert.Single(savedSearches);
    }

    [Fact]
    public async Task ListAsync_IgnoresLegacyOwnerlessSavedSearches()
    {
        Directory.CreateDirectory(_tempRoot);
        await File.WriteAllTextAsync(
            Path.Combine(_tempRoot, "saved-searches.json"),
            """
            [
              {
                "id": "legacy-1",
                "name": "Legacy",
                "query": "West",
                "filters": {},
                "sort": "relevance",
                "pageSize": 10,
                "createdAt": "2026-05-21T12:00:00Z"
              }
            ]
            """);

        var service = CreateService();

        var savedSearches = await service.ListAsync("user-1");

        Assert.Empty(savedSearches);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private SavedSearchService CreateService()
    {
        var configuration = new ConfigurationManager
        {
            ["SavedSearches:Path"] = "saved-searches.json"
        };

        return new SavedSearchService(new SearchConfiguration(configuration, new StubWebHostEnvironment(_tempRoot)));
    }

    private sealed class StubWebHostEnvironment(string contentRootPath) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "StudentSearch.Api.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.Combine(contentRootPath, "src", "backend");
        public string EnvironmentName { get; set; } = "Development";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
