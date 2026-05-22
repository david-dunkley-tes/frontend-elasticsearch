using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Tests.Services;

public sealed class VersionInfoProviderTests
{
    [Fact]
    public void GetVersionInfo_UsesAssemblyAndFallbackValuesByDefault()
    {
        var provider = new VersionInfoProvider(CreateConfiguration(), new StubHostEnvironment());

        var versionInfo = provider.GetVersionInfo();

        Assert.Equal("StudentSearch.Api.Tests", versionInfo.Service);
        Assert.NotEqual("unknown", versionInfo.Version);
        Assert.Equal("local", versionInfo.Commit);
        Assert.Equal("unknown", versionInfo.BuildTime);
        Assert.Equal("Development", versionInfo.Environment);
    }

    [Fact]
    public void GetVersionInfo_UsesConfiguredBuildValues()
    {
        var provider = new VersionInfoProvider(
            CreateConfiguration(new Dictionary<string, string?>
            {
                ["APP_VERSION"] = "1.2.3",
                ["GIT_COMMIT"] = "abc1234",
                ["BUILD_TIME"] = "2026-05-22T09:15:00Z"
            }),
            new StubHostEnvironment());

        var versionInfo = provider.GetVersionInfo();

        Assert.Equal("1.2.3", versionInfo.Version);
        Assert.Equal("abc1234", versionInfo.Commit);
        Assert.Equal("2026-05-22T09:15:00Z", versionInfo.BuildTime);
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?>? values = null)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values ?? new Dictionary<string, string?>())
            .Build();
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "StudentSearch.Api.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
