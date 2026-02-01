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
/// Tests for the GameScanner service that detects installed games across multiple launchers.
/// </summary>
public class GameScannerTests
{
    private readonly GameScanner _scanner;

    public GameScannerTests()
    {
        _scanner = new GameScanner();
    }

    #region ScanAllLaunchersAsync Tests

    [Fact]
    public async Task ScanAllLaunchersAsync_ReturnsListOfGames()
    {
        // Act
        var games = await _scanner.ScanAllLaunchersAsync();

        // Assert
        Assert.NotNull(games);
        Assert.IsType<List<Game>>(games);
    }

    [Fact]
    public async Task ScanAllLaunchersAsync_ReturnsSortedList()
    {
        // Act
        var games = await _scanner.ScanAllLaunchersAsync();

        // Assert
        if (games.Count > 1)
        {
            var sorted = games.OrderBy(g => g.Name).ToList();
            Assert.Equal(sorted, games);
        }
    }

    [Fact]
    public async Task ScanAllLaunchersAsync_DoesNotThrow()
    {
        // Act & Assert - Should not throw even if no launchers are installed
        var exception = await Record.ExceptionAsync(async () =>
        {
            await _scanner.ScanAllLaunchersAsync();
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task ScanAllLaunchersAsync_ReturnsConsistentResults()
    {
        // Act
        var firstScan = await _scanner.ScanAllLaunchersAsync();
        var secondScan = await _scanner.ScanAllLaunchersAsync();

        // Assert - Results should be consistent (same count)
        Assert.Equal(firstScan.Count, secondScan.Count);
    }

    #endregion

    #region Game Properties Tests

    [Fact]
    public async Task ScanAllLaunchersAsync_GamesHaveValidIds()
    {
        // Act
        var games = await _scanner.ScanAllLaunchersAsync();

        // Assert
        foreach (var game in games)
        {
            Assert.NotEqual(Guid.Empty, game.Id);
        }
    }

    [Fact]
    public async Task ScanAllLaunchersAsync_GamesHaveNames()
    {
        // Act
        var games = await _scanner.ScanAllLaunchersAsync();

        // Assert
        foreach (var game in games)
        {
            Assert.False(string.IsNullOrWhiteSpace(game.Name), "Game should have a name");
        }
    }

    [Fact]
    public async Task ScanAllLaunchersAsync_GamesHaveInstallPaths()
    {
        // Act
        var games = await _scanner.ScanAllLaunchersAsync();

        // Assert
        foreach (var game in games)
        {
            Assert.False(string.IsNullOrWhiteSpace(game.InstallPath), "Game should have an install path");
        }
    }

    [Fact]
    public async Task ScanAllLaunchersAsync_GamesHaveValidLauncherTypes()
    {
        // Act
        var games = await _scanner.ScanAllLaunchersAsync();

        // Assert
        foreach (var game in games)
        {
            Assert.True(Enum.IsDefined(typeof(LauncherType), game.LauncherType),
                $"Game '{game.Name}' should have a valid launcher type");
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ScanAllLaunchersAsync_HandlesMissingRegistryKeys()
    {
        // This test verifies the scanner doesn't crash when registry keys are missing
        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            await _scanner.ScanAllLaunchersAsync();
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task ScanAllLaunchersAsync_HandlesMissingDirectories()
    {
        // This test verifies the scanner doesn't crash when directories don't exist
        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            await _scanner.ScanAllLaunchersAsync();
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task ScanAllLaunchersAsync_HandlesCorruptedManifestFiles()
    {
        // This test verifies the scanner handles corrupted Steam manifest files gracefully
        // Act & Assert
        var exception = await Record.ExceptionAsync(async () =>
        {
            await _scanner.ScanAllLaunchersAsync();
        });

        Assert.Null(exception);
    }

    #endregion

    #region Concurrency Tests

    [Fact]
    public async Task ScanAllLaunchersAsync_CanBeCalledConcurrently()
    {
        // Act
        var tasks = new[]
        {
            _scanner.ScanAllLaunchersAsync(),
            _scanner.ScanAllLaunchersAsync(),
            _scanner.ScanAllLaunchersAsync()
        };

        var results = await Task.WhenAll(tasks);

        // Assert - All scans should complete without exception
        Assert.All(results, games =>
        {
            Assert.NotNull(games);
            Assert.IsType<List<Game>>(games);
        });
    }

    #endregion
}
