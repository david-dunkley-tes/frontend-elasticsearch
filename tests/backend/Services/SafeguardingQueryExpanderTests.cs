using StudentSearch.Api.Services;

namespace StudentSearch.Api.Tests.Services;

public sealed class SafeguardingQueryExpanderTests
{
    [Fact]
    public void Expand_BridgesPoliceToOperationEncompass()
    {
        var result = SafeguardingQueryExpander.Expand("Have any children had the police called");

        Assert.Contains("Have any children had the police called", result);
        Assert.Contains("Operation Encompass", result, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Expand_BridgesSocialWorkerToChildrensSocialCare()
    {
        var result = SafeguardingQueryExpander.Expand("any social worker involvement");

        Assert.Contains("children's social care", result, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Expand_LeavesUncoveredConceptsUntouched()
    {
        // Arson/fire is deliberately absent from the data and the ontology, so it must NOT be expanded —
        // the gap stays a clean hallucination test.
        const string question = "Do any schools have issues with arson or fire";

        Assert.Equal(question, SafeguardingQueryExpander.Expand(question));
    }

    [Fact]
    public void Expand_ReturnsInputWhenNothingMatches()
    {
        Assert.Equal("favourite lunch options", SafeguardingQueryExpander.Expand("favourite lunch options"));
    }
}
