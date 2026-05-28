using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using StudentSearch.Api.Controllers;
using StudentSearch.Api.Interfaces;
using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Tests.Controllers;

public sealed class AdminControllerTests
{
    [Fact]
    public async Task Reindex_ReturnsOkInDevelopment()
    {
        var response = new ReindexResponse("students", 24, "ok");
        var reindexService = new StubReindexService(response);
        var controller = new AdminController(reindexService, new StubWebHostEnvironment("Development"));

        var result = await controller.Reindex();

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(response, ok.Value);
        Assert.True(reindexService.WasCalled);
    }

    [Fact]
    public async Task Reindex_ReturnsNotFoundOutsideDevelopment()
    {
        var reindexService = new StubReindexService(new ReindexResponse("students", 24, "ok"));
        var controller = new AdminController(reindexService, new StubWebHostEnvironment("Production"));

        var result = await controller.Reindex();

        Assert.IsType<NotFoundResult>(result);
        Assert.False(reindexService.WasCalled);
    }

    private sealed class StubReindexService(ReindexResponse response) : IReindexService
    {
        public bool WasCalled { get; private set; }

        public Task<ReindexResponse> ReindexAsync()
        {
            WasCalled = true;
            return Task.FromResult(response);
        }
    }

    private sealed class StubWebHostEnvironment(string environmentName) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "StudentSearch.Api.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = environmentName;
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
