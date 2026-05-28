using System.Text;
using System.Text.Json;
using StudentSearch.Api.Infrastructure;
using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public static class DevAccessTokenFactory
{
    public static DevAccessToken SwaggerUser { get; } = new(
        "dev-kingfisher-academy",
        "Kingfisher Academy",
        [new AuthorizationScope("school", SchoolId: "SCH-KINGFISHER")]);

    public static string Encode(DevAccessToken token)
    {
        var json = JsonSerializer.Serialize(token, JsonDefaults.Web);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
