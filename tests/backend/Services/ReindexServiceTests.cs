using StudentSearch.Api.Interfaces;
using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Tests.Services;

public sealed class ReindexServiceTests
{
    [Fact]
    public async Task ReindexAsync_DelegatesToStudentIndexSeeder()
    {
        var seeder = new CapturingStudentIndexSeeder();
        var service = new ReindexService(seeder);

        var response = await service.ReindexAsync();

        Assert.True(seeder.WasCalled);
        Assert.Equal("students", response.IndexName);
        Assert.Equal(24, response.DocumentsIndexed);
        Assert.Equal("ok", response.Status);
    }

    private sealed class CapturingStudentIndexSeeder : IStudentIndexSeeder
    {
        public bool WasCalled { get; private set; }

        public Task<ReindexResponse> ReindexAsync()
        {
            WasCalled = true;
            return Task.FromResult(new ReindexResponse("students", 24, "ok"));
        }
    }
}
