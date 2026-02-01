using System.IO;
using System.Net.Http;
using System.Text.Json;
using OpenTweak.Models;

namespace OpenTweak.Services;

/// <summary>
/// Service for fetching game data from PCGamingWiki using the Cargo API.
/// Uses structured queries instead of HTML scraping for reliability.
/// </summary>
public class PCGWService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://www.pcgamingwiki.com/w/api.php";

    public PCGWService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "OpenTweak/1.0 (https://github.com/your-repo)");
    }

    /// <summary>
    /// Fetches all available tweak recipes for a game from PCGW.
    /// </summary>
    public async Task<List<TweakRecipe>> GetTweaksForGameAsync(string gameTitle, Guid gameId)
    {
        var recipes = new List<TweakRecipe>();

        // Fetch from different settings tables
        var videoSettings = await QueryCargoTableAsync("Video_settings", gameTitle);
        var audioSettings = await QueryCargoTableAsync("Audio_settings", gameTitle);
        var inputSettings = await QueryCargoTableAsync("Input_settings", gameTitle);

        recipes.AddRange(ParseVideoSettings(videoSettings, gameId, gameTitle));
        recipes.AddRange(ParseAudioSettings(audioSettings, gameId, gameTitle));
        recipes.AddRange(ParseInputSettings(inputSettings, gameId, gameTitle));

        return recipes;
    }

    /// <summary>
    /// Queries a PCGW Cargo table for a specific game.
    /// </summary>
    private async Task<JsonDocument?> QueryCargoTableAsync(string table, string gameTitle)
    {
        try
        {
            // Build Cargo API query
            var fields = table switch
            {
                "Video_settings" => "Page,Widescreen,Multi-monitor,Ultra-widescreen,4K_Ultra_HD,HDR,Windowed,Borderless_fullscreen,Anisotropic_filtering,Anti-aliasing,V-sync,60_FPS,120_FPS,Uncapped_FPS",
                "Audio_settings" => "Page,Separate_volume_controls,Surround_sound,Subtitles,Closed_captions,Mute_on_focus_lost",
                "Input_settings" => "Page,Remappable_controls,Controller_support,Full_controller_support,Controller_types,Input_prompt_override,Haptic_feedback,Touchscreen,Keyboard_and_mouse_on_consoles",
                _ => "Page"
            };

            var url = $"{BaseUrl}?action=cargoquery" +
                      $"&tables={Uri.EscapeDataString(table)}" +
                      $"&fields={Uri.EscapeDataString(fields)}" +
                      $"&where={Uri.EscapeDataString($"Page='{EscapeWikiTitle(gameTitle)}'")}"+
                      "&format=json";

            var response = await _httpClient.GetStringAsync(url);
            return JsonDocument.Parse(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PCGW query error for {table}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Searches PCGW for games matching a query.
    /// </summary>
    public async Task<List<string>> SearchGamesAsync(string query)
    {
        var results = new List<string>();

        try
        {
            var url = $"{BaseUrl}?action=opensearch&search={Uri.EscapeDataString(query)}&limit=10&namespace=0&format=json";
            var response = await _httpClient.GetStringAsync(url);

            using var doc = JsonDocument.Parse(response);
            var root = doc.RootElement;

            if (root.GetArrayLength() > 1)
            {
                var titles = root[1];
                foreach (var title in titles.EnumerateArray())
                {
                    var titleStr = title.GetString();
                    if (!string.IsNullOrEmpty(titleStr))
                        results.Add(titleStr);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PCGW search error: {ex.Message}");
        }

        return results;
    }

    #region Settings Parsers

    private List<TweakRecipe> ParseVideoSettings(JsonDocument? doc, Guid gameId, string gameTitle)
    {
        var recipes = new List<TweakRecipe>();
        if (doc == null) return recipes;

        try
        {
            var cargoQuery = doc.RootElement.GetProperty("cargoquery");
            foreach (var item in cargoQuery.EnumerateArray())
            {
                var title = item.GetProperty("title");

                // Check for common video fixes
                AddVideoTweakIfNeeded(recipes, gameId, gameTitle, title, "Uncapped FPS", "60_FPS", "120_FPS", "Uncapped FPS");
                AddVideoTweakIfNeeded(recipes, gameId, gameTitle, title, "Ultrawide Support", "Ultra-widescreen");
                AddVideoTweakIfNeeded(recipes, gameId, gameTitle, title, "Borderless Fullscreen", "Borderless fullscreen");
            }
        }
        catch { }

        return recipes;
    }

    private List<TweakRecipe> ParseAudioSettings(JsonDocument? doc, Guid gameId, string gameTitle)
    {
        var recipes = new List<TweakRecipe>();
        if (doc == null) return recipes;

        try
        {
            var cargoQuery = doc.RootElement.GetProperty("cargoquery");
            foreach (var item in cargoQuery.EnumerateArray())
            {
                var title = item.GetProperty("title");

                // Check for common audio fixes
                AddAudioTweakIfNeeded(recipes, gameId, gameTitle, title, "Surround Sound", "Surround sound");
            }
        }
        catch { }

        return recipes;
    }

    private List<TweakRecipe> ParseInputSettings(JsonDocument? doc, Guid gameId, string gameTitle)
    {
        var recipes = new List<TweakRecipe>();
        if (doc == null) return recipes;

        try
        {
            var cargoQuery = doc.RootElement.GetProperty("cargoquery");
            foreach (var item in cargoQuery.EnumerateArray())
            {
                var title = item.GetProperty("title");

                // Check for controller support status
                if (TryGetProperty(title, "Controller support", out var controllerSupport))
                {
                    if (controllerSupport == "false" || controllerSupport == "limited")
                    {
                        recipes.Add(new TweakRecipe
                        {
                            GameId = gameId,
                            Category = TweakCategory.Input,
                            Description = "Controller support may require additional configuration",
                            RiskLevel = "Low",
                            SourceUrl = $"https://www.pcgamingwiki.com/wiki/{Uri.EscapeDataString(gameTitle)}"
                        });
                    }
                }
            }
        }
        catch { }

        return recipes;
    }

    #endregion

    #region Helper Methods

    private void AddVideoTweakIfNeeded(List<TweakRecipe> recipes, Guid gameId, string gameTitle,
        JsonElement title, string description, params string[] properties)
    {
        foreach (var prop in properties)
        {
            if (TryGetProperty(title, prop, out var value))
            {
                if (value == "hackable" || value == "limited")
                {
                    recipes.Add(new TweakRecipe
                    {
                        GameId = gameId,
                        Category = TweakCategory.Video,
                        Description = $"{description}: Requires configuration tweak",
                        RiskLevel = "Medium",
                        SourceUrl = $"https://www.pcgamingwiki.com/wiki/{Uri.EscapeDataString(gameTitle)}"
                    });
                    return;
                }
            }
        }
    }

    private void AddAudioTweakIfNeeded(List<TweakRecipe> recipes, Guid gameId, string gameTitle,
        JsonElement title, string description, params string[] properties)
    {
        foreach (var prop in properties)
        {
            if (TryGetProperty(title, prop, out var value))
            {
                if (value == "hackable" || value == "limited")
                {
                    recipes.Add(new TweakRecipe
                    {
                        GameId = gameId,
                        Category = TweakCategory.Audio,
                        Description = $"{description}: Requires configuration tweak",
                        RiskLevel = "Low",
                        SourceUrl = $"https://www.pcgamingwiki.com/wiki/{Uri.EscapeDataString(gameTitle)}"
                    });
                    return;
                }
            }
        }
    }

    private bool TryGetProperty(JsonElement element, string propertyName, out string value)
    {
        value = string.Empty;

        // Normalize property name for JSON (spaces to underscores, etc.)
        var normalizedName = propertyName.Replace(" ", "_").Replace("-", "_");

        if (element.TryGetProperty(normalizedName, out var prop))
        {
            value = prop.GetString() ?? string.Empty;
            return !string.IsNullOrEmpty(value);
        }

        return false;
    }

    private string EscapeWikiTitle(string title)
    {
        // Escape single quotes for SQL-like where clause
        return title.Replace("'", "''");
    }

    #endregion
}
