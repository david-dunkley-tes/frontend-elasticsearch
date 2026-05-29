using System.Security.Claims;
using System.Text;
using System.Text.Json;
using StudentSearch.Api.Infrastructure;
using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public sealed class DevBearerAuthenticationMiddleware(RequestDelegate next)
{
    public const string ScopesClaimType = "scopes";

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsOptions(context.Request.Method) ||
            !context.Request.Path.StartsWithSegments("/api") ||
            context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context);
            return;
        }

        if (!TryReadBearerToken(context.Request.Headers.Authorization.ToString(), out var bearerToken) ||
            !TryDecodeToken(bearerToken, out var token))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("A valid bearer token is required.");
            return;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, token.Sub),
            new("sub", token.Sub),
            new(ScopesClaimType, JsonSerializer.Serialize(token.Scopes, JsonDefaults.Web))
        };

        if (!string.IsNullOrWhiteSpace(token.Name))
        {
            claims.Add(new(ClaimTypes.Name, token.Name));
            claims.Add(new("name", token.Name));
        }

        context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "DevBearer"));
        await next(context);
    }

    private static bool TryReadBearerToken(string authorization, out string token)
    {
        token = string.Empty;
        const string prefix = "Bearer ";
        if (!authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        token = authorization[prefix.Length..].Trim();
        return token.Length > 0;
    }

    private static bool TryDecodeToken(string bearerToken, out DevAccessToken token)
    {
        token = null!;
        try
        {
            var json = Encoding.UTF8.GetString(Base64UrlDecode(bearerToken));
            var decoded = JsonSerializer.Deserialize<DevAccessToken>(json, JsonDefaults.Web);
            if (decoded is null || string.IsNullOrWhiteSpace(decoded.Sub) || decoded.Scopes.Count == 0)
            {
                return false;
            }

            token = decoded;
            return token.Scopes.All(IsValidScope);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidScope(AuthorizationScope scope)
    {
        return scope.Type.ToLowerInvariant() switch
        {
            "global" => true,
            "school" => !string.IsNullOrWhiteSpace(scope.SchoolId),
            "trust" => !string.IsNullOrWhiteSpace(scope.TrustId),
            _ => false
        };
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + ((4 - padded.Length % 4) % 4), '=');
        return Convert.FromBase64String(padded);
    }
}
