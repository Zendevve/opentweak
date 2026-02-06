// OpenTweak - PC Game Optimization Tool
// Copyright 2024-2025 OpenTweak Contributors
// Licensed under PolyForm Shield License 1.0.0
// See LICENSE.md for full terms.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Moq;
using Microsoft.Extensions.Logging;
using OpenTweak.Models;
using OpenTweak.Services;
using Xunit;

namespace OpenTweak.Tests.Services;

/// <summary>
/// Tests for TweakEngine validation methods and JSON/XML edge cases.
/// These tests address gaps identified in architectural review:
/// - Registry recipe filtering
/// - JSON/XML file creation safety
/// - Validation stage behavior
/// </summary>
public class TweakEngineValidationTests : IDisposable
{
    private readonly Mock<IBackupService> _mockBackupService;
    private readonly TweakEngine _tweakEngine;
    private readonly string _tempDirectory;

    public TweakEngineValidationTests()
    {
        _mockBackupService = new Mock<IBackupService>();
        var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<TweakEngine>>();
        _tweakEngine = new TweakEngine(_mockBackupService.Object, mockLogger.Object);
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

    #region ValidateRecipe Tests

    [Fact]
    public void ValidateRecipe_RegistryTarget_SetsUnsupported()
    {
        // Arrange
        var recipe = new TweakRecipe
        {
            TargetType = TweakTargetType.Registry,
            Key = "TestKey",
            FilePath = "HKEY_CURRENT_USER\\Software\\Test",
            Value = "1"
        };

        // Act
        var result = _tweakEngine.ValidateRecipe(recipe);

        // Assert
        Assert.False(result.IsSupported);
        Assert.Contains("Registry", result.UnsupportedReason);
    }

    [Fact]
    public void ValidateRecipe_IniTarget_SetsSupported()
    {
        // Arrange
        var configPath = Path.Combine(_tempDirectory, "test.ini");
        File.WriteAllText(configPath, "[Section]\nKey=Value\n");

        var recipe = new TweakRecipe
        {
            TargetType = TweakTargetType.IniFile,
            Key = "TestKey",
            FilePath = configPath,
            Section = "Section",
            Value = "NewValue"
        };

        // Act
        var result = _tweakEngine.ValidateRecipe(recipe);

        // Assert
        Assert.True(result.IsSupported);
        Assert.Null(result.UnsupportedReason);
    }

    [Fact]
    public void ValidateRecipe_MissingFile_WithoutAllowCreation_SetsUnsupported()
    {
        // Arrange
        var recipe = new TweakRecipe
        {
            TargetType = TweakTargetType.IniFile,
            Key = "TestKey",
            FilePath = Path.Combine(_tempDirectory, "nonexistent.ini"),
            AllowFileCreation = false,
            Value = "NewValue"
        };

        // Act
        var result = _tweakEngine.ValidateRecipe(recipe);

        // Assert
        Assert.False(result.IsSupported);
        Assert.Contains("does not exist", result.UnsupportedReason);
    }

    [Fact]
    public void ValidateRecipe_MissingFile_WithAllowCreation_SetsSupported()
    {
        // Arrange
        var recipe = new TweakRecipe
        {
            TargetType = TweakTargetType.IniFile,
            Key = "TestKey",
            FilePath = Path.Combine(_tempDirectory, "nonexistent.ini"),
            AllowFileCreation = true,
            Value = "NewValue"
        };

        // Act
        var result = _tweakEngine.ValidateRecipe(recipe);

        // Assert
        Assert.True(result.IsSupported);
        Assert.Null(result.UnsupportedReason);
    }

    [Fact]
    public void ValidateRecipe_EmptyKey_SetsUnsupported()
    {
        // Arrange
        var configPath = Path.Combine(_tempDirectory, "test.ini");
        File.WriteAllText(configPath, "[Section]\nKey=Value\n");

        var recipe = new TweakRecipe
        {
            TargetType = TweakTargetType.IniFile,
            Key = "",
            FilePath = configPath,
            Value = "NewValue"
        };

        // Act
        var result = _tweakEngine.ValidateRecipe(recipe);

        // Assert
        Assert.False(result.IsSupported);
        Assert.Contains("no key", result.UnsupportedReason);
    }

    #endregion

    #region FilterSupportedRecipes Tests

    [Fact]
    public void FilterSupportedRecipes_MixedList_PartitionsCorrectly()
    {
        // Arrange
        var configPath = Path.Combine(_tempDirectory, "test.ini");
        File.WriteAllText(configPath, "[Section]\nKey=Value\n");

        var recipes = new List<TweakRecipe>
        {
            // Supported - INI with existing file
            new TweakRecipe
            {
                TargetType = TweakTargetType.IniFile,
                Key = "Key1",
                FilePath = configPath,
                Value = "Value1"
            },
            // Unsupported - Registry
            new TweakRecipe
            {
                TargetType = TweakTargetType.Registry,
                Key = "RegKey",
                FilePath = "HKEY_CURRENT_USER\\Test",
                Value = "1"
            },
            // Supported - INI with AllowFileCreation
            new TweakRecipe
            {
                TargetType = TweakTargetType.IniFile,
                Key = "Key2",
                FilePath = Path.Combine(_tempDirectory, "new.ini"),
                AllowFileCreation = true,
                Value = "Value2"
            },
            // Unsupported - missing file without AllowFileCreation
            new TweakRecipe
            {
                TargetType = TweakTargetType.IniFile,
                Key = "Key3",
                FilePath = Path.Combine(_tempDirectory, "missing.ini"),
                AllowFileCreation = false,
                Value = "Value3"
            }
        };

        // Act
        var (supported, unsupported) = _tweakEngine.FilterSupportedRecipes(recipes);

        // Assert
        Assert.Equal(2, supported.Count);
        Assert.Equal(2, unsupported.Count);

        // Verify Registry is in unsupported
        Assert.Contains(unsupported, r => r.TargetType == TweakTargetType.Registry);

        // Verify existing file is in supported
        Assert.Contains(supported, r => r.Key == "Key1");
    }

    [Fact]
    public void FilterSupportedRecipes_AllRegistry_ReturnsEmptySupported()
    {
        // Arrange
        var recipes = new List<TweakRecipe>
        {
            new TweakRecipe { TargetType = TweakTargetType.Registry, Key = "Key1", FilePath = "HKEY_LOCAL_MACHINE\\Test", Value = "1" },
            new TweakRecipe { TargetType = TweakTargetType.Registry, Key = "Key2", FilePath = "HKEY_CURRENT_USER\\Test", Value = "2" }
        };

        // Act
        var (supported, unsupported) = _tweakEngine.FilterSupportedRecipes(recipes);

        // Assert
        Assert.Empty(supported);
        Assert.Equal(2, unsupported.Count);
    }

    [Fact]
    public void FilterSupportedRecipes_EmptyList_ReturnsEmptyLists()
    {
        // Arrange
        var recipes = new List<TweakRecipe>();

        // Act
        var (supported, unsupported) = _tweakEngine.FilterSupportedRecipes(recipes);

        // Assert
        Assert.Empty(supported);
        Assert.Empty(unsupported);
    }

    #endregion

    #region JSON Edge Case Tests

    [Fact]
    public async Task ApplyTweaksAsync_JsonFile_PreservesExistingKeys()
    {
        // Arrange
        var game = new Game { Id = Guid.NewGuid(), Name = "Test Game", InstallPath = _tempDirectory };
        var configPath = Path.Combine(_tempDirectory, "config.json");
        await File.WriteAllTextAsync(configPath, "{ \"Existing\": \"Value\", \"Graphics\": { \"MotionBlur\": true } }");

        var snapshot = new Snapshot { Id = Guid.NewGuid(), GameId = game.Id, BackupPath = Path.Combine(_tempDirectory, "backup"), FilesBackedUp = new List<string>() };
        _mockBackupService.Setup(x => x.CreateSnapshotAsync(game, It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        var recipes = new List<TweakRecipe>
        {
            new TweakRecipe
            {
                Id = Guid.NewGuid(),
                IsEnabled = true,
                FilePath = configPath,
                Section = "Graphics",
                Key = "MotionBlur",
                Value = "false",
                TargetType = TweakTargetType.JsonFile
            }
        };

        // Act
        await _tweakEngine.ApplyTweaksAsync(game, recipes);

        // Assert
        var content = await File.ReadAllTextAsync(configPath);
        Assert.Contains("\"Existing\"", content); // Original key preserved
        Assert.Contains("false", content); // New value applied
    }

    [Fact]
    public async Task ApplyTweaksAsync_JsonFile_NestedSection_CreatesIntermediateObjects()
    {
        // Arrange
        var game = new Game { Id = Guid.NewGuid(), Name = "Test Game", InstallPath = _tempDirectory };
        var configPath = Path.Combine(_tempDirectory, "config.json");
        await File.WriteAllTextAsync(configPath, "{ }");

        var snapshot = new Snapshot { Id = Guid.NewGuid(), GameId = game.Id, BackupPath = Path.Combine(_tempDirectory, "backup"), FilesBackedUp = new List<string>() };
        _mockBackupService.Setup(x => x.CreateSnapshotAsync(game, It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        var recipes = new List<TweakRecipe>
        {
            new TweakRecipe
            {
                Id = Guid.NewGuid(),
                IsEnabled = true,
                FilePath = configPath,
                Section = "Graphics.Advanced.PostProcessing",
                Key = "MotionBlur",
                Value = "false",
                TargetType = TweakTargetType.JsonFile
            }
        };

        // Act
        var result = await _tweakEngine.ApplyTweaksAsync(game, recipes);

        // Assert
        Assert.True(result.AllSucceeded, string.Join(", ", result.FailedTweaks.Select(f => f.ErrorMessage)));
        var content = await File.ReadAllTextAsync(configPath);
        Assert.Contains("Graphics", content);
        Assert.Contains("Advanced", content);
        Assert.Contains("PostProcessing", content);
        Assert.Contains("MotionBlur", content);
    }

    [Fact]
    public async Task ApplyTweaksAsync_JsonFile_TypeCoercion_BooleanValue()
    {
        // Arrange
        var game = new Game { Id = Guid.NewGuid(), Name = "Test Game", InstallPath = _tempDirectory };
        var configPath = Path.Combine(_tempDirectory, "config.json");
        await File.WriteAllTextAsync(configPath, "{ \"Settings\": { } }");

        var snapshot = new Snapshot { Id = Guid.NewGuid(), GameId = game.Id, BackupPath = Path.Combine(_tempDirectory, "backup"), FilesBackedUp = new List<string>() };
        _mockBackupService.Setup(x => x.CreateSnapshotAsync(game, It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        var recipes = new List<TweakRecipe>
        {
            new TweakRecipe
            {
                Id = Guid.NewGuid(),
                IsEnabled = true,
                FilePath = configPath,
                Section = "Settings",
                Key = "EnableFeature",
                Value = "true", // String that should be coerced to boolean
                TargetType = TweakTargetType.JsonFile
            }
        };

        // Act
        await _tweakEngine.ApplyTweaksAsync(game, recipes);

        // Assert
        var content = await File.ReadAllTextAsync(configPath);
        // Should be JSON boolean true, not string "true"
        Assert.Contains(": true", content);
        Assert.DoesNotContain("\"true\"", content);
    }

    [Fact]
    public async Task ApplyTweaksAsync_JsonFile_InvalidExisting_ThrowsWithClearError()
    {
        // Arrange
        var game = new Game { Id = Guid.NewGuid(), Name = "Test Game", InstallPath = _tempDirectory };
        var configPath = Path.Combine(_tempDirectory, "invalid.json");
        await File.WriteAllTextAsync(configPath, "{ invalid json content");

        var snapshot = new Snapshot { Id = Guid.NewGuid(), GameId = game.Id, BackupPath = Path.Combine(_tempDirectory, "backup"), FilesBackedUp = new List<string>() };
        _mockBackupService.Setup(x => x.CreateSnapshotAsync(game, It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        var recipes = new List<TweakRecipe>
        {
            new TweakRecipe
            {
                Id = Guid.NewGuid(),
                IsEnabled = true,
                FilePath = configPath,
                Key = "Setting",
                Value = "Value",
                TargetType = TweakTargetType.JsonFile
            }
        };

        // Act
        var result = await _tweakEngine.ApplyTweaksAsync(game, recipes);

        // Assert
        Assert.False(result.AllSucceeded);
        Assert.Single(result.FailedTweaks);
        Assert.Contains("invalid", result.FailedTweaks[0].ErrorMessage.ToLower());
    }

    #endregion

    #region XML Edge Case Tests

    [Fact]
    public async Task ApplyTweaksAsync_XmlFile_AttributeSyntax_SetsCorrectly()
    {
        // Arrange
        var game = new Game { Id = Guid.NewGuid(), Name = "Test Game", InstallPath = _tempDirectory };
        var configPath = Path.Combine(_tempDirectory, "config.xml");
        await File.WriteAllTextAsync(configPath, "<Configuration><Graphics/></Configuration>");

        var snapshot = new Snapshot { Id = Guid.NewGuid(), GameId = game.Id, BackupPath = Path.Combine(_tempDirectory, "backup"), FilesBackedUp = new List<string>() };
        _mockBackupService.Setup(x => x.CreateSnapshotAsync(game, It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        var recipes = new List<TweakRecipe>
        {
            new TweakRecipe
            {
                Id = Guid.NewGuid(),
                IsEnabled = true,
                FilePath = configPath,
                Section = "Graphics",
                Key = "@enabled", // @ prefix for attributes
                Value = "true",
                TargetType = TweakTargetType.XmlFile
            }
        };

        // Act
        var result = await _tweakEngine.ApplyTweaksAsync(game, recipes);

        // Assert
        Assert.True(result.AllSucceeded, string.Join(", ", result.FailedTweaks.Select(f => f.ErrorMessage)));
        var content = await File.ReadAllTextAsync(configPath);
        Assert.Contains("enabled=\"true\"", content);
    }

    [Fact]
    public async Task ApplyTweaksAsync_XmlFile_NestedPath_CreatesElements()
    {
        // Arrange
        var game = new Game { Id = Guid.NewGuid(), Name = "Test Game", InstallPath = _tempDirectory };
        var configPath = Path.Combine(_tempDirectory, "config.xml");
        await File.WriteAllTextAsync(configPath, "<Configuration></Configuration>");

        var snapshot = new Snapshot { Id = Guid.NewGuid(), GameId = game.Id, BackupPath = Path.Combine(_tempDirectory, "backup"), FilesBackedUp = new List<string>() };
        _mockBackupService.Setup(x => x.CreateSnapshotAsync(game, It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        var recipes = new List<TweakRecipe>
        {
            new TweakRecipe
            {
                Id = Guid.NewGuid(),
                IsEnabled = true,
                FilePath = configPath,
                Section = "Graphics/Advanced/PostProcessing",
                Key = "MotionBlur",
                Value = "false",
                TargetType = TweakTargetType.XmlFile
            }
        };

        // Act
        var result = await _tweakEngine.ApplyTweaksAsync(game, recipes);

        // Assert
        Assert.True(result.AllSucceeded, string.Join(", ", result.FailedTweaks.Select(f => f.ErrorMessage)));
        var content = await File.ReadAllTextAsync(configPath);
        Assert.Contains("<Graphics>", content);
        Assert.Contains("<Advanced>", content);
        Assert.Contains("<PostProcessing>", content);
        Assert.Contains("<MotionBlur>false</MotionBlur>", content);
    }

    [Fact]
    public async Task ApplyTweaksAsync_XmlFile_PreservesExistingNodes()
    {
        // Arrange
        var game = new Game { Id = Guid.NewGuid(), Name = "Test Game", InstallPath = _tempDirectory };
        var configPath = Path.Combine(_tempDirectory, "config.xml");
        await File.WriteAllTextAsync(configPath, "<Configuration><ExistingNode>Keep Me</ExistingNode><Graphics><OtherSetting>Value</OtherSetting></Graphics></Configuration>");

        var snapshot = new Snapshot { Id = Guid.NewGuid(), GameId = game.Id, BackupPath = Path.Combine(_tempDirectory, "backup"), FilesBackedUp = new List<string>() };
        _mockBackupService.Setup(x => x.CreateSnapshotAsync(game, It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        var recipes = new List<TweakRecipe>
        {
            new TweakRecipe
            {
                Id = Guid.NewGuid(),
                IsEnabled = true,
                FilePath = configPath,
                Section = "Graphics",
                Key = "MotionBlur",
                Value = "false",
                TargetType = TweakTargetType.XmlFile
            }
        };

        // Act
        await _tweakEngine.ApplyTweaksAsync(game, recipes);

        // Assert
        var content = await File.ReadAllTextAsync(configPath);
        Assert.Contains("ExistingNode", content);
        Assert.Contains("Keep Me", content);
        Assert.Contains("OtherSetting", content);
        Assert.Contains("MotionBlur", content);
    }

    [Fact]
    public async Task ApplyTweaksAsync_XmlFile_InvalidExisting_ThrowsWithClearError()
    {
        // Arrange
        var game = new Game { Id = Guid.NewGuid(), Name = "Test Game", InstallPath = _tempDirectory };
        var configPath = Path.Combine(_tempDirectory, "invalid.xml");
        await File.WriteAllTextAsync(configPath, "<Configuration><Unclosed>");

        var snapshot = new Snapshot { Id = Guid.NewGuid(), GameId = game.Id, BackupPath = Path.Combine(_tempDirectory, "backup"), FilesBackedUp = new List<string>() };
        _mockBackupService.Setup(x => x.CreateSnapshotAsync(game, It.IsAny<List<string>>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(snapshot);

        var recipes = new List<TweakRecipe>
        {
            new TweakRecipe
            {
                Id = Guid.NewGuid(),
                IsEnabled = true,
                FilePath = configPath,
                Key = "Setting",
                Value = "Value",
                TargetType = TweakTargetType.XmlFile
            }
        };

        // Act
        var result = await _tweakEngine.ApplyTweaksAsync(game, recipes);

        // Assert
        Assert.False(result.AllSucceeded);
        Assert.Single(result.FailedTweaks);
        Assert.Contains("invalid", result.FailedTweaks[0].ErrorMessage.ToLower());
    }

    #endregion
}
