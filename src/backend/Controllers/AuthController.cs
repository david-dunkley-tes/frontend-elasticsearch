using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using StudentSearch.Api.Infrastructure;
using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    [HttpGet("me")]
    public ActionResult<CurrentUserResponse> Me()
    {
        var sub = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(sub))
        {
            return Unauthorized();
        }

        var scopesJson = User.FindFirstValue(DevBearerAuthenticationMiddleware.ScopesClaimType) ?? "[]";
        var scopes = JsonSerializer.Deserialize<List<AuthorizationScope>>(scopesJson, JsonDefaults.Web) ?? [];

        return Ok(new CurrentUserResponse(sub, User.FindFirstValue("name") ?? User.Identity?.Name, scopes));
    }
}
