using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Tests.Services;

public sealed class StudentSearchServiceTests
{
    [Fact]
    public async Task SearchAsync_NormalizesRequestBeforeCallingIndex()
    {
        var index = new CapturingStudentSearchIndex();
        var service = new StudentSearchService(index);
        var request = new SearchRequest(
            Query: "  West  ",
            Filters: new Dictionary<string, List<string>>
            {
                ["school"] = ["westbrook college", "", "westbrook college", "  "],
                ["yearGroup"] = ["year 9", "year 9"]
            },
            Page: -5,
            PageSize: 0);

        await service.SearchAsync(request);

        Assert.NotNull(index.CapturedRequest);
        Assert.Equal("West", index.CapturedRequest.Query);
        Assert.Equal(1, index.CapturedRequest.Page);
        Assert.Equal(25, index.CapturedRequest.PageSize);
        Assert.Equal(["westbrook college"], index.CapturedRequest.Filters["school"]);
        Assert.Equal(["year 9"], index.CapturedRequest.Filters["yearGroup"]);
    }

    [Fact]
    public async Task SearchAsync_ClampsPageSizeToOneHundred()
    {
        var index = new CapturingStudentSearchIndex();
        var service = new StudentSearchService(index);

        await service.SearchAsync(new SearchRequest(Page: 2, PageSize: 500));

        Assert.NotNull(index.CapturedRequest);
        Assert.Equal(2, index.CapturedRequest.Page);
        Assert.Equal(100, index.CapturedRequest.PageSize);
    }

    private sealed class CapturingStudentSearchIndex : IStudentSearchIndex
    {
        public SearchRequest CapturedRequest { get; private set; } = null!;

        public Task<SearchResponse> SearchAsync(SearchRequest request)
        {
            CapturedRequest = request;
            return Task.FromResult(new SearchResponse(0, 0, 0, [], new Dictionary<string, FacetResponse>(), null));
        }
    }
}
