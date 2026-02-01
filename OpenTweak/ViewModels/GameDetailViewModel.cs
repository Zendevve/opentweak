using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OpenTweak.Models;
using OpenTweak.Services;

namespace OpenTweak.ViewModels;

/// <summary>
/// ViewModel for the game detail view.
/// Shows tweaks and handles apply/revert operations.
/// </summary>
public partial class GameDetailViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    private readonly TweakEngine _tweakEngine;
    private readonly BackupService _backupService;
    private readonly PCGWService _pcgwService;

    [ObservableProperty]
    private Game? _game;

    [ObservableProperty]
    private ObservableCollection<TweakRecipe> _tweaks = new();

    [ObservableProperty]
    private ObservableCollection<Snapshot> _snapshots = new();

    [ObservableProperty]
    private ObservableCollection<TweakEngine.TweakChange> _pendingChanges = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isApplying;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _showDiffPreview;

    public GameDetailViewModel()
    {
        _databaseService = new DatabaseService();
        _backupService = new BackupService();
        _tweakEngine = new TweakEngine(_backupService);
        _pcgwService = new PCGWService();
    }

    /// <summary>
    /// Loads data for a specific game.
    /// </summary>
    public async Task LoadGameAsync(Game game)
    {
        Game = game;
        Tweaks.Clear();
        Snapshots.Clear();
        PendingChanges.Clear();

        try
        {
            IsLoading = true;

            // Load cached tweaks
            var cachedTweaks = _databaseService.GetRecipesForGame(game.Id);
            foreach (var tweak in cachedTweaks)
            {
                Tweaks.Add(tweak);
            }

            // If no cached tweaks, fetch from wiki
            if (!Tweaks.Any())
            {
                await FetchWikiTweaksAsync();
            }

            // Load snapshots
            var gameSnapshots = await _backupService.GetSnapshotsForGameAsync(game);
            foreach (var snapshot in gameSnapshots)
            {
                Snapshots.Add(snapshot);
            }

            StatusMessage = $"{Tweaks.Count} tweaks available";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Load failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Fetches tweaks from PCGW.
    /// </summary>
    [RelayCommand]
    private async Task FetchWikiTweaksAsync()
    {
        if (Game == null) return;

        try
        {
            IsLoading = true;
            StatusMessage = "Fetching from PCGW...";

            var title = Game.PCGWTitle ?? Game.Name;
            var recipes = await _pcgwService.GetTweaksForGameAsync(title, Game.Id);

            Tweaks.Clear();
            foreach (var recipe in recipes)
            {
                Tweaks.Add(recipe);
            }

            // Cache in database
            _databaseService.DeleteRecipesForGame(Game.Id);
            _databaseService.UpsertRecipes(recipes);

            StatusMessage = $"Found {recipes.Count} tweaks";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Fetch failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Previews what changes will be made (Diff view).
    /// </summary>
    [RelayCommand]
    private async Task PreviewChangesAsync()
    {
        if (Game == null) return;

        var enabledTweaks = Tweaks.Where(t => t.IsEnabled);
        var changes = await _tweakEngine.PreviewTweaksAsync(enabledTweaks);

        PendingChanges.Clear();
        foreach (var change in changes)
        {
            PendingChanges.Add(change);
        }

        ShowDiffPreview = true;
        StatusMessage = $"{changes.Count} changes will be made";
    }

    /// <summary>
    /// Applies all enabled tweaks with backup.
    /// THE BIG "OPTIMIZE" BUTTON!
    /// </summary>
    [RelayCommand]
    private async Task ApplyTweaksAsync()
    {
        if (Game == null) return;

        var enabledTweaks = Tweaks.Where(t => t.IsEnabled).ToList();
        if (!enabledTweaks.Any())
        {
            StatusMessage = "No tweaks selected";
            return;
        }

        try
        {
            IsApplying = true;
            StatusMessage = "Creating backup...";

            var snapshot = await _tweakEngine.ApplyTweaksAsync(Game, enabledTweaks);

            if (snapshot != null)
            {
                Snapshots.Insert(0, snapshot);

                // Update game's last tweaked date
                Game.LastTweakedDate = DateTime.UtcNow;
                _databaseService.UpsertGame(Game);
            }

            ShowDiffPreview = false;
            StatusMessage = $"Applied {enabledTweaks.Count} tweaks! Backup created.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Apply failed: {ex.Message}";
        }
        finally
        {
            IsApplying = false;
        }
    }

    /// <summary>
    /// Restores a snapshot (rollback).
    /// </summary>
    [RelayCommand]
    private async Task RestoreSnapshotAsync(Snapshot snapshot)
    {
        if (Game == null) return;

        try
        {
            IsApplying = true;
            StatusMessage = "Restoring backup...";

            var success = await _backupService.RestoreSnapshotAsync(snapshot, Game);

            if (success)
            {
                StatusMessage = $"Restored backup from {snapshot.Timestamp:g}";

                // Refresh snapshot list
                var index = Snapshots.IndexOf(snapshot);
                if (index >= 0)
                {
                    Snapshots[index] = snapshot; // Trigger UI update
                }
            }
            else
            {
                StatusMessage = "Restore failed - some files could not be restored";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Restore failed: {ex.Message}";
        }
        finally
        {
            IsApplying = false;
        }
    }

    /// <summary>
    /// Deletes a snapshot.
    /// </summary>
    [RelayCommand]
    private void DeleteSnapshot(Snapshot snapshot)
    {
        if (_backupService.DeleteSnapshot(snapshot))
        {
            Snapshots.Remove(snapshot);
            StatusMessage = "Snapshot deleted";
        }
    }

    /// <summary>
    /// Toggles a tweak's enabled state.
    /// </summary>
    [RelayCommand]
    private void ToggleTweak(TweakRecipe tweak)
    {
        tweak.IsEnabled = !tweak.IsEnabled;
        _databaseService.UpsertRecipe(tweak);

        var enabledCount = Tweaks.Count(t => t.IsEnabled);
        StatusMessage = $"{enabledCount} tweaks selected";
    }

    /// <summary>
    /// Enables all tweaks.
    /// </summary>
    [RelayCommand]
    private void EnableAllTweaks()
    {
        foreach (var tweak in Tweaks)
        {
            tweak.IsEnabled = true;
        }
        _databaseService.UpsertRecipes(Tweaks);
        StatusMessage = $"All {Tweaks.Count} tweaks enabled";
    }

    /// <summary>
    /// Disables all tweaks.
    /// </summary>
    [RelayCommand]
    private void DisableAllTweaks()
    {
        foreach (var tweak in Tweaks)
        {
            tweak.IsEnabled = false;
        }
        _databaseService.UpsertRecipes(Tweaks);
        StatusMessage = "All tweaks disabled";
    }
}
