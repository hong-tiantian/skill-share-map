using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SkillShareMap.Data;
using SkillShareMap.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SkillShareMap.Services;

public class AIAssistantService : IAIAssistantService
{
    private readonly ApplicationDbContext _context;
    private readonly ITaskService _taskService;
    private readonly IAuthService _authService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string? _apiKey;
    private readonly string _model;
    private readonly string _baseUrl;

    public AIAssistantService(
        ApplicationDbContext context,
        ITaskService taskService,
        IAuthService authService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _context = context;
        _taskService = taskService;
        _authService = authService;
        _httpClientFactory = httpClientFactory;
        _apiKey = configuration["DeepSeek:ApiKey"];
        _model = configuration["DeepSeek:Model"] ?? "deepseek-chat";
        _baseUrl = configuration["DeepSeek:BaseUrl"] ?? "https://api.deepseek.com";
    }

    public async Task<string> SendMessageAsync(int userId, string message)
    {
        var conversation = await GetOrCreateConversationAsync(userId);

        var userMessage = new AIMessage
        {
            ConversationId = conversation.Id,
            Content = message,
            Role = AIMessageRole.User,
            SentAt = DateTime.UtcNow
        };
        _context.AIMessages.Add(userMessage);

        var response = await GenerateResponseAsync(userId, message, conversation);

        var aiMessage = new AIMessage
        {
            ConversationId = conversation.Id,
            Content = response,
            Role = AIMessageRole.Assistant,
            SentAt = DateTime.UtcNow
        };
        _context.AIMessages.Add(aiMessage);

        conversation.LastMessageAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return response;
    }

    public async Task<AIConversation?> GetConversationAsync(int userId)
    {
        return await _context.AIConversations
            .Include(c => c.Messages.OrderBy(m => m.SentAt))
            .FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<List<SkillTask>> GetRecommendedTasksAsync(int userId)
    {
        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null) return new List<SkillTask>();

        var tasks = await _taskService.GetTasksAsync(status: SkillTaskStatus.Open);
        var userProgress = await _context.UserSkillProgress
            .Where(p => p.UserId == userId)
            .ToListAsync();

        return tasks
            .Where(t => t.Status == SkillTaskStatus.Open)
            .Where(t => userProgress.Any(p => p.Category == t.Category))
            .OrderByDescending(t => userProgress.FirstOrDefault(p => p.Category == t.Category)?.TotalXp ?? 0)
            .Take(10)
            .ToList();
    }

    public async Task<List<string>> GetSkillSuggestionsAsync(int userId)
    {
        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null) return new List<string>();

        var userProgress = await _context.UserSkillProgress
            .Where(p => p.UserId == userId)
            .ToListAsync();

        return Enum.GetValues<TaskCategory>()
            .Where(c => !userProgress.Any(p => p.Category == c))
            .Select(c => GetCategoryDescription(c))
            .ToList();
    }

    public async Task<bool> ClearConversationAsync(int userId)
    {
        var conversation = await _context.AIConversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (conversation == null) return false;

        _context.AIMessages.RemoveRange(conversation.Messages);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<AIConversation> GetOrCreateConversationAsync(int userId)
    {
        var conversation = await _context.AIConversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (conversation == null)
        {
            conversation = new AIConversation
            {
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                LastMessageAt = DateTime.UtcNow
            };
            _context.AIConversations.Add(conversation);
            await _context.SaveChangesAsync();
        }

        return conversation;
    }

    private async Task<string> GenerateResponseAsync(int userId, string message, AIConversation conversation)
    {
        var user = await _authService.GetUserByIdAsync(userId);
        if (user == null) return "I'm sorry, I couldn't find your user profile.";

        if (!string.IsNullOrWhiteSpace(_apiKey))
            return await CallDeepSeekAsync(user, userId, message, conversation);

        return await FallbackKeywordResponse(user, userId, message);
    }

    private async Task<string> CallDeepSeekAsync(User user, int userId, string message, AIConversation conversation)
    {
        try
        {
            var userContext = await BuildUserContext(user, userId);
            var systemPrompt = BuildSystemPrompt(user, userContext);

            var chatMessages = new List<object>
            {
                new { role = "system", content = systemPrompt }
            };

            // Add recent conversation history (last 10 messages for context)
            var recentMessages = conversation.Messages
                .OrderByDescending(m => m.SentAt)
                .Take(10)
                .Reverse()
                .ToList();

            foreach (var msg in recentMessages)
            {
                chatMessages.Add(new
                {
                    role = msg.Role == AIMessageRole.User ? "user" : "assistant",
                    content = msg.Content
                });
            }

            chatMessages.Add(new { role = "user", content = message });

            var requestBody = new
            {
                model = _model,
                messages = chatMessages,
                max_tokens = 800,
                temperature = 0.8
            };

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var json = JsonSerializer.Serialize(requestBody);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{_baseUrl}/v1/chat/completions", httpContent);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return await FallbackKeywordResponse(await _authService.GetUserByIdAsync(userId) ?? user, userId, message);
            }

            var result = JsonSerializer.Deserialize<DeepSeekResponse>(responseText);
            var content = result?.Choices?.FirstOrDefault()?.Message?.Content;

            return !string.IsNullOrWhiteSpace(content)
                ? content
                : await FallbackKeywordResponse(user, userId, message);
        }
        catch
        {
            return await FallbackKeywordResponse(user, userId, message);
        }
    }

    private string BuildSystemPrompt(User user, string userContext)
    {
        return $"""
You are **Archon**, the Guild Master of the Skill Share Guild — a wise, encouraging RPG quest advisor NPC.

PERSONALITY:
- Speak like a knowledgeable guild master: warm, motivating, with light RPG flavor
- Address the player by name ("{user.Username}")
- Use gaming metaphors (quests, XP, skill trees, leveling up) but keep it natural
- Be concise — 2-4 short paragraphs max
- Give actionable, specific advice based on the player's actual data

PLAYER DATA:
{userContext}

CAPABILITIES:
- Recommend quests (tasks) based on the player's skill levels
- Advise on skill development and which categories to explore
- Report on wallet balance and earnings
- Summarize profile stats and progress
- Motivate and celebrate achievements

RULES:
- Never invent data — only reference what's in PLAYER DATA
- Keep responses under 200 words
- Use plain text, no markdown formatting
- If asked something outside your scope, gently redirect to quest/skill topics
""";
    }

    private async Task<string> BuildUserContext(User user, int userId)
    {
        var progress = await _context.UserSkillProgress
            .Where(p => p.UserId == userId)
            .ToListAsync();

        var completedTasks = await _context.SkillTasks
            .Where(t => t.AssignedToId == userId && t.Status == SkillTaskStatus.Completed)
            .CountAsync();

        var openTasks = await _context.SkillTasks
            .Where(t => t.Status == SkillTaskStatus.Open)
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .ToListAsync();

        var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);

        var totalXp = progress.Sum(p => p.TotalXp);
        var tier = totalXp >= 1500 ? "Master" : totalXp >= 600 ? "Expert" : totalXp >= 200 ? "Advanced" : totalXp >= 50 ? "Skilled" : "Newbie";

        var sb = new StringBuilder();
        sb.AppendLine($"Username: {user.Username}");
        sb.AppendLine($"Role: {user.Role}");
        sb.AppendLine($"Reputation: {user.ReputationLevel}/5 stars");
        sb.AppendLine($"Total XP: {totalXp} (Tier: {tier})");
        sb.AppendLine($"Completed Quests: {completedTasks}");
        var balanceStr = wallet != null ? wallet.Balance.ToString("F2") : "0.00";
        sb.AppendLine($"Wallet Balance: ${balanceStr}");

        if (progress.Any())
        {
            sb.AppendLine("\nSkill Progress:");
            foreach (var p in progress.OrderByDescending(p => p.TotalXp))
            {
                var catTier = p.TotalXp >= 1500 ? "Master" : p.TotalXp >= 600 ? "Expert" : p.TotalXp >= 200 ? "Advanced" : p.TotalXp >= 50 ? "Skilled" : "Newbie";
                sb.AppendLine($"  - {GetCategoryName(p.Category)}: {p.TotalXp} XP ({catTier})");
            }
        }
        else
        {
            sb.AppendLine("\nSkill Progress: None yet (new adventurer)");
        }

        if (openTasks.Any())
        {
            sb.AppendLine("\nAvailable Quests:");
            foreach (var t in openTasks)
                sb.AppendLine($"  - \"{t.Title}\" ({GetCategoryName(t.Category)}) — ${t.Budget}");
        }

        return sb.ToString();
    }

    private async Task<string> FallbackKeywordResponse(User user, int userId, string message)
    {
        var lowerMessage = message.ToLower();

        if (lowerMessage.Contains("recommend") || lowerMessage.Contains("task") || lowerMessage.Contains("find") || lowerMessage.Contains("quest"))
        {
            var tasks = await GetRecommendedTasksAsync(userId);
            if (!tasks.Any())
                return $"Hmm, {user.Username}, the quest board is quiet for your specialties right now. Check back soon — or explore a new skill branch to unlock more opportunities!";

            var sb = new StringBuilder();
            sb.AppendLine($"Ah, {user.Username}! I've scoured the quest board and found {tasks.Count} quests matching your talents:\n");
            foreach (var task in tasks.Take(5))
            {
                sb.AppendLine($"  ⚔ {task.Title} — ${task.Budget}");
                sb.AppendLine($"    Category: {GetCategoryName(task.Category)}");
            }
            sb.AppendLine("\nShall I tell you more about any of these, adventurer?");
            return sb.ToString();
        }

        if (lowerMessage.Contains("skill") || lowerMessage.Contains("learn") || lowerMessage.Contains("improve") || lowerMessage.Contains("train"))
        {
            var suggestions = await GetSkillSuggestionsAsync(userId);
            if (!suggestions.Any())
                return $"Impressive, {user.Username}! You've explored every skill branch. A true polymath! Focus on mastering your existing disciplines to reach Expert and Master tiers.";

            var sb = new StringBuilder();
            sb.AppendLine($"Wise of you to seek growth, {user.Username}! These unexplored skill branches await:\n");
            foreach (var suggestion in suggestions.Take(5))
                sb.AppendLine($"  ✦ {suggestion}");
            sb.AppendLine("\nAccept quests in these categories to begin your training!");
            return sb.ToString();
        }

        if (lowerMessage.Contains("wallet") || lowerMessage.Contains("balance") || lowerMessage.Contains("money") || lowerMessage.Contains("gold"))
        {
            var wallet = await _context.Wallets.FirstOrDefaultAsync(w => w.UserId == userId);
            if (wallet == null)
                return $"Strange... I cannot locate your treasury, {user.Username}. The guild treasurer must be on break!";
            return $"Your treasury holds ${wallet.Balance:F2}, {user.Username}. Complete more quests to fill your coffers!";
        }

        if (lowerMessage.Contains("profile") || lowerMessage.Contains("stats") || lowerMessage.Contains("progress") || lowerMessage.Contains("level"))
        {
            var progress = await _context.UserSkillProgress.Where(p => p.UserId == userId).ToListAsync();
            var completedTasks = await _context.SkillTasks.Where(t => t.AssignedToId == userId && t.Status == SkillTaskStatus.Completed).CountAsync();
            var totalXp = progress.Sum(p => p.TotalXp);
            var tier = totalXp >= 1500 ? "Master" : totalXp >= 600 ? "Expert" : totalXp >= 200 ? "Advanced" : totalXp >= 50 ? "Skilled" : "Newbie";

            var sb = new StringBuilder();
            sb.AppendLine($"Here stands {user.Username}, a {tier}-rank adventurer!\n");
            sb.AppendLine($"  ⚡ Total XP: {totalXp}");
            sb.AppendLine($"  ⭐ Reputation: {user.ReputationLevel}/5 stars");
            sb.AppendLine($"  ✅ Quests Completed: {completedTasks}");
            sb.AppendLine($"  📊 Skill Branches: {progress.Count}");

            if (progress.Any())
            {
                sb.AppendLine("\nTop skills:");
                foreach (var p in progress.OrderByDescending(p => p.TotalXp).Take(3))
                    sb.AppendLine($"  • {GetCategoryName(p.Category)}: {p.TotalXp} XP");
            }
            return sb.ToString();
        }

        if (lowerMessage.Contains("help") || lowerMessage.Contains("hello") || lowerMessage.Contains("hi") || string.IsNullOrWhiteSpace(lowerMessage))
        {
            return $"Greetings, {user.Username}! I am Archon, your Guild Master.\n\n" +
                   "I can assist you with:\n" +
                   "  ⚔ Finding recommended quests\n" +
                   "  📖 Suggesting skill branches to explore\n" +
                   "  💰 Checking your treasury balance\n" +
                   "  📊 Reviewing your adventurer stats\n\n" +
                   "What would you like to know, adventurer?";
        }

        var defaults = new[]
        {
            $"An intriguing question, {user.Username}! I specialize in quest recommendations and skill guidance. Try asking me to 'recommend quests' or 'check my stats'!",
            $"The scrolls don't cover that topic, {user.Username}. But I can help you find quests, develop skills, or check your treasury. What interests you?",
            $"Hmm, that's beyond my guild duties, {user.Username}. I'm best at quest guidance, skill advice, and tracking your progress. What shall we explore?"
        };
        return defaults[new Random().Next(defaults.Length)];
    }

    private string GetCategoryName(TaskCategory category) => category switch
    {
        TaskCategory.StudyHelp => "Study Help",
        TaskCategory.TechHelp => "Tech Help",
        TaskCategory.CreativeDesign => "Creative Design",
        TaskCategory.PhotoVideo => "Photo & Video",
        TaskCategory.WritingEditing => "Writing & Editing",
        TaskCategory.LanguageHelp => "Language Help",
        _ => category.ToString()
    };

    private string GetCategoryDescription(TaskCategory category) => category switch
    {
        TaskCategory.StudyHelp => "Study Help — Tutoring, homework, exam prep",
        TaskCategory.TechHelp => "Tech Help — Programming, IT support, troubleshooting",
        TaskCategory.CreativeDesign => "Creative Design — Graphic design, UI/UX, art",
        TaskCategory.PhotoVideo => "Photo & Video — Photography, videography, editing",
        TaskCategory.WritingEditing => "Writing & Editing — Content writing, proofreading",
        TaskCategory.LanguageHelp => "Language Help — Translation, language tutoring",
        _ => category.ToString()
    };
}

internal class DeepSeekResponse
{
    [JsonPropertyName("choices")]
    public List<DeepSeekChoice>? Choices { get; set; }
}

internal class DeepSeekChoice
{
    [JsonPropertyName("message")]
    public DeepSeekMessage? Message { get; set; }
}

internal class DeepSeekMessage
{
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}
