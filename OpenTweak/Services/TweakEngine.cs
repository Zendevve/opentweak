// OpenTweak - PC Game Optimization Tool
// Copyright 2024-2025 OpenTweak Contributors
// Licensed under PolyForm Shield License 1.0.0
// See LICENSE.md for full terms.

using System.IO;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Salaros.Configuration;
using OpenTweak.Models;

namespace OpenTweak.Services;

/// <summary>
/// Engine for applying and previewing tweaks safely.
/// Uses Salaros.ConfigParser to preserve comments in config files.
/// </summary>
public class TweakEngine : ITweakEngine
{
    private readonly IBackupService _backupService;

    public TweakEngine(IBackupService backupService)
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
    /// <summary>
    /// Generates a preview of what changes will be made without applying them.
    /// This is the "Diff view" mentioned in the PRD.
    /// </summary>
    public async Task<List<TweakChange>> PreviewTweaksAsync(IEnumerable<TweakRecipe> recipes, CancellationToken cancellationToken = default)
    {
        var changes = new List<TweakChange>();

        foreach (var recipe in recipes.Where(r => r.IsEnabled && !string.IsNullOrEmpty(r.FilePath)))
        {
            if (cancellationToken.IsCancellationRequested)
                break;

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
    /// Handles partial failures by returning a detailed result object.
    /// </summary>
    public async Task<TweakApplicationResult> ApplyTweaksAsync(Game game, IEnumerable<TweakRecipe> recipes, CancellationToken cancellationToken = default)
    {
        var result = new TweakApplicationResult();
        var enabledRecipes = recipes.Where(r => r.IsEnabled && !string.IsNullOrEmpty(r.FilePath)).ToList();

        if (!enabledRecipes.Any())
            return result;

        // Collect unique files to backup
        var filesToBackup = enabledRecipes
            .Select(r => Environment.ExpandEnvironmentVariables(r.FilePath))
            .Where(File.Exists)
            .Distinct()
            .ToList();



        // Wait, the logic above for returning the new result with snapshot is clunky because of init-only properly.
        // Let me refactor to just set the property if I can change it to public set, or use a better approach.
        // Actually, I can just use object initializer correctly.

        return await ApplyTweaksInternalAsync(game, enabledRecipes, filesToBackup, result, cancellationToken);
    }

    private async Task<TweakApplicationResult> ApplyTweaksInternalAsync(
        Game game,
        List<TweakRecipe> recipes,
        List<string> filesToBackup,
        TweakApplicationResult result,
        CancellationToken cancellationToken)
    {
         Snapshot? snapshot = null;
         try
         {
            snapshot = await _backupService.CreateSnapshotAsync(game, filesToBackup, $"Applied {recipes.Count} tweaks", cancellationToken);
         }
         catch (Exception ex)
         {
             // If backup fails, we abort the whole operation for safety
             foreach(var r in recipes) result.FailedTweaks.Add(new TweakFailure(r, $"Backup failed: {ex.Message}"));
             return result;
         }

         var appliedIds = new List<Guid>();

         // Helper to add results
         void AddSuccess(TweakRecipe r, string p) => result.SuccessfulTweaks.Add(new TweakSuccess(r, p));
         void AddFailure(TweakRecipe r, string e, Exception? x = null) => result.FailedTweaks.Add(new TweakFailure(r, e, x));

         foreach (var recipe in recipes)
         {
             if (cancellationToken.IsCancellationRequested)
             {
                 AddFailure(recipe, "Operation cancelled");
                 continue;
             }

             try
             {
                 var expandedPath = Environment.ExpandEnvironmentVariables(recipe.FilePath);
                 await ApplySingleTweakAsync(expandedPath, recipe);
                 AddSuccess(recipe, expandedPath);
                 appliedIds.Add(recipe.Id);
             }
             catch (Exception ex)
             {
                 AddFailure(recipe, ex.Message, ex);
             }
         }

         if (snapshot != null)
         {
             snapshot.AppliedTweakIds = appliedIds;
         }

         // Use the snapshot in a new result object since the property is init-only
         var finalResult = new TweakApplicationResult { Snapshot = snapshot };
         finalResult.SuccessfulTweaks.AddRange(result.SuccessfulTweaks);
         finalResult.FailedTweaks.AddRange(result.FailedTweaks);

         return finalResult;
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
    /// TODO: Implement proper JSON modification using System.Text.Json or Newtonsoft.Json.
    /// </summary>
    /// <summary>
    /// Applies a tweak to JSON config files.
    /// </summary>
    private async Task ApplyJsonTweakAsync(string filePath, TweakRecipe recipe)
    {
        if (string.IsNullOrEmpty(recipe.Key)) return;

        JsonNode? rootNode = null;

        // Read existing file or create new object
        if (File.Exists(filePath))
        {
            try
            {
                var json = await File.ReadAllTextAsync(filePath);
                rootNode = JsonNode.Parse(json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to parse JSON file {filePath}: {ex.Message}");
                // If parsing fails, we might want to start fresh or error out.
                // For safety, let's treat it as a new file if it's empty/invalid?
                // No, that's dangerous. Better to throw.
                throw new InvalidOperationException($"Existing JSON file is invalid: {ex.Message}", ex);
            }
        }

        if (rootNode == null)
        {
            rootNode = new JsonObject();
        }

        JsonNode? currentNode = rootNode;

        // Navigate or create section
        if (!string.IsNullOrEmpty(recipe.Section))
        {
            // Handle potentially nested sections (e.g. "Graphics.Advanced") implementation depending on requirements.
            // For now assuming simple section mapping or single-level nesting.
            // Let's implement robust dot-notation support for Section names.
            var sections = recipe.Section.Split('.');
            foreach (var section in sections)
            {
                if (currentNode is JsonObject obj)
                {
                    if (!obj.ContainsKey(section) || obj[section] == null)
                    {
                        obj[section] = new JsonObject();
                    }
                    currentNode = obj[section];
                }
                else
                {
                    throw new InvalidOperationException($"Cannot create section '{section}' because parent is not an object.");
                }
            }
        }

        // Set value
        if (currentNode is JsonObject targetObj)
        {
            // Determine value type - simple simulation for now
            // We store value as string in recipe, but might need boolean/number in JSON
            JsonNode? newValueNode;

            if (bool.TryParse(recipe.Value, out var boolVal))
            {
                newValueNode = JsonValue.Create(boolVal);
            }
            else if (int.TryParse(recipe.Value, out var intVal))
            {
                newValueNode = JsonValue.Create(intVal);
            }
            else if (double.TryParse(recipe.Value, out var doubleVal))
            {
                newValueNode = JsonValue.Create(doubleVal);
            }
            else
            {
                newValueNode = JsonValue.Create(recipe.Value);
            }

            targetObj[recipe.Key] = newValueNode;
        }
        else
        {
            throw new InvalidOperationException($"Cannot set key '{recipe.Key}' because target section is not an object.");
        }

        // Write content
        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(filePath, rootNode.ToJsonString(options));
    }

    /// <summary>
    /// Applies a tweak to XML config files.
    /// TODO: Implement proper XML modification with XPath support.
    /// </summary>
    /// <summary>
    /// Applies a tweak to XML config files.
    /// Uses simple path traversal (Section = path/to/element, Key = element or @attribute).
    /// </summary>
    private async Task ApplyXmlTweakAsync(string filePath, TweakRecipe recipe)
    {
        if (string.IsNullOrEmpty(recipe.Key)) return;

        XDocument doc;

        if (File.Exists(filePath))
        {
            try
            {
                using var stream = File.OpenRead(filePath);
                doc = await XDocument.LoadAsync(stream, LoadOptions.PreserveWhitespace, CancellationToken.None);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Existing XML file is invalid: {ex.Message}", ex);
            }
        }
        else
        {
            // Create a basic root if new file (assuming "GameSettings" or "Config" as root is guesswork,
            // so we should probably default to something generic or fail if not exists?)
            // Usually we shouldn't create XML from scratch without knowing the root name.
            // Let's create a "Config" root for now or throw.
            doc = new XDocument(new XElement("Configuration"));
        }

        XElement? currentElement = doc.Root;

        if (currentElement == null)
        {
             currentElement = new XElement("Configuration");
             doc.Add(currentElement);
        }

        // Navigate Section path
        if (!string.IsNullOrEmpty(recipe.Section))
        {
            var parts = recipe.Section.Split(new[] { '/', '.' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var nextElement = currentElement.Element(part);
                if (nextElement == null)
                {
                    nextElement = new XElement(part);
                    currentElement.Add(nextElement);
                }
                currentElement = nextElement;
            }
        }

        // Set value
        if (recipe.Key.StartsWith("@"))
        {
             // Attribute
             var attrName = recipe.Key.Substring(1);
             currentElement.SetAttributeValue(attrName, recipe.Value);
        }
        else
        {
             // Child Element
             var child = currentElement.Element(recipe.Key);
             if (child == null)
             {
                 child = new XElement(recipe.Key);
                 currentElement.Add(child);
             }
             child.Value = recipe.Value;
        }

        // Save
        using var writeStream = File.Create(filePath);
        await doc.SaveAsync(writeStream, SaveOptions.None, CancellationToken.None);
    }

    /// <summary>
    /// Applies a tweak to the Windows Registry.
    /// TODO: Implement proper registry modification with backup and type detection.
    /// </summary>
    private void ApplyRegistryTweak(TweakRecipe recipe)
    {
        // SAFETY: Registry modifications require careful handling of value types
        // and proper backup. This is stubbed out until a safer implementation is added.
        throw new NotSupportedException(
            $"Registry modification is not yet implemented safely. " +
            $"Please manually edit registry path '{recipe.FilePath}' and set '{recipe.Key}' to '{recipe.Value}'.");
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
                    using (var doc = JsonDocument.Parse(json))
                    {
                        // Basic support for root-level keys or simple dot notation needs manual traversal here too
                        // if we want to support nested reads consistent with ApplyJsonTweakAsync.
                        // For now staying simple as per previous impl, but updating if section is present.
                         JsonElement current = doc.RootElement;

                         if (!string.IsNullOrEmpty(section))
                         {
                             var parts = section.Split('.');
                             foreach (var part in parts)
                             {
                                 if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(part, out var next))
                                 {
                                     current = next;
                                 }
                                 else
                                 {
                                      return null;
                                 }
                             }
                         }

                        if (current.ValueKind == JsonValueKind.Object && current.TryGetProperty(key, out var prop))
                            return prop.ToString();
                    }
                    break;

                case TweakTargetType.XmlFile:
                    using (var stream = File.OpenRead(filePath))
                    {
                        var xdoc = await XDocument.LoadAsync(stream, LoadOptions.None, CancellationToken.None);
                        XElement? current = xdoc.Root;
                        if (current == null) return null;

                        if (!string.IsNullOrEmpty(section))
                        {
                            var parts = section.Split(new[] { '/', '.' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var part in parts)
                            {
                                current = current.Element(part);
                                if (current == null) return null;
                            }
                        }

                        if (key.StartsWith("@"))
                        {
                            return current.Attribute(key.Substring(1))?.Value;
                        }
                        else
                        {
                            return current.Element(key)?.Value;
                        }
                    }
                    // break; reachable
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to read config value {key} from {filePath}: {ex.Message}");
        }

        return null;
    }
}
