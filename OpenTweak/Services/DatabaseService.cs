using System.IO;
using LiteDB;
using OpenTweak.Models;

namespace OpenTweak.Services;

/// <summary>
/// Database service using LiteDB for persistent storage.
/// Single-file NoSQL database - no external dependencies.
/// </summary>
public class DatabaseService : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<Game> _games;
    private readonly ILiteCollection<TweakRecipe> _recipes;
    private readonly ILiteCollection<Snapshot> _snapshots;

    public DatabaseService()
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenTweak", "opentweaks.db");

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        _db = new LiteDatabase(dbPath);
        _games = _db.GetCollection<Game>("games");
        _recipes = _db.GetCollection<TweakRecipe>("recipes");
        _snapshots = _db.GetCollection<Snapshot>("snapshots");

        // Create indexes for faster lookup
        _games.EnsureIndex(g => g.Name);
        _games.EnsureIndex(g => g.AppId);
        _recipes.EnsureIndex(r => r.GameId);
        _snapshots.EnsureIndex(s => s.GameId);
    }

    #region Games

    public IEnumerable<Game> GetAllGames() => _games.FindAll();

    public Game? GetGame(Guid id) => _games.FindById(id);

    public Game? GetGameByAppId(string appId, LauncherType launcher) =>
        _games.FindOne(g => g.AppId == appId && g.LauncherType == launcher);

    public void UpsertGame(Game game) => _games.Upsert(game);

    public void UpsertGames(IEnumerable<Game> games)
    {
        foreach (var game in games)
            _games.Upsert(game);
    }

    public bool DeleteGame(Guid id) => _games.Delete(id);

    #endregion

    #region Recipes

    public IEnumerable<TweakRecipe> GetRecipesForGame(Guid gameId) =>
        _recipes.Find(r => r.GameId == gameId);

    public void UpsertRecipe(TweakRecipe recipe) => _recipes.Upsert(recipe);

    public void UpsertRecipes(IEnumerable<TweakRecipe> recipes)
    {
        foreach (var recipe in recipes)
            _recipes.Upsert(recipe);
    }

    public bool DeleteRecipe(Guid id) => _recipes.Delete(id);

    public void DeleteRecipesForGame(Guid gameId)
    {
        _recipes.DeleteMany(r => r.GameId == gameId);
    }

    #endregion

    #region Snapshots

    public IEnumerable<Snapshot> GetSnapshotsForGame(Guid gameId) =>
        _snapshots.Find(s => s.GameId == gameId).OrderByDescending(s => s.Timestamp);

    public void UpsertSnapshot(Snapshot snapshot) => _snapshots.Upsert(snapshot);

    public bool DeleteSnapshot(Guid id) => _snapshots.Delete(id);

    #endregion

    public void Dispose()
    {
        _db.Dispose();
    }
}
