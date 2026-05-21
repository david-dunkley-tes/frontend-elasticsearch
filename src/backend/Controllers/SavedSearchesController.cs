using Microsoft.AspNetCore.Mvc;
using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Controllers;

[ApiController]
[Route("api/saved-searches")]
public sealed class SavedSearchesController : ControllerBase
{
    private readonly ISavedSearchService _savedSearchService;

    public SavedSearchesController(ISavedSearchService savedSearchService)
    {
        _savedSearchService = savedSearchService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SavedSearch>>> List()
    {
        return Ok(await _savedSearchService.ListAsync());
    }

    [HttpPost]
    public async Task<ActionResult<SavedSearch>> Save(SaveSearchRequest request)
    {
        try
        {
            var savedSearch = await _savedSearchService.SaveAsync(request);
            return CreatedAtAction(nameof(List), new { id = savedSearch.Id }, savedSearch);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        return await _savedSearchService.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
