using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using OpenTweak.Models;

namespace OpenTweak.Services;

/// <summary>
/// Service for interacting with PCGamingWiki API and data.
/// This is the "Twiki-Killer" - extracting actual fix instructions from wiki pages.
/// </summary>
public class PCGWService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "https://www.pcgamingwiki.com/w/api.php";

    public PCGWService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "OpenTweak/1.0 (Automated Game Tweaks)");
    }

    #region Public API Methods

    /// <summary>
    /// Searches for a game on PCGamingWiki and returns basic info.
    /// </summary>
    public async Task<PCGWGameInfo?> SearchGameAsync(string gameTitle)
    {
        try
        {
            // Search for the game
            var searchUrl = $"{_baseUrl}?action=query&list=search&srsearch={Uri.EscapeDataString(gameTitle)}&format=json&srlimit=5";
            var searchResponse = await _httpClient.GetStringAsync(searchUrl);
            var searchData = JsonSerializer.Deserialize<JsonElement>(searchResponse);

            if (!searchData.TryGetProperty("query", out var query) ||
                !query.TryGetProperty("search", out var searchResults))
            {
                return null;
            }

            // Find the best match
            foreach (var result in searchResults.EnumerateArray())
            {
                var title = result.GetProperty("title").GetString();
                if (string.IsNullOrEmpty(title)) continue;

                // Check if it's a game page (not a category or file)
                if (!title.StartsWith("Category:") && !title.StartsWith("File:"))
                {
                    // Get full page content
                    var pageInfo = await GetPageContentAsync(title);
                    if (pageInfo != null)
                    {
                        return pageInfo;
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error searching PCGW: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets detailed information about a game including all fix instructions.
    /// </summary>
    public async Task<PCGWGameInfo?> GetPageContentAsync(string pageTitle)
    {
        try
        {
            // Get the wiki page content
            var contentUrl = $"{_baseUrl}?action=parse&page={Uri.EscapeDataString(pageTitle)}&prop=wikitext&format=json";
            var contentResponse = await _httpClient.GetStringAsync(contentUrl);
            var contentData = JsonSerializer.Deserialize<JsonElement>(contentResponse);

            if (!contentData.TryGetProperty("parse", out var parse) ||
                !parse.TryGetProperty("wikitext", out var wikitext))
            {
                return null;
            }

            var wikiText = wikitext.GetString();
            if (string.IsNullOrEmpty(wikiText))
            {
                return null;
            }

            // Extract game information
            var gameInfo = new PCGWGameInfo
            {
                Title = pageTitle,
                WikiUrl = $"https://www.pcgamingwiki.com/wiki/{Uri.EscapeDataString(pageTitle.Replace(" ", "_"))}",
                ConfigFiles = ExtractConfigFilePaths(wikiText),
                SaveGameLocations = ExtractSaveGamePaths(wikiText),
                AvailableTweaks = new List<TweakRecipe>()
            };

            // Extract actual fix instructions from the wiki text
            var recipes = ExtractRecipesFromWikiText(wikiText, Guid.NewGuid(), gameInfo.Title);
            gameInfo.AvailableTweaks.AddRange(recipes);

            return gameInfo;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error getting PCGW page content: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Gets available tweaks for a specific game.
    /// </summary>
    public async Task<List<TweakRecipe>> GetAvailableTweaksAsync(string gameTitle, Guid gameId)
    {
        var gameInfo = await SearchGameAsync(gameTitle);
        if (gameInfo?.AvailableTweaks == null)
        {
            return new List<TweakRecipe>();
        }

        // Update the GameId in all recipes
        foreach (var tweak in gameInfo.AvailableTweaks)
        {
            tweak.GameId = gameId;
        }

        return gameInfo.AvailableTweaks;
    }

    #endregion

    #region Data Extraction Methods

    /// <summary>
    /// Extracts configuration file paths from wiki text.
    /// </summary>
    private List<string> ExtractConfigFilePaths(string wikiText)
    {
        var paths = new List<string>();

        try
        {
            // Look for configuration file paths in the wiki text
            // Common patterns in PCGamingWiki
            var configPatterns = new[]
            {
                @"Configuration file\(s\) location.*?({{.*?}})",
                @"{{Game data/config\\|([^}]+)}}",
                @"{{p\\|game}}[/\\]([^\s\n]+\.(?:ini|cfg|conf|xml|json|yaml|yml))",
                @"{{p\\|hkcu}}[/\\]([^\s\n]+)",
                @"{{p\\|hklm}}[/\\]([^\s\n]+)",
                @"{{p\\|appdata}}[/\\]([^\s\n]+)",
                @"{{p\\|userprofile\\documents}}[/\\]([^\s\n]+)"
            };

            foreach (var pattern in configPatterns)
            {
                var matches = Regex.Matches(wikiText, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        var path = match.Groups[1].Value.Trim();
                        if (!string.IsNullOrEmpty(path) && !paths.Contains(path))
                        {
                            paths.Add(path);
                        }
                    }
                }
            }

            // Also look for explicit file paths mentioned in the text
            var filePathPattern = @"([A-Za-z]:\\[^\n\r]*\.(?:ini|cfg|conf|xml|json|yaml|yml))";
            var fileMatches = Regex.Matches(wikiText, filePathPattern, RegexOptions.IgnoreCase);
            foreach (Match match in fileMatches)
            {
                var path = match.Value;
                if (!paths.Contains(path))
                {
                    paths.Add(path);
                }
            }
        }
        catch { }

        return paths;
    }

    /// <summary>
    /// Extracts save game locations from wiki text.
    /// </summary>
    private List<string> ExtractSaveGamePaths(string wikiText)
    {
        var paths = new List<string>();

        try
        {
            // Look for save game location patterns
            var savePatterns = new[]
            {
                @"Save game cloud syncing.*?({{.*?}})",
                @"{{Game data/saves\\|([^}]+)}}",
                @"{{p\\|appdata}}[/\\]([^\s\n]+)",
                @"{{p\\|userprofile\\documents}}[/\\]([^\s\n]+)",
                @"{{p\\|game}}[/\\]([^\s\n]+save[^\s\n]*)"
            };

            foreach (var pattern in savePatterns)
            {
                var matches = Regex.Matches(wikiText, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
                foreach (Match match in matches)
                {
                    if (match.Groups.Count > 1)
                    {
                        var path = match.Groups[1].Value.Trim();
                        if (!string.IsNullOrEmpty(path) && !paths.Contains(path))
                        {
                            paths.Add(path);
                        }
                    }
                }
            }
        }
        catch { }

        return paths;
    }

    #endregion

    #region Wiki Text Recipe Extraction

    /// <summary>
    /// Extracts specific fix instructions from wiki page content.
    /// This is the "Twiki-Killer" feature - parsing actual fix instructions.
    /// </summary>
    private List<TweakRecipe> ExtractRecipesFromWikiText(string wikiText, Guid gameId, string gameTitle)
    {
        var recipes = new List<TweakRecipe>();

        // Look for fix instructions in various formats
        recipes.AddRange(ExtractIniFixes(wikiText, gameId, gameTitle));
        recipes.AddRange(ExtractRegistryFixes(wikiText, gameId, gameTitle));
        recipes.AddRange(ExtractCommandLineFixes(wikiText, gameId, gameTitle));

        return recipes;
    }

    /// <summary>
    /// Extracts INI/CFG file modifications from wiki text.
    /// </summary>
    private List<TweakRecipe> ExtractIniFixes(string wikiText, Guid gameId, string gameTitle)
    {
        var recipes = new List<TweakRecipe>();

        // Pattern: Look for code blocks or preformatted text with INI-style content
        var iniPattern = @"(?:Edit|Modify|Change|Set)\b\s*(?:the\s*)?(?:following\s*)?(?:in\s*)?(?:the\s*)?(?:file\s*)?`?([^`\n]+\.(?:ini|cfg|conf))`?[^\n]*\n*(?:.*?)\[([^\]]+)\]?\s*([\w_]+)\s*=\s*([^\n]+)";

        var matches = Regex.Matches(wikiText, iniPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            var filePath = match.Groups[1].Value.Trim();
            var section = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;
            var key = match.Groups[3].Value.Trim();
            var value = match.Groups[4].Value.Trim();

            recipes.Add(new TweakRecipe
            {
                GameId = gameId,
                Category = TweakCategory.Other,
                Description = $"Set {key}={value} in {Path.GetFileName(filePath)}",
                TargetType = TweakTargetType.IniFile,
                FilePath = filePath,
                Section = section,
                Key = key,
                Value = value,
                RiskLevel = "Medium",
                SourceUrl = $"https://www.pcgamingwiki.com/wiki/{Uri.EscapeDataString(gameTitle)}"
            });
        }

        // Alternative pattern: Look for preformatted code blocks
        var codeBlockPattern = @"<pre>(.*?)</pre>";
        var codeMatches = Regex.Matches(wikiText, codeBlockPattern, RegexOptions.Singleline);

        foreach (Match match in codeMatches)
        {
            var content = match.Groups[1].Value;

            // Check if it looks like an INI file
            if (content.Contains("=") && (content.Contains("[") || content.Contains("]")))
            {
                var lines = content.Split('\n');
                string? currentSection = null;

                foreach (var line in lines)
                {
                    var trimmedLine = line.Trim();

                    // Check for section header
                    var sectionMatch = Regex.Match(trimmedLine, @"^\[([^\]]+)\]$");
                    if (sectionMatch.Success)
                    {
                        currentSection = sectionMatch.Groups[1].Value;
                        continue;
                    }

                    // Check for key=value pair
                    var kvMatch = Regex.Match(trimmedLine, @"^([\w_]+)\s*=\s*(.+)$");
                    if (kvMatch.Success && currentSection != null)
                    {
                        var key = kvMatch.Groups[1].Value.Trim();
                        var value = kvMatch.Groups[2].Value.Trim();

                        recipes.Add(new TweakRecipe
                        {
                            GameId = gameId,
                            Category = TweakCategory.Other,
                            Description = $"Set {key}={value} in config file",
                            TargetType = TweakTargetType.IniFile,
                            Section = currentSection,
                            Key = key,
                            Value = value,
                            RiskLevel = "Medium",
                            SourceUrl = $"https://www.pcgamingwiki.com/wiki/{Uri.EscapeDataString(gameTitle)}"
                        });
                    }
                }
            }
        }

        return recipes;
    }

    /// <summary>
    /// Extracts registry modifications from wiki text.
    /// </summary>
    private List<TweakRecipe> ExtractRegistryFixes(string wikiText, Guid gameId, string gameTitle)
    {
        var recipes = new List<TweakRecipe>();

        // Pattern for registry edits
        var regPattern = @"(?:Registry|regedit).*?(?:path|key)[:=]*\s*[``""']?(HKEY_[^``""'\n]+)[``""']?[^\n]*(?:value|name)[:=]*\s*[``""']?([\w_]+)[``""']?[^\n]*(?:data|value)[:=]*\s*[``""']?([^``""'\n]+)[``""']?";

        var matches = Regex.Matches(wikiText, regPattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

        foreach (Match match in matches)
        {
            recipes.Add(new TweakRecipe
            {
                GameId = gameId,
                Category = TweakCategory.Other,
                Description = $"Registry modification: {match.Groups[2].Value}",
                TargetType = TweakTargetType.Registry,
                FilePath = match.Groups[1].Value,
                Key = match.Groups[2].Value,
                Value = match.Groups[3].Value.Trim(),
                RiskLevel = "High",
                SourceUrl = $"https://www.pcgamingwiki.com/wiki/{Uri.EscapeDataString(gameTitle)}"
            });
        }

        return recipes;
    }

    /// <summary>
    /// Extracts command-line arguments and launch options.
    /// </summary>
    private List<TweakRecipe> ExtractCommandLineFixes(string wikiText, Guid gameId, string gameTitle)
    {
        var recipes = new List<TweakRecipe>();

        // Pattern for command-line arguments
        var cmdPattern = @"(?:command.?line|launch option|argument).*?(?:add|use|set)\s*[`""']?(-[\w-]+)[`""']?";

        var matches = Regex.Matches(wikiText, cmdPattern, RegexOptions.IgnoreCase);

        foreach (Match match in matches)
        {
            recipes.Add(new TweakRecipe
            {
                GameId = gameId,
                Category = TweakCategory.Other,
                Description = $"Launch option: {match.Groups[1].Value}",
                TargetType = TweakTargetType.Other,
                Key = "LaunchOptions",
                Value = match.Groups[1].Value,
                RiskLevel = "Low",
                SourceUrl = $"https://www.pcgamingwiki.com/wiki/{Uri.EscapeDataString(gameTitle)}"
            });
        }

        return recipes;
    }

    #endregion
}

/// <summary>
/// Represents game information from PCGamingWiki.
/// </summary>
public class PCGWGameInfo
{
    public string Title { get; set; } = string.Empty;
    public string WikiUrl { get; set; } = string.Empty;
    public List<string> ConfigFiles { get; set; } = new();
    public List<string> SaveGameLocations { get; set; } = new();
    public List<TweakRecipe> AvailableTweaks { get; set; } = new();
}
