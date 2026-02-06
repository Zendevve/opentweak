// OpenTweak - PC Game Optimization Tool
// Copyright 2024-2025 OpenTweak Contributors
// Licensed under PolyForm Shield License 1.0.0
// See LICENSE.md for full terms.

namespace OpenTweak.Models;

/// <summary>
/// Category of tweak based on PCGW table structure.
/// </summary>
public enum TweakCategory
{
    Video,
    Audio,
    Input,
    Network,
    Other
}

/// <summary>
/// Type of configuration target the tweak modifies.
/// </summary>
public enum TweakTargetType
{
    IniFile,
    CfgFile,
    XmlFile,
    JsonFile,
    Registry,
    Other
}

/// <summary>
/// Represents a deterministic tweak recipe parsed from PCGW.
/// Unlike AI agents that "figure it out" each time, recipes are parsed once
/// and executed the same way every time.
/// </summary>
public class TweakRecipe
{
    /// <summary>
    /// Unique identifier for the recipe.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The game this tweak applies to.
    /// </summary>
    public Guid GameId { get; set; }

    /// <summary>
    /// The name of the tweak (e.g. "Disable Motion Blur").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Category from PCGW (Video, Audio, Input, etc.).
    /// </summary>
    public TweakCategory Category { get; set; }

    /// <summary>
    /// Human-readable description of what this tweak does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The type of configuration this tweak modifies.
    /// </summary>
    public TweakTargetType TargetType { get; set; }

    /// <summary>
    /// Path to the config file (can use environment variables like %USERPROFILE%).
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// For INI files: the section name (e.g., "[Graphics]").
    /// </summary>
    public string? Section { get; set; }

    /// <summary>
    /// The key/property name to modify.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// The recommended value to set.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// The original/default value (for documentation).
    /// </summary>
    public string? OriginalValue { get; set; }

    /// <summary>
    /// Source URL from PCGW for reference.
    /// </summary>
    public string? SourceUrl { get; set; }

    /// <summary>
    /// Whether this tweak is enabled for batch apply.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Risk level: Low (cosmetic), Medium (gameplay), High (stability).
    /// </summary>
    public string RiskLevel { get; set; } = "Low";

    /// <summary>
    /// Whether this recipe can be applied by the current engine.
    /// Set by validation stage; recipes marked false are filtered from UI.
    /// </summary>
    public bool IsSupported { get; set; } = true;

    /// <summary>
    /// If IsSupported is false, explains why (e.g., "Registry tweaks not yet implemented").
    /// </summary>
    public string? UnsupportedReason { get; set; }

    /// <summary>
    /// When true, allows creating new config files if they don't exist.
    /// Default is false for safety - prevents accidental file creation.
    /// </summary>
    public bool AllowFileCreation { get; set; } = false;
}
