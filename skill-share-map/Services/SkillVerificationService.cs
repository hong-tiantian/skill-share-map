using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SkillShareMap.Data;
using SkillShareMap.Models;
using SkillShareMap.Models.DTOs;

namespace SkillShareMap.Services;

public class SkillVerificationService : ISkillVerificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<SkillVerificationService> _logger;

    private static readonly Dictionary<TaskCategory, string> CategoryLabels = new()
    {
        [TaskCategory.StudyHelp] = "Study Help",
        [TaskCategory.TechHelp] = "Tech Help",
        [TaskCategory.CreativeDesign] = "Creative Design",
        [TaskCategory.PhotoVideo] = "Photo & Video",
        [TaskCategory.WritingEditing] = "Writing & Editing",
        [TaskCategory.LanguageHelp] = "Language Help",
    };

    public SkillVerificationService(
        ApplicationDbContext context,
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<SkillVerificationService> logger)
    {
        _context = context;
        _httpFactory = httpFactory;
        _config = config;
        _logger = logger;
    }

    public async Task<SkillScanResult> ScanAsync(int userId, string? githubUsername, string? transcriptText)
    {
        githubUsername = githubUsername?.Trim();
        transcriptText = transcriptText?.Trim();

        // 1. Gather real evidence from the user's artifacts.
        var artifactSb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(githubUsername))
        {
            var gh = await FetchGithubAsync(githubUsername);
            if (!string.IsNullOrWhiteSpace(gh))
                artifactSb.AppendLine($"## GitHub public repositories for @{githubUsername}\n{gh}");
            else
                artifactSb.AppendLine($"## GitHub @{githubUsername}: no public repositories found.");
        }
        if (!string.IsNullOrWhiteSpace(transcriptText))
        {
            // Cap pasted text to keep the prompt bounded.
            var clipped = transcriptText.Length > 6000 ? transcriptText[..6000] : transcriptText;
            artifactSb.AppendLine($"## Pasted transcript / past work\n{clipped}");
        }

        if (artifactSb.Length == 0)
            throw new ArgumentException("Provide a GitHub username or transcript text to verify.");

        // 2. Ask DeepSeek to score skills with an evidence trail (strict JSON).
        var content = await CallDeepSeekAsync(SystemPrompt(), artifactSb.ToString());
        var parsed = ParseSkills(content);

        var result = new SkillScanResult
        {
            UserId = userId,
            Source = !string.IsNullOrWhiteSpace(githubUsername) ? githubUsername! : "transcript",
            ScannedAt = DateTime.UtcNow,
            Summary = parsed.Summary,
            Skills = parsed.Skills,
        };

        // 3. Persist verified scores into UserSkillProgress so the RPG visuals reflect them.
        //    Verification is treated as authoritative for the scanned categories.
        foreach (var skill in result.Skills)
        {
            if (!Enum.TryParse<TaskCategory>(skill.Category, ignoreCase: true, out var cat))
                continue;

            skill.Category = cat.ToString();
            skill.Label = CategoryLabels.GetValueOrDefault(cat, cat.ToString());
            skill.Score = Math.Clamp(skill.Score, 0, 100);
            var xp = ScoreToXp(skill.Score);
            var tier = TierFromXp(xp);
            skill.Tier = tier.ToString();

            var prog = await _context.UserSkillProgress
                .FirstOrDefaultAsync(p => p.UserId == userId && p.Category == cat);
            if (prog == null)
            {
                prog = new UserSkillProgress { UserId = userId, Category = cat };
                _context.UserSkillProgress.Add(prog);
            }
            prog.TotalXp = xp;
            prog.CurrentTier = tier;
            prog.LastUpdated = DateTime.UtcNow;
        }
        await _context.SaveChangesAsync();

        return result;
    }

    // --- GitHub ---

    private async Task<string> FetchGithubAsync(string username)
    {
        try
        {
            var http = _httpFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(15);
            http.DefaultRequestHeaders.UserAgent.ParseAdd("SkillShareMap-SkillVerifier");
            http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");

            var url = $"https://api.github.com/users/{Uri.EscapeDataString(username)}/repos?per_page=100&sort=pushed";
            var resp = await http.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("GitHub fetch failed for {User}: {Status}", username, resp.StatusCode);
                return string.Empty;
            }

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return string.Empty;

            var sb = new StringBuilder();
            var count = 0;
            foreach (var repo in doc.RootElement.EnumerateArray())
            {
                if (repo.TryGetProperty("fork", out var f) && f.ValueKind == JsonValueKind.True)
                    continue;
                var name = GetStr(repo, "name");
                var lang = GetStr(repo, "language");
                var desc = GetStr(repo, "description");
                var stars = repo.TryGetProperty("stargazers_count", out var s) && s.ValueKind == JsonValueKind.Number ? s.GetInt32() : 0;
                sb.AppendLine($"- {name} [{(string.IsNullOrEmpty(lang) ? "n/a" : lang)}] stars:{stars} — {desc}");
                if (++count >= 40) break;
            }
            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GitHub fetch threw for {User}", username);
            return string.Empty;
        }
    }

    private static string GetStr(JsonElement el, string prop) =>
        el.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    // --- DeepSeek ---

    private static string SystemPrompt() => """
        You are a strict skill verifier for a campus skill-sharing platform. You are given a user's
        real artifacts (public GitHub repositories and/or pasted transcript text). Score their
        proficiency ONLY in these six categories, using the exact enum names:
        StudyHelp, TechHelp, CreativeDesign, PhotoVideo, WritingEditing, LanguageHelp.

        Rules:
        - Only score a category if the artifacts provide concrete evidence. Do NOT invent skills.
        - score is 0-100 and must reflect the strength of the evidence (more/deeper proof = higher).
        - Every skill MUST include an evidence array citing specific artifacts (repo names,
          languages, course names/grades). weight is 0-1 and the weights within a skill should
          roughly sum to 1.
        - Omit categories with no evidence rather than scoring them 0.

        Respond with STRICT JSON only, no prose, in exactly this shape:
        {
          "summary": "one short sentence about the strongest area",
          "skills": [
            {
              "category": "TechHelp",
              "score": 78,
              "evidence": [
                { "source": "GitHub", "detail": "142 commits in react-dashboard (TypeScript)", "weight": 0.6 }
              ]
            }
          ]
        }
        """;

    private async Task<string> CallDeepSeekAsync(string system, string user)
    {
        var key = _config["DeepSeek:ApiKey"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("DeepSeek API key is not configured (set DeepSeek:ApiKey).");

        var baseUrl = (_config["DeepSeek:BaseUrl"] ?? "https://api.deepseek.com").TrimEnd('/');
        var model = _config["DeepSeek:Model"] ?? "deepseek-chat";

        var http = _httpFactory.CreateClient();
        http.Timeout = TimeSpan.FromSeconds(60);

        var body = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = system },
                new { role = "user", content = user },
            },
            temperature = 0.2,
            response_format = new { type = "json_object" },
        };

        var req = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var resp = await http.SendAsync(req);
        var respJson = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogError("DeepSeek error {Status}: {Body}", resp.StatusCode, respJson);
            throw new InvalidOperationException($"DeepSeek request failed ({(int)resp.StatusCode}).");
        }

        using var doc = JsonDocument.Parse(respJson);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString() ?? "{}";
    }

    private ParsedScan ParseSkills(string content)
    {
        try
        {
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var parsed = JsonSerializer.Deserialize<ParsedScan>(content, opts);
            if (parsed?.Skills == null)
                return new ParsedScan();
            // Defensive: drop skills with no evidence.
            parsed.Skills = parsed.Skills.Where(s => s.Evidence is { Count: > 0 }).ToList();
            return parsed;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse DeepSeek skill JSON: {Content}", content);
            return new ParsedScan();
        }
    }

    private sealed class ParsedScan
    {
        public string? Summary { get; set; }
        public List<VerifiedSkill> Skills { get; set; } = new();
    }

    // --- XP / tier mapping (keeps the existing XP-driven RPG visuals consistent) ---

    // Maps a 0-100 verified score onto the platform's XP tier floors
    // (Skilled 50 / Advanced 200 / Expert 600 / Master 1500).
    private static int ScoreToXp(int score) => score switch
    {
        >= 90 => 1500 + (score - 90) * 30,
        >= 70 => 600 + (score - 70) * 45,
        >= 45 => 200 + (score - 45) * 16,
        >= 20 => 50 + (score - 20) * 6,
        _ => score * 2,
    };

    private static BadgeTier TierFromXp(int xp) => xp switch
    {
        >= 1500 => BadgeTier.Master,
        >= 600 => BadgeTier.Expert,
        >= 200 => BadgeTier.Advanced,
        >= 50 => BadgeTier.Skilled,
        _ => BadgeTier.Newbie,
    };
}
