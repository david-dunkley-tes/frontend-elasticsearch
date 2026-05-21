using Microsoft.AspNetCore.Mvc;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly IWebHostEnvironment _environment;
    private readonly IReindexService _reindexService;

    public AdminController(IReindexService reindexService, IWebHostEnvironment environment)
    {
        _reindexService = reindexService;
        _environment = environment;
    }

    [HttpPost("reindex")]
    public async Task<IActionResult> Reindex()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        var response = await _reindexService.ReindexAsync();
        return Ok(response);
    }
}
