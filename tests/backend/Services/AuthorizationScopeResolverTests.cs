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

    [Fact]
    public async Task ResolveRoleScopeAsync_MoreSpecificSchoolScopeOverridesTrustRole()
    {
        WriteSeedData([
            CreateRecord("SCH-KINGFISHER", "TRUST-COASTAL"),
            CreateRecord("SCH-OAKWOOD", "TRUST-COASTAL"),
            CreateRecord("SCH-EASTGATE", "TRUST-COASTAL")
        ]);
        var resolver = CreateResolver();
        var user = CreateUser([
            new AuthorizationScope("school", SchoolId: "SCH-KINGFISHER", Role: ["DSL"]),
            new AuthorizationScope("trust", TrustId: "TRUST-COASTAL")
        ]);

        var dsl = await resolver.ResolveRoleScopeAsync(user, Roles.Dsl);

        // School scope (most specific) grants DSL for Kingfisher; the trust scope carries no DSL role
        // for the rest, so only Kingfisher is in the safeguarding scope.
        Assert.False(dsl.IsGlobal);
        Assert.Equal(["SCH-KINGFISHER"], dsl.SchoolIds.ToArray());
    }

    [Fact]
    public async Task ResolveRoleScopeAsync_MoreSpecificEmptyRoleOverridesBroaderDsl()
    {
        WriteSeedData([
            CreateRecord("SCH-KINGFISHER", "TRUST-COASTAL"),
            CreateRecord("SCH-OAKWOOD", "TRUST-COASTAL"),
            CreateRecord("SCH-EASTGATE", "TRUST-COASTAL")
        ]);
        var resolver = CreateResolver();
        var user = CreateUser([
            new AuthorizationScope("global", Role: ["DSL"]),
            new AuthorizationScope("school", SchoolId: "SCH-KINGFISHER", Role: [])
        ]);

        var dsl = await resolver.ResolveRoleScopeAsync(user, Roles.Dsl);

        // Global grants DSL everywhere, but the more specific (empty-role) school scope removes Kingfisher.
        Assert.False(dsl.IsGlobal);
        Assert.Equal(["SCH-EASTGATE", "SCH-OAKWOOD"], dsl.SchoolIds.Order(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    [Fact]
    public async Task ResolveRoleScopeAsync_GlobalDslRoleGrantsEverySchool()
    {
        WriteSeedData([
            CreateRecord("SCH-A", "TRUST-1"),
            CreateRecord("SCH-B", null)
        ]);
        var resolver = CreateResolver();
        var user = CreateUser([new AuthorizationScope("global", Role: ["DSL"])]);

        var dsl = await resolver.ResolveRoleScopeAsync(user, Roles.Dsl);

        Assert.True(dsl.IsGlobal);
    }

    [Fact]
    public async Task ResolveRoleScopeAsync_GrantsNothingWithoutTheRole()
    {
        WriteSeedData([CreateRecord("SCH-KINGFISHER", "TRUST-COASTAL")]);
        var resolver = CreateResolver();
        var user = CreateUser([new AuthorizationScope("school", SchoolId: "SCH-KINGFISHER")]);

        var dsl = await resolver.ResolveRoleScopeAsync(user, Roles.Dsl);

        Assert.False(dsl.IsGlobal);
        Assert.Empty(dsl.SchoolIds);
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
