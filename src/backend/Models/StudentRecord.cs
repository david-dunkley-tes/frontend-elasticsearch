namespace StudentSearch.Api.Models;

public sealed record StudentRecord(Student Student, School School, Trust? Trust, SafeguardingLog? SafeguardingLog = null);

public sealed record Student(string Id, string ForeName, string Surname, string FullName, string YearGroup);

public sealed record School(string Id, string Name, string Address);

public sealed record Trust(string Id, string Name);

public sealed record SafeguardingLog(string Category, string Date, string Narrative, string? NarrativeRedacted = null);
