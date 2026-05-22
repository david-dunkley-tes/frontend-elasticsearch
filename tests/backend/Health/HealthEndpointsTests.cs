using System.Net;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StudentSearch.Api.Infrastructure.Elasticsearch;

namespace StudentSearch.Api.Tests.Health;

public sealed class HealthEndpointsTests
{
    [Fact]
    public async Task LiveEndpoint_ReportsOnlyTheSelfCheckAndStaysHealthyWhenElasticsearchIsDown()
    {
        await using var factory = CreateFactory(elasticsearchHealthy: false);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync(response);
        Assert.Equal("Healthy", body?["status"]?.GetValue<string>());
        Assert.NotNull(body?["checks"]?["self"]);
        Assert.Null(body?["checks"]?["elasticsearch"]);
    }

    [Fact]
    public async Task ReadyEndpoint_ReportsOnlyTheElasticsearchCheckWhenItIsHealthy()
    {
        await using var factory = CreateFactory(elasticsearchHealthy: true);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await ReadJsonAsync(response);
        Assert.Equal("Healthy", body?["status"]?.GetValue<string>());
        Assert.NotNull(body?["checks"]?["elasticsearch"]);
        Assert.Null(body?["checks"]?["self"]);
    }

    [Fact]
    public async Task ReadyEndpoint_ReturnsServiceUnavailableWhenElasticsearchIsDown()
    {
        await using var factory = CreateFactory(elasticsearchHealthy: false);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var body = await ReadJsonAsync(response);
        Assert.Equal("Unhealthy", body?["checks"]?["elasticsearch"]?["status"]?.GetValue<string>());
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task HealthEndpoints_AreReachableWithoutABearerToken(string path)
    {
        await using var factory = CreateFactory(elasticsearchHealthy: true);
        using var client = factory.CreateClient();

        var response = await client.GetAsync(path);

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ApiEndpoints_StillRequireABearerToken()
    {
        await using var factory = CreateFactory(elasticsearchHealthy: true);
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateFactory(bool elasticsearchHealthy)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IElasticsearchGateway>();
                services.AddSingleton<IElasticsearchGateway>(new StubElasticsearchGateway(elasticsearchHealthy));
            });
        });
    }

    private static async Task<JsonNode?> ReadJsonAsync(HttpResponseMessage response)
    {
        return JsonNode.Parse(await response.Content.ReadAsStringAsync());
    }

    private sealed class StubElasticsearchGateway(bool healthy) : IElasticsearchGateway
    {
        public Task<JsonNode?> SendAsync(HttpMethod method, string path, JsonNode? body = null)
        {
            if (!healthy)
            {
                throw new InvalidOperationException("Elasticsearch is unavailable.");
            }

            return Task.FromResult<JsonNode?>(JsonNode.Parse("{}"));
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
