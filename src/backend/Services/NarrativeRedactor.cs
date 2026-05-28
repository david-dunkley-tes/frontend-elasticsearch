using System.Text.RegularExpressions;
using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public sealed class NarrativeRedactor : INarrativeRedactor
{
    private const string StudentPlaceholder = "[student]";
    private const string SchoolPlaceholder = "[school]";

    public string Redact(string narrative, Student student, School school)
    {
        if (string.IsNullOrWhiteSpace(narrative))
        {
            return narrative;
        }

        var result = narrative;
        result = ReplaceWord(result, student.FullName, StudentPlaceholder);
        result = ReplaceWord(result, student.ForeName, StudentPlaceholder);
        result = ReplaceWord(result, student.Surname, StudentPlaceholder);
        result = ReplaceWord(result, school.Name, SchoolPlaceholder);
        return result;
    }

    private static string ReplaceWord(string input, string target, string replacement)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return input;
        }
        return Regex.Replace(input, $@"\b{Regex.Escape(target)}\b", replacement, RegexOptions.IgnoreCase);
    }
}
