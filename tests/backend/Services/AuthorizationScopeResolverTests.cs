using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using StudentSearch.Api.Configuration;
using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Tests.Services;

public sealed class AuthorizationScopeResolverTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task ResolveAsync_ExpandsTrustAndSchoolScopesAsUnion()
    {
        WriteSeedData([
            CreateRecord("SCH-NORTH-1", "TRUST-NORTHSHIRE"),
            CreateRecord("SCH-NORTH-2", "TRUST-NORTHSHIRE"),
            CreateRecord("SCH-WESTBROOK", "TRUST-COASTAL"),
            CreateRecord("SCH-OTHER", null)
        ]);
        var resolver = CreateResolver();
        var user = CreateUser([
            new AuthorizationScope("trust", TrustId: "TRUST-NORTHSHIRE"),
            new AuthorizationScope("school", SchoolId: "SCH-WESTBROOK")
        ]);

        var scope = await resolver.ResolveAsync(user);

        Assert.False(scope.IsGlobal);
        Assert.Equal(
            ["SCH-NORTH-1", "SCH-NORTH-2", "SCH-WESTBROOK"],
            scope.SchoolIds.Order(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    [Fact]
    public async Task ResolveAsync_ReturnsGlobalWhenGlobalScopeIsPresent()
    {
        var resolver = CreateResolver();
        var user = CreateUser([new AuthorizationScope("global")]);

        var scope = await resolver.ResolveAsync(user);

        Assert.True(scope.IsGlobal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
    }

    private AuthorizationScopeResolver CreateResolver()
    {
        var configuration = new ConfigurationManager
        {
            ["SeedData:Path"] = "data/students.seed.json"
        };

        return new AuthorizationScopeResolver(new SearchConfiguration(configuration, new StubWebHostEnvironment(_tempRoot)));
    }

    private void WriteSeedData(List<StudentRecord> records)
    {
        var dataDirectory = Path.Combine(_tempRoot, "data");
        Directory.CreateDirectory(dataDirectory);
        File.WriteAllText(
            Path.Combine(dataDirectory, "students.seed.json"),
            JsonSerializer.Serialize(records, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    private static StudentRecord CreateRecord(string schoolId, string? trustId)
    {
        return new StudentRecord(
            new Student(Guid.NewGuid().ToString("N"), "Test", "Student", "Test Student", "Year 8"),
            new School(schoolId, schoolId, "Test Address"),
            trustId is null ? null : new Trust(trustId, trustId));
    }

    private static ClaimsPrincipal CreateUser(List<AuthorizationScope> scopes)
    {
        var claims = new[]
        {
            new Claim("sub", "test-user"),
            new Claim(DevBearerAuthenticationMiddleware.ScopesClaimType, JsonSerializer.Serialize(scopes, new JsonSerializerOptions(JsonSerializerDefaults.Web)))
        };

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
    }

    private sealed class StubWebHostEnvironment(string contentRootPath) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "StudentSearch.Api.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.Combine(contentRootPath, "src", "backend");
        public string EnvironmentName { get; set; } = "Development";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
