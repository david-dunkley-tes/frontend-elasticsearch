using Microsoft.AspNetCore.Mvc;
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
        var controller = new SearchController(service);

        var result = await controller.Search(new SearchRequest(Query: "West"));

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
        Assert.Equal("West", service.CapturedRequest.Query);
    }

    private sealed class StubStudentSearchService : IStudentSearchService
    {
        private readonly SearchResponse _response;

        public StubStudentSearchService(SearchResponse response)
        {
            _response = response;
        }

        public SearchRequest CapturedRequest { get; private set; } = null!;

        public Task<SearchResponse> SearchAsync(SearchRequest request)
        {
            CapturedRequest = request;
            return Task.FromResult(_response);
        }
    }
}
