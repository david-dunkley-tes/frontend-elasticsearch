using StudentSearch.Api.Models;

namespace StudentSearch.Api.Services;

public interface INarrativeRedactor
{
    string Redact(string narrative, Student student, School school);
}
