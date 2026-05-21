using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Tests.Services;

public sealed class DevBearerAuthenticationMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_WithValidBearerToken_SetsUserPrincipal()
    {
        var middleware = new DevBearerAuthenticationMiddleware(_ => Task.CompletedTask);
        var context = CreateApiContext();
        context.Request.Headers.Authorization = $"Bearer {Encode(new DevAccessToken("dev-user", "Dev User", [new AuthorizationScope("global")]))}";

        await middleware.InvokeAsync(context);

        Assert.True(context.User.Identity?.IsAuthenticated);
        Assert.Equal("dev-user", context.User.FindFirst("sub")?.Value);
        Assert.Contains("global", context.User.FindFirst(DevBearerAuthenticationMiddleware.ScopesClaimType)?.Value);
    }

    [Fact]
    public async Task InvokeAsync_WithoutBearerToken_ReturnsUnauthorized()
    {
        var middleware = new DevBearerAuthenticationMiddleware(_ => Task.CompletedTask);
        var context = CreateApiContext();

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status401Unauthorized, context.Response.StatusCode);
    }

    private static DefaultHttpContext CreateApiContext()
    {
        return new DefaultHttpContext
        {
            Request =
            {
                Method = HttpMethods.Post,
                Path = "/api/search"
            }
        };
    }

    private static string Encode(DevAccessToken token)
    {
        var json = JsonSerializer.Serialize(token, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
