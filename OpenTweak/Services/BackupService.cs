// OpenTweak - PC Game Optimization Tool
// Copyright 2024-2025 OpenTweak Contributors
// Licensed under PolyForm Shield License 1.0.0
// See LICENSE.md for full terms.

using System.IO;
using System.Security.Cryptography;
using OpenTweak.Common;
using OpenTweak.Models;

namespace OpenTweak.Services;

/// <summary>
/// Service for creating and restoring file backups.
/// This is the "Holy Grail" feature - automatic backup before any change.
/// </summary>
public class BackupService : IBackupService
{
    private readonly string _backupBasePath;

    public BackupService(string? backupBasePath = null)
    {
        if (string.IsNullOrEmpty(backupBasePath))
        {
            // Store backups in AppData to avoid cluttering game directories
            _backupBasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OpenTweak", "Backups");
        }
        else
        {
            _backupBasePath = backupBasePath;
        }

        Directory.CreateDirectory(_backupBasePath);
    }

    /// <summary>
    /// Creates a snapshot of files before applying tweaks.
    /// </summary>
    public async Task<Snapshot?> CreateSnapshotAsync(Game game, IEnumerable<string> filesToBackup, string? description = null, CancellationToken cancellationToken = default)
    {
        var timestamp = DateTime.UtcNow;
        var safeGameName = SanitizeFileName(game.Name);
        var backupFolder = Path.Combine(_backupBasePath, safeGameName, timestamp.ToString("yyyy-MM-dd_HH-mm-ss-fff"));

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
            if (cancellationToken.IsCancellationRequested) return null;
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
                await CopyFileAsync(filePath, backupFilePath, cancellationToken);

                snapshot.FilesBackedUp.Add(filePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to backup {filePath}: {ex.Message}");
            }
        }

        // Save snapshot metadata
        await SaveSnapshotMetadataAsync(snapshot, cancellationToken);

        return snapshot;
    }

    /// <summary>
    /// Restores files from a snapshot.
    /// </summary>
    public async Task<bool> RestoreSnapshotAsync(Snapshot snapshot, Game game, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(snapshot.BackupPath))
            return false;

        var success = true;

        foreach (var originalPath in snapshot.FilesBackedUp)
        {
            if (cancellationToken.IsCancellationRequested) return false;

            try
            {
                var relativePath = GetRelativePath(originalPath, game.InstallPath);
                var backupFilePath = Path.Combine(snapshot.BackupPath, relativePath);

                if (!File.Exists(backupFilePath)) continue;

                // Restore the file
                await CopyFileAsync(backupFilePath, originalPath, cancellationToken);
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
        await SaveSnapshotMetadataAsync(snapshot, cancellationToken);

        return success;
    }

    /// <summary>
    /// Restores files from a snapshot with detailed error information.
    /// </summary>
    public async Task<Result> RestoreSnapshotWithResultAsync(Snapshot snapshot, Game game, CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(snapshot.BackupPath))
            return Result.Failure($"Backup directory not found: {snapshot.BackupPath}");

        var failedFiles = new List<string>();

        foreach (var originalPath in snapshot.FilesBackedUp)
        {
            if (cancellationToken.IsCancellationRequested) return Result.Failure("Operation cancelled");

            try
            {
                var relativePath = GetRelativePath(originalPath, game.InstallPath);
                var backupFilePath = Path.Combine(snapshot.BackupPath, relativePath);

                if (!File.Exists(backupFilePath))
                {
                    failedFiles.Add($"{Path.GetFileName(originalPath)} (backup file missing)");
                    continue;
                }

                await CopyFileAsync(backupFilePath, originalPath, cancellationToken);
            }
            catch (UnauthorizedAccessException)
            {
                failedFiles.Add($"{Path.GetFileName(originalPath)} (access denied - file may be in use)");
            }
            catch (IOException ex)
            {
                failedFiles.Add($"{Path.GetFileName(originalPath)} ({ex.Message})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to restore {originalPath}: {ex.Message}");
                failedFiles.Add($"{Path.GetFileName(originalPath)} (unexpected error)");
            }
        }

        // Update snapshot status
        snapshot.WasRestored = true;
        snapshot.RestoredDate = DateTime.UtcNow;
        await SaveSnapshotMetadataAsync(snapshot, cancellationToken);

        if (failedFiles.Count > 0)
        {
            var fileList = string.Join(", ", failedFiles.Take(3));
            var more = failedFiles.Count > 3 ? $" and {failedFiles.Count - 3} more" : "";
            return Result.Failure($"Failed to restore: {fileList}{more}");
        }

        return Result.Success();
    }

    /// <summary>
    /// Gets all snapshots for a game.
    /// </summary>
    public async Task<List<Snapshot>> GetSnapshotsForGameAsync(Game game, CancellationToken cancellationToken = default)
    {
        var snapshots = new List<Snapshot>();
        var safeGameName = SanitizeFileName(game.Name);
        var gameBackupPath = Path.Combine(_backupBasePath, safeGameName);

        if (!Directory.Exists(gameBackupPath))
            return snapshots;

        foreach (var snapshotDir in Directory.GetDirectories(gameBackupPath))
        {
            if (cancellationToken.IsCancellationRequested) break;

            var metadataPath = Path.Combine(snapshotDir, "snapshot.json");
            if (File.Exists(metadataPath))
            {
                try
                {
                    var json = await File.ReadAllTextAsync(metadataPath, cancellationToken);
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

    private async Task SaveSnapshotMetadataAsync(Snapshot snapshot, CancellationToken cancellationToken)
    {
        var metadataPath = Path.Combine(snapshot.BackupPath, "snapshot.json");
        var json = System.Text.Json.JsonSerializer.Serialize(snapshot, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
        await File.WriteAllTextAsync(metadataPath, json, cancellationToken);
    }

    private async Task CopyFileAsync(string source, string destination, CancellationToken cancellationToken)
    {
        using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        using var destStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true);
        await sourceStream.CopyToAsync(destStream, cancellationToken);
    }

    private string GetRelativePath(string fullPath, string basePath)
    {
        // Handle paths outside the game directory (e.g., user config folders)
        if (!fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
        {
            // Use a stable hash of the full path to create a unique relative path
            // SHA256 ensures the same path always produces the same hash across
            // .NET versions, machine restarts, and different machines
            var hash = GetStablePathHash(fullPath);
            return Path.Combine("_external", hash, Path.GetFileName(fullPath));
        }

        return Path.GetRelativePath(basePath, fullPath);
    }

    /// <summary>
    /// Generates a stable hash for a file path that remains consistent across
    /// .NET versions and application restarts (unlike string.GetHashCode()).
    /// </summary>
    private static string GetStablePathHash(string path)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(path.ToLowerInvariant());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash)[..16]; // First 16 hex chars = 8 bytes
    }

    private string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }

    #endregion
}
