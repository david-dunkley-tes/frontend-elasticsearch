using Microsoft.AspNetCore.Http;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Tests.Services;

public sealed class DevAccessTokenFactoryTests
{
    [Fact]
    public async Task Encode_CreatesTokenAcceptedByDevBearerMiddleware()
    {
        var middleware = new DevBearerAuthenticationMiddleware(_ => Task.CompletedTask);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/auth/me";
        context.Request.Headers.Authorization = $"Bearer {DevAccessTokenFactory.Encode(DevAccessTokenFactory.SwaggerUser)}";

        await middleware.InvokeAsync(context);

        Assert.Equal("dev-kingfisher-academy", context.User.FindFirst("sub")?.Value);
    }
}
