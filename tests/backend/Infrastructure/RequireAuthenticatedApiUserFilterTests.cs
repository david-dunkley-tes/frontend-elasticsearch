using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using StudentSearch.Api.Infrastructure;

namespace StudentSearch.Api.Tests.Infrastructure;

public sealed class RequireAuthenticatedApiUserFilterTests
{
    [Fact]
    public void OnAuthorization_ReturnsUnauthorizedForAnonymousUser()
    {
        var filter = new RequireAuthenticatedApiUserFilter();
        var context = CreateContext(new ClaimsPrincipal(new ClaimsIdentity()));

        filter.OnAuthorization(context);

        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    [Fact]
    public void OnAuthorization_AllowsAuthenticatedUser()
    {
        var filter = new RequireAuthenticatedApiUserFilter();
        var context = CreateContext(new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "user-1")], "Test")));

        filter.OnAuthorization(context);

        Assert.Null(context.Result);
    }

    private static AuthorizationFilterContext CreateContext(ClaimsPrincipal user)
    {
        var httpContext = new DefaultHttpContext { User = user };
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        return new AuthorizationFilterContext(actionContext, []);
    }
}
