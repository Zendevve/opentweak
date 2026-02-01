using System.IO;
using Salaros.Configuration;
using OpenTweak.Models;

namespace OpenTweak.Services;

/// <summary>
/// Engine for applying and previewing tweaks safely.
/// Uses Salaros.ConfigParser to preserve comments in config files.
/// </summary>
public class TweakEngine
{
    private readonly BackupService _backupService;

    public TweakEngine(BackupService backupService)
    {
        _backupService = backupService;
    }

    /// <summary>
    /// Represents a single change that will be made.
    /// </summary>
    public class TweakChange
    {
        public string FilePath { get; set; } = string.Empty;
        public string Section { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string? CurrentValue { get; set; }
        public string NewValue { get; set; } = string.Empty;
        public bool IsNewEntry { get; set; }
    }

    /// <summary>
    /// Generates a preview of what changes will be made without applying them.
    /// This is the "Diff view" mentioned in the PRD.
    /// </summary>
    public async Task<List<TweakChange>> PreviewTweaksAsync(IEnumerable<TweakRecipe> recipes)
    {
        var changes = new List<TweakChange>();

        foreach (var recipe in recipes.Where(r => r.IsEnabled && !string.IsNullOrEmpty(r.FilePath)))
        {
            var expandedPath = Environment.ExpandEnvironmentVariables(recipe.FilePath);

            if (!File.Exists(expandedPath))
            {
                changes.Add(new TweakChange
                {
                    FilePath = expandedPath,
                    Section = recipe.Section ?? "",
                    Key = recipe.Key,
                    CurrentValue = null,
                    NewValue = recipe.Value,
                    IsNewEntry = true
                });
                continue;
            }

            try
            {
                var currentValue = await ReadConfigValueAsync(expandedPath, recipe.Section, recipe.Key, recipe.TargetType);

                changes.Add(new TweakChange
                {
                    FilePath = expandedPath,
                    Section = recipe.Section ?? "",
                    Key = recipe.Key,
                    CurrentValue = currentValue,
                    NewValue = recipe.Value,
                    IsNewEntry = currentValue == null
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preview error for {recipe.Key}: {ex.Message}");
            }
        }

        return changes;
    }

    /// <summary>
    /// Applies tweaks after creating a backup snapshot.
    /// </summary>
    public async Task<Snapshot?> ApplyTweaksAsync(Game game, IEnumerable<TweakRecipe> recipes)
    {
        var enabledRecipes = recipes.Where(r => r.IsEnabled && !string.IsNullOrEmpty(r.FilePath)).ToList();

        if (!enabledRecipes.Any())
            return null;

        // Collect unique files to backup
        var filesToBackup = enabledRecipes
            .Select(r => Environment.ExpandEnvironmentVariables(r.FilePath))
            .Where(File.Exists)
            .Distinct()
            .ToList();

        // Create snapshot BEFORE making any changes
        var snapshot = await _backupService.CreateSnapshotAsync(
            game,
            filesToBackup,
            $"Applied {enabledRecipes.Count} tweaks");

        snapshot.AppliedTweakIds = enabledRecipes.Select(r => r.Id).ToList();

        // Apply each tweak
        foreach (var recipe in enabledRecipes)
        {
            try
            {
                var expandedPath = Environment.ExpandEnvironmentVariables(recipe.FilePath);
                await ApplySingleTweakAsync(expandedPath, recipe);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to apply {recipe.Key}: {ex.Message}");
            }
        }

        return snapshot;
    }

    /// <summary>
    /// Applies a single tweak to a config file.
    /// </summary>
    private async Task ApplySingleTweakAsync(string filePath, TweakRecipe recipe)
    {
        switch (recipe.TargetType)
        {
            case TweakTargetType.IniFile:
            case TweakTargetType.CfgFile:
                await ApplyIniTweakAsync(filePath, recipe);
                break;

            case TweakTargetType.JsonFile:
                await ApplyJsonTweakAsync(filePath, recipe);
                break;

            case TweakTargetType.XmlFile:
                await ApplyXmlTweakAsync(filePath, recipe);
                break;

            case TweakTargetType.Registry:
                ApplyRegistryTweak(recipe);
                break;
        }
    }

    /// <summary>
    /// Applies a tweak to INI/CFG files using Salaros.ConfigParser.
    /// This preserves comments and formatting!
    /// </summary>
    private async Task ApplyIniTweakAsync(string filePath, TweakRecipe recipe)
    {
        // Ensure file exists
        if (!File.Exists(filePath))
        {
            // Create the file with the section and key
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            await File.WriteAllTextAsync(filePath, "");
        }

        // Use Salaros.ConfigParser to preserve comments
        var config = new ConfigParser(filePath, new ConfigParserSettings
        {
            MultiLineValues = MultiLineValues.Simple
        });

        // Set the value (creates section if needed)
        if (!string.IsNullOrEmpty(recipe.Section))
        {
            config.SetValue(recipe.Section, recipe.Key, recipe.Value);
        }
        else
        {
            // Root level setting (no section)
            config.SetValue("", recipe.Key, recipe.Value);
        }

        // Save preserving original formatting
        config.Save(filePath);
    }

    /// <summary>
    /// Applies a tweak to JSON config files.
    /// </summary>
    private async Task ApplyJsonTweakAsync(string filePath, TweakRecipe recipe)
    {
        if (!File.Exists(filePath))
            return;

        var json = await File.ReadAllTextAsync(filePath);

        // Simple JSON modification (for complex nested paths, would need JsonPath)
        // This is a basic implementation
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        var options = new System.Text.Json.JsonWriterOptions { Indented = true };

        using var stream = new MemoryStream();
        using (var writer = new System.Text.Json.Utf8JsonWriter(stream, options))
        {
            // Clone and modify - simplified implementation
            doc.WriteTo(writer);
        }

        // For now, use simple string replacement (basic implementation)
        // A full implementation would use a proper JSON manipulation library
        var pattern = $@"""{recipe.Key}""\s*:\s*[^,\r\n\}}]+";
        var replacement = $@"""{recipe.Key}"": {recipe.Value}";

        var modified = System.Text.RegularExpressions.Regex.Replace(json, pattern, replacement);
        await File.WriteAllTextAsync(filePath, modified);
    }

    /// <summary>
    /// Applies a tweak to XML config files.
    /// </summary>
    private async Task ApplyXmlTweakAsync(string filePath, TweakRecipe recipe)
    {
        if (!File.Exists(filePath))
            return;

        var doc = new System.Xml.XmlDocument();
        doc.Load(filePath);

        // Find the node by key (simplified - assumes key is element name)
        var nodes = doc.GetElementsByTagName(recipe.Key);
        if (nodes.Count > 0)
        {
            nodes[0]!.InnerText = recipe.Value;
        }

        doc.Save(filePath);
    }

    /// <summary>
    /// Applies a tweak to the Windows Registry.
    /// </summary>
    private void ApplyRegistryTweak(TweakRecipe recipe)
    {
        // Registry path format: HKEY_CURRENT_USER\Software\Game\Key
        // The FilePath contains the registry path, Key is the value name, Value is the data

        try
        {
            var parts = recipe.FilePath.Split('\\', 2);
            if (parts.Length != 2) return;

            var rootKey = parts[0].ToUpper() switch
            {
                "HKEY_CURRENT_USER" or "HKCU" => Microsoft.Win32.Registry.CurrentUser,
                "HKEY_LOCAL_MACHINE" or "HKLM" => Microsoft.Win32.Registry.LocalMachine,
                _ => null
            };

            if (rootKey == null) return;

            using var key = rootKey.OpenSubKey(parts[1], true);
            key?.SetValue(recipe.Key, recipe.Value);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Registry tweak failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Reads the current value from a config file.
    /// </summary>
    private async Task<string?> ReadConfigValueAsync(string filePath, string? section, string key, TweakTargetType targetType)
    {
        if (!File.Exists(filePath))
            return null;

        try
        {
            switch (targetType)
            {
                case TweakTargetType.IniFile:
                case TweakTargetType.CfgFile:
                    var config = new ConfigParser(filePath);
                    return config.GetValue(section ?? "", key);

                case TweakTargetType.JsonFile:
                    var json = await File.ReadAllTextAsync(filePath);
                    using (var doc = System.Text.Json.JsonDocument.Parse(json))
                    {
                        if (doc.RootElement.TryGetProperty(key, out var prop))
                            return prop.ToString();
                    }
                    break;
            }
        }
        catch { }

        return null;
    }
}
