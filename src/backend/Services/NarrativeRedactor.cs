using System.Text.RegularExpressions;
using StudentSearch.Api.Interfaces;
using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public sealed class NarrativeRedactor : INarrativeRedactor
{
    private const string StudentPlaceholder = "[student]";
    private const string SchoolPlaceholder = "[school]";
    private const string TeacherPlaceholder = "[teacher]";

    // Honorifics are too generic to redact safely on their own; only the name parts identify a teacher.
    private static readonly HashSet<string> Titles = new(StringComparer.OrdinalIgnoreCase)
    {
        "mr", "mrs", "ms", "miss", "mx", "dr"
    };

    public string Redact(string narrative, Student student, School school, string? teacherName = null)
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
        result = RedactTeacher(result, teacherName);
        return result;
    }

    private static string RedactTeacher(string input, string? teacherName)
    {
        if (string.IsNullOrWhiteSpace(teacherName))
        {
            return input;
        }

        // Redact the full teacher name first, then each name part (so "Priya" or "Patel"
        // alone is caught), skipping honorifics like "Mr"/"Ms" that aren't identifying.
        var result = ReplaceWord(input, teacherName, TeacherPlaceholder);
        foreach (var part in teacherName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (part.Length >= 3 && !Titles.Contains(part))
            {
                result = ReplaceWord(result, part, TeacherPlaceholder);
            }
        }
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
