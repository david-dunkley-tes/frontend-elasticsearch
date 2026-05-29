namespace StudentSearch.Api.Services;

/// <summary>
/// Lightweight safeguarding "ontology" for query expansion: when a question mentions any term in a
/// concept group, the group's sibling terms are appended to the text that gets embedded for retrieval.
/// This bridges domain jargon the general embedding model represents weakly (e.g. "police" ↔
/// "Operation Encompass", "social worker" ↔ "children's social care") so semantically-related records
/// are recalled. Only the retrieval text is expanded — the original question still drives the answer.
/// </summary>
public static class SafeguardingQueryExpander
{
    private static readonly string[][] ConceptGroups =
    [
        ["police", "officers", "law enforcement", "Operation Encompass", "domestic call-out", "arrested"],
        ["social worker", "social care", "children's social care", "children's services", "social services", "statutory referral", "MASH"],
        ["early help", "family support", "multi-agency", "MARAC"],
        ["CAMHS", "mental health", "counsellor", "wellbeing", "anxiety", "low mood", "self-harm"],
        ["NSPCC", "helpline", "child protection"],
        ["domestic abuse", "domestic violence", "domestic incident"],
        ["county lines", "exploitation", "gang", "grooming", "trafficking"],
        ["online safety", "social media", "inappropriate messages", "indecent images"],
        ["neglect", "hygiene", "matted hair", "body odour", "unwashed", "soiled uniform"],
        ["food insecurity", "hungry", "free school meals", "food bank"],
        ["bullying", "bullied", "victim", "perpetrator", "name-calling"],
        ["physical abuse", "smacked", "bruising", "injury", "chastisement"],
        ["attendance", "absence", "truancy", "persistent absence"],
        ["young carer", "caring responsibilities", "carer"],
        ["weapon", "knife", "blade", "scissors"],
        ["bereavement", "grief", "passed away", "died"],
    ];

    /// <summary>
    /// Returns the question augmented with sibling terms from any matched concept group(s). If nothing
    /// matches (or every sibling is already present), the original question is returned unchanged.
    /// </summary>
    public static string Expand(string question)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return question;
        }

        var lower = question.ToLowerInvariant();
        var additions = new List<string>();
        foreach (var group in ConceptGroups)
        {
            if (group.Any(term => lower.Contains(term.ToLowerInvariant())))
            {
                additions.AddRange(group.Where(term => !lower.Contains(term.ToLowerInvariant())));
            }
        }

        var extra = additions.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return extra.Count == 0 ? question : $"{question} {string.Join(' ', extra)}";
    }
}
