using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StudentSearch.Api.Health;

namespace StudentSearch.Api.Tests.Health;

public sealed class HealthCheckResponseWriterTests
{
    [Fact]
    public async Task WriteJsonAsync_WritesApplicationJsonResponse()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var report = CreateReport();

        await HealthCheckResponseWriter.WriteJsonAsync(context, report);

        Assert.Equal("application/json", context.Response.ContentType);
    }

    [Fact]
    public async Task WriteJsonAsync_IncludesReportStatusDurationAndChecks()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var report = CreateReport();

        await HealthCheckResponseWriter.WriteJsonAsync(context, report);

        context.Response.Body.Position = 0;
        var body = await JsonNode.ParseAsync(context.Response.Body);

        Assert.Equal("Unhealthy", body?["status"]?.GetValue<string>());
        Assert.Equal("Healthy", body?["checks"]?["self"]?["status"]?.GetValue<string>());
        Assert.Equal("Ready", body?["checks"]?["self"]?["description"]?.GetValue<string>());
        Assert.Equal("Unhealthy", body?["checks"]?["elasticsearch"]?["status"]?.GetValue<string>());
        Assert.Equal("Unavailable", body?["checks"]?["elasticsearch"]?["description"]?.GetValue<string>());
        Assert.NotNull(body?["duration"]);
        Assert.NotNull(body?["checks"]?["self"]?["duration"]);
        Assert.NotNull(body?["checks"]?["elasticsearch"]?["duration"]);
    }

    private static HealthReport CreateReport()
    {
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["self"] = new(
                HealthStatus.Healthy,
                "Ready",
                TimeSpan.FromMilliseconds(3),
                exception: null,
                data: new Dictionary<string, object>()),
            ["elasticsearch"] = new(
                HealthStatus.Unhealthy,
                "Unavailable",
                TimeSpan.FromMilliseconds(7),
                new InvalidOperationException("failed"),
                data: new Dictionary<string, object>())
        };

        return new HealthReport(entries, TimeSpan.FromMilliseconds(10));
    }
}
