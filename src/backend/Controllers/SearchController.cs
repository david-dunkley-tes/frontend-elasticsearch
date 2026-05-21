using Microsoft.AspNetCore.Mvc;
using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SearchController : ControllerBase
{
    private readonly IStudentSearchService _searchService;

    public SearchController(IStudentSearchService searchService)
    {
        _searchService = searchService;
    }

    [HttpPost]
    public async Task<ActionResult<SearchResponse>> Search(SearchRequest request)
    {
        var response = await _searchService.SearchAsync(request);
        return Ok(response);
    }
}
