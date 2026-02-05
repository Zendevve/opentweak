// OpenTweak - PC Game Optimization Tool
// Copyright 2024-2025 OpenTweak Contributors
// Licensed under PolyForm Shield License 1.0.0
// See LICENSE.md for full terms.

using System.Collections.Generic;

namespace OpenTweak.Models;

/// <summary>
/// Represents the result of an operation to apply a set of tweaks.
/// Handles partial failures where some tweaks succeed and others fail.
/// </summary>
public class TweakApplicationResult
{
    /// <summary>
    /// The backup snapshot created before any changes were made.
    /// Can be used to restore the original state if needed.
    /// </summary>
    public Snapshot? Snapshot { get; init; }

    /// <summary>
    /// List of tweaks that were successfully applied.
    /// </summary>
    public List<TweakSuccess> SuccessfulTweaks { get; } = new();

    /// <summary>
    /// List of tweaks that failed to apply.
    /// </summary>
    public List<TweakFailure> FailedTweaks { get; } = new();

    /// <summary>
    /// Indicates if there were any failures during the operation.
    /// </summary>
    public bool HasFailures => FailedTweaks.Count > 0;

    /// <summary>
    /// Indicates if all tweaks were applied successfully.
    /// </summary>
    public bool AllSucceeded => !HasFailures && SuccessfulTweaks.Count > 0;
}

/// <summary>
/// Represents a successfully applied tweak.
/// </summary>
public record TweakSuccess(TweakRecipe Recipe, string FilePath);

/// <summary>
/// Represents a tweak that failed to apply.
/// </summary>
public record TweakFailure(TweakRecipe Recipe, string ErrorMessage, Exception? Exception = null);
