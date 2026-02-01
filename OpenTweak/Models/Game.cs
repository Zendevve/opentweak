// OpenTweak - PC Game Optimization Tool
// Copyright 2024-2025 OpenTweak Contributors
// Licensed under PolyForm Shield License 1.0.0
// See LICENSE.md for full terms.

namespace OpenTweak.Models;

/// <summary>
/// Represents the type of game launcher/store.
/// </summary>
public enum LauncherType
{
    Steam,
    Epic,
    GOG,
    Xbox,
    Manual
}

/// <summary>
/// Represents a detected or manually added game.
/// </summary>
public class Game
{
    /// <summary>
    /// Unique identifier for the game (LiteDB uses this as primary key).
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Display name of the game.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Full path to the game's installation directory.
    /// </summary>
    public string InstallPath { get; set; } = string.Empty;

    /// <summary>
    /// The launcher/store this game was detected from.
    /// </summary>
    public LauncherType LauncherType { get; set; }

    /// <summary>
    /// Store-specific application ID (Steam AppId, Epic CatalogItemId, etc.).
    /// </summary>
    public string? AppId { get; set; }

    /// <summary>
    /// Known configuration file paths relative to install or user directories.
    /// </summary>
    public List<string> ConfigPaths { get; set; } = new();

    /// <summary>
    /// Path to the game's cover image or icon.
    /// </summary>
    public string? CoverImagePath { get; set; }

    /// <summary>
    /// PCGW page title for wiki lookups (may differ from display name).
    /// </summary>
    public string? PCGWTitle { get; set; }

    /// <summary>
    /// When this game was first detected/added.
    /// </summary>
    public DateTime AddedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time tweaks were applied to this game.
    /// </summary>
    public DateTime? LastTweakedDate { get; set; }
}
