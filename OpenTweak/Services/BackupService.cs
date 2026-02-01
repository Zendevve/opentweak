using System.IO;
using OpenTweak.Models;

namespace OpenTweak.Services;

/// <summary>
/// Service for creating and restoring file backups.
/// This is the "Holy Grail" feature - automatic backup before any change.
/// </summary>
public class BackupService
{
    private readonly string _backupBasePath;

    public BackupService()
    {
        // Store backups in AppData to avoid cluttering game directories
        _backupBasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenTweak", "Backups");

        Directory.CreateDirectory(_backupBasePath);
    }

    /// <summary>
    /// Creates a snapshot of files before applying tweaks.
    /// </summary>
    public async Task<Snapshot> CreateSnapshotAsync(Game game, IEnumerable<string> filesToBackup, string? description = null)
    {
        var timestamp = DateTime.UtcNow;
        var safeGameName = SanitizeFileName(game.Name);
        var backupFolder = Path.Combine(_backupBasePath, safeGameName, timestamp.ToString("yyyy-MM-dd_HH-mm-ss"));

        Directory.CreateDirectory(backupFolder);

        var snapshot = new Snapshot
        {
            GameId = game.Id,
            Timestamp = timestamp,
            BackupPath = backupFolder,
            Description = description ?? $"Backup before applying tweaks"
        };

        foreach (var filePath in filesToBackup)
        {
            if (!File.Exists(filePath)) continue;

            try
            {
                // Preserve relative path structure in backup
                var relativePath = GetRelativePath(filePath, game.InstallPath);
                var backupFilePath = Path.Combine(backupFolder, relativePath);

                // Ensure directory exists
                var backupDir = Path.GetDirectoryName(backupFilePath);
                if (!string.IsNullOrEmpty(backupDir))
                    Directory.CreateDirectory(backupDir);

                // Copy the file
                await CopyFileAsync(filePath, backupFilePath);

                snapshot.FilesBackedUp.Add(filePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to backup {filePath}: {ex.Message}");
            }
        }

        // Save snapshot metadata
        await SaveSnapshotMetadataAsync(snapshot);

        return snapshot;
    }

    /// <summary>
    /// Restores files from a snapshot.
    /// </summary>
    public async Task<bool> RestoreSnapshotAsync(Snapshot snapshot, Game game)
    {
        if (!Directory.Exists(snapshot.BackupPath))
            return false;

        var success = true;

        foreach (var originalPath in snapshot.FilesBackedUp)
        {
            try
            {
                var relativePath = GetRelativePath(originalPath, game.InstallPath);
                var backupFilePath = Path.Combine(snapshot.BackupPath, relativePath);

                if (!File.Exists(backupFilePath)) continue;

                // Restore the file
                await CopyFileAsync(backupFilePath, originalPath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to restore {originalPath}: {ex.Message}");
                success = false;
            }
        }

        // Update snapshot status
        snapshot.WasRestored = true;
        snapshot.RestoredDate = DateTime.UtcNow;
        await SaveSnapshotMetadataAsync(snapshot);

        return success;
    }

    /// <summary>
    /// Gets all snapshots for a game.
    /// </summary>
    public async Task<List<Snapshot>> GetSnapshotsForGameAsync(Game game)
    {
        var snapshots = new List<Snapshot>();
        var safeGameName = SanitizeFileName(game.Name);
        var gameBackupPath = Path.Combine(_backupBasePath, safeGameName);

        if (!Directory.Exists(gameBackupPath))
            return snapshots;

        foreach (var snapshotDir in Directory.GetDirectories(gameBackupPath))
        {
            var metadataPath = Path.Combine(snapshotDir, "snapshot.json");
            if (File.Exists(metadataPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(metadataPath);
                    var snapshot = System.Text.Json.JsonSerializer.Deserialize<Snapshot>(json);
                    if (snapshot != null)
                        snapshots.Add(snapshot);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to read snapshot metadata from {metadataPath}: {ex.Message}");
                }
            }
        }

        return snapshots.OrderByDescending(s => s.Timestamp).ToList();
    }

    /// <summary>
    /// Deletes a snapshot and its files.
    /// </summary>
    public bool DeleteSnapshot(Snapshot snapshot)
    {
        try
        {
            if (Directory.Exists(snapshot.BackupPath))
            {
                Directory.Delete(snapshot.BackupPath, true);
                return true;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to delete snapshot: {ex.Message}");
        }

        return false;
    }

    #region Private Helpers

    private async Task SaveSnapshotMetadataAsync(Snapshot snapshot)
    {
        var metadataPath = Path.Combine(snapshot.BackupPath, "snapshot.json");
        var json = System.Text.Json.JsonSerializer.Serialize(snapshot, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(metadataPath, json);
    }

    private async Task CopyFileAsync(string source, string destination)
    {
        using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        using var destStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await sourceStream.CopyToAsync(destStream);
    }

    private string GetRelativePath(string fullPath, string basePath)
    {
        // Handle paths outside the game directory (e.g., user config folders)
        if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            // Use a hash of the full path to create a unique relative path
            var hash = fullPath.GetHashCode().ToString("X8");
            return Path.Combine("_external", hash, Path.GetFileName(fullPath));
        }

        return Path.GetRelativePath(basePath, fullPath);
    }

    private string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }

    #endregion
}
