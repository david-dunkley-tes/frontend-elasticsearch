using System.Text.Json.Nodes;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StudentSearch.Api.Infrastructure.Elasticsearch;

namespace StudentSearch.Api.Tests.Infrastructure;

public sealed class ElasticsearchHealthCheckTests
{
    [Fact]
    public async Task CheckHealthAsync_ReturnsHealthyWhenClusterHealthResponds()
    {
        var healthCheck = new ElasticsearchHealthCheck(new StubElasticsearchGateway(JsonNode.Parse("{}")));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Healthy, result.Status);
    }

    [Fact]
    public async Task CheckHealthAsync_ReturnsUnhealthyWhenClusterHealthFails()
    {
        var healthCheck = new ElasticsearchHealthCheck(new StubElasticsearchGateway(Exception: new InvalidOperationException("unavailable")));

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext());

        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.IsType<InvalidOperationException>(result.Exception);
    }

    private sealed class StubElasticsearchGateway(JsonNode? response = null, Exception? Exception = null) : IElasticsearchGateway
    {
        public Task<JsonNode?> SendAsync(HttpMethod method, string path, JsonNode? body = null)
        {
            if (Exception is not null)
            {
                throw Exception;
            }

            Assert.Equal(HttpMethod.Get, method);
            Assert.Equal("/_cluster/health", path);
            return Task.FromResult(response);
        }

        public Task<string> SendRawAsync(HttpMethod method, string path, string body)
        {
            throw new NotSupportedException();
        }

        public Task<bool> DeleteIfExistsAsync(string path)
        {
            throw new NotSupportedException();
        }
    }
}
