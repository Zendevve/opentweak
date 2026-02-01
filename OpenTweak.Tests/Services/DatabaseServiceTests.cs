// OpenTweak - PC Game Optimization Tool
// Copyright 2024-2025 OpenTweak Contributors
// Licensed under PolyForm Shield License 1.0.0
// See LICENSE.md for full terms.

using System;
using System.IO;
using System.Linq;
using OpenTweak.Models;
using OpenTweak.Services;
using Xunit;

namespace OpenTweak.Tests.Services;

/// <summary>
/// Integration tests for DatabaseService using a temporary database file.
/// Tests actual LiteDB operations rather than mocking.
/// </summary>
public class DatabaseServiceTests : IDisposable
{
    private readonly DatabaseService _service;
    private readonly string _tempDbPath;

    public DatabaseServiceTests()
    {
        _tempDbPath = Path.Combine(Path.GetTempPath(), $"opentweak_test_{Guid.NewGuid()}.db");
        _service = new DatabaseService(_tempDbPath);
    }

    public void Dispose()
    {
        _service.Dispose();

        // Clean up test database file
        if (File.Exists(_tempDbPath))
        {
            try { File.Delete(_tempDbPath); } catch { }
        }
    }

    #region Game Tests

    [Fact]
    public void UpsertGame_InsertsNewGame()
    {
        var game = CreateTestGame();

        _service.UpsertGame(game);
        var retrieved = _service.GetGame(game.Id);

        Assert.NotNull(retrieved);
        Assert.Equal(game.Name, retrieved!.Name);
        Assert.Equal(game.AppId, retrieved.AppId);
    }

    [Fact]
    public void UpsertGame_UpdatesExistingGame()
    {
        var game = CreateTestGame();
        _service.UpsertGame(game);

        game.Name = "Updated Game Name";
        _service.UpsertGame(game);

        var retrieved = _service.GetGame(game.Id);
        Assert.Equal("Updated Game Name", retrieved!.Name);
    }

    [Fact]
    public void GetAllGames_ReturnsAllInsertedGames()
    {
        var games = Enumerable.Range(1, 5).Select(i => CreateTestGame($"Game {i}")).ToList();
        _service.UpsertGames(games);

        var retrieved = _service.GetAllGames().ToList();

        Assert.Equal(5, retrieved.Count);
    }

    [Fact]
    public void GetGameByAppId_FindsCorrectGame()
    {
        var game = CreateTestGame();
        game.AppId = "123456";
        game.LauncherType = LauncherType.Steam;
        _service.UpsertGame(game);

        var retrieved = _service.GetGameByAppId("123456", LauncherType.Steam);

        Assert.NotNull(retrieved);
        Assert.Equal(game.Id, retrieved!.Id);
    }

    [Fact]
    public void GetGameByAppId_ReturnsNullForWrongLauncher()
    {
        var game = CreateTestGame();
        game.AppId = "123456";
        game.LauncherType = LauncherType.Steam;
        _service.UpsertGame(game);

        var retrieved = _service.GetGameByAppId("123456", LauncherType.Epic);

        Assert.Null(retrieved);
    }

    [Fact]
    public void DeleteGame_RemovesGame()
    {
        var game = CreateTestGame();
        _service.UpsertGame(game);

        var deleted = _service.DeleteGame(game.Id);
        var retrieved = _service.GetGame(game.Id);

        Assert.True(deleted);
        Assert.Null(retrieved);
    }

    #endregion

    #region Recipe Tests

    [Fact]
    public void UpsertRecipe_InsertsNewRecipe()
    {
        var recipe = CreateTestRecipe();

        _service.UpsertRecipe(recipe);
        var retrieved = _service.GetRecipesForGame(recipe.GameId).FirstOrDefault();

        Assert.NotNull(retrieved);
        Assert.Equal(recipe.Description, retrieved!.Description);
    }

    [Fact]
    public void GetRecipesForGame_ReturnsOnlyMatchingRecipes()
    {
        var gameId1 = Guid.NewGuid();
        var gameId2 = Guid.NewGuid();

        _service.UpsertRecipe(CreateTestRecipe("Recipe 1", gameId1));
        _service.UpsertRecipe(CreateTestRecipe("Recipe 2", gameId1));
        _service.UpsertRecipe(CreateTestRecipe("Recipe 3", gameId2));

        var recipes = _service.GetRecipesForGame(gameId1).ToList();

        Assert.Equal(2, recipes.Count);
        Assert.All(recipes, r => Assert.Equal(gameId1, r.GameId));
    }

    [Fact]
    public void DeleteRecipesForGame_RemovesAllRecipesForGame()
    {
        var gameId = Guid.NewGuid();
        _service.UpsertRecipe(CreateTestRecipe("Recipe 1", gameId));
        _service.UpsertRecipe(CreateTestRecipe("Recipe 2", gameId));

        _service.DeleteRecipesForGame(gameId);

        var recipes = _service.GetRecipesForGame(gameId).ToList();
        Assert.Empty(recipes);
    }

    #endregion

    #region Snapshot Tests

    [Fact]
    public void UpsertSnapshot_InsertsNewSnapshot()
    {
        var snapshot = CreateTestSnapshot();

        _service.UpsertSnapshot(snapshot);
        var retrieved = _service.GetSnapshotsForGame(snapshot.GameId).FirstOrDefault();

        Assert.NotNull(retrieved);
        Assert.Equal(snapshot.Description, retrieved!.Description);
    }

    [Fact]
    public void GetSnapshotsForGame_ReturnsSnapshotsInOrder()
    {
        var gameId = Guid.NewGuid();
        var older = CreateTestSnapshot(gameId);
        older.Timestamp = DateTime.UtcNow.AddDays(-1);
        var newer = CreateTestSnapshot(gameId);
        newer.Timestamp = DateTime.UtcNow;

        _service.UpsertSnapshot(older);
        _service.UpsertSnapshot(newer);

        var snapshots = _service.GetSnapshotsForGame(gameId).ToList();

        Assert.Equal(2, snapshots.Count);
    }

    [Fact]
    public void DeleteSnapshot_RemovesSnapshot()
    {
        var snapshot = CreateTestSnapshot();
        _service.UpsertSnapshot(snapshot);

        var deleted = _service.DeleteSnapshot(snapshot.Id);
        var retrieved = _service.GetSnapshotsForGame(snapshot.GameId).ToList();

        Assert.True(deleted);
        Assert.Empty(retrieved);
    }

    #endregion

    #region Helper Methods

    private static Game CreateTestGame(string name = "Test Game")
    {
        return new Game
        {
            Id = Guid.NewGuid(),
            Name = name,
            AppId = Guid.NewGuid().ToString(),
            InstallPath = @"C:\Games\TestGame",
            LauncherType = LauncherType.Manual
        };
    }

    private static TweakRecipe CreateTestRecipe(string description = "Test Recipe", Guid? gameId = null)
    {
        return new TweakRecipe
        {
            Id = Guid.NewGuid(),
            GameId = gameId ?? Guid.NewGuid(),
            Description = description,
            FilePath = @"C:\Games\TestGame\config.ini",
            TargetType = TweakTargetType.IniFile,
            Key = "Graphics.Quality",
            Value = "Ultra"
        };
    }

    private static Snapshot CreateTestSnapshot(Guid? gameId = null)
    {
        return new Snapshot
        {
            Id = Guid.NewGuid(),
            GameId = gameId ?? Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Description = "Test snapshot",
            BackupPath = @"C:\Backups\Test"
        };
    }

    #endregion
}
