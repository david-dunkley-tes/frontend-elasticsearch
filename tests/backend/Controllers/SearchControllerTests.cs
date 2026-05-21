using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using StudentSearch.Api.Controllers;
using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Tests.Controllers;

public sealed class SearchControllerTests
{
    [Fact]
    public async Task Search_ReturnsOkWithServiceResponse()
    {
        var expected = new SearchResponse(12, 3, 4, [], new Dictionary<string, FacetResponse>(), null);
        var service = new StubStudentSearchService(expected);
        var controller = new SearchController(service, new StubAuthorizationScopeResolver(AuthorizedSchoolScope.Global));

        var result = await controller.Search(new SearchRequest(Query: "West"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
        Assert.Equal("West", service.CapturedRequest.Query);
        Assert.Same(AuthorizedSchoolScope.Global, service.CapturedAuthorizationScope);
    }

    private sealed class StubStudentSearchService(SearchResponse response) : IStudentSearchService
    {
        public SearchRequest CapturedRequest { get; private set; } = null!;
        public AuthorizedSchoolScope CapturedAuthorizationScope { get; private set; } = null!;

        public Task<SearchResponse> SearchAsync(SearchRequest request, AuthorizedSchoolScope authorizationScope)
        {
            CapturedRequest = request;
            CapturedAuthorizationScope = authorizationScope;
            return Task.FromResult(response);
        }
    }

    private sealed class StubAuthorizationScopeResolver(AuthorizedSchoolScope authorizationScope) : IAuthorizationScopeResolver
    {
        public Task<AuthorizedSchoolScope> ResolveAsync(ClaimsPrincipal user)
        {
            return Task.FromResult(authorizationScope);
        }
    }
}
