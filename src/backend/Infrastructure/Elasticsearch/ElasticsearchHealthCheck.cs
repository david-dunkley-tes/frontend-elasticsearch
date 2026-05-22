using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace StudentSearch.Api.Infrastructure.Elasticsearch;

public sealed class ElasticsearchHealthCheck(IElasticsearchGateway gateway) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await gateway.SendAsync(HttpMethod.Get, "/_cluster/health");
            return HealthCheckResult.Healthy("Elasticsearch cluster health endpoint responded.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Elasticsearch cluster health endpoint did not respond successfully.", exception);
        }
    }
}
