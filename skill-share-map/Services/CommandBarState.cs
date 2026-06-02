using SkillShareMap.Models;

namespace SkillShareMap.Services;

/// <summary>
/// Circuit-scoped state for the global AI command bar (⌘K).
/// The command bar parses a natural-language line into a <see cref="MapCommand"/>
/// and dispatches it; pages (the map / Index) subscribe to <see cref="OnCommand"/>
/// and act on it. This replaces the old conversational companion paradigm:
/// "say one line → it executes", rather than a back-and-forth chat.
/// </summary>
public class CommandBarState
{
    /// <summary>Whether the command palette is currently open.</summary>
    public bool IsOpen { get; private set; }

    /// <summary>Optional text to pre-fill the input with when the palette opens.</summary>
    public string? SeedText { get; private set; }

    /// <summary>Fires when the palette is opened/closed (for the launcher + palette UI).</summary>
    public event Action? OnOpenChange;

    /// <summary>Fires when a parsed command should be executed by the map page.</summary>
    public event Func<MapCommand, Task>? OnCommand;

    public void Open(string? seed = null)
    {
        SeedText = seed;
        IsOpen = true;
        OnOpenChange?.Invoke();
    }

    public void Toggle()
    {
        IsOpen = !IsOpen;
        OnOpenChange?.Invoke();
    }

    public void Close()
    {
        IsOpen = false;
        OnOpenChange?.Invoke();
    }

    public string? ConsumeSeed()
    {
        var s = SeedText;
        SeedText = null;
        return s;
    }

    /// <summary>Dispatch a parsed command to whichever page is listening (the map).</summary>
    public async Task DispatchAsync(MapCommand cmd)
    {
        var handler = OnCommand;
        if (handler != null)
            await handler.Invoke(cmd);
    }
}

/// <summary>A parsed, one-shot instruction produced by the command bar.</summary>
public class MapCommand
{
    /// <summary>Human-readable echo of what the command resolved to (shown briefly).</summary>
    public string Echo { get; set; } = "";

    /// <summary>Raw text the user typed.</summary>
    public string Raw { get; set; } = "";

    public List<TaskCategory> Categories { get; set; } = new();
    public bool UrgentOnly { get; set; }

    /// <summary>null = leave current view; true = remote list; false = onsite map.</summary>
    public bool? Remote { get; set; }

    public bool SortByBudgetDesc { get; set; }

    /// <summary>Ask the passive AI layer to highlight personalised picks.</summary>
    public bool RecommendForMe { get; set; }

    public bool HasAnyEffect =>
        Categories.Count > 0 || UrgentOnly || Remote.HasValue || SortByBudgetDesc || RecommendForMe;
}
