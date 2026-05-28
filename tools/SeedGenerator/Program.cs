using System.Text.Json;

namespace SeedGenerator;

internal sealed record StudentRecord(Student Student, School School, Trust? Trust);
internal sealed record Student(string Id, string ForeName, string Surname, string FullName, string YearGroup);
internal sealed record School(string Id, string Name, string Address);
internal sealed record Trust(string Id, string Name);

internal sealed record SchoolDef(string Id, string Name, string Address, string? TrustId, string? TrustName);

internal static class Program
{
    private const int RngSeed = 20260528;
    private const int MinPerYear = 18;
    private const int MaxPerYear = 22;
    private const int FirstStudentNumber = 10001;

    private static readonly string[] YearGroups =
    [
        "Reception",
        "Year 1",
        "Year 2",
        "Year 3",
        "Year 4",
        "Year 5",
        "Year 6",
    ];

    private static readonly SchoolDef[] Schools =
    [
        new("SCH-BEACON-HILL", "Beacon Hill School", "14 Beacon Road, Exeter", null, null),
        new("SCH-BRIDGEWATER-HIGH", "Bridgewater Primary School", "2 Canal Street, Warrington", "TRUST-BRIDGE-LEARNING", "Bridge Learning Trust"),
        new("SCH-CEDAR-GROVE", "Cedar Grove Primary School", "27 Cedar Avenue, Coventry", "TRUST-MIDLANDS-EDUCATION", "Midlands Education Trust"),
        new("SCH-EASTGATE", "Eastgate School", "45 Eastgate, Lincoln", null, null),
        new("SCH-HARBOUR-VIEW", "Harbour View School", "5 Marina Street, Plymouth", "TRUST-COASTAL-SCHOOLS", "Coastal Schools Trust"),
        new("SCH-HARINGTON-COMMUNITY", "Harington Community Primary", "4 Station Road, Leeds", "TRUST-NORTHSHIRE", "Northshire Learning Trust"),
        new("SCH-HARRINGTON-PRIMARY", "Harrington Primary School", "12 North Road, York", "TRUST-NORTHSHIRE", "Northshire Learning Trust"),
        new("SCH-HILLCREST", "Hillcrest Primary School", "9 Hillcrest Road, Bristol", "TRUST-CITY-LEARNING", "City Learning Collective"),
        new("SCH-KINGFISHER", "Kingfisher Primary School", "10 Riverbank, Preston", "TRUST-COASTAL-SCHOOLS", "Coastal Schools Trust"),
        new("SCH-LAKESIDE-GRAMMAR", "Lakeside Primary School", "54 Lake View, Kendal", "TRUST-LAKES-EDUCATION", "Lakes Education Trust"),
        new("SCH-MEADOW-VIEW", "Meadow View Primary", "31 Meadow Road, Doncaster", "TRUST-NORTHERN-LEARNING", "Northern Learning Partnership"),
        new("SCH-NORTHFIELD", "Northfield Primary School", "21 High Street, Sheffield", "TRUST-NORTHERN-LEARNING", "Northern Learning Partnership"),
        new("SCH-OAKWOOD", "Oakwood School", "3 Green Lane, Bradford", "TRUST-NORTHSHIRE", "Northshire Learning Trust"),
        new("SCH-RIVERSIDE-HIGH", "Riverside Primary School", "88 Mill Lane, Manchester", null, null),
        new("SCH-SOUTHBANK-STUDIO", "Southbank Primary School", "6 Foundry Lane, Nottingham", "TRUST-CITY-LEARNING", "City Learning Collective"),
        new("SCH-ST-CUTHBERTS", "St Cuthbert's School", "7 Abbey Road, Durham", null, null),
        new("SCH-WESTBROOK", "Westbrook Primary School", "19 Market Place, Hull", "TRUST-COASTAL-SCHOOLS", "Coastal Schools Trust"),
        new("SCH-WOODLAND-PARK", "Woodland Park School", "18 Forest Road, Norwich", "TRUST-EASTERN-LEARNING", "Eastern Learning Trust"),
    ];

    private static readonly string[] Forenames =
    [
        "Amelia", "Oliver", "Isla", "George", "Mia", "Harry", "Ava", "Noah", "Lily", "Leo",
        "Sophie", "Charlie", "Grace", "Jack", "Ella", "Oscar", "Freya", "Alfie", "Evie", "Henry",
        "Poppy", "Theo", "Rosie", "Arthur", "Ivy", "Jacob", "Emily", "Thomas", "Phoebe", "William",
        "Florence", "Edward", "Daisy", "James", "Sienna", "Finley", "Maisie", "Archie", "Sophia", "Lucas",
        "Hannah", "Max", "Erin", "Reuben", "Maya", "Joshua", "Imogen", "Samuel", "Aria", "Toby",
        "Bella", "Ethan", "Ruby", "Joseph", "Hattie", "Jude", "Beatrice", "Felix", "Esme", "Nathan",
    ];

    private static readonly string[] Surnames =
    [
        "Harrington", "Harington", "Patel", "Jones", "Smith", "Brown", "Taylor", "Davies", "Wilson", "Evans",
        "Thomas", "Roberts", "Johnson", "Walker", "Wright", "Robinson", "Thompson", "White", "Hughes", "Edwards",
        "Green", "Hall", "Wood", "Harris", "Martin", "Jackson", "Clarke", "Clark", "Turner", "Hill",
        "Scott", "Cooper", "Morris", "Ward", "Moore", "King", "Watson", "Baker", "Lewis", "Bennett",
        "Khan", "Singh", "Carter", "Phillips", "Mitchell", "Parker", "Murphy", "Bailey", "Reid", "Murray",
        "Cox", "Richardson", "Cole", "Foster", "Stewart", "Powell", "Holmes", "Webb", "Gray", "Russell",
    ];

    public static int Main(string[] args)
    {
        var outputPath = args.Length > 0 ? Path.GetFullPath(args[0]) : ResolveDefaultOutputPath();
        Console.WriteLine($"Writing seed to {outputPath}");

        var rng = new Random(RngSeed);
        var records = new List<StudentRecord>();
        var nextId = FirstStudentNumber;

        foreach (var school in Schools)
        {
            var schoolDto = new School(school.Id, school.Name, school.Address);
            var trustDto = school.TrustId is null ? null : new Trust(school.TrustId, school.TrustName!);

            foreach (var yearGroup in YearGroups)
            {
                var count = rng.Next(MinPerYear, MaxPerYear + 1);
                for (var i = 0; i < count; i++)
                {
                    var foreName = Forenames[rng.Next(Forenames.Length)];
                    var surname = Surnames[rng.Next(Surnames.Length)];
                    var id = $"S{nextId++}";
                    var student = new Student(id, foreName, surname, $"{foreName} {surname}", yearGroup);
                    records.Add(new StudentRecord(student, schoolDto, trustDto));
                }
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
        File.WriteAllText(outputPath, JsonSerializer.Serialize(records, options));

        Console.WriteLine($"Wrote {records.Count} students across {Schools.Length} schools and {YearGroups.Length} year groups.");
        return 0;
    }

    private static string ResolveDefaultOutputPath()
    {
        var dir = AppContext.BaseDirectory;
        while (!Directory.Exists(Path.Combine(dir, ".git")))
        {
            var parent = Directory.GetParent(dir);
            if (parent is null)
            {
                throw new InvalidOperationException("Could not locate repository root (no .git directory found in any parent).");
            }
            dir = parent.FullName;
        }
        return Path.Combine(dir, "data", "students.seed.json");
    }
}
