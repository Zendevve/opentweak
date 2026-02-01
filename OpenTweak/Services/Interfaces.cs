// OpenTweak - PC Game Optimization Tool
// Copyright 2024-2025 OpenTweak Contributors
// Licensed under PolyForm Shield License 1.0.0
// See LICENSE.md for full terms.

using OpenTweak.Models;

namespace OpenTweak.Services;

/// <summary>
/// Interface for game scanning operations across different launchers.
/// </summary>
public interface IGameScanner
{
    /// <summary>
    /// Scans all supported launchers for installed games.
    /// </summary>
    Task<List<Game>> ScanAllLaunchersAsync();

    /// <summary>
    /// Adds a game manually by path.
    /// </summary>
    Game AddManualGame(string name, string installPath);
}

/// <summary>
/// Interface for PCGamingWiki API interactions.
/// </summary>
public interface IPCGWService
{
    /// <summary>
    /// Searches for a game on PCGamingWiki.
    /// </summary>
    Task<PCGWGameInfo?> SearchGameAsync(string gameTitle);

    /// <summary>
    /// Gets page content from PCGamingWiki.
    /// </summary>
    Task<PCGWGameInfo?> GetPageContentAsync(string pageTitle);

    /// <summary>
    /// Gets available tweaks for a game.
    /// </summary>
    Task<List<TweakRecipe>> GetAvailableTweaksAsync(string gameTitle, Guid gameId);
}

/// <summary>
/// Interface for applying and previewing configuration tweaks.
/// </summary>
public interface ITweakEngine
{
    /// <summary>
    /// Previews what changes will be made by the tweaks.
    /// </summary>
    Task<List<TweakEngine.TweakChange>> PreviewTweaksAsync(IEnumerable<TweakRecipe> recipes);

    /// <summary>
    /// Applies tweaks to a game, creating a backup first.
    /// </summary>
    Task<Snapshot?> ApplyTweaksAsync(Game game, IEnumerable<TweakRecipe> recipes);
}

/// <summary>
/// Interface for backup and restore operations.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates a snapshot of the specified files.
    /// </summary>
    Task<Snapshot> CreateSnapshotAsync(Game game, IEnumerable<string> filesToBackup, string? description = null);

    /// <summary>
    /// Restores files from a snapshot.
    /// </summary>
    Task<bool> RestoreSnapshotAsync(Snapshot snapshot, Game game);

    /// <summary>
    /// Restores files from a snapshot with detailed error information.
    /// </summary>
    Task<Common.Result> RestoreSnapshotWithResultAsync(Snapshot snapshot, Game game);

    /// <summary>
    /// Gets all snapshots for a game.
    /// </summary>
    Task<List<Snapshot>> GetSnapshotsForGameAsync(Game game);

    /// <summary>
    /// Deletes a snapshot and its backup files.
    /// </summary>
    bool DeleteSnapshot(Snapshot snapshot);
}

/// <summary>
/// Interface for database operations.
/// </summary>
public interface IDatabaseService
{
    // Games
    IEnumerable<Game> GetAllGames();
    Game? GetGame(Guid id);
    Game? GetGameByAppId(string appId, LauncherType launcher);
    void UpsertGame(Game game);
    void UpsertGames(IEnumerable<Game> games);
    bool DeleteGame(Guid id);

    // Recipes
    IEnumerable<TweakRecipe> GetRecipesForGame(Guid gameId);
    void UpsertRecipe(TweakRecipe recipe);
    void UpsertRecipes(IEnumerable<TweakRecipe> recipes);
    bool DeleteRecipe(Guid id);
    void DeleteRecipesForGame(Guid gameId);

    // Snapshots
    IEnumerable<Snapshot> GetSnapshotsForGame(Guid gameId);
    void UpsertSnapshot(Snapshot snapshot);
    bool DeleteSnapshot(Guid id);
}
