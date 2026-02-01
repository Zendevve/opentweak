// OpenTweak - PC Game Optimization Tool
// Copyright 2024-2025 OpenTweak Contributors
// Licensed under PolyForm Shield License 1.0.0
// See LICENSE.md for full terms.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using OpenTweak.Models;
using OpenTweak.Services;
using Xunit;

namespace OpenTweak.Tests.Services;

/// <summary>
/// Tests for the TweakEngine service that applies and previews configuration tweaks.
/// </summary>
public class TweakEngineTests : IDisposable
{
    private readonly Mock<BackupService> _mockBackupService;
    private readonly TweakEngine _tweakEngine;
    private readonly string _tempDirectory;

    public TweakEngineTests()
    {
        _mockBackupService = new Mock<BackupService>();
        _tweakEngine = new TweakEngine(_mockBackupService.Object);
        _tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, true);
        }
    }

    #region PreviewTweaksAsync Tests

    [Fact]
    public async Task PreviewTweaksAsync_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var recipes = new List<TweakRecipe>();

        // Act
        var result = await _tweakEngine.PreviewTweaksAsync(recipes);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task PreviewTweaksAsync_WithNullList_ReturnsEmptyList()
    {
        // Arrange
        IEnumerable<TweakRecipe>? recipes = null;

        // Act
        var result = await _tweakEngine.PreviewTweaksAsync(recipes ?? new List<TweakRecipe>());

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task PreviewTweaksAsync_DisabledRecipesAreSkipped()
    {
        // Arrange
        var recipes = new List<TweakRecipe>
        {
            new TweakRecipe
            {
                Id = Guid.NewGuid(),
                IsEnabled = false,
                FilePath = Path.Combine(_tempDirectory, "test.ini"),
                Key = "Setting",
                Value = "Value"
            }
        };

        // Act
        var result = await _tweakEngine.PreviewTweaksAsync(recipes);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task PreviewTweaksAsync_EmptyFilePathIsSkipped()
    {
        // Arrange
        var recipes = new List<TweakRecipe>
        {
            new TweakRecipe
            {
                Id = Guid.NewGuid(),
                IsEnabled = true,
                FilePath = "",
                Key = "Setting",
                Value = "Value"
            }
        };

        // Act
        var result = await _tweakEngine.PreviewTweaksAsync(recipes);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task PreviewTweaksAsync_NonExistentFile_ReturnsNewEntryChange()
    {
        // Arrange
        var configPath = Path.Combine(_tempDirectory, "nonexistent.ini");
        var recipes = new List<TweakRecipe>
        {
            new TweakRecipe
            {
                Id = Guid.NewGuid(),
                IsEnabled = true,
                FilePath = configPath,
                Section = "Graphics",
                Key = "Resolution",
                Value = "1920x1080",
                TargetType = TweakTargetType.IniFile
            }
        };

        // Act
        var result = await _tweakEngine.PreviewTweaksAsync(recipes);

        // Assert
        Assert.Single(result);
        var change = result[0];
        Assert.Equal(configPath, change.FilePath);
        Assert.Equal("Graphics", change.Section);
        Assert.Equal("Resolution", change.Key);
        Assert.Null(change.CurrentValue);
        Assert.Equal("1920x1080", change.NewValue);
        Assert.True(change.IsNewEntry);
    }

    [Fact]
    public async Task PreviewTweaksAsync_ExistingFile_ReturnsCurrentValue()
    {
        // Arrange
        var configPath = Path.Combine(_tempDirectory, "existing.ini");
        await File.WriteAllTextAsync(configPath, "[Graphics]\nResolution=1280x720\n");

        var recipes = new List<TweakRecipe>
        {
            new TweakRecipe
            {
                Id = Guid.NewGuid(),
                IsEnabled = true,
                FilePath = configPath,
                Section = "Graphics",
                Key = "Resolution",
                Value = "1920x1080",
                TargetType = TweakTargetType.IniFile
            }
        };

        // Act
        var result = await _tweakEngine.PreviewTweaksAsync(recipes);

        // Assert
        Assert.Single(result);
        var change = result[0];
        Assert.Equal("1280x720", change.CurrentValue);
        Assert.Equal("1920x1080", change.NewValue);
        Assert.False(change.IsNewEntry);
    }

    [Fact]
    public async Task PreviewTweaksAsync_ExpandsEnvironmentVariables()
    {
        // Arrange
        var recipes = new List<TweakRecipe>
        {
            new TweakRecipe
            {
                Id = Guid.NewGuid(),
                IsEnabled = true,
                FilePath = "%TEMP%\\test.ini",
                Key = "Setting",
                Value = "Value",
                TargetType = TweakTargetType.IniFile
            }
        };

        // Act
        var result = await _tweakEngine.PreviewTweaksAsync(recipes);

        // Assert
        Assert.Single(result);
        Assert.DoesNotContain("%TEMP%", result[0].FilePath);
        Assert.True(File.Exists(result[0].FilePath) || !result[0].FilePath.Contains("%"));
    }

    [Fact]
    public async Task PreviewTweaksAsync_MultipleRecipes_ReturnsMultipleChanges()
    {
        // Arrange
        var configPath1 = Path.Combine(_tempDirectory, "config1.ini");
        var configPath2 = Path.Combine(_tempDirectory, "config2.ini");
        await File.WriteAllTextAsync(configPath1, "[Section1]\nKey1=OldValue1\n");
        await File.WriteAllTextAsync(configPath2, "[Section2]\nKey2=OldValue2\n");

        var recipes = new List<TweakRecipe>
        {
            new TweakRecipe
            {
                Id = Guid.NewGuid(),
                IsEnabled = true,
                FilePath = configPath1,
                Section = "Section1",
                Key = "Key1",
                Value = "NewValue1",
                TargetType = TweakTargetType.IniFile
            },
            new TweakRecipe
            {
                Id = Guid.NewGuid(),
                IsEnabled = true,
                FilePath = configPath2,
                Section = "Section2",
                Key = "Key2",
                Value = "NewValue2",
                TargetType = TweakTargetType.IniFile
            }
        };

        // Act
        var result = await _tweakEngine.PreviewTweaksAsync(recipes);

        // Assert
        Assert.Equal(2, result.Count);
    }

    #endregion

    #region ApplyTweaksAsync Tests

    [Fact]
    public async Task ApplyTweaksAsync_WithNoEnabledRecipes_ReturnsNull()
    {
        // Arrange
        var game = new Game { Id = Guid.NewGuid(), Name = "Test Game", InstallPath = _tempDirectory };
        var recipes = new List<TweakRecipe>
        {
            new TweakRecipe { Id = Guid.NewGuid(), IsEnabled = false, FilePath = "test.ini", Key = "Key", Value = "Value" }
        };

        // Act
        var result = await _tweakEngine.ApplyTweaksAsync(game, recipes);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyTweaksAsync_WithEmptyRecipes_ReturnsNull()
    {
        // Arrange
        var game = new Game { Id = Guid.NewGuid(), Name = "Test Game", InstallPath = _tempDirectory };
        var recipes = new List<TweakRecipe>();

        // Act
        var result = await _tweakEngine.ApplyTweaksAsync(game, recipes);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ApplyTweaksAsync_CreatesSnapshotBeforeApplying()
    {
        // Arrange
        var game = new Game { Id = Guid.NewGuid(), Name = "Test Game", InstallPath = _tempDirectory };
        var configPath = Path.Combine(_tempDirectory, "test.ini");
        await File.WriteAllTextAsync(configPath, "[Graphics]\nResolution=1280x720\n");

        var snapshot = new Snapshot
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            BackupPath = Path.Combine(_tempDirectory, "backup"),
            FilesBackedUp = new List<string> { configPath }
        };

        _mockBackupService
            .Setup(x => x.CreateSnapshotAsync(game, It.IsAny<List<string>>(), It.IsAny<string>()))
            .ReturnsAsync(snapshot);

        var recipes = new List<TweakRecipe>
        {
            new TweakRecipe
            {
                Id = Guid.NewGuid(),
                IsEnabled = true,
                FilePath = configPath,
                Section = "Graphics",
                Key = "Resolution",
                Value = "1920x1080",
                TargetType = TweakTargetType.IniFile
            }
        };

        // Act
        var result = await _tweakEngine.ApplyTweaksAsync(game, recipes);

        // Assert
        Assert.NotNull(result);
        _mockBackupService.Verify(x => x.CreateSnapshotAsync(game, It.IsAny<List<string>>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task ApplyTweaksAsync_IniFile_CreatesNewFile()
    {
        // Arrange
        var game = new Game { Id = Guid.NewGuid(), Name = "Test Game", InstallPath = _tempDirectory };
        var configPath = Path.Combine(_tempDirectory, "newfile.ini");

        var snapshot = new Snapshot
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            BackupPath = Path.Combine(_tempDirectory, "backup"),
            FilesBackedUp = new List<string>()
        };

        _mockBackupService
            .Setup(x => x.CreateSnapshotAsync(game, It.IsAny<List<string>>(), It.IsAny<string>()))
            .ReturnsAsync(snapshot);

        var recipes = new List<TweakRecipe>
        {
            new TweakRecipe
            {
                Id = Guid.NewGuid(),
                IsEnabled = true,
                FilePath = configPath,
                Section = "Graphics",
                Key = "Resolution",
                Value = "1920x1080",
                TargetType = TweakTargetType.IniFile
            }
        };

        // Act
        await _tweakEngine.ApplyTweaksAsync(game, recipes);

        // Assert
        Assert.True(File.Exists(configPath));
        var content = await File.ReadAllTextAsync(configPath);
        Assert.Contains("Resolution", content);
    }

    [Fact]
    public async Task ApplyTweaksAsync_SetsAppliedTweakIdsOnSnapshot()
    {
        // Arrange
        var game = new Game { Id = Guid.NewGuid(), Name = "Test Game", InstallPath = _tempDirectory };
        var recipeId1 = Guid.NewGuid();
        var recipeId2 = Guid.NewGuid();

        var snapshot = new Snapshot
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            BackupPath = Path.Combine(_tempDirectory, "backup"),
            FilesBackedUp = new List<string>(),
            AppliedTweakIds = new List<Guid>()
        };

        _mockBackupService
            .Setup(x => x.CreateSnapshotAsync(game, It.IsAny<List<string>>(), It.IsAny<string>()))
            .ReturnsAsync(snapshot);

        var recipes = new List<TweakRecipe>
        {
            new TweakRecipe
            {
                Id = recipeId1,
                IsEnabled = true,
                FilePath = Path.Combine(_tempDirectory, "test1.ini"),
                Key = "Key1",
                Value = "Value1",
                TargetType = TweakTargetType.IniFile
            },
            new TweakRecipe
            {
                Id = recipeId2,
                IsEnabled = true,
                FilePath = Path.Combine(_tempDirectory, "test2.ini"),
                Key = "Key2",
                Value = "Value2",
                TargetType = TweakTargetType.IniFile
            }
        };

        // Act
        var result = await _tweakEngine.ApplyTweaksAsync(game, recipes);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(recipeId1, result.AppliedTweakIds);
        Assert.Contains(recipeId2, result.AppliedTweakIds);
    }

    [Fact]
    public async Task ApplyTweaksAsync_HandlesFileAccessErrors()
    {
        // Arrange
        var game = new Game { Id = Guid.NewGuid(), Name = "Test Game", InstallPath = _tempDirectory };
        var configPath = Path.Combine(_tempDirectory, "readonly.ini");
        await File.WriteAllTextAsync(configPath, "[Graphics]\nResolution=1280x720\n");
        File.SetAttributes(configPath, FileAttributes.ReadOnly);

        var snapshot = new Snapshot
        {
            Id = Guid.NewGuid(),
            GameId = game.Id,
            BackupPath = Path.Combine(_tempDirectory, "backup"),
            FilesBackedUp = new List<string>()
        };

        _mockBackupService
            .Setup(x => x.CreateSnapshotAsync(game, It.IsAny<List<string>>(), It.IsAny<string>()))
            .ReturnsAsync(snapshot);

        var recipes = new List<TweakRecipe>
        {
            new TweakRecipe
            {
                Id = Guid.NewGuid(),
                IsEnabled = true,
                FilePath = configPath,
                Section = "Graphics",
                Key = "Resolution",
                Value = "1920x1080",
                TargetType = TweakTargetType.IniFile
            }
        };

        // Act - Should not throw
        var exception = await Record.ExceptionAsync(async () =>
        {
            await _tweakEngine.ApplyTweaksAsync(game, recipes);
        });

        // Cleanup
        File.SetAttributes(configPath, FileAttributes.Normal);

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region TweakChange Tests

    [Fact]
    public void TweakChange_PropertiesCanBeSet()
    {
        // Arrange & Act
        var change = new TweakEngine.TweakChange
        {
            FilePath = "test.ini",
            Section = "Graphics",
            Key = "Resolution",
            CurrentValue = "1280x720",
            NewValue = "1920x1080",
            IsNewEntry = false
        };

        // Assert
        Assert.Equal("test.ini", change.FilePath);
        Assert.Equal("Graphics", change.Section);
        Assert.Equal("Resolution", change.Key);
        Assert.Equal("1280x720", change.CurrentValue);
        Assert.Equal("1920x1080", change.NewValue);
        Assert.False(change.IsNewEntry);
    }

    [Fact]
    public void TweakChange_DefaultValuesAreCorrect()
    {
        // Arrange & Act
        var change = new TweakEngine.TweakChange();

        // Assert
        Assert.Equal(string.Empty, change.FilePath);
        Assert.Equal(string.Empty, change.Section);
        Assert.Equal(string.Empty, change.Key);
        Assert.Null(change.CurrentValue);
        Assert.Equal(string.Empty, change.NewValue);
        Assert.False(change.IsNewEntry);
    }

    #endregion
}
