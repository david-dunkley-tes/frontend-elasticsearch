using Microsoft.AspNetCore.Mvc;
using StudentSearch.Api.Configuration;
using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Controllers;

[ApiController]
[Route("api/ask")]
public sealed class RagController(
    RagConfiguration ragConfiguration,
    IRagService ragService,
    IAuthorizationScopeResolver authorizationScopeResolver) : ControllerBase
{
    [HttpGet("health")]
    public ActionResult<RagHealth> Health()
    {
        var reason = ragConfiguration.IsEnabled ? null : ragConfiguration.DisabledReason;
        return Ok(new RagHealth(ragConfiguration.IsEnabled, reason));
    }

    [HttpPost]
    public async Task<ActionResult<RagAnswer>> Ask(RagRequest request, CancellationToken cancellationToken)
    {
        if (!ragConfiguration.IsEnabled)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "AI Ask is not configured",
                Detail = ragConfiguration.DisabledReason,
            });
        }

        var authorizationScope = await authorizationScopeResolver.ResolveAsync(User);
        var answer = await ragService.AskAsync(request, authorizationScope, cancellationToken);
        return Ok(answer);
    }
}
