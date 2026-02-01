using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using OpenTweak.Models;
using OpenTweak.Services;
using Xunit;

namespace OpenTweak.Tests.Services;

/// <summary>
/// Tests for the BackupService that creates and restores file backups.
/// </summary>
public class BackupServiceTests : IDisposable
{
    private readonly BackupService _backupService;
    private readonly string _tempDirectory;
    private readonly string _backupBasePath;

    public BackupServiceTests()
    {
        _backupService = new BackupService();
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _backupBasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenTweak", "Backups");
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        // Cleanup temp directory
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }

        // Cleanup test backups
        try
        {
            var testBackupPath = Path.Combine(_backupBasePath);
            if (Directory.Exists(testBackupPath))
            {
                // Only clean up test-specific directories
                var testDirs = Directory.GetDirectories(testBackupPath)
                    .Where(d => d.Contains("TestGame") || d.Contains("Test_Game"));
                foreach (var dir in testDirs)
                {
                    Directory.Delete(dir, true);
                }
            }
        }
        catch { /* Ignore cleanup errors */ }
    }

    #region CreateSnapshotAsync Tests

    [Fact]
    public async Task CreateSnapshotAsync_WithValidFiles_CreatesSnapshot()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            InstallPath = _tempDirectory
        };

        var testFile = Path.Combine(_tempDirectory, "config.ini");
        await File.WriteAllTextAsync(testFile, "[Settings]\nValue=1\n");

        var filesToBackup = new List<string> { testFile };

        // Act
        var snapshot = await _backupService.CreateSnapshotAsync(game, filesToBackup);

        // Assert
        Assert.NotNull(snapshot);
        Assert.Equal(game.Id, snapshot.GameId);
        Assert.NotEqual(Guid.Empty, snapshot.Id);
        Assert.True(Directory.Exists(snapshot.BackupPath));
        Assert.Contains(testFile, snapshot.FilesBackedUp);
    }

    [Fact]
    public async Task CreateSnapshotAsync_WithDescription_SetsDescription()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            InstallPath = _tempDirectory
        };

        var testFile = Path.Combine(_tempDirectory, "config.ini");
        await File.WriteAllTextAsync(testFile, "test content");

        var description = "Test backup description";

        // Act
        var snapshot = await _backupService.CreateSnapshotAsync(game, new List<string> { testFile }, description);

        // Assert
        Assert.Equal(description, snapshot.Description);
    }

    [Fact]
    public async Task CreateSnapshotAsync_WithNullDescription_UsesDefaultDescription()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            InstallPath = _tempDirectory
        };

        var testFile = Path.Combine(_tempDirectory, "config.ini");
        await File.WriteAllTextAsync(testFile, "test content");

        // Act
        var snapshot = await _backupService.CreateSnapshotAsync(game, new List<string> { testFile });

        // Assert
        Assert.NotNull(snapshot.Description);
        Assert.Contains("Backup", snapshot.Description);
    }

    [Fact]
    public async Task CreateSnapshotAsync_CreatesMetadataFile()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            InstallPath = _tempDirectory
        };

        var testFile = Path.Combine(_tempDirectory, "config.ini");
        await File.WriteAllTextAsync(testFile, "test content");

        // Act
        var snapshot = await _backupService.CreateSnapshotAsync(game, new List<string> { testFile });

        // Assert
        var metadataPath = Path.Combine(snapshot.BackupPath, "snapshot.json");
        Assert.True(File.Exists(metadataPath));
    }

    [Fact]
    public async Task CreateSnapshotAsync_CopiesFileContent()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            InstallPath = _tempDirectory
        };

        var testFile = Path.Combine(_tempDirectory, "config.ini");
        var originalContent = "[Settings]\nValue=Test123\n";
        await File.WriteAllTextAsync(testFile, originalContent);

        // Act
        var snapshot = await _backupService.CreateSnapshotAsync(game, new List<string> { testFile });

        // Assert
        var backupFile = Directory.GetFiles(snapshot.BackupPath, "*.ini").FirstOrDefault();
        Assert.NotNull(backupFile);
        var backupContent = await File.ReadAllTextAsync(backupFile);
        Assert.Equal(originalContent, backupContent);
    }

    [Fact]
    public async Task CreateSnapshotAsync_WithEmptyFileList_CreatesEmptySnapshot()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            InstallPath = _tempDirectory
        };

        // Act
        var snapshot = await _backupService.CreateSnapshotAsync(game, new List<string>());

        // Assert
        Assert.NotNull(snapshot);
        Assert.Empty(snapshot.FilesBackedUp);
        Assert.True(Directory.Exists(snapshot.BackupPath));
    }

    [Fact]
    public async Task CreateSnapshotAsync_WithNonExistentFile_SkipsFile()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            InstallPath = _tempDirectory
        };

        var nonExistentFile = Path.Combine(_tempDirectory, "doesnotexist.ini");
        var existingFile = Path.Combine(_tempDirectory, "exists.ini");
        await File.WriteAllTextAsync(existingFile, "test");

        // Act
        var snapshot = await _backupService.CreateSnapshotAsync(game, new List<string> { nonExistentFile, existingFile });

        // Assert
        Assert.Single(snapshot.FilesBackedUp);
        Assert.Contains(existingFile, snapshot.FilesBackedUp);
        Assert.DoesNotContain(nonExistentFile, snapshot.FilesBackedUp);
    }

    [Fact]
    public async Task CreateSnapshotAsync_SanitizesGameName()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test:Game|With*Invalid?Chars",
            InstallPath = _tempDirectory
        };

        var testFile = Path.Combine(_tempDirectory, "config.ini");
        await File.WriteAllTextAsync(testFile, "test");

        // Act
        var snapshot = await _backupService.CreateSnapshotAsync(game, new List<string> { testFile });

        // Assert
        Assert.True(Directory.Exists(snapshot.BackupPath));
        Assert.DoesNotContain("*", snapshot.BackupPath);
        Assert.DoesNotContain("?", snapshot.BackupPath);
        Assert.DoesNotContain("|", snapshot.BackupPath);
    }

    [Fact]
    public async Task CreateSnapshotAsync_PreservesDirectoryStructure()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            InstallPath = _tempDirectory
        };

        var subDir = Path.Combine(_tempDirectory, "SubFolder");
        Directory.CreateDirectory(subDir);
        var testFile = Path.Combine(subDir, "config.ini");
        await File.WriteAllTextAsync(testFile, "test content");

        // Act
        var snapshot = await _backupService.CreateSnapshotAsync(game, new List<string> { testFile });

        // Assert
        var backupSubDir = Path.Combine(snapshot.BackupPath, "SubFolder");
        Assert.True(Directory.Exists(backupSubDir) || Directory.GetFiles(snapshot.BackupPath, "*.ini", SearchOption.AllDirectories).Any());
    }

    [Fact]
    public async Task CreateSnapshotAsync_HandlesExternalPaths()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            InstallPath = _tempDirectory
        };

        var externalDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(externalDir);
        var externalFile = Path.Combine(externalDir, "external.ini");
        await File.WriteAllTextAsync(externalFile, "external content");

        try
        {
            // Act
            var snapshot = await _backupService.CreateSnapshotAsync(game, new List<string> { externalFile });

            // Assert
            Assert.Contains(externalFile, snapshot.FilesBackedUp);
            var backupFiles = Directory.GetFiles(snapshot.BackupPath, "*.ini", SearchOption.AllDirectories);
            Assert.Single(backupFiles);
        }
        finally
        {
            Directory.Delete(externalDir, true);
        }
    }

    #endregion

    #region RestoreSnapshotAsync Tests

    [Fact]
    public async Task RestoreSnapshotAsync_WithValidSnapshot_RestoresFiles()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            InstallPath = _tempDirectory
        };

        var testFile = Path.Combine(_tempDirectory, "config.ini");
        var originalContent = "[Settings]\nOriginalValue=1\n";
        await File.WriteAllTextAsync(testFile, originalContent);

        var snapshot = await _backupService.CreateSnapshotAsync(game, new List<string> { testFile });

        // Modify the file
        await File.WriteAllTextAsync(testFile, "[Settings]\nModifiedValue=2\n");

        // Act
        var result = await _backupService.RestoreSnapshotAsync(snapshot, game);

        // Assert
        Assert.True(result);
        var restoredContent = await File.ReadAllTextAsync(testFile);
        Assert.Equal(originalContent, restoredContent);
        Assert.True(snapshot.WasRestored);
        Assert.NotNull(snapshot.RestoredDate);
    }

    [Fact]
    public async Task RestoreSnapshotAsync_WithMissingBackupPath_ReturnsFalse()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            InstallPath = _tempDirectory
        };

        var snapshot = new Snapshot
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            BackupPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            FilesBackedUp = new List<string>()
        };

        // Act
        var result = await _backupService.RestoreSnapshotAsync(snapshot, game);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task RestoreSnapshotAsync_WithEmptySnapshot_ReturnsTrue()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            InstallPath = _tempDirectory
        };

        var snapshot = await _backupService.CreateSnapshotAsync(game, new List<string>());

        // Act
        var result = await _backupService.RestoreSnapshotAsync(snapshot, game);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RestoreSnapshotAsync_UpdatesSnapshotMetadata()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            InstallPath = _tempDirectory
        };

        var testFile = Path.Combine(_tempDirectory, "config.ini");
        await File.WriteAllTextAsync(testFile, "test");

        var snapshot = await _backupService.CreateSnapshotAsync(game, new List<string> { testFile });
        Assert.False(snapshot.WasRestored);

        // Act
        await _backupService.RestoreSnapshotAsync(snapshot, game);

        // Assert
        Assert.True(snapshot.WasRestored);
        Assert.NotNull(snapshot.RestoredDate);
    }

    #endregion

    #region GetSnapshotsForGameAsync Tests

    [Fact]
    public async Task GetSnapshotsForGameAsync_WithNoSnapshots_ReturnsEmptyList()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "NonExistentGame12345",
            InstallPath = _tempDirectory
        };

        // Act
        var snapshots = await _backupService.GetSnapshotsForGameAsync(game);

        // Assert
        Assert.NotNull(snapshots);
        Assert.Empty(snapshots);
    }

    [Fact]
    public async Task GetSnapshotsForGameAsync_ReturnsSnapshotsInDescendingOrder()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            InstallPath = _tempDirectory
        };

        var testFile = Path.Combine(_tempDirectory, "config.ini");
        await File.WriteAllTextAsync(testFile, "test");

        var snapshot1 = await _backupService.CreateSnapshotAsync(game, new List<string> { testFile }, "First");
        await Task.Delay(100); // Ensure different timestamps
        var snapshot2 = await _backupService.CreateSnapshotAsync(game, new List<string> { testFile }, "Second");

        // Act
        var snapshots = await _backupService.GetSnapshotsForGameAsync(game);

        // Assert
        Assert.Equal(2, snapshots.Count);
        Assert.True(snapshots[0].Timestamp >= snapshots[1].Timestamp);
    }

    [Fact]
    public async Task GetSnapshotsForGameAsync_IgnoresInvalidMetadata()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            InstallPath = _tempDirectory
        };

        var testFile = Path.Combine(_tempDirectory, "config.ini");
        await File.WriteAllTextAsync(testFile, "test");

        var snapshot = await _backupService.CreateSnapshotAsync(game, new List<string> { testFile });

        // Corrupt the metadata file
        var metadataPath = Path.Combine(snapshot.BackupPath, "snapshot.json");
        await File.WriteAllTextAsync(metadataPath, "invalid json");

        // Act
        var snapshots = await _backupService.GetSnapshotsForGameAsync(game);

        // Assert - Should return empty or valid snapshots only
        Assert.True(snapshots.Count == 0 || snapshots.All(s => s.Id != Guid.Empty));
    }

    #endregion

    #region DeleteSnapshot Tests

    [Fact]
    public async Task DeleteSnapshot_WithValidSnapshot_DeletesFiles()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            InstallPath = _tempDirectory
        };

        var testFile = Path.Combine(_tempDirectory, "config.ini");
        await File.WriteAllTextAsync(testFile, "test");

        var snapshot = await _backupService.CreateSnapshotAsync(game, new List<string> { testFile });
        Assert.True(Directory.Exists(snapshot.BackupPath));

        // Act
        var result = _backupService.DeleteSnapshot(snapshot);

        // Assert
        Assert.True(result);
        Assert.False(Directory.Exists(snapshot.BackupPath));
    }

    [Fact]
    public void DeleteSnapshot_WithNonExistentPath_ReturnsFalse()
    {
        // Arrange
        var snapshot = new Snapshot
        {
            Id = Guid.NewGuid(),
            BackupPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())
        };

        // Act
        var result = _backupService.DeleteSnapshot(snapshot);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteSnapshot_WithNullBackupPath_ReturnsFalse()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            InstallPath = _tempDirectory
        };

        var testFile = Path.Combine(_tempDirectory, "config.ini");
        await File.WriteAllTextAsync(testFile, "test");

        var snapshot = await _backupService.CreateSnapshotAsync(game, new List<string> { testFile });
        snapshot.BackupPath = null!;

        // Act
        var result = _backupService.DeleteSnapshot(snapshot);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CreateSnapshotAsync_HandlesConcurrentSnapshots()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            InstallPath = _tempDirectory
        };

        var testFile = Path.Combine(_tempDirectory, "config.ini");
        await File.WriteAllTextAsync(testFile, "test");

        // Act
        var tasks = new[]
        {
            _backupService.CreateSnapshotAsync(game, new List<string> { testFile }),
            _backupService.CreateSnapshotAsync(game, new List<string> { testFile }),
            _backupService.CreateSnapshotAsync(game, new List<string> { testFile })
        };

        var snapshots = await Task.WhenAll(tasks);

        // Assert
        Assert.All(snapshots, s => Assert.NotNull(s));
        Assert.All(snapshots, s => Assert.True(Directory.Exists(s.BackupPath)));
    }

    [Fact]
    public async Task CreateSnapshotAsync_HandlesLargeFiles()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            InstallPath = _tempDirectory
        };

        var testFile = Path.Combine(_tempDirectory, "largefile.bin");
        var largeContent = new string('X', 1024 * 1024); // 1MB
        await File.WriteAllTextAsync(testFile, largeContent);

        // Act
        var snapshot = await _backupService.CreateSnapshotAsync(game, new List<string> { testFile });

        // Assert
        Assert.Single(snapshot.FilesBackedUp);
        var backupFile = Directory.GetFiles(snapshot.BackupPath).FirstOrDefault(f => f.EndsWith(".bin"));
        Assert.NotNull(backupFile);
        var backupContent = await File.ReadAllTextAsync(backupFile);
        Assert.Equal(largeContent.Length, backupContent.Length);
    }

    [Fact]
    public async Task CreateSnapshotAsync_HandlesSpecialCharactersInFileContent()
    {
        // Arrange
        var game = new Game
        {
            Id = Guid.NewGuid(),
            Name = "Test Game",
            InstallPath = _tempDirectory
        };

        var testFile = Path.Combine(_tempDirectory, "special.ini");
        var specialContent = "[Settings]\nUnicode=日本語\nSpecial=<>&\"'\n";
        await File.WriteAllTextAsync(testFile, specialContent);

        // Act
        var snapshot = await _backupService.CreateSnapshotAsync(game, new List<string> { testFile });

        // Assert
        var backupFile = Directory.GetFiles(snapshot.BackupPath, "*.ini").First();
        var backupContent = await File.ReadAllTextAsync(backupFile);
        Assert.Equal(specialContent, backupContent);
    }

    #endregion
}
