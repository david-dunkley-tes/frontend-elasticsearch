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
    public async Task<ActionResult<SafeguardingAvailability>> Availability()
    {
        if (!ragConfiguration.IsEnabled)
        {
            return Ok(new SafeguardingAvailability(false, ragConfiguration.DisabledReason));
        }

        var safeguardingScope = await authorizationScopeResolver.ResolveRoleScopeAsync(User, Roles.Dsl);
        if (!safeguardingScope.GrantsAnySchool)
        {
            return Ok(new SafeguardingAvailability(false, "Your role does not include safeguarding (DSL) access."));
        }

        return Ok(new SafeguardingAvailability(true, null));
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

        var safeguardingScope = await authorizationScopeResolver.ResolveRoleScopeAsync(User, Roles.Dsl);
        if (!safeguardingScope.GrantsAnySchool)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Safeguarding access denied",
                Detail = "Your role does not include safeguarding (DSL) access.",
            });
        }

        // Retrieval is restricted to the schools the caller is DSL for, not their full viewing scope.
        var answer = await safeguardingService.AskAsync(request, safeguardingScope, cancellationToken);
        return Ok(answer);
    }
}
