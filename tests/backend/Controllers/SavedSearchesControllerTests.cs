using Microsoft.AspNetCore.Mvc;
using StudentSearch.Api.Controllers;
using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Tests.Controllers;

public sealed class SavedSearchesControllerTests
{
    [Fact]
    public async Task List_ReturnsSavedSearches()
    {
        var savedSearch = CreateSavedSearch();
        var service = new StubSavedSearchService([savedSearch]);
        var controller = new SavedSearchesController(service);

        var result = await controller.List();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<IReadOnlyList<SavedSearch>>(ok.Value);
        Assert.Same(savedSearch, value[0]);
    }

    [Fact]
    public async Task Save_ReturnsCreatedSavedSearch()
    {
        var savedSearch = CreateSavedSearch();
        var service = new StubSavedSearchService([], savedSearch);
        var controller = new SavedSearchesController(service);

        var result = await controller.Save(new SaveSearchRequest(Name: "Westbrook"));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Same(savedSearch, created.Value);
    }

    [Fact]
    public async Task Save_ReturnsBadRequestForInvalidSavedSearch()
    {
        var service = new StubSavedSearchService([], SaveException: new ArgumentException("Saved search name is required."));
        var controller = new SavedSearchesController(service);

        var result = await controller.Save(new SaveSearchRequest(Name: ""));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Saved search name is required.", badRequest.Value);
    }

    [Fact]
    public async Task Delete_ReturnsNoContentWhenSavedSearchExists()
    {
        var service = new StubSavedSearchService([], DeleteResult: true);
        var controller = new SavedSearchesController(service);

        var result = await controller.Delete("saved-1");

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNotFoundWhenSavedSearchDoesNotExist()
    {
        var service = new StubSavedSearchService([], DeleteResult: false);
        var controller = new SavedSearchesController(service);

        var result = await controller.Delete("missing");

        Assert.IsType<NotFoundResult>(result);
    }

    private static SavedSearch CreateSavedSearch()
    {
        return new SavedSearch("saved-1", "Westbrook", "West", [], "relevance", 10, DateTimeOffset.UtcNow);
    }

    private sealed class StubSavedSearchService(
        IReadOnlyList<SavedSearch> savedSearches,
        SavedSearch? SaveResult = null,
        ArgumentException? SaveException = null,
        bool DeleteResult = false) : ISavedSearchService
    {
        public Task<IReadOnlyList<SavedSearch>> ListAsync()
        {
            return Task.FromResult(savedSearches);
        }

        public Task<SavedSearch> SaveAsync(SaveSearchRequest request)
        {
            if (SaveException is not null)
            {
                throw SaveException;
            }

            return Task.FromResult(SaveResult!);
        }

        public Task<bool> DeleteAsync(string id)
        {
            return Task.FromResult(DeleteResult);
        }
    }
}
