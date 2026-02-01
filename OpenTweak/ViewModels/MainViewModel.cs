using System.IO;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenTweak.Models;
using OpenTweak.Services;

namespace OpenTweak.ViewModels;

/// <summary>
/// Main ViewModel for the application.
/// Handles game list, scanning, and navigation.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly GameScanner _gameScanner;
    private readonly DatabaseService _databaseService;
    private readonly PCGWService _pcgwService;

    [ObservableProperty]
    private ObservableCollection<Game> _games = new();

    [ObservableProperty]
    private Game? _selectedGame;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    public MainViewModel()
    {
        _gameScanner = new GameScanner();
        _databaseService = new DatabaseService();
        _pcgwService = new PCGWService();

        // Load cached games on startup
        LoadCachedGames();
    }

    /// <summary>
    /// Loads games from the local database cache.
    /// </summary>
    private void LoadCachedGames()
    {
        var cachedGames = _databaseService.GetAllGames();
        foreach (var game in cachedGames.OrderBy(g => g.Name))
        {
            Games.Add(game);
        }
    }

    /// <summary>
    /// Scans all launchers for installed games.
    /// </summary>
    [RelayCommand]
    private async Task ScanForGamesAsync()
    {
        if (IsScanning) return;

        try
        {
            IsScanning = true;
            StatusMessage = "Scanning for games...";

            var detectedGames = await _gameScanner.ScanAllLaunchersAsync();

            // Merge with existing games (don't overwrite user-edited data)
            foreach (var game in detectedGames)
            {
                var existing = _databaseService.GetGameByAppId(game.AppId ?? "", game.LauncherType);

                if (existing == null)
                {
                    _databaseService.UpsertGame(game);
                    Games.Add(game);
                }
                else
                {
                    // Update install path if it changed
                    existing.InstallPath = game.InstallPath;
                    _databaseService.UpsertGame(existing);
                }
            }

            StatusMessage = $"Found {detectedGames.Count} games";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsScanning = false;
        }
    }

    /// <summary>
    /// Adds a game manually by path.
    /// </summary>
    [RelayCommand]
    private void AddManualGame(string path)
    {
        if (!Directory.Exists(path)) return;

        var name = Path.GetFileName(path);
        var game = _gameScanner.AddManualGame(name, path);

        _databaseService.UpsertGame(game);
        Games.Add(game);

        StatusMessage = $"Added {name}";
    }

    /// <summary>
    /// Removes a game from the library (doesn't uninstall).
    /// </summary>
    [RelayCommand]
    private void RemoveGame(Game game)
    {
        _databaseService.DeleteGame(game.Id);
        Games.Remove(game);

        if (SelectedGame == game)
            SelectedGame = null;

        StatusMessage = $"Removed {game.Name}";
    }

    /// <summary>
    /// Refreshes wiki data for the selected game.
    /// </summary>
    [RelayCommand]
    private async Task RefreshWikiDataAsync()
    {
        if (SelectedGame == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = $"Fetching wiki data for {SelectedGame.Name}...";

            var title = SelectedGame.PCGWTitle ?? SelectedGame.Name;
            var recipes = await _pcgwService.GetAvailableTweaksAsync(title, SelectedGame.Id);

            // Save to database
            _databaseService.DeleteRecipesForGame(SelectedGame.Id);
            _databaseService.UpsertRecipes(recipes);

            StatusMessage = $"Found {recipes.Count} tweaks for {SelectedGame.Name}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Wiki fetch failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Filters games by search query.
    /// </summary>
    partial void OnSearchQueryChanged(string value)
    {
        // In a real app, this would filter the observable collection
        // For now, just show how many match
        if (string.IsNullOrWhiteSpace(value))
        {
            StatusMessage = $"{Games.Count} games";
        }
        else
        {
            var matches = Games.Count(g => g.Name.Contains(value, StringComparison.OrdinalIgnoreCase));
            StatusMessage = $"{matches} games match '{value}'";
        }
    }
}
