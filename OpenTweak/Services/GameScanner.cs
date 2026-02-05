// OpenTweak - PC Game Optimization Tool
// Copyright 2024-2025 OpenTweak Contributors
// Licensed under PolyForm Shield License 1.0.0
// See LICENSE.md for full terms.

using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using OpenTweak.Models;

namespace OpenTweak.Services;

/// <summary>
/// Scans for installed games across multiple launchers (Steam, Epic, GOG, Xbox, Manual).
/// Returns a unified list of Game objects regardless of source.
/// </summary>
public class GameScanner : IGameScanner
{
    private readonly List<Game> _detectedGames = new();

    /// <summary>
    /// Scans all supported launchers and returns detected games.
    /// </summary>
    public async Task<List<Game>> ScanAllLaunchersAsync(CancellationToken cancellationToken = default)
    {
        _detectedGames.Clear();

        if (cancellationToken.IsCancellationRequested)
            return _detectedGames;

        var tasks = new List<Task>
        {
            Task.Run(() => ScanSteam(cancellationToken), cancellationToken),
            Task.Run(() => ScanEpicGames(cancellationToken), cancellationToken),
            Task.Run(() => ScanGOG(cancellationToken), cancellationToken),
            Task.Run(() => ScanXbox(cancellationToken), cancellationToken)
        };

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (OperationCanceledException)
        {
            // Ignore cancellation exceptions
        }

        return _detectedGames.OrderBy(g => g.Name).ToList();
    }

    #region Steam Scanner

    /// <summary>
    /// Scans Steam libraries by parsing libraryfolders.vdf and appmanifest files.
    /// </summary>
    private void ScanSteam(CancellationToken cancellationToken)
    {
        try
        {
            var steamPaths = GetSteamLibraryPaths();

            foreach (var libraryPath in steamPaths)
            {
                if (cancellationToken.IsCancellationRequested) return;

                var steamAppsPath = Path.Combine(libraryPath, "steamapps");
                if (!Directory.Exists(steamAppsPath)) continue;

                var manifestFiles = Directory.GetFiles(steamAppsPath, "appmanifest_*.acf");

                foreach (var manifest in manifestFiles)
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    var game = ParseSteamManifest(manifest, steamAppsPath);
                    if (game != null)
                    {
                        lock (_detectedGames)
                        {
                            _detectedGames.Add(game);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Steam scan error: {ex.Message}");
        }
    }

    private List<string> GetSteamLibraryPaths()
    {
        var paths = new List<string>();

        // Find Steam install path from registry
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Valve\Steam");
            var steamPath = key?.GetValue("SteamPath") as string;

            if (!string.IsNullOrEmpty(steamPath))
            {
                paths.Add(steamPath);

                // Parse libraryfolders.vdf for additional library paths
                var libraryFoldersPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                if (File.Exists(libraryFoldersPath))
                {
                    var content = File.ReadAllText(libraryFoldersPath);
                    // Match "path" values in VDF format
                    var pathMatches = Regex.Matches(content, @"""path""\s+""([^""]+)""", RegexOptions.IgnoreCase);

                    foreach (Match match in pathMatches)
                    {
                        var libPath = match.Groups[1].Value.Replace(@"\\", @"\");
                        if (Directory.Exists(libPath) && !paths.Contains(libPath))
                        {
                            paths.Add(libPath);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to read Steam library folders from registry: {ex.Message}");
        }

        return paths;
    }

    private Game? ParseSteamManifest(string manifestPath, string steamAppsPath)
    {
        try
        {
            var content = File.ReadAllText(manifestPath);

            var appIdMatch = Regex.Match(content, @"""appid""\s+""(\d+)""");
            var nameMatch = Regex.Match(content, @"""name""\s+""([^""]+)""");
            var installDirMatch = Regex.Match(content, @"""installdir""\s+""([^""]+)""");

            if (!appIdMatch.Success || !nameMatch.Success || !installDirMatch.Success)
                return null;

            var installPath = Path.Combine(steamAppsPath, "common", installDirMatch.Groups[1].Value);

            if (!Directory.Exists(installPath))
                return null;

            return new Game
            {
                Name = nameMatch.Groups[1].Value,
                InstallPath = installPath,
                LauncherType = LauncherType.Steam,
                AppId = appIdMatch.Groups[1].Value,
                PCGWTitle = nameMatch.Groups[1].Value // May need normalization
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to parse Steam manifest {manifestPath}: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region Epic Games Scanner

    /// <summary>
    /// Scans Epic Games by parsing .item manifest files.
    /// </summary>
    private void ScanEpicGames(CancellationToken cancellationToken)
    {
        try
        {
            var manifestsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "Epic", "EpicGamesLauncher", "Data", "Manifests");

            if (!Directory.Exists(manifestsPath)) return;

            var itemFiles = Directory.GetFiles(manifestsPath, "*.item");

            foreach (var itemFile in itemFiles)
            {
                if (cancellationToken.IsCancellationRequested) return;

                var game = ParseEpicManifest(itemFile);
                if (game != null)
                {
                    lock (_detectedGames)
                    {
                        _detectedGames.Add(game);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Epic scan error: {ex.Message}");
        }
    }

    private Game? ParseEpicManifest(string itemPath)
    {
        try
        {
            var json = File.ReadAllText(itemPath);
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var root = doc.RootElement;

            var displayName = root.GetProperty("DisplayName").GetString();
            var installLocation = root.GetProperty("InstallLocation").GetString();
            var catalogItemId = root.TryGetProperty("CatalogItemId", out var catId)
                ? catId.GetString()
                : null;

            if (string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(installLocation))
                return null;

            if (!Directory.Exists(installLocation))
                return null;

            return new Game
            {
                Name = displayName,
                InstallPath = installLocation,
                LauncherType = LauncherType.Epic,
                AppId = catalogItemId,
                PCGWTitle = displayName
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to parse Epic manifest {itemPath}: {ex.Message}");
            return null;
        }
    }

    #endregion

    #region GOG Scanner

    /// <summary>
    /// Scans GOG Galaxy by reading registry keys.
    /// </summary>
    private void ScanGOG(CancellationToken cancellationToken)
    {
        try
        {
            // Check both 32-bit and 64-bit registry locations
            var registryPaths = new[]
            {
                @"SOFTWARE\WOW6432Node\GOG.com\Games",
                @"SOFTWARE\GOG.com\Games"
            };

            foreach (var regPath in registryPaths)
            {
                if (cancellationToken.IsCancellationRequested) return;

                using var gamesKey = Registry.LocalMachine.OpenSubKey(regPath);
                if (gamesKey == null) continue;

                foreach (var gameIdStr in gamesKey.GetSubKeyNames())
                {
                    if (cancellationToken.IsCancellationRequested) return;

                    using var gameKey = gamesKey.OpenSubKey(gameIdStr);
                    if (gameKey == null) continue;

                    var gameName = gameKey.GetValue("gameName") as string;
                    var gamePath = gameKey.GetValue("path") as string;
                    var gameId = gameKey.GetValue("gameID") as string;

                    if (string.IsNullOrEmpty(gameName) || string.IsNullOrEmpty(gamePath))
                        continue;

                    if (!Directory.Exists(gamePath))
                        continue;

                    lock (_detectedGames)
                    {
                        _detectedGames.Add(new Game
                        {
                            Name = gameName,
                            InstallPath = gamePath,
                            LauncherType = LauncherType.GOG,
                            AppId = gameId ?? gameIdStr,
                            PCGWTitle = gameName
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"GOG scan error: {ex.Message}");
        }
    }

    #endregion

    #region Xbox/Microsoft Store Scanner

    /// <summary>
    /// Scans Xbox/Microsoft Store games (basic implementation).
    /// </summary>
    private void ScanXbox(CancellationToken cancellationToken)
    {
        try
        {
            // Xbox Game Pass games are in WindowsApps or XboxGames folders
            var xboxPaths = new[]
            {
                @"C:\XboxGames",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Packages")
            };

            foreach (var basePath in xboxPaths)
            {
                if (cancellationToken.IsCancellationRequested) return;
                if (!Directory.Exists(basePath)) continue;

                // XboxGames folder has a simpler structure
                if (basePath.EndsWith("XboxGames"))
                {
                    foreach (var gameDir in Directory.GetDirectories(basePath))
                    {
                        if (cancellationToken.IsCancellationRequested) return;

                        var contentDir = Path.Combine(gameDir, "Content");
                        if (Directory.Exists(contentDir))
                        {
                            var gameName = Path.GetFileName(gameDir);
                            lock (_detectedGames)
                            {
                                _detectedGames.Add(new Game
                                {
                                    Name = gameName,
                                    InstallPath = contentDir,
                                    LauncherType = LauncherType.Xbox,
                                    PCGWTitle = gameName
                                });
                            }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Xbox scan error: {ex.Message}");
        }
    }

    #endregion

    #region Manual Game

    /// <summary>
    /// Adds a game manually by specifying its path.
    /// </summary>
    public Game AddManualGame(string name, string installPath)
    {
        var game = new Game
        {
            Name = name,
            InstallPath = installPath,
            LauncherType = LauncherType.Manual,
            PCGWTitle = name
        };

        return game;
    }

    #endregion
}
