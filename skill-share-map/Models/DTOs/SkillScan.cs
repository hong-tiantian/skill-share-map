namespace SkillShareMap.Models.DTOs;

// Request to verify a user's skills from real artifacts (GitHub / transcript).
public class SkillScanRequest
{
    public int UserId { get; set; }
    public string? GithubUsername { get; set; }
    public string? TranscriptText { get; set; }
}

// Result of the verifiable-skill engine — mirrors the frontend `SkillScanResult` type.
public class SkillScanResult
{
    public int UserId { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public string? Summary { get; set; }
    public List<VerifiedSkill> Skills { get; set; } = new();
}

public class VerifiedSkill
{
    public string Category { get; set; } = string.Empty;   // TaskCategory enum name
    public string? Label { get; set; }
    public int Score { get; set; }                          // 0-100 verified proficiency
    public string Tier { get; set; } = "Newbie";
    public List<SkillEvidence> Evidence { get; set; } = new();
}

public class SkillEvidence
{
    public string Source { get; set; } = string.Empty;     // GitHub | Transcript | Platform
    public string Detail { get; set; } = string.Empty;
    public double Weight { get; set; }                       // 0-1 contribution to the score
}
