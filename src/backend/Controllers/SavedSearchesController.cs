using Microsoft.AspNetCore.Mvc;
using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Controllers;

[ApiController]
[Route("api/saved-searches")]
public sealed class SavedSearchesController(ISavedSearchService savedSearchService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SavedSearch>>> List()
    {
        return Ok(await savedSearchService.ListAsync());
    }

    [HttpPost]
    public async Task<ActionResult<SavedSearch>> Save(SaveSearchRequest request)
    {
        try
        {
            var savedSearch = await savedSearchService.SaveAsync(request);
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
        return await savedSearchService.DeleteAsync(id) ? NoContent() : NotFound();
    }
}
