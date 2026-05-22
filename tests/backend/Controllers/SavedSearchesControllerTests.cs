using System.Security.Claims;
using Microsoft.AspNetCore.Http;
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
        var controller = CreateController(service);

        var result = await controller.List();

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = Assert.IsAssignableFrom<IReadOnlyList<SavedSearch>>(ok.Value);
        Assert.Same(savedSearch, value[0]);
        Assert.Equal("user-1", service.ListOwnerSub);
    }

    [Fact]
    public async Task Save_ReturnsCreatedSavedSearch()
    {
        var savedSearch = CreateSavedSearch();
        var service = new StubSavedSearchService([], savedSearch);
        var controller = CreateController(service);

        var result = await controller.Save(new SaveSearchRequest(Name: "Westbrook"));

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Same(savedSearch, created.Value);
        Assert.Equal("user-1", service.SaveOwnerSub);
    }

    [Fact]
    public async Task Save_ReturnsBadRequestForInvalidSavedSearch()
    {
        var service = new StubSavedSearchService([], SaveException: new ArgumentException("Saved search name is required."));
        var controller = CreateController(service);

        var result = await controller.Save(new SaveSearchRequest(Name: ""));

        var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Saved search name is required.", badRequest.Value);
    }

    [Fact]
    public async Task Delete_ReturnsNoContentWhenSavedSearchExists()
    {
        var service = new StubSavedSearchService([], DeleteResult: true);
        var controller = CreateController(service);

        var result = await controller.Delete("saved-1");

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("user-1", service.DeleteOwnerSub);
    }

    [Fact]
    public async Task Delete_ReturnsNotFoundWhenSavedSearchDoesNotExist()
    {
        var service = new StubSavedSearchService([], DeleteResult: false);
        var controller = CreateController(service);

        var result = await controller.Delete("missing");

        Assert.IsType<NotFoundResult>(result);
    }

    private static SavedSearch CreateSavedSearch()
    {
        return new SavedSearch("saved-1", "Westbrook", "West", [], "relevance", 10, DateTimeOffset.UtcNow);
    }

    private static SavedSearchesController CreateController(StubSavedSearchService service)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-1")], "Test"));
        return new SavedSearchesController(service)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            }
        };
    }

    private sealed class StubSavedSearchService(
        IReadOnlyList<SavedSearch> savedSearches,
        SavedSearch? SaveResult = null,
        ArgumentException? SaveException = null,
        bool DeleteResult = false) : ISavedSearchService
    {
        public string? ListOwnerSub { get; private set; }
        public string? SaveOwnerSub { get; private set; }
        public string? DeleteOwnerSub { get; private set; }

        public Task<IReadOnlyList<SavedSearch>> ListAsync(string ownerSub)
        {
            ListOwnerSub = ownerSub;
            return Task.FromResult(savedSearches);
        }

        public Task<SavedSearch> SaveAsync(string ownerSub, SaveSearchRequest request)
        {
            SaveOwnerSub = ownerSub;
            if (SaveException is not null)
            {
                throw SaveException;
            }

            return Task.FromResult(SaveResult!);
        }

        public Task<bool> DeleteAsync(string ownerSub, string id)
        {
            DeleteOwnerSub = ownerSub;
            return Task.FromResult(DeleteResult);
        }
    }
}
