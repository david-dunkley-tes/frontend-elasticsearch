using StudentSearch.Api.Models;

namespace StudentSearch.Api.Tests.Models;

public sealed class AuthorizedSchoolScopeTests
{
    [Fact]
    public void GrantsAnySchool_IsTrue_ForGlobal()
    {
        Assert.True(AuthorizedSchoolScope.Global.GrantsAnySchool);
    }

    [Fact]
    public void GrantsAnySchool_IsTrue_WhenSchoolsPresent()
    {
        var scope = new AuthorizedSchoolScope(false, ["SCH-KINGFISHER"]);

        Assert.True(scope.GrantsAnySchool);
    }

    [Fact]
    public void GrantsAnySchool_IsFalse_WhenEmptyAndNotGlobal()
    {
        var scope = new AuthorizedSchoolScope(false, []);

        Assert.False(scope.GrantsAnySchool);
    }
}
