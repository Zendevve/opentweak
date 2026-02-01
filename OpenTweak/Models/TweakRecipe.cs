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
    Registry
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
}
