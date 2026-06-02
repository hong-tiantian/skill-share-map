using SkillShareMap.Models.DTOs;

namespace SkillShareMap.Services;

public interface ISkillVerificationService
{
    /// <summary>
    /// Score a user's skills from real artifacts (GitHub repos and/or pasted transcript text)
    /// using DeepSeek, returning per-category scores backed by an evidence trail, and persisting
    /// the result into UserSkillProgress so the RPG character card / skill tree reflect it.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the DeepSeek API key is not configured.</exception>
    Task<SkillScanResult> ScanAsync(int userId, string? githubUsername, string? transcriptText);
}
