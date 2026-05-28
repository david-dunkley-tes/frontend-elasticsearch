using StudentSearch.Api.Models;

namespace StudentSearch.Api.Interfaces;

public interface INarrativeRedactor
{
    string Redact(string narrative, Student student, School school);
}
