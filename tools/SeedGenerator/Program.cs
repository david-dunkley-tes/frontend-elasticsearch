using System.Globalization;
using System.Text.Json;

namespace SeedGenerator;

internal sealed record StudentRecord(Student Student, School School, Trust? Trust, SafeguardingLog? SafeguardingLog);
internal sealed record Student(string Id, string ForeName, string Surname, string FullName, string YearGroup);
internal sealed record School(string Id, string Name, string Address);
internal sealed record Trust(string Id, string Name);
internal sealed record SafeguardingLog(string Category, string Date, string Narrative);

internal sealed record SchoolDef(string Id, string Name, string Address, string? TrustId, string? TrustName);

internal sealed record SafeguardingCategory(string Name, int Weight, string[] Templates);

internal static class Program
{
    private const int RngSeed = 20260528;
    private const int MinPerYear = 18;
    private const int MaxPerYear = 22;
    private const int FirstStudentNumber = 10001;
    private const double SafeguardingProbability = 0.15;

    private static readonly DateOnly ReferenceDate = new(2026, 5, 1);
    private const int NarrativeWindowDays = 180;

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

    // Every narrative is fully synthetic. Weights bias the distribution toward the most
    // commonly logged DSL categories so the demo set feels realistic. {ForeName} and
    // {DateStr} are filled in per student; pronouns are intentionally avoided so we
    // don't have to track student gender.
    private static readonly SafeguardingCategory[] SafeguardingCategories =
    [
        new("Attendance", 3,
        [
            "Attendance dropped to 76% over the spring term. Parent stated transportation difficulties when contacted on {DateStr}. Action plan agreed with home-school liaison.",
            "Repeated late marks throughout the week of {DateStr}, often arriving after 9.30am. Pattern coincides with reported difficulties at home. Class teacher monitoring initiated.",
            "{ForeName} absent without notification for three consecutive days from {DateStr}. Welfare check completed by safeguarding lead; family found to be in temporary housing.",
        ]),
        new("Bullying", 2,
        [
            "Parents reported that {ForeName} has been excluded from playground games by a specific group of older pupils over two consecutive weeks. {ForeName} was tearful in class on {DateStr}.",
            "{ForeName} disclosed to a teaching assistant on {DateStr} that two children in the same class have been calling them names every day this term. Restorative meeting arranged.",
            "Lunchtime supervisor observed {ForeName} sitting alone repeatedly during the week of {DateStr}. {ForeName} confirmed being told not to join the group by a Year 6 pupil. Class teacher informed parents.",
        ]),
        new("HygieneNeglect", 2,
        [
            "{ForeName} arrived at school on {DateStr} wearing the same uniform unwashed for the third day running. School provided breakfast and a change of jumper. Family contacted by SENCO.",
            "Class teacher noted that {ForeName}'s appearance has deteriorated over the half-term, with matted hair and body odour concerns. Sensitive conversation held with parent on {DateStr}.",
            "Lunchtime staff observed {ForeName} eating quickly and asking for second portions repeatedly throughout the week of {DateStr}. Suspected food insecurity at home; place offered in free breakfast club.",
        ]),
        new("OnlineSafety", 2,
        [
            "{ForeName} disclosed to the ICT teacher on {DateStr} that an adult contact via an online game had been requesting photographs. Police safeguarding unit notified.",
            "A class peer reported that {ForeName} had shared a screenshot of an inappropriate message received via an older sibling's account. Concern raised on {DateStr}.",
            "{ForeName} mentioned to the class teacher on {DateStr} that they sometimes use a parent's social media account without supervision. Online safety conversation arranged with parents.",
        ]),
        new("Bereavement", 1,
        [
            "{ForeName}'s grandmother passed away on {DateStr}. Class teacher to monitor for signs of distress and offer access to the school counsellor.",
            "Father has been diagnosed with a serious illness and is currently undergoing treatment, as disclosed by mother on {DateStr}. {ForeName} appears unusually quiet during lessons.",
            "Mother contacted school on {DateStr} to inform staff that the family is going through bereavement following the death of {ForeName}'s uncle. Supportive measures put in place.",
        ]),
        new("DomesticAbuse", 1,
        [
            "Parent disclosed at parents' evening on {DateStr} that there had been a recent domestic incident involving the police at home. {ForeName} appeared withdrawn during morning registration the following week.",
            "{ForeName} mentioned to a one-to-one support worker on {DateStr} that they hide in their bedroom when their parents shout. Concern logged; referral to children's social care pending.",
            "Operation Encompass notification received on {DateStr} regarding a domestic call-out at the family home overnight. {ForeName} appeared tired but otherwise settled during morning lessons.",
        ]),
        new("MentalHealth", 2,
        [
            "{ForeName} expressed to a teaching assistant on {DateStr} that they feel sad all the time and are having trouble sleeping. Pastoral support team engaged.",
            "Class teacher observed {ForeName} crying in the toilets on three separate occasions during the week of {DateStr}. School counsellor referral submitted.",
            "{ForeName} has shown a marked withdrawal from peer interaction since returning from half-term. {ForeName}'s mother shared concerns about mood at home on {DateStr}.",
        ]),
        new("YoungCarer", 1,
        [
            "Disclosed by {ForeName} on {DateStr} that they regularly help care for a parent with mobility difficulties before school. Young carers liaison contacted.",
            "Parent revealed during a phone call on {DateStr} that {ForeName} takes on significant caring responsibilities for a younger sibling with medical needs. Support plan agreed; school to provide regular check-ins.",
            "{ForeName} arrived late on {DateStr} explaining that they had been helping a chronically ill family member with medication. Young carer status confirmed via family conversation.",
        ]),
        new("PhysicalChastisement", 1,
        [
            "{ForeName} disclosed to a teaching assistant on {DateStr} that they had been smacked at home over the weekend. A visible mark on the upper arm was noted; immediate referral to children's social care.",
            "Class teacher observed bruising on {ForeName}'s wrist on {DateStr}. {ForeName} initially gave inconsistent explanations. DSL convened a multi-agency strategy meeting.",
            "{ForeName} stated during a one-to-one conversation on {DateStr} that a parent uses a belt as discipline. Statutory referral to children's social care submitted.",
        ]),
        new("Weapons", 1,
        [
            "Knife found in {ForeName}'s school bag during a routine check on {DateStr}. {ForeName} claimed they were carrying it for protection on the walk home. Parents informed; referred to the local PCSO.",
            "{ForeName} brought a pair of kitchen scissors into school on {DateStr}, showing them to peers in the playground. Item confiscated; behavioural plan and home conversation initiated.",
            "A small pocket knife was found in {ForeName}'s locker on {DateStr} following a tip-off from another pupil. {ForeName} stated it belonged to an older sibling; family meeting scheduled.",
        ]),
        new("CountyLines", 1,
        [
            "{ForeName} was observed being collected from school on {DateStr} by an older male previously unknown to staff. Mother confirmed she did not recognise the description. Police community liaison alerted.",
            "Class teacher noted that {ForeName} has been receiving frequent messages on a second mobile phone recently brought into school. Concern about contextual safeguarding raised on {DateStr}.",
            "{ForeName} disclosed to a Year 6 peer mentor on {DateStr} that an older teenager from another estate has been offering them paid work. Immediate referral to MASH; family informed.",
        ]),
        new("SexualHarassmentBetweenPupils", 1,
        [
            "{ForeName} reported to the class teacher on {DateStr} that another pupil in the same year has been making inappropriate comments about their appearance. Investigation under peer-on-peer procedures begun.",
            "Two peers disclosed that {ForeName} has been targeted by name-calling of a sexualised nature in the corridor between lessons. Pattern logged on {DateStr}; restorative process initiated.",
            "Lunchtime supervisor witnessed an inappropriate physical incident involving {ForeName} on {DateStr}. Statements taken from both pupils; behavioural plan agreed.",
        ]),
        new("BehaviouralEscalation", 2,
        [
            "{ForeName} had three behavioural escalations during the week of {DateStr}, requiring removal from class on each occasion. Pattern coincides with parental separation noted in the family file.",
            "Class teacher reports that {ForeName}'s outbursts have escalated in frequency and intensity over the past fortnight. Pastoral conversation arranged for {DateStr}; SENCO consultation booked.",
            "{ForeName} threw a chair during morning lessons on {DateStr}; no peers were harmed. Reflection conversation held; behavioural support plan reviewed.",
        ]),
        new("FoodInsecurity", 1,
        [
            "{ForeName} reported to dinner staff on {DateStr} that they had not eaten breakfast or dinner the previous day. Family contacted; school food bank referral made.",
            "Class teacher observed {ForeName} repeatedly asking peers for spare food at lunch throughout the week of {DateStr}. Sensitive conversation with parent arranged; place secured in free breakfast club.",
            "{ForeName}'s elder sibling shared that the family is currently struggling financially, as confirmed during a home visit on {DateStr}. School-based food pantry referral processed.",
        ]),
        new("FamilySubstanceMisuse", 1,
        [
            "{ForeName} mentioned to a teaching assistant on {DateStr} that a parent sleeps a lot during the day and has lots of empty cans around the house. Concern recorded; referral to family support team.",
            "Mother contacted school on {DateStr} to disclose her own struggles with substance dependency and to request support for {ForeName}. Action plan agreed; counsellor referral made.",
            "Class teacher observed {ForeName} arriving on {DateStr} with strong unfamiliar smells on their clothing. Family conversation surfaced ongoing parental substance use; multi-agency support engaged.",
        ]),
    ];

    public static int Main(string[] args)
    {
        var outputPath = args.Length > 0 ? Path.GetFullPath(args[0]) : ResolveDefaultOutputPath();
        Console.WriteLine($"Writing seed to {outputPath}");

        var weightedCategories = ExpandWeights(SafeguardingCategories);
        var rng = new Random(RngSeed);
        var records = new List<StudentRecord>();
        var nextId = FirstStudentNumber;
        var safeguardingCount = 0;

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
                    var log = MaybeBuildSafeguardingLog(rng, weightedCategories, foreName);
                    if (log is not null)
                    {
                        safeguardingCount++;
                    }
                    records.Add(new StudentRecord(student, schoolDto, trustDto, log));
                }
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
        File.WriteAllText(outputPath, JsonSerializer.Serialize(records, options));

        Console.WriteLine($"Wrote {records.Count} students across {Schools.Length} schools and {YearGroups.Length} year groups.");
        Console.WriteLine($"{safeguardingCount} students ({(double)safeguardingCount / records.Count:P1}) carry a synthetic safeguarding log.");
        return 0;
    }

    private static SafeguardingLog? MaybeBuildSafeguardingLog(Random rng, SafeguardingCategory[] weighted, string foreName)
    {
        if (rng.NextDouble() >= SafeguardingProbability)
        {
            return null;
        }

        var category = weighted[rng.Next(weighted.Length)];
        var template = category.Templates[rng.Next(category.Templates.Length)];
        var date = ReferenceDate.AddDays(-rng.Next(1, NarrativeWindowDays + 1));
        var dateStr = date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);
        var narrative = template.Replace("{ForeName}", foreName).Replace("{DateStr}", dateStr);
        return new SafeguardingLog(category.Name, date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), narrative);
    }

    private static SafeguardingCategory[] ExpandWeights(SafeguardingCategory[] categories)
    {
        var expanded = new List<SafeguardingCategory>();
        foreach (var category in categories)
        {
            for (var i = 0; i < category.Weight; i++)
            {
                expanded.Add(category);
            }
        }
        return expanded.ToArray();
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
