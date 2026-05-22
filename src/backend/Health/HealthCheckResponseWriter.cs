using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace StudentSearch.Api.Health;

public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = false };

    public static Task WriteJsonAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration,
            checks = report.Entries.ToDictionary(
                entry => entry.Key,
                entry => new
                {
                    status = entry.Value.Status.ToString(),
                    description = entry.Value.Description,
                    duration = entry.Value.Duration
                })
        };

        return JsonSerializer.SerializeAsync(context.Response.Body, response, JsonOptions);
    }
}
