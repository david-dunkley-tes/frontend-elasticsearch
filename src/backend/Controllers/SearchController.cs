using Microsoft.AspNetCore.Mvc;
using StudentSearch.Api.Interfaces;
using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SearchController(
    IStudentSearchService searchService,
    IAuthorizationScopeResolver authorizationScopeResolver) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<SearchResponse>> Search(SearchRequest request)
    {
        var authorizationScope = await authorizationScopeResolver.ResolveAsync(User);
        var safeguardingScope = await authorizationScopeResolver.ResolveRoleScopeAsync(User, Roles.Dsl);
        var response = await searchService.SearchAsync(request, authorizationScope, safeguardingScope);
        return Ok(response);
    }
}
