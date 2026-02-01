# Feature: PCGWIntegration

Links:
Architecture: [`docs/Architecture/Overview.md`](../Architecture/Overview.md)
Code: [`OpenTweak/Services/PCGWService.cs`](../../OpenTweak/Services/PCGWService.cs)
Models: [`OpenTweak/Models/TweakRecipe.cs`](../../OpenTweak/Models/TweakRecipe.cs)
Tests: [`OpenTweak.Tests/Services/PCGWServiceTests.cs`](../../OpenTweak.Tests/Services/PCGWServiceTests.cs)

---

## Summary

Integrates with PCGamingWiki's Cargo API to fetch game information and parse wiki text into deterministic tweak recipes. Extracts configuration file paths, save game locations, and fix instructions from wiki pages.

---

## User Story

As a user, when I select a game, I want OpenTweak to automatically fetch available tweaks from PCGamingWiki. The app should show me what configuration changes are available (like disabling motion blur or fixing frame rate caps) and let me apply them safely.

---

## Flowchart

```mermaid
flowchart TD
    Start([User selects game]) --> Search[SearchGameAsync]
    Search --> API1[Cargo API: search query]
    API1 --> Match{Best match?}
    Match -->|Yes| GetPage[GetPageContentAsync]
    Match -->|No| NoResults[Show no results]

    GetPage --> API2[Cargo API: parse wikitext]
    API2 --> Extract[Extract Data]

    Extract --> ConfigFiles[ExtractConfigFilePaths]
    Extract --> SaveGames[ExtractSaveGamePaths]
    Extract --> Recipes[ExtractRecipesFromWikiText]

    ConfigFiles --> Pattern1[Regex: {{Game data/config|...}}]
    SaveGames --> Pattern2[Regex: {{Game data/saves|...}}]

    Recipes --> IniFixes[ExtractIniFixes]
    Recipes --> RegFixes[ExtractRegistryFixes]
    Recipes --> CmdFixes[ExtractCommandLineFixes]

    IniFixes --> ParseCode[Parse <pre> blocks]
    RegFixes --> ParseReg[Parse registry patterns]
    CmdFixes --> ParseCmd[Parse launch options]

    ParseCode --> CreateRecipes[Create TweakRecipe objects]
    ParseReg --> CreateRecipes
    ParseCmd --> CreateRecipes

    CreateRecipes --> Cache[Cache to LiteDB]
    Cache --> Display[Display in GameDetailView]
```

---

## API/Interface

### PCGWService

```csharp
public class PCGWService
{
    /// <summary>
    /// Searches for a game on PCGamingWiki.
    /// </summary>
    public async Task<PCGWGameInfo?> SearchGameAsync(string gameTitle);

    /// <summary>
    /// Gets full page content with all fix instructions.
    /// </summary>
    public async Task<PCGWGameInfo?> GetPageContentAsync(string pageTitle);

    /// <summary>
    /// Gets available tweaks for a specific game.
    /// </summary>
    public async Task<List<TweakRecipe>> GetAvailableTweaksAsync(string gameTitle, Guid gameId);
}
```

### PCGWGameInfo

```csharp
public class PCGWGameInfo
{
    public string Title { get; set; }
    public string WikiUrl { get; set; }
    public List<string> ConfigFiles { get; set; }
    public List<string> SaveGameLocations { get; set; }
    public List<TweakRecipe> AvailableTweaks { get; set; }
}
```

### TweakRecipe

```csharp
public class TweakRecipe
{
    public Guid Id { get; set; }
    public Guid GameId { get; set; }
    public TweakCategory Category { get; set; }  // Video, Audio, Input, Network
    public TweakTargetType TargetType { get; set; }  // IniFile, Registry, etc.
    public string Description { get; set; }
    public string FilePath { get; set; }  // Supports %USERPROFILE% variables
    public string? Section { get; set; }  // INI section
    public string Key { get; set; }
    public string Value { get; set; }
    public string RiskLevel { get; set; }  // Low, Medium, High
    public string? SourceUrl { get; set; }
    public bool IsEnabled { get; set; }
}
```

---

## Configuration

### Cargo API Endpoints

| Endpoint | Purpose |
|----------|---------|
| `action=query&list=search` | Search for game pages |
| `action=parse&prop=wikitext` | Get raw wiki content |

### Base URL
```
https://www.pcgamingwiki.com/w/api.php
```

### User-Agent Header
```
OpenTweak/1.0 (Automated Game Tweaks)
```

---

## Wiki Text Parsing

### Config File Patterns

```csharp
// Game data templates
{{Game data/config|Windows|...}}
{{p|game}}\path\to\config.ini
{{p|appdata}}\GameName\settings.cfg
{{p|hkcu}}\Software\Game\Settings
```

### Recipe Extraction Patterns

| Type | Pattern | Example |
|------|---------|---------|
| INI | `[Section]\nKey=Value` | `[Graphics]\nMotionBlur=0` |
| Registry | `HKEY_...\Key\Value` | `HKEY_CURRENT_USER\Game\Setting` |
| Command Line | `-argument` | `-novid -high` |

### Extraction Methods

1. **ExtractIniFixes**: Parses `<pre>` blocks and INI-style patterns
2. **ExtractRegistryFixes**: Matches registry key/value patterns
3. **ExtractCommandLineFixes**: Finds launch option arguments

---

## Error Handling

| Scenario | Behavior |
|----------|----------|
| API timeout | Return empty list, show cached data if available |
| No wiki page found | Show "No tweaks available" message |
| Parse failure | Log error, skip individual recipe |
| Rate limited | Retry with exponential backoff |
