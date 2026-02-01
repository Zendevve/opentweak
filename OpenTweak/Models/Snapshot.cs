namespace OpenTweak.Models;

/// <summary>
/// Represents a backup snapshot of files before tweaks are applied.
/// This is the "Holy Grail" feature - automatic backup before any change.
/// </summary>
public class Snapshot
{
    /// <summary>
    /// Unique identifier for this snapshot.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The game this snapshot is associated with.
    /// </summary>
    public Guid GameId { get; set; }

    /// <summary>
    /// When this snapshot was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Base path where backup files are stored.
    /// Format: /Backups/{GameName}/{Timestamp}/
    /// </summary>
    public string BackupPath { get; set; } = string.Empty;

    /// <summary>
    /// List of original file paths that were backed up.
    /// </summary>
    public List<string> FilesBackedUp { get; set; } = new();

    /// <summary>
    /// Description of what tweaks were applied after this backup.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// IDs of the tweak recipes that were applied.
    /// </summary>
    public List<Guid> AppliedTweakIds { get; set; } = new();

    /// <summary>
    /// Whether this snapshot has been restored (used for history).
    /// </summary>
    public bool WasRestored { get; set; }

    /// <summary>
    /// When this snapshot was restored (if applicable).
    /// </summary>
    public DateTime? RestoredDate { get; set; }
}
