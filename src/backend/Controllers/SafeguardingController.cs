using Microsoft.AspNetCore.Mvc;
using StudentSearch.Api.Configuration;
using StudentSearch.Api.Interfaces;
using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Controllers;

[ApiController]
[Route("api/safeguarding")]
public sealed class SafeguardingController(
    RagConfiguration ragConfiguration,
    ISafeguardingService safeguardingService,
    IAuthorizationScopeResolver authorizationScopeResolver) : ControllerBase
{
    [HttpGet("availability")]
    public ActionResult<SafeguardingAvailability> Availability()
    {
        var reason = ragConfiguration.IsEnabled ? null : ragConfiguration.DisabledReason;
        return Ok(new SafeguardingAvailability(ragConfiguration.IsEnabled, reason));
    }

    [HttpPost]
    public async Task<ActionResult<SafeguardingAnswer>> Ask(SafeguardingQuestion request, CancellationToken cancellationToken)
    {
        if (!ragConfiguration.IsEnabled)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "Safeguarding ask is not available",
                Detail = ragConfiguration.DisabledReason,
            });
        }

        var authorizationScope = await authorizationScopeResolver.ResolveAsync(User);
        var answer = await safeguardingService.AskAsync(request, authorizationScope, cancellationToken);
        return Ok(answer);
    }
}
