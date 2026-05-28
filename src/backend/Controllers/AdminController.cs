using Microsoft.AspNetCore.Mvc;
using StudentSearch.Api.Interfaces;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController(IReindexService reindexService, IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("reindex")]
    public async Task<IActionResult> Reindex()
    {
        if (!environment.IsDevelopment())
        {
            return NotFound();
        }

        var response = await reindexService.ReindexAsync();
        return Ok(response);
    }
}
