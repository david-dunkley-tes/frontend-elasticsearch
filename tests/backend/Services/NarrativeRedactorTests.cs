using StudentSearch.Api.Models;
using StudentSearch.Api.Services;

namespace StudentSearch.Api.Tests.Services;

public sealed class NarrativeRedactorTests
{
    private static readonly Student Student = new("S10042", "Freya", "Newton", "Freya Newton", "Year 5");
    private static readonly School School = new("SCH-KINGFISHER", "Kingfisher Primary School", "10 Riverbank, Preston");

    [Fact]
    public void Redact_ReplacesStudentAndSchoolNames()
    {
        var redactor = new NarrativeRedactor();

        var result = redactor.Redact("Freya was upset at Kingfisher Primary School.", Student, School);

        Assert.Equal("[student] was upset at [school].", result);
    }

    [Fact]
    public void Redact_ProtectsTeacherNameLikeStudentName()
    {
        var redactor = new NarrativeRedactor();

        var result = redactor.Redact(
            "Ms Priya Patel raised a concern; Patel also spoke to the family about Freya.",
            Student,
            School,
            "Ms Priya Patel");

        Assert.DoesNotContain("Priya", result);
        Assert.DoesNotContain("Patel", result);
        Assert.Contains("[teacher]", result);
        Assert.DoesNotContain("Freya", result);
    }

    [Fact]
    public void Redact_DoesNotRedactBareHonorific()
    {
        var redactor = new NarrativeRedactor();

        // "Ms" on its own is not identifying and must survive, so unrelated prose is untouched.
        var result = redactor.Redact("Ms Smith from the council visited.", Student, School, "Ms Priya Patel");

        Assert.Contains("Ms Smith", result);
    }
}
